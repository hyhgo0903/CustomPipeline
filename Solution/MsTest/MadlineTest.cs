namespace Tests
{
    using System;
    using MadPipeline;
    using Infrastructure;
    using System.Buffers;
    using MadPipeline.MadngineSource;

    public abstract class MadlineTest : IDisposable
    {
        protected const int MaximumSizeHigh = 65;
        protected const int MaximumSizeLow = 6;

        private readonly TestMemoryPool pool;

        protected Madline SmallMadline { get; }
        protected IMadlineWriter SmallMadWriter => SmallMadline;
        protected IMadlineReader SmallMadReader => SmallMadline;

        protected MadlineTest(int pauseWriterThreshold = MaximumSizeHigh, int resumeWriterThreshold = MaximumSizeLow)
        {
            this.pool = new TestMemoryPool();
            this.SmallMadline = new Madline(
                new MadlineOptions(
                    this.pool,
                    pauseWriterThreshold,
                    resumeWriterThreshold
                ));
        }
        
        public void Dispose()
        {
            this.SmallMadWriter.CompleteWriter();
            this.SmallMadReader.CompleteReader();
            this.pool.Dispose();
        }
    }
}
