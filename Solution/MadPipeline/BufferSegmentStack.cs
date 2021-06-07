namespace MadPipeline
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    // 버퍼세그먼트 스택 또한 기본적으로 Pipeline과 동일하게 이용
    internal struct BufferSegmentStack
    {
        private SegmentAsValueType[] array;
        private int size;

        public BufferSegmentStack(int size)
        {
            this.array = new SegmentAsValueType[size];
            this.size = 0;
        }

        public int Count => this.size;

        public bool TryPop([NotNullWhen(true)] out BufferSegment? result)
        {
            var stackSize = this.size - 1;
            SegmentAsValueType[] segmentArray = this.array;

            if ((uint)stackSize >= (uint)segmentArray.Length)
            {
                result = default;
                return false;
            }
            this.size = stackSize;
            result = segmentArray[stackSize];
            segmentArray[stackSize] = default;
            return true;
        }

        public void Push(BufferSegment item)
        {
            var stackSize = this.size;
            SegmentAsValueType[] segmentArray = this.array;

            if ((uint)stackSize < (uint)segmentArray.Length)
            {
                segmentArray[stackSize] = item;
                this.size = stackSize + 1;
            }
            else
            {
                this.PushWithResize(item);
            }
        }

        private void PushWithResize(BufferSegment item)
        {
            Array.Resize(ref this.array, 2 * this.array.Length);
            this.array[size] = item;
            this.size++;
        }

        private readonly struct SegmentAsValueType
        {
            private readonly BufferSegment value;
            private SegmentAsValueType(BufferSegment newValue) => this.value = newValue;
            public static implicit operator SegmentAsValueType(BufferSegment s) => new (s);
            public static implicit operator BufferSegment(SegmentAsValueType s) => s.value;
        }
    }
}
