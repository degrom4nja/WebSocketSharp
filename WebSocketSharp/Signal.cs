using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Net.Security;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace WebSocketSharp.Signal
{
    public class Signal
    {
        private const string CHARACTERS = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private int code;
        private string message;
        private SslStream stream;
        private byte[] buffer;
        private CancellationTokenSource cancel;
        private ManualResetEvent resetEvent;
        private System.Timers.Timer timer;
        public delegate void ReleaseEventHandler();
        public delegate void CompleteEventHandler(int code, string message);
        public delegate void MessageEventHandler(string message);
        public delegate void BinaryEventHandler(byte[] binary);
        public delegate void CancelEventHandler();
        public event CancelEventHandler Canceled;
        public event EventHandler<EventArgs> Connected;
        public event MessageEventHandler ReceiveMessage;
        public event BinaryEventHandler BinaryReady;
        public event CompleteEventHandler Completed;
        public event ReleaseEventHandler Released;
        private FrameType lastFrameType;
        private StringBuilder textBuilder;
        private bool canceled;

        public Signal()
        {
            code = SignalCode.SUCCESS;
            canceled = false;
            message = "successful completed.";
        }

        public void Connect(string url)
        {
            Connect(url, null);
        }

        public void Connect(string url, Dictionary<string, string> header)
        {
            Task.Run(async () =>
            {
                Uri uri = new Uri(url);
                IPAddress[] addresses = await Dns.GetHostAddressesAsync(uri.Host);

                object[] objs = new object[] { uri, header };

                SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
                eventArgs.RemoteEndPoint = new IPEndPoint(addresses[0], uri.Port);
                eventArgs.UserToken = new object[] { uri, header };
                eventArgs.Completed += SocketEvent_Completed;
                Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, eventArgs);

                await Task.Delay(5000);
                if (eventArgs.ConnectSocket == null || !eventArgs.ConnectSocket.Connected)
                {
                    Socket.CancelConnectAsync(eventArgs);
                    Trace.WriteLine("Connecting Cancel.");
                }
            });
        }

        private void SocketEvent_Completed(object sender, SocketAsyncEventArgs e)
        {
            object[] objs = (object[])e.UserToken;
            Uri uri = (Uri)objs[0];
            Dictionary<string, string> header = (Dictionary<string, string>)objs[1];

            Task.Run(async () =>
            {
                if (e.ConnectSocket != null && e.ConnectSocket.Connected)
                {
                    using (stream = new SslStream(new NetworkStream(e.ConnectSocket)))
                    {
                        await stream.AuthenticateAsClientAsync(uri.Host);
                        if (await CreateHandshake(uri, header))
                            await ReadFrameAsync(stream);
                    }
                }
                Trace.WriteLine("WebSocketSharp: SslStream release.");
                Released?.Invoke();
            });
        }

        private string MakeWebSocketKey() {

            Random rand = new Random();
            string key = string.Empty;
            for(int i = 0; i < 22; i++)
                key += CHARACTERS[rand.Next(CHARACTERS.Length)];

            return key + "==";
        }

        public async Task SendMessage(string message)
        {
            byte[] bytes = EncodeText(message);
            if (stream != null && stream.CanWrite)
                await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        public ulong ReadInt64(byte[] buffer, int index)
        {
            return (ulong)(buffer[index] << 56 | buffer[index + 1] << 48 | buffer[index + 2] << 40 | buffer[index + 3] << 32 | buffer[index + 4] << 24 |
                buffer[index + 5] << 16 | buffer[index + 6] << 8 | buffer[index + 7]);
        }

        public ushort ReadInt16(byte[] buffer, int index)
        {
            return (ushort)(buffer[index] << 8 | buffer[index + 1]);
        }

        public void EncodeInt16(byte[] buffer, int index, ushort value)
        {
            buffer[index] = (byte)(value >> 8);
            buffer[index + 1] = (byte)value;
        }

        public void EncodeInt64(byte[] buffer, int index, ulong value)
        {
            buffer[index] = (byte)(value >> 56);
            buffer[index + 1] = (byte)(value >> 48);
            buffer[index + 2] = (byte)(value >> 40);
            buffer[index + 3] = (byte)(value >> 32);
            buffer[index + 4] = (byte)(value >> 24);
            buffer[index + 5] = (byte)(value >> 16);
            buffer[index + 6] = (byte)(value >> 8);
            buffer[index + 7] = (byte)value;
        }

        private string MakeHandshakeHeader(string host, string pathAndQuery, Dictionary<string, string> header)
        {
            Dictionary<string, string> kvp = new Dictionary<string, string>();

            StringBuilder builder = new StringBuilder();
            builder.Append("GET ");
            builder.Append(pathAndQuery);
            builder.Append(" HTTP/1.1\r\nHost: ");
            builder.Append(host);
            //builder.Append("\r\nUpgrade: websocket\r\nAccept-Encoding: gzip, deflate, br\r\nAccept-Language: ja,en-US;q=0.9,en;q=0.8\r\nCache-Control: no-cache\r\nConnection: upgrade\r\nSec-WebSocket-Version: 13\r\nSec-WebSocket-Key: E4WSEcseoWr4csPLS2QJHA==\r\nSec-WebSocket-Extensions: permessage-deflate; client_max_window_bits\r\n");

            string key = MakeWebSocketKey();
            builder.Append("\r\nUpgrade: websocket\r\nCache-Control: no-cache\r\nConnection: upgrade\r\nSec-WebSocket-Version: 13\r\nSec-WebSocket-Key: " + key + "\r\n");


            if (header != null)
            {
                foreach (KeyValuePair<string, string> dictionary in header)
                    builder.AppendLine(dictionary.Key + ": " + dictionary.Value);
            }

            builder.AppendLine();
            return builder.ToString();
        }

        private async Task<bool> CreateHandshake(Uri uri, Dictionary<string, string> requestHeader)
        {
            string header = MakeHandshakeHeader(uri.Host, uri.PathAndQuery, requestHeader);

            byte[] bytes = Encoding.UTF8.GetBytes(header);
            buffer = new byte[4096];

            if (!stream.CanWrite)
                return false;

            await stream.WriteAsync(bytes, 0, bytes.Length);

            if (!stream.CanRead)
                return false;

            int read = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (read < 1)
                return false;

            string response = Encoding.UTF8.GetString(buffer, 0, read);
            if (response.StartsWith("HTTP/1.1 101"))
            {
                Connected?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                Completed?.Invoke(SignalCode.FAIL, response);
            }
            return false;
        }

        public byte[] EncodeText(string text)
        {
            byte[] payload = Encoding.UTF8.GetBytes(text);
            return EncodeFrame(payload, FrameType.TEXT, true);
        }

        private byte[] EncodeFrame(byte[] payload, byte frameType, bool masked)
        {
            byte[] header;
            int maskLen = masked ? 4 : 0;

            if (payload.Length < 126)
            {
                header = new byte[2];
                header[1] = (byte)(0x80 | payload.Length);

            }
            else if (payload.Length < 65536)
            {
                header = new byte[4];
                header[1] = 0xfe;
                EncodeInt16(header, 2, (ushort)payload.Length);

            }
            else
            {
                header = new byte[10];
                header[1] = 0xff;
                EncodeInt64(header, 2, (ulong)payload.Length);
            }

            header[0] = (byte)(0x80 | frameType);

            byte[] frame = new byte[header.Length + maskLen + payload.Length];
            Array.Copy(header, 0, frame, 0, header.Length);

            if (masked)
            {
                byte[] maskKey = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    Random rd = new Random();
                    rd.NextBytes(maskKey);
                }
                Array.Copy(maskKey, 0, frame, header.Length, 4);

                for (int i = 0; i < payload.Length; i++)
                    payload[i] = (byte)(payload[i] ^ maskKey[i % 4]);
            }

            Array.Copy(payload, 0, frame, header.Length + maskLen, payload.Length);
            return frame;
        }

        private void DecryptMask(WebSocketFrame frame)
        {
            for (int i = 0; i < frame.Payload.Length; i++)
                frame.Payload[i] = (byte)(frame.Payload[i] ^ frame.MaskKey[i % 4]);
        }

        public void Dispose()
        {
            canceled = true;

        }

        private async Task<int> MaskedReady(WebSocketFrame frame, SslStream stream)
        {
            if (canceled)
                return 0;

            int offset = 0;
            int len = frame.IsMasked ? 4 : 0;

            if (frame.Length < 126 && len == 0)
                return 1;

            if (frame.Length == 126)
            {
                if (frame.IsMasked)
                {
                    len = 6;
                    offset = 2;
                }
                else
                    len = 2;
            }
            else if (frame.Length == 127)
            {
                if (frame.IsMasked)
                {
                    len = 12;
                    offset = 8;
                }
                else
                    len = 8;
            }

            byte[] buffer = new byte[len];
            int r = await stream.ReadAsync(buffer, 0, len);
            if (r == 0)
                return 0;

            if (frame.Length == 126)
                frame.Length = frame.Remainder = ReadInt16(buffer, 0);
            else if (frame.Length == 127)
                frame.Length = frame.Remainder = (long)ReadInt64(buffer, 0);

            if (frame.IsMasked)
                frame.MaskKey = new byte[] { buffer[offset], buffer[offset + 1], buffer[offset + 2], buffer[offset + 3] };

            return r;
        }

        private async Task<int> PayloadReady(WebSocketFrame frame, SslStream stream)
        {
            if (canceled)
                return 0;

            int offset = (int)(frame.Length - frame.Remainder);

            if(offset == 0)
                frame.Payload = new byte[frame.Length];

            int r = await stream.ReadAsync(frame.Payload, offset, (int)frame.Remainder);
            if (r == 0)
                return 0;

            frame.Remainder -= r;
            if (frame.Remainder > 0)
            {
                await PayloadReady(frame, stream);
                return r;
            }

            if (frame.IsMasked)
            {
                for (int i = 0; i < frame.Length; i++)
                    frame.Payload[i] = (byte)(frame.Payload[i] ^ frame.MaskKey[i % 4]);
            }
            return r;
        }

        private async Task<int> HeaderReady(WebSocketFrame frame, SslStream stream)
        {
            if (canceled)
                return 0;

            byte[] buffer = new byte[2];
            int r = await stream.ReadAsync(buffer, 0, 2);
            if (r == 0)
                return 0;

            frame.Fin = (buffer[0] & 0x80) == 0x80;
            frame.Type = (byte)(buffer[0] & 0x0f);
            frame.Compressed = (buffer[0] & 0x40) == 0x40;
            frame.IsMasked = (buffer[1] & 0x80) == 0x80;

            frame.Length = frame.Remainder = buffer[1] & 0x7f;

            if (frame.Length == 0)
                return 0;

            return r;
        }

        private async Task<WebSocketFrame> ReadFrame(SslStream stream)
        {
            if (canceled)
                return null;

            WebSocketFrame frame = new WebSocketFrame();
            int r = await HeaderReady(frame, stream);
            if (r == 0)
                return null;

            r = await MaskedReady(frame, stream);
            if (r == 0)
                return null;

            r = await PayloadReady(frame, stream);
            if (r == 0)
                return null;

            return frame;
        }

        private async Task ReadFrameAsync(SslStream stream)
        {
            if (canceled)
                return;

            WebSocketFrame frame = await ReadFrame(stream);
            if(frame == null)
            {
                canceled = true;
                return;
            }

            await ParsePayload(frame, stream);

            if (frame.Type != FrameType.DISCONNECT)
                await ReadFrameAsync(stream);

        }

        private async Task ParsePayload(WebSocketFrame frame, SslStream stream)
        {
            switch (frame.Type)
            {
                case FrameType.CONTINUATION:
                    {
                        string text = Encoding.UTF8.GetString(frame.Payload);
                        textBuilder.Append(text);
                        if (frame.Fin)
                            ReceiveMessage?.Invoke(textBuilder.ToString());
                    }
                    break;
                case FrameType.TEXT:
                    {
                        string text = Encoding.UTF8.GetString(frame.Payload);
                        if (frame.Fin)
                            ReceiveMessage?.Invoke(text);
                        else
                        {
                            textBuilder = new StringBuilder();
                            textBuilder.Append(text);
                        }
                    }
                    break;
                case FrameType.BINARY:
                    {
                        BinaryReady?.Invoke(frame.Payload);
                    }
                    break;
                case FrameType.PING:
                    {
                        byte[] bytes = EncodeFrame(frame.Payload, FrameType.PONG, false);
                        await stream.WriteAsync(bytes, 0, bytes.Length);
                        Trace.WriteLine("WebSocketSharp Signal send ping.");
                    }
                    break;
                case FrameType.DISCONNECT:
                    {
                        Trace.WriteLine("WebSocketSharp disconnect: FrameType is 0x08");
                        string text = Encoding.UTF8.GetString(frame.Payload);
                        Trace.WriteLine(text);
                        code = SignalCode.DISCONNECT;
                        message = "Disconnect: FrameType is 0x08";
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
