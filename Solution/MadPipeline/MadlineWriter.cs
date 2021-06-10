namespace MadPipeline
{
    using System;
    using System.Buffers;
    using MadngineSource;

    public interface IMadlineWriter : IBufferWriter<byte>
    {
        public bool TryWrite(in ReadOnlyMemory<byte> source);
        public Signal DoWrite(in ReadOnlyMemory<byte> source, bool execute = false);
        public bool Flush();
        public void CompleteWriter();
    }
}