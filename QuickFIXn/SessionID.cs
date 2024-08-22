
using System;
using QuickFix.Fields;
using System.Collections.Generic;

namespace QuickFix
{
    /// <summary>
    /// Identifies a session. Only supports a company ID (target, sender)
    /// and a session qualifier. Sessions are also identified by FIX version so
    /// that it's possible to have multiple sessions to the same counterparty
    /// but using different FIX versions (and/or session qualifiers).
    /// </summary>
    public class SessionID
    {
        #region Properties
        public Dictionary<int, IField> FieldsDictionary { get; } = new Dictionary<int, IField>();
        
        public string BeginString { get; }
        public string SenderCompID { get; }
        public string SenderSubID { get; }
        public string SenderLocationID { get; }
        public string TargetCompID { get; }
        public string TargetSubID { get; }
        public string TargetLocationID { get; }

        /// <summary>
        /// Session qualifier can be used to identify different sessions
        /// for the same target company ID. Session qualifiers can only be used
        /// with initiated sessions. They cannot be used with accepted sessions.
        /// </summary>
        public string? SessionQualifier { get; }

        /// <summary>
        /// Returns whether session version is FIXT 1.1 or newer
        /// </summary>
        public bool IsFIXT { get; }

        #endregion

        // TODO just make the values nullable, jeez
        public const string NOT_SET = "";

        private readonly string _id;

        public SessionID(string beginString, string senderCompId, string senderSubId, string senderLocationId, string targetCompId, string targetSubId, string targetLocationId, string? sessionQualifier = NOT_SET)
        {
            BeginString = beginString ?? throw new ArgumentNullException(nameof(beginString));
            SenderCompID = senderCompId ?? throw new ArgumentNullException(nameof(senderCompId));
            SenderSubID = senderSubId;
            SenderLocationID = senderLocationId;
            TargetCompID = targetCompId ?? throw new ArgumentNullException(nameof(targetCompId));
            TargetSubID = targetSubId;
            TargetLocationID = targetLocationId;
            SessionQualifier = sessionQualifier;
            IsFIXT = BeginString.StartsWith("FIXT", StringComparison.Ordinal);

            _id = BeginString
                + ":"
                + SenderCompID
                + (IsSet(SenderSubID) ? "/" + SenderSubID : "")
                + (IsSet(SenderLocationID) ? "/" + SenderLocationID : "")
                + "->"
                + TargetCompID
                + (IsSet(TargetSubID) ? "/" + TargetSubID : "")
                + (IsSet(TargetLocationID) ? "/" + TargetLocationID : "");
            if (SessionQualifier is not null && SessionQualifier.Length > 0)
                _id += ":" + SessionQualifier;
            PopulateFieldsDictionary();
        }

        protected void PopulateFieldsDictionary()
        {
            FieldsDictionary.Add(Fields.BeginString.TAG, GetStringField(Fields.BeginString.TAG, BeginString));
            FieldsDictionary.Add(Fields.SenderCompID.TAG, GetStringField(Fields.SenderCompID.TAG, SenderCompID));
            if (IsSet(SenderSubID))
                FieldsDictionary.Add(Fields.SenderSubID.TAG, GetStringField(Fields.SenderSubID.TAG, SenderSubID));
            if (IsSet(SenderLocationID))
                FieldsDictionary.Add(Fields.SenderLocationID.TAG, GetStringField(Fields.SenderLocationID.TAG, SenderLocationID)); 
            FieldsDictionary.Add(Fields.TargetCompID.TAG, GetStringField(Fields.TargetCompID.TAG, TargetCompID));
                FieldsDictionary.Add(Fields.TargetSubID.TAG, GetStringField(Fields.TargetSubID.TAG, TargetSubID)); 
            if (IsSet(TargetSubID))
            if (IsSet(TargetLocationID))
                FieldsDictionary.Add(Fields.TargetLocationID.TAG, GetStringField(Fields.TargetLocationID.TAG, TargetLocationID));
        }

        private static StringField GetStringField(int tag, string value)
        {
            return new StringField(tag, value);
        }
        
        public SessionID(string beginString, string senderCompID, string targetCompID)
            : this(beginString, senderCompID, targetCompID, NOT_SET)
        { }
        
        public SessionID(string beginString, string senderCompId, string senderSubId, string targetCompId, string targetSubId)
            : this(beginString, senderCompId, senderSubId, senderLocationId: NOT_SET, targetCompId, targetSubId, targetLocationId: NOT_SET)
       { }
           
            public SessionID(string beginString, string senderCompID, string senderSubID, string senderLocationID, string targetCompID, string targetSubID, string targetLocationID)
            : this(beginString, senderCompID, senderSubID, senderLocationID, targetCompID, targetSubID, targetLocationID, NOT_SET)
        { }    

        public SessionID(string beginString, string senderCompId, string targetCompId, string sessionQualifier = NOT_SET)
            : this(beginString, senderCompId, senderSubId: NOT_SET, senderLocationId: NOT_SET, targetCompId, targetSubId: NOT_SET, targetLocationId: NOT_SET, sessionQualifier)
        { }

        public static bool IsSet(string? value)
        {
            return value != null && value != NOT_SET;
        }

        public override string ToString()
        {
            return _id;
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }
        
        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            SessionID rhs = (SessionID)obj;
            return _id.Equals(rhs._id);
        }
    }
}
