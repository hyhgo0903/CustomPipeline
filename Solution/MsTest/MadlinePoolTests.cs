using System;
using System.Buffers;
using System.Threading.Tasks;
using MadPipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Infrastructure;

namespace Tests
{
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
                MadlineWriter writableBuffer = madline.Writer.WriteEmpty(writeSize);
                writableBuffer.Flush();
            }
            
            madline.Reader.ReadAsync(out var readResult);
            madline.Reader.AdvanceTo(readResult.End);

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
                MadlineWriter writableBuffer = madline.Writer.WriteEmpty(writeSize);
                writableBuffer.Flush();
            }

            madline.Reader.ReadAsync(out var readResult);
            madline.Writer.WriteEmpty(writeSize);
            madline.Reader.AdvanceTo(readResult.End);
            madline.Writer.Flush();

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
            var buffer = madline.Writer.GetMemory(writeSize);
            madline.Writer.Advance(buffer.Length);
            madline.Writer.GetMemory(buffer.Length);
            madline.Writer.Advance(writeSize);
            madline.Writer.Flush();

            Assert.AreEqual(2, pool.CurrentlyRentedBlocks);

            // Read everything
            madline.Reader.ReadAsync(out var readResult);
            madline.Reader.AdvanceTo(readResult.End);

            // Try writing more
            madline.Writer.WriteAsync(new byte[writeSize]);

            Assert.AreEqual(1, pool.CurrentlyRentedBlocks);
            Assert.AreEqual(2, pool.DisposedBlocks);
        }

        [TestMethod]
        public void MultipleCompleteReaderWriterCauseDisposeOnlyOnce()
        {
            var pool = new DisposeTrackingBufferPool();

            var madline = new Madline(new MadlineOptions(pool));
            madline.Writer.WriteAsync(new byte[] { 1 });

            madline.Writer.Complete();
            madline.Reader.Complete();
            Assert.AreEqual(1, pool.DisposedBlocks);

            madline.Writer.Complete();
            madline.Reader.Complete();
            Assert.AreEqual(1, pool.DisposedBlocks);
        }

        [TestMethod]
        public void RentsMinimumSegmentSize()
        {
            var pool = new DisposeTrackingBufferPool();
            var writeSize = 512;

            var madline = new Madline(new MadlineOptions(pool, minimumSegmentSize: 2020));

            Memory<byte> buffer = madline.Writer.GetMemory(writeSize);
            int allocatedSize = buffer.Length;
            madline.Writer.Advance(buffer.Length);
            buffer = madline.Writer.GetMemory(1);
            int ensuredSize = buffer.Length;
            madline.Writer.Flush();

            madline.Reader.Complete();
            madline.Writer.Complete();

            Assert.AreEqual(2020, ensuredSize);
            Assert.AreEqual(2020, allocatedSize);
        }

        [TestMethod]
        public void ReturnsWriteHeadOnComplete()
        {
            var pool = new DisposeTrackingBufferPool();
            var madline = new Madline(new MadlineOptions(pool));
            madline.Writer.GetMemory(512);

            madline.Reader.Complete();
            madline.Writer.Complete();
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
            madline.Writer.GetMemory(512);
            madline.Writer.GetMemory(4096);

            madline.Reader.Complete();
            madline.Writer.Complete();
            Assert.AreEqual(0, pool.CurrentlyRentedBlocks);
            Assert.AreEqual(2, pool.DisposedBlocks);
        }

        [TestMethod]
        public void WriteDuringReadIsNotReturned()
        {
            var pool = new DisposeTrackingBufferPool();

            const int writeSize = 512;

            var madline = new Madline(new MadlineOptions(pool));
            madline.Writer.WriteAsync(new byte[writeSize]);

            madline.Writer.GetMemory(writeSize);

            madline.Reader.ReadAsync(out var readResult);
            
            madline.Reader.AdvanceTo(readResult.End);
            madline.Writer.Write(new byte[writeSize]);
            madline.Writer.Flush();

            Assert.AreEqual(1, pool.CurrentlyRentedBlocks);
        }

        [TestMethod]
        public async Task OnWriterCompletedCalledAfterBlocksReturned()
        {
            var pool = new DisposeTrackingBufferPool();

            var madline = new Madline(new MadlineOptions(pool));
            madline.Writer.WriteAsync(new byte[1]);

            Assert.AreEqual(1, pool.CurrentlyRentedBlocks);

            madline.Reader.Complete();
            madline.Writer.Complete();
        }

        [TestMethod]
        public async Task OnReaderCompletedCalledAfterBlocksReturned()
        {
            var pool = new DisposeTrackingBufferPool();

            var madline = new Madline(new MadlineOptions(pool));
            madline.Writer.WriteAsync(new byte[1]);

            Assert.AreEqual(1, pool.CurrentlyRentedBlocks);


            madline.Writer.Complete();
            madline.Reader.Complete();
        }
        
    }
}
