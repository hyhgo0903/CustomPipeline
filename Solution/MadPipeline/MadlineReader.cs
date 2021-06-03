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
<<<<<<< HEAD
=======
<<<<<<< HEAD
>>>>>>> main

        public bool TryRead(out ReadResult result, int targetLength = -1)
            => this.madline.TryRead(out result, targetLength);
        public Future<ReadResult> DoRead(out ReadResult result)
            => this.madline.DoRead(out result);
        public void Advance(SequencePosition consumed) => this.madline.AdvanceReader(consumed);
        public void Advance(SequencePosition consumed, SequencePosition examined) => this.madline.AdvanceReader(consumed, examined);
<<<<<<< HEAD
=======
=======
        /// <summary>
        /// <see cref="MadlineReader"/>로 읽기를 하며, 필요시 이어나갈 작업을 입력받습니다.
        /// </summary>
        /// <param name="readResult">읽은 뒤 반환할 <see cref="ReadOnlySequence"/>입니다.</param>
        /// <param name="afterReadCallback">읽기 후 이어나갈 콜백함수를 지정합니다.</param>
        /// <returns>읽기에 성공한 경우 true, 읽기 예약이 되거나 실패한 경우는 false로 반환합니다.</returns>
        public bool ReadAsync(out ReadOnlySequence<byte> readResult, Action<ReadOnlySequence<byte>>? afterReadCallback = null)
            => this.madline.TryRead(out readResult, afterReadCallback);
        public void AdvanceTo(SequencePosition consumed) => this.madline.AdvanceReader(consumed);
        public void AdvanceTo(SequencePosition consumed, SequencePosition examined) => this.madline.AdvanceReader(consumed, examined);
>>>>>>> parent of 4c458c2 (리뷰 전 푸시)
>>>>>>> main
        public void Complete() => this.madline.CompleteReader();
    }
}