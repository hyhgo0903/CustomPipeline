namespace MadPipeline
{
    using System;
    using System.Buffers;
    using MadngineSource;

    public interface IMadlineWriter : IBufferWriter<byte>
    {
        public bool TryWrite(in ReadOnlyMemory<byte> source);
        public Signal DoWrite();
        public bool Flush();
        public void CompleteWriter();
    }
}