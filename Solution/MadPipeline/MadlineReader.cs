namespace MadPipeline
{
    using System;
    using MadngineSource;

    public interface IMadlineReader
    {
        // 생성자에서 this로 입력받음
        public bool TryRead(out ReadResult result, int targetLength);
        public Future<ReadResult> DoRead(out ReadResult result, int targetLength, bool execute = false);
        public void AdvanceTo(in SequencePosition consumed);
        public void CompleteReader();
    }
}