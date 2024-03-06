using System;
using System.Text;
using Utils.Collections;

namespace QuickFix
{
    /// <summary>
    /// Parses bytestream into messages
    /// </summary>
    public class Parser
    {
        private readonly ProducerConsumerBuffer<byte[]> _producerConsumerBuffer = new(4, () => new byte[512]);
        private readonly byte[] _seperatorBytes;
        private readonly byte[] _beginStringBytes;
        private readonly byte[] _bodyLengthBytes;
        private readonly byte[] _checkSumBytes;
        private readonly Encoding _encoding;

        private byte[] _buffer;
        private int _usedBufferLength = 0;
        private readonly char[] _currentMsg = new char[512];
        public Parser(Encoding encoding)
        {
            _encoding = encoding;
            _beginStringBytes = encoding.GetBytes("8=");
            _bodyLengthBytes = encoding.GetBytes('\u0001' + "9=");
            _checkSumBytes = encoding.GetBytes('\u0001' + "10=");
            _seperatorBytes = encoding.GetBytes("\u0001");
            _buffer = _producerConsumerBuffer.Dequeue();
        }

        private void DoAddToStream(ReadOnlySpan<byte> data, int bytesAdded)
        {
            if (_buffer.Length < _usedBufferLength + bytesAdded)
                System.Array.Resize<byte>(ref _buffer, (_usedBufferLength + bytesAdded));
            data.CopyTo(_buffer.AsSpan().Slice(_usedBufferLength));
            _usedBufferLength += bytesAdded;
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

            if (_buffer.Length < 2)//too short
                return false;

            ReadOnlySpan<byte> buf = _buffer.AsSpan();

            var msgStartPos = buf.IndexOf(_beginStringBytes);
            if (-1 == msgStartPos)//cant find 8= string
                return false;

            buf = buf.Slice(msgStartPos);//slice the buffer to start from 8=

            int totalMsgLength = 0;
            int innerLength = 0;

            try
            {
                if (!ExtractLength(out innerLength, out totalMsgLength, _buffer, msgStartPos))//get length of message and position of next tag(after 9->length)
                    return false;


                totalMsgLength += innerLength;//move to end of message
                if (buf.Length < totalMsgLength)
                    return false;//length value was wrong 

                int index = buf.Slice(totalMsgLength - 1).IndexOf(_checkSumBytes);//look for checksum tag
                if (-1 == index)
                    return false;
                totalMsgLength += index + 4;//move to value of 10=

                index = buf.Slice(totalMsgLength).IndexOf(_seperatorBytes);//last separator
                if (-1 == index)
                    return false;//no separator found
                totalMsgLength += index + 1;

                var totalChars = _encoding.GetChars(_buffer, msgStartPos, totalMsgLength, _currentMsg, 0);//cut message to size
                msg = _currentMsg.AsSpan(0, totalChars);
                _buffer = RemoveAndSwitch(_buffer, totalMsgLength + msgStartPos); //remove message from buffer
                return true;
            }
            catch (MessageParseError e)
            {
                if ((innerLength > 0) && (totalMsgLength + msgStartPos) <= _buffer.Length)
                    _buffer = RemoveAndSwitch(_buffer, (totalMsgLength + msgStartPos));
                else
                    _buffer = RemoveAndSwitch(_buffer, _buffer.Length);
                throw e;
            }
        }

        public bool ExtractLength(out int bodyLength, out int bytesConsumed, string buf)
        {
            return ExtractLength(out bodyLength, out bytesConsumed, _encoding.GetBytes(buf), 0);
        }

        private bool ExtractLength(out int bodyLength, out int bytesConsumed, byte[] buffer, int offset)
        {
            bodyLength = 0;
            bytesConsumed = 0;

            ReadOnlySpan<byte> buf = buffer.AsSpan().Slice(offset);

            if (buf.Length < 1)
                return false;
            int startPos = buf.IndexOf(_bodyLengthBytes);
            if (-1 == startPos)
                return false;
            startPos += 3;

            int endPos = buf.Slice(startPos).IndexOf(_seperatorBytes);
            if (-1 == endPos)
                return false;

            string strLength = _encoding.GetString(buffer, startPos + offset, endPos);
            try
            {
                bodyLength = Fields.Converters.IntConverter.Convert(strLength);
                if (bodyLength < 0)
                    throw new MessageParseError("Invalid BodyLength (" + bodyLength + ")");
            }
            catch (FieldConvertError e)
            {
                throw new MessageParseError(e.Message, e);
            }

            bytesConsumed = startPos + endPos + 1;
            return true;
        }

        private byte[] RemoveAndSwitch(byte[] array, int offset)
        {
            byte[] returnByte = _producerConsumerBuffer.Dequeue();
            var copyCount = Math.Max(0, _usedBufferLength - offset);
            System.Buffer.BlockCopy(array, offset, returnByte, 0, copyCount);
            Array.Clear(array, 0, _usedBufferLength);
            _usedBufferLength = copyCount;
            _producerConsumerBuffer.Enqueue(array);
            return returnByte;
        }
    }
}

