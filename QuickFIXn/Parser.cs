using My_Collections;
using System;
using System.Buffers;

namespace QuickFix
{
    /// <summary>
    /// </summary>
    public class Parser
    {
        private static readonly ProducerConsumerBuffer<byte[]> _producerConsumerBuffer = new ProducerConsumerBuffer<byte[]>(16, true, true, () => new byte[1024]);
        private static readonly byte[] Message9TagWithLeadingSeparator = System.Text.Encoding.UTF8.GetBytes("\x01" + "9=");
        private static readonly byte[] MessageChecksumTagWithLeadingSeparator = System.Text.Encoding.UTF8.GetBytes("\x01" + "10=");
        private static readonly byte[] MessageBeginStringTag = System.Text.Encoding.UTF8.GetBytes("8=");
        private static readonly byte[] MessageSeparatorTag = System.Text.Encoding.UTF8.GetBytes("\x01");

        private byte[] buffer_;
        private int usedBufferLength;

        public Parser()
        {
            buffer_ = _producerConsumerBuffer.Dequeue();
        }

        private void DoAddToStream(byte[] data, int bytesAdded)
        {
            if (buffer_.Length < usedBufferLength + bytesAdded)
                System.Array.Resize<byte>(ref buffer_, (usedBufferLength + bytesAdded));
            System.Buffer.BlockCopy(data, 0, buffer_, usedBufferLength, bytesAdded);
            usedBufferLength += bytesAdded;
        }

        public void AddToStream(ReadOnlySpan<byte> data)
        {
            DoAddToStream(data.ToArray(), data.Length);
        }

        public void AddToStream(byte[] data)
        {
            DoAddToStream(data, data.Length);
        }

        public bool ReadFixMessage(out string msg)
        {
            msg = "";

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

                msg = System.Text.Encoding.UTF8.GetString(buffer_, msgStartPos, totalMsgLength);//cut message to size
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
            return ExtractLength(out length, out pos, System.Text.Encoding.UTF8.GetBytes(buf), 0);
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

            string strLength = System.Text.Encoding.UTF8.GetString(buffer, startPos + offset, endPos);
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

        private bool Fail(string what)
        {
            System.Console.WriteLine("Parser failed: " + what);
            return false;
        }

        private byte[] RemoveAndSwitch(byte[] array, int count)
        {
            byte[] returnByte = _producerConsumerBuffer.Dequeue();
            System.Buffer.BlockCopy(array, count, returnByte, 0, array.Length - count);
            usedBufferLength -= count;
            Array.Clear(array, 0, array.Length);
            _producerConsumerBuffer.Enqueue(array);
            return returnByte;
        }
    }
}

