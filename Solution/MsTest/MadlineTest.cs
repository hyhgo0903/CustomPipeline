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
        protected IMadlineWriter MadWriter => Madline;
        protected IMadlineReader MadReader => Madline;

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
            this.MadWriter.CompleteWriter();
            this.MadReader.CompleteReader();
            this.pool.Dispose();
        }
    }
}
