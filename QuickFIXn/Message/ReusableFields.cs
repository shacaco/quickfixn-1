using System;
using QuickFix.Fields;
using System.Linq;
using System.Threading;

namespace QuickFix;

public class ReusableFields
{
    private int _stringFieldsCounter;
    private int _decimalFieldsCounter;
    private int _dateTimeFieldsCounter;
    private int _charFieldsCounter;
    private int _booleanFieldsCounter;
    private int _intFieldsCounter;
    private StringField[] _reusableStringFields;
    private DecimalField[] _reusableDecimalFields;
    private DateTimeField[] _reusableDateTimeFields;
    private CharField[] _reusableCharFields;
    private BooleanField[] _reusableBooleanFields;
    private IntField[] _reusableIntFields;

    public class ReusableFieldsLengths
    {
        internal int StringFieldsCounter { get; }
        internal int DecimalFieldsCounter { get; }
        internal int DateTimeFieldsCounter { get; }
        internal int CharFieldsCounter { get; }
        internal int BooleanFieldsCounter { get; }
        internal int IntFieldsCounter { get; }

        internal ReusableFieldsLengths(int stringFieldsCount, int dateTimeFieldsCount, int decimalFieldsCount, int charFieldsCount,
            int booleanFieldsCount, int intFieldsCount)
        {
            StringFieldsCounter = stringFieldsCount;
            DateTimeFieldsCounter = dateTimeFieldsCount;
            DecimalFieldsCounter = decimalFieldsCount;
            CharFieldsCounter = charFieldsCount;
            BooleanFieldsCounter = booleanFieldsCount;
            IntFieldsCounter = intFieldsCount;
        }
    }

    internal ReusableFields(ReusableFieldsLengths lengths)
    {
        _reusableStringFields =
            new StringField[lengths.StringFieldsCounter].Select(i => new StringField(-1)).ToArray();
        _reusableDecimalFields =
            new DecimalField[lengths.DecimalFieldsCounter].Select(i => new DecimalField(-1)).ToArray();
        _reusableDateTimeFields =
            new DateTimeField[lengths.DateTimeFieldsCounter].Select(i => new DateTimeField(-1)).ToArray();
        _reusableCharFields =
            new CharField[lengths.CharFieldsCounter].Select(i => new CharField(-1)).ToArray();
        _reusableBooleanFields =
            new BooleanField[lengths.BooleanFieldsCounter].Select(i => new BooleanField(-1)).ToArray();
        _reusableIntFields =
            new IntField[lengths.IntFieldsCounter].Select(i => new IntField(-1)).ToArray();
        ResetCounters();
    }

    internal void ResetCounters()
    {
        _stringFieldsCounter = _decimalFieldsCounter = _dateTimeFieldsCounter = _charFieldsCounter = _booleanFieldsCounter = _intFieldsCounter = -1;
    }

    public StringField GetNextReusableStringField()
    {
        var index = Interlocked.Increment(ref _stringFieldsCounter);
        return GetField(index, ref _reusableStringFields, () => new StringField(-1));
    }

    public DateTimeField GetNextReusableDateTimeField()
    {
        var index = Interlocked.Increment(ref _dateTimeFieldsCounter);
        return GetField(index, ref _reusableDateTimeFields, () => new DateTimeField(-1));
    }

    public DecimalField GetNextReusableDecimalField()
    {
        var index = Interlocked.Increment(ref _decimalFieldsCounter);
        return GetField(index, ref _reusableDecimalFields, () => new DecimalField(-1));
    }

    public CharField GetNextReusableCharField()
    {
        var index = Interlocked.Increment(ref _charFieldsCounter);
        return GetField(index, ref _reusableCharFields, () => new CharField(-1));
    }

    public BooleanField GetNextReusableBooleanField()
    {
        var index = Interlocked.Increment(ref _booleanFieldsCounter);
        return GetField(index, ref _reusableBooleanFields, () => new BooleanField(-1));
    }

    public IntField GetNextReusableIntField()
    {
        var index = Interlocked.Increment(ref _intFieldsCounter);
        return GetField(index, ref _reusableIntFields, () => new IntField(-1));
    }

    private T GetField<T>(int index, ref T[] array, Func<T> factory) where T : IField
    {
        if(index < array.Length)
            return array[index];
        lock (array)
        {
            if (index < array.Length)
                return array[index];
         
            Array.Resize(ref array, array.Length + 10);
            for (int i = index; i < array.Length; i++)
            {
                array[i] = factory.Invoke();
            }

            return array[index];
        }
    }
}