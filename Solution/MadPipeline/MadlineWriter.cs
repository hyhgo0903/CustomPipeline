using System;
using System.Buffers;
using MadPipeline.MadngineSource;

namespace MadPipeline
{
    public sealed class MadlineWriter : IBufferWriter<byte>
    {
        // 생성자에서 this로 입력받음
        private readonly Madline madline;

        public MadlineWriter(Madline madline)
        {
            this.madline = madline;
        }

        public Memory<byte> GetMemory(int sizeHint) => this.madline.GetMemory(sizeHint);
        public Span<byte> GetSpan(int sizeHint) => this.madline.GetSpan(sizeHint);
        public void Advance(int bytes) => this.madline.Advance(bytes);
        public void Complete() => this.madline.CompleteWriter();
        public bool TryWrite(in ReadOnlyMemory<byte> source, int TargetLength = -1) => this.madline.TryWrite(in source, TargetLength);
        public Signal DoWrite(in ReadOnlyMemory<byte> source) => this.madline.DoWrite(in source);
        public void Flush() => this.madline.CommitUnsynchronized();
        
    }
}