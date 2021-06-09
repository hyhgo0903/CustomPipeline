namespace Tests
{
    using System;
    using System.Buffers;
    using MadPipeline;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Infrastructure;

    [TestClass]
    public sealed class MadlinePoolTests
    {
        [TestMethod]
        public void AdvanceToEndReturnsAllBlocks()
        {
            var pool = new DisposeTrackingBufferPool();

            var writeSize = 512;

            var madline = new Madline(new MadlineOptions(pool));
            while (pool.CurrentlyRentedBlocks != 3)
            {
                var writableBuffer = madline.WriteEmpty(writeSize);
                writableBuffer.Flush();
            }
      
            madline.TryRead(out var readResult, 0);
            madline.AdvanceTo(readResult.Buffer.End);

            Assert.AreEqual(0, pool.CurrentlyRentedBlocks);
            Assert.AreEqual(3, pool.DisposedBlocks);
        }

        [TestMethod]
        public void AdvanceToEndReturnsAllButOneBlockIfWritingBeforeAdvance()
        {
            var pool = new DisposeTrackingBufferPool();

            const int writeSize = 512;

            var madline = new Madline(new MadlineOptions(pool));
            while (pool.CurrentlyRentedBlocks != 3)
            {
                madline.WriteEmpty(writeSize);
                madline.Flush();
            }

            madline.TryRead(out var readResult, 0);
            madline.WriteEmpty(writeSize);
            madline.AdvanceTo(readResult.Buffer.End);
            madline.Flush();

            Assert.AreEqual(1, pool.CurrentlyRentedBlocks);
            Assert.AreEqual(2, pool.DisposedBlocks);
        }

        [TestMethod]
        public void CanWriteAfterReturningMultipleBlocks()
        {
            var pool = new DisposeTrackingBufferPool();

            const int writeSize = 512;

            var madline = new Madline(new MadlineOptions(pool));

            // Write two blocks
            var buffer = madline.GetMemory(writeSize);
            madline.Advance(buffer.Length);
            madline.GetMemory(buffer.Length);
            madline.Advance(writeSize);
            madline.Flush();

            Assert.AreEqual(2, pool.CurrentlyRentedBlocks);

            // DoRead everything
            madline.TryRead(out var readResult, 0);
            madline.AdvanceTo(readResult.Buffer.End);

            // Try writing more
            madline.TryWrite(new byte[writeSize]);

            Assert.AreEqual(1, pool.CurrentlyRentedBlocks);
            Assert.AreEqual(2, pool.DisposedBlocks);
        }

        [TestMethod]
        public void MultipleCompleteReaderWriterCauseDisposeOnlyOnce()
        {
            var pool = new DisposeTrackingBufferPool();

            var madline = new Madline(new MadlineOptions(pool));
            madline.TryWrite(new byte[] { 1 });

            madline.CompleteWriter();
            madline.CompleteReader();
            Assert.AreEqual(1, pool.DisposedBlocks);

            madline.CompleteWriter();
            madline.CompleteReader();
            Assert.AreEqual(1, pool.DisposedBlocks);
        }

        [TestMethod]
        public void RentsMinimumSegmentSize()
        {
            var pool = new DisposeTrackingBufferPool();
            var writeSize = 512;

            var madline = new Madline(new MadlineOptions(pool, minimumSegmentSize: 2020));

            Memory<byte> buffer = madline.GetMemory(writeSize);
            int allocatedSize = buffer.Length;
            madline.Advance(buffer.Length);
            buffer = madline.GetMemory(1);
            int ensuredSize = buffer.Length;
            madline.Flush();

            madline.CompleteReader();
            madline.CompleteWriter();

            Assert.AreEqual(2020, ensuredSize);
            Assert.AreEqual(2020, allocatedSize);
        }

        [TestMethod]
        public void ReturnsWriteHeadOnComplete()
        {
            var pool = new DisposeTrackingBufferPool();
            var madline = new Madline(new MadlineOptions(pool));
            madline.GetMemory(512);

            madline.CompleteReader();
            madline.CompleteWriter();
            Assert.AreEqual(0, pool.CurrentlyRentedBlocks);
            Assert.AreEqual(1, pool.DisposedBlocks);
        }

        [TestMethod]
        public void ReturnsWriteHeadWhenRequestingLargerBlock()
        {
            var pool = new DisposeTrackingBufferPool();
            var options = new MadlineOptions(pool,
                minimumSegmentSize: 2048);

            var madline = new Madline(options);
            madline.GetMemory(512);
            madline.GetMemory(4096);

            madline.CompleteReader();
            madline.CompleteWriter();
            Assert.AreEqual(0, pool.CurrentlyRentedBlocks);
            Assert.AreEqual(2, pool.DisposedBlocks);
        }

        [TestMethod]
        public void WriteDuringReadIsNotReturned()
        {
            var pool = new DisposeTrackingBufferPool();

            const int writeSize = 512;

            var madline = new Madline(new MadlineOptions(pool));
            madline.TryWrite(new byte[writeSize]);

            madline.GetMemory(writeSize);

            madline.TryRead(out var readResult, 0);
      
            madline.AdvanceTo(readResult.Buffer.End);
            madline.Write(new byte[writeSize]);
            madline.Flush();

            Assert.AreEqual(1, pool.CurrentlyRentedBlocks);
        }

        [TestMethod]
        public void OnWriterCompletedCalledAfterBlocksReturned()
        {
            var pool = new DisposeTrackingBufferPool();

            var madline = new Madline(new MadlineOptions(pool));
            madline.TryWrite(new byte[1]);

            Assert.AreEqual(1, pool.CurrentlyRentedBlocks);

            madline.CompleteReader();
            madline.CompleteWriter();
        }

        [TestMethod]
        public void OnReaderCompletedCalledAfterBlocksReturned()
        {
            var pool = new DisposeTrackingBufferPool();

            var madline = new Madline(new MadlineOptions(pool));
            madline.TryWrite(new byte[1]);

            Assert.AreEqual(1, pool.CurrentlyRentedBlocks);


            madline.CompleteReader();
            madline.CompleteWriter();
        }
  
    }
}
