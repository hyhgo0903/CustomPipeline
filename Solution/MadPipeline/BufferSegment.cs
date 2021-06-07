namespace MadPipeline
{
    using System;
    using System.Buffers;
    using System.Diagnostics;

    // BufferSegment는 파이프라인 참조하여 가져오려 했지만, 거기서 internal class라...
    // ReadOnlySequenceSegment<T> 자체가 추상 클래스라 구현이 필요한데, 여기서 하는 것.
    internal sealed class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        private IMemoryOwner<byte>? memoryOwner;
        private byte[]? array;
        private BufferSegment? next;
        private int end;
        
        // 바이트가 범위의 끝점
        public int End
        {
            get => end;
            set
            {
                Debug.Assert(value <= AvailableMemory.Length);

                this.end = value;
                this.Memory = AvailableMemory[..value];
            }
        }
        
        // 링크드 리스트처럼 다음 세그먼트를 지정
        // -> 메모리 연속되어 있지 않아도 참조할 수 있음
        public BufferSegment? NextSegment
        {
            get => next;
            set
            {
                this.Next = value;
                this.next = value;
            }
        }
        
        public Memory<byte> AvailableMemory { get; private set; }

        public int Length => this.End;

        public int WritableBytes => this.AvailableMemory.Length - this.End;

        public void SetOwnedMemory(IMemoryOwner<byte> owner)
        {
            this.memoryOwner = owner;
            this.AvailableMemory = owner.Memory;
        }

        public void SetOwnedMemory(byte[] arrayPoolBuffer)
        {
            this.array = arrayPoolBuffer;
            this.AvailableMemory = arrayPoolBuffer;
        }

        public void ResetMemory()
        {
            IMemoryOwner<byte>? owner = memoryOwner;
            if (owner != null)
            {
                memoryOwner = null;
                owner.Dispose();
            }
            else
            {
                Debug.Assert(array != null);
                ArrayPool<byte>.Shared.Return(array);
                array = null;
            }

            this.Next = null;
            this.RunningIndex = 0;
            this.Memory = default;
            this.next = null;
            this.end = 0;
            this.AvailableMemory = default;
        }
        
        public void SetNext(BufferSegment? segment)
        {
            Debug.Assert(segment != null);
            Debug.Assert(this.Next == null);

            this.NextSegment = segment;

            segment = this;

            while (segment.Next != null)
            {
                Debug.Assert(segment.NextSegment != null);
                segment.NextSegment.RunningIndex = segment.RunningIndex + segment.Length;
                segment = segment.NextSegment;
            }
        }
        
        internal static long GetLength(BufferSegment startSegment, int startIndex, BufferSegment endSegment, int endIndex)
        {
            return (endSegment.RunningIndex + (uint)endIndex) - (startSegment.RunningIndex + (uint)startIndex);
        }
        
        internal static long GetLength(long startPosition, BufferSegment endSegment, int endIndex)
        {
            return (endSegment.RunningIndex + (uint)endIndex) - startPosition;
        }
    }
}
