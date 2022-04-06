using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketSharp.Signal
{
    class FrameType
    {
        public const byte CONTINUATION = 0x00;
        public const byte TEXT = 0x01;
        public const byte BINARY = 0x02;
        public const byte PING = 0x09;
        public const byte PONG = 0x0A;

        public const byte CANCEL = 0xfd;
        public const byte ERROR = 0xfe;
        public const byte DISCONNECT = 0x08;
    }
}
