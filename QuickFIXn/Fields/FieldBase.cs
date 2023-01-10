using System;
using System.Text;

namespace QuickFix.Fields
{
    /// <summary>
    /// Base class for all field types
    /// </summary>
    /// <typeparam name="T">Internal storage type</typeparam>
    public abstract class FieldBase<T> : IField
    {
        private readonly StringBuilder _sb = new StringBuilder(64);

        /// <summary>
        /// Constructs a new field with the specified tag and value
        /// </summary>
        /// <param name="tag">the FIX tag number</param>
        /// <param name="obj">the value of the field</param>
        protected FieldBase(int tag, T obj)
        {
            _tag = tag;
            _obj = obj;
            _valChanged = _fieldChanged = true;
        }

        #region Properties
        public T Obj
        {
            get { return _obj; }
            set
            {
                _obj = value;
                OnDataChanged();
            }
        }

        /// <summary>
        /// the FIX tag number
        /// </summary>
        public override int Tag
        {
            get { return _tag; }
            set
            {
                _tag = value;
                OnDataChanged();
            }
        }
        #endregion

        public FieldBase<T> Set(int tag, T obj)
        {
            Tag = tag;
            Obj = obj;
            return this;
        }

        /// <summary>
        /// returns full fix string (e.g. "tag=val")
        /// </summary>
        public override string toStringField()
        {
            if (_fieldChanged)
                makeStringField();
            return _stringField ??= _sb.ToString();
        }

        /// <summary>
        /// returns field value formatted for fix
        /// </summary>
        public override string ToString()
        {
            if (_valChanged)
                makeStringVal();
            return _stringVal;
        }

        /// <summary>
        /// Value equality test
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            FieldBase<T> f = (FieldBase<T>)obj;
            return this.Tag == f.Tag && this.Obj.Equals(f.Obj);
        }

        public override int GetHashCode()
        {
            return Tag ^ Obj.GetHashCode();
        }

        /// <summary>
        /// length of formatted field (including tag=val\001)
        /// </summary>
        public override int getLength()
        {
            if (_fieldChanged)
                makeStringField();
            return _bytesLength;
        }

        /// <summary>
        /// checksum
        /// </summary>
        public override unsafe int  getTotal()
        {
            if (_fieldChanged)
                makeStringField();
            return _bytesTotal;
        }

        private unsafe void SetByteParams()
        {
            char* buffer = stackalloc char[_sb.Length];
            for (int i = 0; i < _sb.Length; i++)
            {
                buffer[i] = _sb[i];
            }

            var bytePtrLength = (int) (_sb.Length * 1.5) + 3;
            byte* bytePtr = stackalloc byte[bytePtrLength];
            _bytesLength = CharEncoding.DefaultEncoding.GetBytes(buffer, _sb.Length, bytePtr, bytePtrLength) + 1;
         
            int sum = 0;
            for (int i = 0; i < _bytesLength - 1; i++)
            {
                sum += bytePtr[i];
            }

            _bytesTotal = sum + 1; // +1 for SOH
        }

        protected abstract string makeString();

        /// <summary>
        /// returns tag=val
        /// </summary>
        private void makeStringField()
        {
            _stringField = null;
            makeStringVal();
            const char equals = '=';
            _sb.Append(Tag);
            _sb.Append(equals);
            _sb.Append(_stringVal);
            SetByteParams();
            _fieldChanged = false;
        }

        private void makeStringVal()
        {
            _stringVal = makeString();
            _valChanged = false;
        }

        public override StringBuilder appendStringFieldTo(StringBuilder builder)
        {
            if (_fieldChanged)
                makeStringField();
            builder.Append(_sb);
            return builder;
        }     

        protected void OnDataChanged()
        {
            _valChanged = _fieldChanged = true;
            _sb.Clear();
        }

        #region Private members

        private string _stringField;
        private bool _valChanged;
        private bool _fieldChanged;
        private int _bytesTotal;
        private int _bytesLength;
        private T _obj;
        private int _tag;
        private string _stringVal;
    
        #endregion
    }
}