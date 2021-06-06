using System;
using System.Buffers;
using MadPipeline.MadngineSource;

namespace MadPipeline
{
    public sealed class MadlineReader
    {
        // 생성자에서 this로 입력받음
        private readonly Madline madline;

        public MadlineReader(Madline madline)
        {
            this.madline = madline;
        }

        public bool TryRead(out ReadResult result, int targetLength = -1)
            => this.madline.TryRead(out result, targetLength);
        public Future<ReadResult> DoRead() => this.madline.DoRead();
        public void Advance(SequencePosition consumed) => this.madline.AdvanceReader(consumed);
        public void Advance(SequencePosition consumed, SequencePosition examined) => this.madline.AdvanceReader(consumed, examined);
        public void Complete() => this.madline.CompleteReader();
    }
}