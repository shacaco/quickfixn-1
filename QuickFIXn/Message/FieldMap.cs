#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickFix.Fields;
using QuickFix.Fields.Converters;

namespace QuickFix
{
    /// <summary>
    /// Field container used by messages, groups, and composites
    /// </summary>
    public class FieldMap : IEnumerable<KeyValuePair<int, IField>> {
        private SortedDictionary<int, IField> _fields = new();

        /// FIXME sorted dict is a hack to get quasi-correct field order
        private Dictionary<int, List<Group>> _groups = new();

        /// <summary>
        /// order of field tags as an integer array
        /// </summary>
        public int[] FieldOrder { get; private set; } = Array.Empty<int>();

        /// <summary>
        /// Used for validation.  Only set during Message parsing.
        /// </summary>
        public List<IField> RepeatedTags { get; private set; } = new();

        /// <summary>
        /// Default constructor
        /// </summary>
        public FieldMap(ReusableFields.ReusableFieldsLengths lengths = null)
        {
            _fields = new Dictionary<int, Fields.IField>(100);
            _groups = new Dictionary<int, List<Group>>();
            this.RepeatedTags = new List<Fields.IField>();
            ReusableFields = new ReusableFields(lengths ?? new ReusableFields.ReusableFieldsLengths(5,5,5,5,5,5));
        }

        /// <summary>
        /// Constructor with field order
        /// </summary>
        /// <param name="fieldOrd"></param>
        public FieldMap(int[] fieldOrd)
            : this()
        {
            FieldOrder = fieldOrd;
        }

        /// <summary>
        /// FIXME this should probably make a deeper copy
        /// </summary>
        /// <param name="src">The QuickFix.FieldMap to copy</param>
        /// <returns>A copy of the given QuickFix.FieldMap</returns>
        public FieldMap(FieldMap src)
        {
            CopyStateFrom(src);
        }

        public void CopyStateFrom(FieldMap src)
        {
            FieldOrder = src.FieldOrder;

            this._fields = src._fields.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetCopy());

            this._groups = src._groups.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(g=> new Group(g)).ToList());

