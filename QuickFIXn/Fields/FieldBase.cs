using System;

namespace QuickFix.Fields
{
    /// <summary>
    /// Base class for all field types
    /// </summary>
    /// <typeparam name="T">Internal storage type</typeparam>
    public abstract class FieldBase<T> : IField
    {
        /// <summary>
        /// Constructs a new field with the specified tag and value
        /// </summary>
        /// <param name="tag">the FIX tag number</param>
        /// <param name="obj">the value of the field</param>
        public FieldBase(int tag, T obj)
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
                _valChanged = _fieldChanged = true;
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
                _valChanged = _fieldChanged = true;
            }
        }
        #endregion

        /// <summary>
        /// returns full fix string (e.g. "tag=val")
        /// </summary>
        public override string toStringField()
        {
            if (_fieldChanged)
                makeStringField();
            return _stringField;
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
            return CharEncoding.DefaultEncoding.GetByteCount(_stringField) + 1; // +1 for SOH
        }

        /// <summary>
        /// checksum
        /// </summary>
        public override int getTotal()
        {
            if (_fieldChanged)
                makeStringField();

            int sum = 0;
            byte[] array = CharEncoding.DefaultEncoding.GetBytes(_stringField);
            for (int i = 0; i < array.Length; i++)
            {
                sum += array[i];
            }

            return (sum + 1); // +1 for SOH
        }

        protected abstract string makeString();

        /// <summary>
        /// returns tag=val
        /// </summary>
        private void makeStringField()
        {
            makeStringVal();
            _stringField = Tag + "=" + _stringVal;
            _fieldChanged = false;
        }

        private void makeStringVal()
        {
            _stringVal = makeString();
            _valChanged = false;
        }

        #region Private members
        private string _stringField;
        private bool _valChanged;
        private bool _fieldChanged;
        private T _obj;
        private int _tag;
        private string _stringVal;
        #endregion
    }
}