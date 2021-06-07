namespace Tests.Infrastructure
{
    using System;
    using System.Buffers;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    public class TestMemoryPool : MemoryPool<byte>
    {
        private MemoryPool<byte> pool = Shared;

        private bool disposed;

        public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
        {
            this.CheckDisposed();
            return new PooledMemory(this.pool.Rent(minBufferSize), this);
        }

        protected override void Dispose(bool disposing)
        {
            this.disposed = true;
        }

        public override int MaxBufferSize => 4096;

        internal void CheckDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(nameof(TestMemoryPool));
            }
        }

        private class PooledMemory : MemoryManager<byte>
        {
            private IMemoryOwner<byte> owner;

            private readonly TestMemoryPool pool;

            private int referenceCount;
            

            public PooledMemory(IMemoryOwner<byte> owner, TestMemoryPool pool)
            {
                this.owner = owner;
                this.pool = pool;
                this.referenceCount = 1;
            }

            protected override void Dispose(bool disposing)
            {
                this.pool.CheckDisposed();
            }

            public override MemoryHandle Pin(int elementIndex = 0)
            {
                this.pool.CheckDisposed();
                Interlocked.Increment(ref referenceCount);

                if (!MemoryMarshal.TryGetArray(this.owner.Memory, out ArraySegment<byte> segment))
                {
                    throw new InvalidOperationException();
                }

                unsafe
                {
                    try
                    {
                        if ((uint)elementIndex > (uint)segment.Count)
                        {
                            throw new ArgumentOutOfRangeException(nameof(elementIndex));
                        }

                        GCHandle handle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);

                        return new MemoryHandle(Unsafe.Add<byte>(((void*)handle.AddrOfPinnedObject()),
                            elementIndex + segment.Offset), handle, this);
                    }
                    catch
                    {
                        this.Unpin();
                        throw;
                    }
                }
            }

            public override void Unpin()
            {
                this.pool.CheckDisposed();

                int newRefCount = Interlocked.Decrement(ref referenceCount);

                if (newRefCount < 0)
                    throw new InvalidOperationException();
            }

            protected override bool TryGetArray(out ArraySegment<byte> segment)
            {
                this.pool.CheckDisposed();
                return MemoryMarshal.TryGetArray(this.owner.Memory, out segment);
            }

            public override Memory<byte> Memory
            {
                get
                {
                    this.pool.CheckDisposed();
                    return this.owner.Memory;
                }
            }

            public override Span<byte> GetSpan()
            {
                this.pool.CheckDisposed();
                return this.owner.Memory.Span;
            }
        }
    }
}
