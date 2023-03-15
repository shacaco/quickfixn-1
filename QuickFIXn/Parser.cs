using My_Collections;
using System;
using System.Buffers;

namespace QuickFix
{
    /// <summary>
    /// Parses bytestream into messages
    /// </summary>
    public class Parser
    {
        private readonly ProducerConsumerBuffer<byte[]> _producerConsumerBuffer = new(4, () => new byte[1024]);
        private static readonly byte[] Message9TagWithLeadingSeparator = CharEncoding.DefaultEncoding.GetBytes("\x01" + "9=");
        private static readonly byte[] MessageChecksumTagWithLeadingSeparator = CharEncoding.DefaultEncoding.GetBytes("\x01" + "10=");
        private static readonly byte[] MessageBeginStringTag = CharEncoding.DefaultEncoding.GetBytes("8=");
        private static readonly byte[] MessageSeparatorTag = CharEncoding.DefaultEncoding.GetBytes("\x01");

        private byte[] buffer_;
        private int usedBufferLength;
        private readonly char[] _currentMsg = new char[1024];

        public Parser()
        {
            buffer_ = _producerConsumerBuffer.Dequeue();
        }

        private void DoAddToStream(ReadOnlySpan<byte> data, int bytesAdded)
        {
            if (buffer_.Length < usedBufferLength + bytesAdded)
                System.Array.Resize<byte>(ref buffer_, (usedBufferLength + bytesAdded));
            data.CopyTo(buffer_.AsSpan().Slice(usedBufferLength));
            usedBufferLength += bytesAdded;
        }

        public void AddToStream(ReadOnlySpan<byte> data)
        {
            DoAddToStream(data, data.Length);
        }

        public void AddToStream(byte[] data)
        {
            DoAddToStream(data, data.Length);
        }

        public bool ReadFixMessage(out ReadOnlySpan<char> msg)
        {
            msg = null;

            if (buffer_.Length < 2)//too short
                return false;

            ReadOnlySpan<byte> buf = buffer_.AsSpan();

            var msgStartPos = buf.IndexOf(MessageBeginStringTag);
            if (-1 == msgStartPos)//cant find 8= string
                return false;

            buf = buf.Slice(msgStartPos);//slice the buffer to start from 8=

            int totalMsgLength = 0;
            int innerLength = 0;

            try
            {
                if (!ExtractLength(out innerLength, out totalMsgLength, buffer_, msgStartPos))//get length of message and position of next tag(after 9->length)
                    return false;


                totalMsgLength += innerLength;//move to end of message
                if (buf.Length < totalMsgLength)
                    return false;//length value was wrong 

                int index = buf.Slice(totalMsgLength - 1).IndexOf(MessageChecksumTagWithLeadingSeparator);//look for checksum tag
                if (-1 == index)
                    return false;
                totalMsgLength += index + 4;//move to value of 10=

                index = buf.Slice(totalMsgLength).IndexOf(MessageSeparatorTag);//last separator
                if (-1 == index)
                    return false;//no separator found
                totalMsgLength += index + 1;

                var totalChars = CharEncoding.DefaultEncoding.GetChars(buffer_, msgStartPos, totalMsgLength, _currentMsg, 0);//cut message to size
                msg = _currentMsg.AsSpan(0, totalChars);
                buffer_ = RemoveAndSwitch(buffer_, totalMsgLength + msgStartPos); //remove message from buffer
                return true;
            }
            catch (MessageParseError e)
            {
                if ((innerLength > 0) && (totalMsgLength + msgStartPos) <= buffer_.Length)
                    buffer_ = RemoveAndSwitch(buffer_, (totalMsgLength + msgStartPos));
                else
                    buffer_ = RemoveAndSwitch(buffer_, buffer_.Length);
                throw e;
            }
        }

        public bool ExtractLength(out int length, out int pos, string buf)
        {
            return ExtractLength(out length, out pos, CharEncoding.DefaultEncoding.GetBytes(buf), 0);
        }

        private static bool ExtractLength(out int lengthValue, out int pos, byte[] buffer, int offset)
        {
            lengthValue = 0;
            pos = 0;
            ReadOnlySpan<byte> buf = buffer.AsSpan().Slice(offset);

            if (buf.Length < 1)
                return false;
            int startPos = buf.IndexOf(Message9TagWithLeadingSeparator);
            if (-1 == startPos)
                return false;
            startPos += 3;

            int endPos = buf.Slice(startPos).IndexOf(MessageSeparatorTag);
            if (-1 == endPos)
                return false;

            string strLength = CharEncoding.DefaultEncoding.GetString(buffer, startPos + offset, endPos);
            try
            {
                lengthValue = Fields.Converters.IntConverter.Convert(strLength);
                if (lengthValue < 0)
                    throw new MessageParseError("Invalid BodyLength (" + lengthValue + ")");
            }
            catch (FieldConvertError e)
            {
                throw new MessageParseError(e.Message, e);
            }

            pos = startPos + endPos + 1;
            return true;
        }

        private byte[] RemoveAndSwitch(byte[] array, int offset)
        {
            byte[] returnByte = _producerConsumerBuffer.Dequeue();
            var copyCount = Math.Max(0, usedBufferLength - offset);
            System.Buffer.BlockCopy(array, offset, returnByte, 0, copyCount);
            Array.Clear(array, 0, usedBufferLength);
            usedBufferLength = copyCount;
            _producerConsumerBuffer.Enqueue(array);
            return returnByte;
        }
    }
}

