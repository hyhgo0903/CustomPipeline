using System;
using MadPipeline;
using Tests.Infrastructure;

namespace Tests
{

    public abstract class MadlineTest : IDisposable
    {
        protected const int MaximumSizeHigh = 65;
        protected const int MaximumSizeLow = 6;

        private readonly TestMemoryPool pool;

        protected Madline Madline { get; }

        protected MadlineTest(int pauseWriterThreshold = MaximumSizeHigh, int resumeWriterThreshold = MaximumSizeLow)
        {
            this.pool = new TestMemoryPool();
            this.Madline = new Madline(
                new MadlineOptions(
                    this.pool,
                    pauseWriterThreshold,
                    resumeWriterThreshold
                ));
        }
        
        public void Dispose()
        {
            this.Madline.Writer.Complete();
            this.Madline.Reader.Complete();
            this.pool.Dispose();
        }
    }
}
