﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuickFIX.NET;
using QuickFIX.NET.Fields;

namespace UnitTests
{
    [TestFixture]
    public class MessageTests
    {
        [Test]
        public void IdentifyMsgTypeTest()
        {
            string msg1 = "\u000135=A\u0001";
            Assert.That(Message.IdentifyType(msg1).Obj, Is.EqualTo(new MsgType("A").Obj));
            string msg2 = "a;sldkfjadls;k\u000135=A\u0001a;sldkfja;sdlfk";
            Assert.That(Message.IdentifyType(msg2).Obj, Is.EqualTo(new MsgType("A").Obj));
            string msg3 = "8=FIX4.2\u00019=12\u0001\u000135=B\u000110=031\u0001";
            Assert.That(Message.IdentifyType(msg3).Obj, Is.EqualTo(new MsgType("B").Obj));
        }

        [Test]
        public void ExtractStringTest()
        {
            string str1 = "8=FIX.4.2\u00019=46\u000135=0\u000134=3\u000149=TW\u0001";
            int pos = 0;
            StringField sf1 = Message.ExtractField(str1, ref pos);
            Assert.That(pos, Is.EqualTo(10));
            Assert.That(sf1.Tag, Is.EqualTo(8));
            Assert.That(sf1.Obj, Is.EqualTo("FIX.4.2"));
            StringField sf2 = Message.ExtractField(str1, ref pos);
            Assert.That(pos, Is.EqualTo(15));
            Assert.That(sf2.Tag, Is.EqualTo(9));
            Assert.That(sf2.Obj, Is.EqualTo("46"));
        }

        [Test]
        public void ExtractStringErrorsTest()
        {
            int pos = 0;
            Assert.Throws(typeof(MessageParseException),
                delegate { Message.ExtractField("=",ref pos); });
            Assert.Throws(typeof(MessageParseException),
                delegate { Message.ExtractField("35=A", ref pos); });
            Assert.Throws(typeof(MessageParseException),
                delegate { Message.ExtractField("\u000135=A", ref pos); });
            Assert.Throws(typeof(MessageParseException),
                delegate { Message.ExtractField("35=\u0001", ref pos); });
        }


        [Test]
        public void CheckSumTest()
        {
            string str1 = "8=FIX.4.2\u00019=46\u000135=0\u000134=3\u000149=TW\u0001" +
                "52=20000426-12:05:06\u000156=ISLD\u0001";
            
            int chksum = 0;
            foreach( char c in str1 )
                chksum += (int)c;
            chksum %= 256;

            str1 += "10=000\u0001";  // checksum field
            Message msg = new Message();
            msg.FromString(str1);
            Assert.That(msg.CheckSum(), Is.EqualTo(chksum));
        }

        [Test]
        public void FromStringTest()
        {
            string str1 = "8=FIX.4.2\u00019=46\u000135=0\u000134=3\u000149=TW\u0001" +
                "52=20000426-12:05:06\u000156=ISLD\u00011=acct123\u000110=000\u0001";
            Message msg = new Message();
            msg.FromString(str1);
            StringField f1 = new StringField(8);
            StringField f2 = new StringField(9);
            StringField f3 = new StringField(35);
            StringField f4 = new StringField(34);
            StringField f5 = new StringField(49);
            StringField f6 = new StringField(52);
            StringField f7 = new StringField(56);
            StringField f8 = new StringField(10);
            StringField f9 = new StringField(1);
            msg.Header.getField(f1);
            msg.Header.getField(f2);
            msg.Header.getField(f3);
            msg.Header.getField(f4);
            msg.Header.getField(f5);
            msg.Header.getField(f6);
            msg.Header.getField(f7);
            msg.getField(f9);
            msg.Trailer.getField(f8);
            Assert.That(f1.Obj, Is.EqualTo("FIX.4.2"));
            Assert.That(f2.Obj, Is.EqualTo("46"));
            Assert.That(f3.Obj, Is.EqualTo("0"));
            Assert.That(f4.Obj, Is.EqualTo("3"));
            Assert.That(f5.Obj, Is.EqualTo("TW"));
            Assert.That(f6.Obj, Is.EqualTo("20000426-12:05:06"));
            Assert.That(f7.Obj, Is.EqualTo("ISLD"));
            Assert.That(f8.Obj, Is.EqualTo("000"));
            Assert.That(f9.Obj, Is.EqualTo("acct123"));
        }

        [Test]
        public void IsHeaderFieldTest()
        {
            Assert.That(Message.IsHeaderField(Tags.BeginString), Is.EqualTo(true));
            Assert.That(Message.IsHeaderField(Tags.TargetCompID), Is.EqualTo(true));
            Assert.That(Message.IsHeaderField(Tags.Account), Is.EqualTo(false));
        }

        [Test]
        public void IsTrailerFieldTest()
        {
            Assert.That(Message.IsTrailerField(Tags.CheckSum), Is.EqualTo(true));
            Assert.That(Message.IsTrailerField(Tags.Price), Is.EqualTo(false));
        }
    }
}