            this.RepeatedTags = new List<Fields.IField>(src.RepeatedTags.Select(t=>t.GetCopy()));
        }

        /// <summary>
        /// Remove a field from the fieldmap
        /// </summary>
        /// <param name="field"></param>
        /// <returns>true if field was removed, false otherwise</returns>
        public bool RemoveField(int field)
        {
            return _fields.Remove(field);
        }

        /// <summary>
        /// set field in the fieldmap
        /// will overwrite field if it exists
        /// </summary>
        public void SetField(IField field)
        {
            _fields[field.Tag] = field;
        }

        /// <summary>
        /// set many fields at the same time
        /// </summary>
        public void SetFields(IEnumerable<IField> fields)
        {
            foreach (var field in fields)
            {
                _fields[field.Tag] = field;
            }
        }

        /// <summary>
        /// Set field, with optional override check
        /// </summary>
        /// <param name="field"></param>
        /// <param name="overwrite">will overwrite existing field if set to true</param>
        /// <returns>false if overwrite=true and is denied</returns>
        public bool SetField(IField field, bool overwrite)
        {
            if (_fields.ContainsKey(field.Tag) && !overwrite)
                return false;

            SetField(field);
            return true;
        }

        public bool SetWithReusableField(int tag, string value)
        {
            return SetField(ReusableFields.GetNextReusableStringField().Set(tag, value), true);
        }

        public bool SetWithReusableField(int tag, DateTime value)
        {
            return SetField(ReusableFields.GetNextReusableDateTimeField().Set(tag, value), true);
        }

        public bool SetWithReusableField(int tag, decimal value)
        {
            return SetField(ReusableFields.GetNextReusableDecimalField().Set(tag, value), true);
        }

        public bool SetWithReusableField(int tag, char value)
        {
            return SetField(ReusableFields.GetNextReusableCharField().Set(tag, value), true);
        }

        public bool SetWithReusableField(int tag, bool value)
        {
            return SetField(ReusableFields.GetNextReusableBooleanField().Set(tag, value), true);
        }

        public bool SetWithReusableField(int tag, int value)
        {
            return SetField(ReusableFields.GetNextReusableIntField().Set(tag, value), true);
        }

        /// <summary>
        /// Gets a boolean field; saves its value into the parameter object, which is also the return value.
        /// </summary>
        /// <param name="field">this field's tag is used to extract the value from the message; that value is saved back into this object</param>
        /// <exception cref="FieldNotFoundException">thrown if <paramref name="field"/> isn't found</exception>
        /// <returns><paramref name="field"/></returns>
        public BooleanField GetField(BooleanField field)
        {
            field.Obj = GetBoolean(field.Tag);
            return field;
        }

        /// <summary>
        /// Gets a string field; saves its value into the parameter object, which is also the return value.
        /// </summary>
        /// <param name="field">this field's tag is used to extract the value from the message; that value is saved back into this object</param>
        /// <exception cref="FieldNotFoundException">thrown if <paramref name="field"/> isn't found</exception>
        /// <returns><paramref name="field"/></returns>
        public StringField GetField(StringField field)
        {
            field.Obj = GetString(field.Tag);
            return field;
        }

        /// <summary>
        /// Gets a char field; saves its value into the parameter object, which is also the return value.
        /// </summary>
        /// <param name="field">this field's tag is used to extract the value from the message; that value is saved back into this object</param>
        /// <exception cref="FieldNotFoundException">thrown if <paramref name="field"/> isn't found</exception>
        /// <returns><paramref name="field"/></returns>
        public CharField GetField(CharField field)
        {
            field.Obj = GetChar(field.Tag);
            return field;
        }

        /// <summary>
        /// Gets a int field; saves its value into the parameter object, which is also the return value.
        /// </summary>
        /// <param name="field">this field's tag is used to extract the value from the message; that value is saved back into this object</param>
        /// <exception cref="FieldNotFoundException">thrown if <paramref name="field"/> isn't found</exception>
        /// <returns><paramref name="field"/></returns>
        public IntField GetField(IntField field)
        {
            field.Obj = GetInt(field.Tag);
            return field;
        }

        /// <summary>
        /// Gets a ulong field; saves its value into the parameter object, which is also the return value.
        /// </summary>
        /// <param name="field">this field's tag is used to extract the value from the message; that value is saved back into this object</param>
        /// <exception cref="FieldNotFoundException">thrown if <paramref name="field"/> isn't found</exception>
        /// <returns><paramref name="field"/></returns>
        public ULongField GetField(ULongField field)
        {
            field.Obj = GetULong(field.Tag);
            return field;
        }

        /// <summary>
        /// Gets a decimal field; saves its value into the parameter object, which is also the return value.
        /// </summary>
        /// <param name="field">this field's tag is used to extract the value from the message; that value is saved back into this object</param>
        /// <exception cref="FieldNotFoundException">thrown if <paramref name="field"/> isn't found</exception>
        /// <returns><paramref name="field"/></returns>
        public DecimalField GetField(DecimalField field)
        {
            field.Obj = GetDecimal(field.Tag);
            return field;
        }

        /// <summary>
        /// Gets a datetime field; saves its value into the parameter object, which is also the return value.
        /// </summary>
        /// <param name="field">this field's tag is used to extract the value from the message; that value is saved back into this object</param>
        /// <exception cref="FieldNotFoundException">thrown if <paramref name="field"/> isn't found</exception>
        /// <returns><paramref name="field"/></returns>
        public DateTimeField GetField(DateTimeField field)
        {
            field.Obj = GetDateTime(field.Tag);
            return field;
        }

        /// <summary>
        /// Gets a date only field; saves its value into the parameter object, which is also the return value.
        /// </summary>
        /// <param name="field">this field's tag is used to extract the value from the message; that value is saved back into this object</param>
        /// <exception cref="FieldNotFoundException">thrown if <paramref name="field"/> isn't found</exception>
        /// <returns><paramref name="field"/></returns>
        public DateOnlyField GetField(DateOnlyField field)
        {
            field.Obj = GetDateOnly(field.Tag);
            return field;
        }

        /// <summary>
        /// Gets a time only field; saves its value into the parameter object, which is also the return value.
        /// </summary>
        /// <param name="field">this field's tag is used to extract the value from the message; that value is saved back into this object</param>
        /// <exception cref="FieldNotFoundException">thrown if <paramref name="field"/> isn't found</exception>
        /// <returns><paramref name="field"/></returns>
        public TimeOnlyField GetField(TimeOnlyField field)
        {
            field.Obj = GetTimeOnly(field.Tag);
            return field;
        }

        /// <summary>
        /// Check to see if field is set
        /// </summary>
        /// <param name="field">Field Object</param>
        /// <returns>true if set</returns>
        public bool IsSetField(IField field)
        {
            return IsSetField(field.Tag);
        }

        /// <summary>
        /// Check to see if field is set
        /// </summary>
        /// <param name="tag">Tag Number</param>
        /// <returns>true if set</returns>
        public bool IsSetField(int tag)
        {
            return _fields.ContainsKey(tag);
        }

        /// <summary>
        /// Add a group to message; the group counter is automatically incremented.
        /// </summary>
        /// <param name="grp">group to add</param>
        public void AddGroup(Group grp)
        {
            AddGroup(grp, true);
        }

        /// <summary>
        /// Add a group to message; optionally auto-increment the counter.
        /// When parsing from a string (e.g. Message::FromString()), we want to leave the counter alone
        /// so we can detect when the counterparty has set it wrong.
        /// </summary>
        /// <param name="grp">group to add</param>
        /// <param name="autoIncCounter">if true, auto-increment the counter, else leave it as-is</param>
        internal void AddGroup(Group grp, bool autoIncCounter)
        {
            // copy, in case user code reuses input object
            Group group = grp.Clone();

            if (!_groups.ContainsKey(group.CounterField))
                _groups.Add(group.CounterField, new List<Group>());
            _groups[group.CounterField].Add(group);

            if (autoIncCounter)
            {
                // increment group size
                int groupsize = _groups[group.CounterField].Count;
                int counttag = group.CounterField;
                IntField count = new IntField(counttag, groupsize);
                this.SetField(count, true);
            }
        }

        /// <summary>
        /// Gets an instance of a group.  Note: use GetGroup(int,Group) if you want
        /// your group as the proper subtype (e.g. NoPartyIDsGroup instead of the generic Group)
        /// </summary>
        /// <param name="num">index of desired group (starting at 1)</param>
        /// <param name="field">counter tag of repeating group</param>
        /// <returns>retrieved group object</returns>
        /// <exception cref="FieldNotFoundException" />
        public Group GetGroup(int num, int field)
        {
            if (!_groups.ContainsKey(field))
                throw new FieldNotFoundException(field);
            if (num <= 0)
                throw new FieldNotFoundException(field);
            if (_groups[field].Count < num)
                throw new FieldNotFoundException(field);

            return _groups[field][num - 1];
        }

        /// <summary>
        /// Extracts a repeating-group item into <paramref name="group"/>
        /// </summary>
        /// <param name="num">index of desired group item (index starts at 1, not 0)</param>
        /// <param name="group">group to populate (<c>group.Field</c> is used by this function to extract the group)</param>
        public void GetGroup(int num, Group group)
        {
            int tag = group.CounterField;
            group.CopyStateFrom(this.GetGroup(num, tag));
        }

        /// <summary>
        /// Gets the integer value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the integer field value</returns>
        /// <exception cref="FieldNotFoundException" />
        public int GetInt(int tag)
        {
            if (!_fields.TryGetValue(tag, out IField? fld))
                throw new FieldNotFoundException(tag);

            if (fld is FieldBase<int> intField)
                return intField.Obj;

            return IntConverter.Convert(fld.ToString());
        }

        /// <summary>
        /// Gets the ulong value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the ulong field value</returns>
        /// <exception cref="FieldNotFoundException" />
        public ulong GetULong(int tag)
        {
            try
            {
                IField fld = _fields[tag];
                if (fld.GetType() == typeof(ULongField))
                    return ((ULongField)fld).Obj;
                return ULongConverter.Convert(fld.ToString());
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new FieldNotFoundException(tag);
            }
        }
 
        /// <summary>
        /// Gets the DateTime value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the DateTime value</returns>
        /// <exception cref="FieldNotFoundException" />
        public DateTime GetDateTime(int tag)
        {
            if (!_fields.TryGetValue(tag, out IField? fld))
                throw new FieldNotFoundException(tag);

            return fld switch
            {
                DateOnlyField dateOnlyField => dateOnlyField.Obj.Date,
                TimeOnlyField timeOnlyField => new DateTime(1980, 01, 01).Add(timeOnlyField.Obj.TimeOfDay),
                FieldBase<DateTime> dateTimeField => dateTimeField.Obj,
                _ => DateTimeConverter.ConvertToDateTime(fld.ToString())
            };
        }

        /// <summary>
        /// Gets the DateOnly value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the DateTime value</returns>
        /// <exception cref="FieldNotFoundException" />
        public DateTime GetDateOnly(int tag)
        {
            if (!_fields.TryGetValue(tag, out IField? fld))
                throw new FieldNotFoundException(tag);

            if (fld is FieldBase<DateTime> dateTimeField)
                return dateTimeField.Obj.Date;

            return DateTimeConverter.ConvertToDateOnly(fld.ToString());
        }

        /// <summary>
        /// Gets the TimeOnly value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the DateTime value</returns>
        /// <exception cref="FieldNotFoundException" />
        public DateTime GetTimeOnly(int tag)
        {
            if (!_fields.TryGetValue(tag, out IField? fld))
                throw new FieldNotFoundException(tag);

            if (fld is FieldBase<DateTime> dateTimeField)
                return new DateTime(1980, 01, 01).Add(dateTimeField.Obj.TimeOfDay);

            return DateTimeConverter.ConvertToTimeOnly(fld.ToString());
        }

        /// <summary>
        /// Gets the boolean value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the bool value</returns>
        /// <exception cref="FieldNotFoundException" />
        public bool GetBoolean(int tag)
        {
            if (!_fields.TryGetValue(tag, out IField? fld))
                throw new FieldNotFoundException(tag);

            if (fld is FieldBase<bool> boolField)
                return boolField.Obj;

            return BoolConverter.Convert(fld.ToString());
        }

        /// <summary>
        /// Gets the string value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the string value</returns>
        /// <exception cref="FieldNotFoundException" />
        public string GetString(int tag)
        {
            if (!_fields.TryGetValue(tag, out IField? fld))
                throw new FieldNotFoundException(tag);

            return fld.ToString();
        }

        /// <summary>
        /// tries to get the string value of a field
        /// </summary>
        public bool TryGetString(int tag, out string value)
        {
            if (!_fields.TryGetValue(tag, out var field))
            {
                value = null;
                return false;
            }
            value = field.ToString();
            return true;
        }

        /// <summary>
        /// Gets the char value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the char value</returns>
        /// <exception cref="FieldNotFoundException" />
        public char GetChar(int tag)
        {
            if (!_fields.TryGetValue(tag, out IField? fld))
                throw new FieldNotFoundException(tag);

            if (fld is FieldBase<char> charField)
                return charField.Obj;

            return CharConverter.Convert(fld.ToString());
        }

        /// <summary>
        /// Gets the decimal value of a field
        /// </summary>
        /// <param name="tag">the FIX tag</param>
        /// <returns>the decimal value</returns>
        /// <exception cref="FieldNotFoundException" />
        public decimal GetDecimal(int tag)
        {
            if (!_fields.TryGetValue(tag, out IField? fld))
                throw new FieldNotFoundException(tag);

            if (fld is FieldBase<decimal> decimalField)
                return decimalField.Obj;

            return DecimalConverter.Convert(fld.ToString());
        }

        /// <summary>
        /// Removes specific group instance
        /// </summary>
        /// <param name="num">num of group (starting at 1)</param>
        /// <param name="field">tag of group</param>
        /// <exception cref="FieldNotFoundException" />
        public void RemoveGroup(int num, int field)
        {
            if (!_groups.ContainsKey(field))
                throw new FieldNotFoundException(field);
            if (num <= 0)
                throw new FieldNotFoundException(field);
            if (_groups[field].Count < num)
                throw new FieldNotFoundException(field);

            if (_groups[field].Count.Equals(1))
                _groups.Remove(field);
            else
                _groups[field].RemoveAt(num - 1);
        }

        /// <summary>
        /// Replaces specific group instance
        /// </summary>
        /// <param name="num">num of group (starting at 1)</param>
        /// <param name="field">tag of group</param>
        /// <param name="group">the group to replace it with</param>
        /// <returns>Group object</returns>
        /// <exception cref="FieldNotFoundException" />
        public Group ReplaceGroup(int num, int field, Group group)
        {
            if (!_groups.ContainsKey(field))
                throw new FieldNotFoundException(field);
            if (num <= 0)
                throw new FieldNotFoundException(field);
            if (_groups[field].Count < num)
                throw new FieldNotFoundException(field);

            return _groups[field][num - 1] = group;
        }

        /// <summary>
        /// Removes fields and groups in message
        /// </summary>
        public virtual void Clear()
        {
            _fields.Clear();
            _groups.Clear();
            RepeatedTags?.Clear();
            ReusableFields.ResetCounters();
            _fieldOrder = null;
        }

        /// <summary>
        /// Checks emptiness of message
        /// </summary>
        /// <returns>true if no fields or groups have been set</returns>
        public bool IsEmpty()
        {
            return (_fields.Count == 0) && (_groups.Count == 0);
        }

        public int CalculateTotal()
        {
            int total = 0;
            foreach (var field in _fields)
            {
                if (field.Value.Tag != Fields.Tags.CheckSum)
                    total += field.Value.getTotal();
            }

            foreach (IField field in this.RepeatedTags)
            {
                if (field.Tag != Fields.Tags.CheckSum)
                    total += field.getTotal();
            }

            foreach (var groupList in _groups)
            {
                foreach (Group group in groupList.Value)
                    total += group.CalculateTotal();
            }
            return total;
        }

        public int CalculateLength()
        {
            int total = 0;
            foreach (var field in _fields)
            {
                if (field.Value != null
                    && field.Value.Tag != Tags.BeginString
                    && field.Value.Tag != Tags.BodyLength
                    && field.Value.Tag != Tags.CheckSum)
                {
                    total += field.Value.getLength();
                }
            }

            foreach (IField field in this.RepeatedTags)
            {
                if (field != null
                    && field.Tag != Tags.BeginString
                    && field.Tag != Tags.BodyLength
                    && field.Tag != Tags.CheckSum)
                {
                    total += field.getLength();
                }
            }
            foreach (var groupList in _groups)
            {
                foreach (Group group in groupList.Value)
                    total += group.CalculateLength();
            }

            return total;
        }

        public virtual StringBuilder CalculateString(bool orderPostFieldOrder, StringBuilder sb)
        {
            var result = CalculateString(sb ?? new StringBuilder(1024), FieldOrder ?? Array.Empty<int>(), orderPostFieldOrder);
            return result;
        }

        private readonly HashSet<int> _groupCounterTags = new HashSet<int>();
        public virtual StringBuilder CalculateString(StringBuilder sb, int[] preFields, bool orderPostFields)
        {
            _groupCounterTags.Clear();
            if (_groups.Count > 0)
                foreach (var kvp in _groups)
                    _groupCounterTags.Add(kvp.Key);

            for (int i = 0; i < preFields.Length; i++)
            {
                var preField = preFields[i];
                if (IsSetField(preField))
                {
                    _fields[preField].AppendFieldAsStringTo(sb).Append(Message.SOH);
                    if (_groupCounterTags.Contains(preField))
                    {
                        List<Group> glist = _groups[preField];
                        foreach (Group g in glist)
                            g.CalculateString(true, sb);
                    }
                }
            }

            if (orderPostFields)
            {
                foreach (var field in _fields.OrderBy(x => x.Key))
                {
                    if (_groupCounterTags.Contains(field.Value.Tag))
                        continue;
                    if (preFields.Contains(field.Value.Tag))
                        continue; //already did this one
                    field.Value.AppendFieldAsStringTo(sb).Append(Message.SOH);
                }
            }
            else
            {
                foreach (var field in _fields)
                {
                    if (_groupCounterTags.Contains(field.Value.Tag))
                        continue;
                    if (preFields.Contains(field.Value.Tag))
                        continue; //already did this one
                    field.Value.AppendFieldAsStringTo(sb).Append(Message.SOH);
                }
            }

            foreach (var counterTag in _groups)
            {
                if (preFields.Contains(counterTag.Key))
                    continue; //already did this one

                List<Group> groupList = _groups[counterTag.Key];
                if (groupList.Count == 0)
                    continue; //probably unnecessary, but it doesn't hurt to check
           
                _fields[counterTag.Key].AppendFieldAsStringTo(sb).Append(Message.SOH);

                foreach (Group group in groupList)
                    group.CalculateString(true, sb);
            }

            return sb;
        }

        /// <summary>
        /// Get count of items in the repeating group
        /// </summary>
        /// <param name="fieldNo">the counter tag of the group</param>
        /// <returns></returns>
        public int GroupCount(int fieldNo) {
            return _groups.ContainsKey(fieldNo) ? _groups[fieldNo].Count : 0;
        }

        /// <summary>
        /// Return a List containing the counter tag for each group in this message.
        /// (The returned List is a static copy.)
        /// </summary>
        /// <returns></returns>
        public List<int> GetGroupTags()
        {
            return new List<int>(_groups.Keys);
        }

        public int GetGroupsCount()
        {
            return _groups.Count;
        }

        #region Private Members
        private Dictionary<int, Fields.IField> _fields; /// FIXME sorted dict is a hack to get quasi-correct field order
        private Dictionary<int, List<Group>> _groups;
        protected int[] _fieldOrder;
        #endregion

        #region Properties
        /// <summary>
        /// Used for validation.  Only set during Message parsing.
        /// </summary>
        public List<Fields.IField> RepeatedTags { get; private set; }

        protected ReusableFields ReusableFields { get; }

        #endregion

        #region IEnumerable<KeyValuePair<int,IField>> Members

        public IEnumerator<KeyValuePair<int, IField>> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        #endregion
    }
}
