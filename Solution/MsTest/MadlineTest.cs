namespace Tests
{
    using System;
    using MadPipeline;
    using Infrastructure;

    public abstract class MadlineTest : IDisposable
    {
        protected const int MaximumSizeHigh = 65;
        protected const int MaximumSizeLow = 6;

        private readonly TestMemoryPool pool;

        protected Madline Madline { get; }
        protected IMadlineWriter MadWriter { get; }
        protected IMadlineReader MadReader { get; }

        protected MadlineTest(int pauseWriterThreshold = MaximumSizeHigh, int resumeWriterThreshold = MaximumSizeLow)
        {
            this.pool = new TestMemoryPool();
            this.Madline = new Madline(
                new MadlineOptions(
                    this.pool,
                    pauseWriterThreshold,
                    resumeWriterThreshold
                ));
            this.MadWriter = Madline;
            this.MadReader = Madline;
        }
        
        public void Dispose()
        {
            this.Madline.CompleteWriter();
            this.Madline.CompleteReader();
            this.pool.Dispose();
        }
    }
}
