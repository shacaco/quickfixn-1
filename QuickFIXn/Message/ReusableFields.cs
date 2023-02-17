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
    private readonly StringField[] _reusableStringFields;
    private readonly DecimalField[] _reusableDecimalFields;
    private readonly DateTimeField[] _reusableDateTimeFields;
    private readonly CharField[] _reusableCharFields;


    internal ReusableFields(int stringFieldsCount = 30, int dateTimeFieldsCount = 30, int decimalFieldsCount = 30, int charFieldsCount = 10)
    {
        _reusableStringFields =
            new StringField[stringFieldsCount].Select(i => new StringField(-1)).ToArray();
        _reusableDecimalFields =
            new DecimalField[dateTimeFieldsCount].Select(i => new DecimalField(-1)).ToArray();
        _reusableDateTimeFields =
            new DateTimeField[decimalFieldsCount].Select(i => new DateTimeField(-1)).ToArray();
        _reusableCharFields =
            new CharField[charFieldsCount].Select(i => new CharField(-1)).ToArray();
        ResetCounters();
    }

    internal void ResetCounters()
    {
        _stringFieldsCounter = _decimalFieldsCounter = _dateTimeFieldsCounter = _charFieldsCounter = -1;
    }

    public StringField GetNextReusableStringField()
    {
        var index = Interlocked.Increment(ref _stringFieldsCounter);
        return _reusableStringFields[index];
    }

    public DateTimeField GetNextReusableDateTimeField()
    {
        var index = Interlocked.Increment(ref _dateTimeFieldsCounter);
        return _reusableDateTimeFields[index];
    }

    public DecimalField GetNextReusableDecimalField()
    {
        var index = Interlocked.Increment(ref _decimalFieldsCounter);
        return _reusableDecimalFields[index];
    }

    public CharField GetNextReusableCharField()
    {
        var index = Interlocked.Increment(ref _charFieldsCounter);
        return _reusableCharFields[index];
    }
}