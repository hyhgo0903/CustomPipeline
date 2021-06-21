namespace MadPipeline
{
    using System;
    using System.Buffers;
    using MadngineSource;

    public interface IMadlineWriter : IBufferWriter<byte>
    {
        public bool TryWrite(in ReadOnlyMemory<byte> source);
        public Signal WriteSignal();
        public bool Flush();
        public void CompleteWriter();
        public bool TryAdvance(int bytesWritten);
        public void CopyToWriteHead(in ReadOnlyMemory<byte> source, in Memory<byte> memory);


    }
}