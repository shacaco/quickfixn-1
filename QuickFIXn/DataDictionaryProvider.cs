using System.Collections.Generic;

namespace QuickFix
{
    public class DataDictionaryProvider
    {
        private Dictionary<string, DataDictionary.DataDictionary> transportDataDictionaries_;
        private Dictionary<string, DataDictionary.DataDictionary> applicationDataDictionaries_;
        private DataDictionary.DataDictionary emptyDataDictionary_;

        public DataDictionaryProvider()
        {
            transportDataDictionaries_ = new Dictionary<string, DataDictionary.DataDictionary>();
            applicationDataDictionaries_ = new Dictionary<string, DataDictionary.DataDictionary>();
            emptyDataDictionary_ = new DataDictionary.DataDictionary();
        }

        /// TODO need to make deeper copy?
        public DataDictionaryProvider(DataDictionaryProvider src)
        {
            transportDataDictionaries_ = new Dictionary<string, DataDictionary.DataDictionary>(src.transportDataDictionaries_);
            applicationDataDictionaries_ = new Dictionary<string, DataDictionary.DataDictionary>(src.applicationDataDictionaries_);
            emptyDataDictionary_ = new DataDictionary.DataDictionary(src.emptyDataDictionary_);
        }

        public void SetSettings(Dictionary settings)
        {
            if (settings.Has(SessionSettings.VALIDATE_FIELDS_OUT_OF_ORDER))
                emptyDataDictionary_.CheckFieldsOutOfOrder = settings.GetBool(SessionSettings.VALIDATE_FIELDS_OUT_OF_ORDER);
            if (settings.Has(SessionSettings.VALIDATE_FIELDS_HAVE_VALUES))
                emptyDataDictionary_.CheckFieldsHaveValues = settings.GetBool(SessionSettings.VALIDATE_FIELDS_HAVE_VALUES);
            if (settings.Has(SessionSettings.VALIDATE_USER_DEFINED_FIELDS))
                emptyDataDictionary_.CheckUserDefinedFields = settings.GetBool(SessionSettings.VALIDATE_USER_DEFINED_FIELDS);
            if (settings.Has(SessionSettings.ALLOW_UNKNOWN_MSG_FIELDS))
                emptyDataDictionary_.AllowUnknownMessageFields = settings.GetBool(SessionSettings.ALLOW_UNKNOWN_MSG_FIELDS);
        }

        public void AddTransportDataDictionary(string beginString, DataDictionary.DataDictionary dataDictionary)
        {
            transportDataDictionaries_[beginString] = dataDictionary;
        }

        public void AddApplicationDataDictionary(string applVerID, DataDictionary.DataDictionary dataDictionary)
        {
            applicationDataDictionaries_[applVerID] = dataDictionary;
        }

        public DataDictionary.DataDictionary GetSessionDataDictionary(string beginString)
        {
            DataDictionary.DataDictionary dd;
            if (!transportDataDictionaries_.TryGetValue(beginString, out dd))
                return emptyDataDictionary_;
            return dd;
        }

        public DataDictionary.DataDictionary GetApplicationDataDictionary(string applVerID)
        {
            DataDictionary.DataDictionary dd;
            if (!applicationDataDictionaries_.TryGetValue(applVerID, out dd))
                return emptyDataDictionary_;
            return dd;
        }
    }
}
