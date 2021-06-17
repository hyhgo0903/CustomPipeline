namespace MadPipeline
{
    using System;
    using MadngineSource;
    using System.Buffers;

    public interface IMadlineReader
    {
        // 생성자에서 this로 입력받음
        public int TryRead(out ReadOnlySequence<byte> result);
        public Future<ReadOnlySequence<byte>>DoRead();
        public void AdvanceTo(in SequencePosition consumed);
        public void CompleteReader();
    }
}