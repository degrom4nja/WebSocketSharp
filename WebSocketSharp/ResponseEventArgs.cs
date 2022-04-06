using System;

namespace WebSocketSharp.Signal
{
    public class ResponseEventArgs : EventArgs
    {
        private string _message;
        private byte[] _binary;
        public string Message { get { return _message; } }
        public byte[] Binary { get { return _binary; } }

        public ResponseEventArgs(string message)
        {
            _message = message;
        }

        public ResponseEventArgs(byte[] binary)
        {
            _binary = binary;
        }
    }
}