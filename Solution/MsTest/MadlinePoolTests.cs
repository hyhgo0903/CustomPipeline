namespace Tests
{
    using System.Buffers;
    using MadPipeline;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Infrastructure;

    [TestClass]
    public sealed class MadlinePoolTests
    {
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

            var buffer = madline.GetMemory(writeSize);
            var allocatedSize = buffer.Length;
            madline.Advance(buffer.Length);
            buffer = madline.GetMemory(1);
            var ensuredSize = buffer.Length;
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

            madline.TryRead(out var readResult);
            
            madline.AdvanceTo(readResult.End);
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
