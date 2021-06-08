namespace MadPipeline
{
    using System;
    using System.Buffers;
    using MadngineSource;

    public interface IMadlineWriter : IBufferWriter<byte>
    {
        public void CompleteWriter();
        public bool TryWrite(ReadOnlyMemory<byte> source, int targetBytes = -1);
        public Signal DoWrite();
        public bool Flush();
    }
}