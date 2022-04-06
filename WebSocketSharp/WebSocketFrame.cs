namespace WebSocketSharp.Signal
{
    public class WebSocketFrame
    {
        public bool Fin { get; set; }
        public byte Type { get; set; }
        public long Length { get; set; }
        public long Offset { get; set; }
        public byte[] Payload { get; set; }

        public byte[] MaskKey { get; set; }

        public bool IsMasked { get; set; }

        public bool Compressed { get; set; }

        public long Remainder { get; set; }
    }
}