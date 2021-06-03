using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Infrastructure
{
    public class DisposeTrackingBufferPool : TestMemoryPool
    {
        public int DisposedBlocks { get; set; }
        public int CurrentlyRentedBlocks { get; set; }

        public override IMemoryOwner<byte> Rent(int size)
        {
            return new DisposeTrackingMemoryManager(new byte[size], this);
        }

        protected override void Dispose(bool disposing)
        {
        }

        private class DisposeTrackingMemoryManager : MemoryManager<byte>
        {
            private byte[] array;

            private readonly DisposeTrackingBufferPool bufferPool;

            public DisposeTrackingMemoryManager(byte[] array, DisposeTrackingBufferPool bufferPool)
            {
                this.array = array;
                this.bufferPool = bufferPool;
                this.bufferPool.CurrentlyRentedBlocks++;
            }

            public override Memory<byte> Memory => CreateMemory(this.array.Length);

            public bool IsDisposed => this.array == null;

            public override MemoryHandle Pin(int elementIndex = 0)
            {
                throw new NotImplementedException();
            }

            public override void Unpin()
            {
                throw new NotImplementedException();
            }

            protected override bool TryGetArray(out ArraySegment<byte> segment)
            {
                if (this.IsDisposed)
                    throw new ObjectDisposedException(nameof(DisposeTrackingBufferPool));
                segment = new ArraySegment<byte>(this.array);
                return true;
            }

            protected override void Dispose(bool disposing)
            {
                this.bufferPool.DisposedBlocks++;
                this.bufferPool.CurrentlyRentedBlocks--;

                this.array = null;
            }

            public override Span<byte> GetSpan()
            {
                if (this.IsDisposed)
                    throw new ObjectDisposedException(nameof(DisposeTrackingBufferPool));
                return this.array;
            }
        }
    }
}
