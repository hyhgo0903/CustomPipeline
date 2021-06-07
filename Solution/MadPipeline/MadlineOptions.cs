namespace MadPipeline
{
    using System.Buffers;

    public sealed class MadlineOptions
    {
        private const int DefaultMinimumSegmentSize = 4096;
        private const int DefaultResumeWriterThreshold = DefaultMinimumSegmentSize * Madline.InitialSegmentPoolSize / 2;
        private const int DefaultPauseWriterThreshold = DefaultMinimumSegmentSize * Madline.InitialSegmentPoolSize;
        private const int DefaultTargetBytes = 5;

        public MadlineOptions(
            MemoryPool<byte>? pool = null,
            long pauseWriterThreshold = -1,
            long resumeWriterThreshold = -1,
            int minimumSegmentSize = -1,
            int defaultTargetBytes = -1)
        {
            if (pauseWriterThreshold == -1)
            {
                pauseWriterThreshold = DefaultPauseWriterThreshold;
            }
            if (resumeWriterThreshold == -1)
            {
                resumeWriterThreshold = DefaultResumeWriterThreshold;
            }

            if (defaultTargetBytes == -1)
            {
                defaultTargetBytes = DefaultTargetBytes;
            }
            this.Pool = pool ?? MemoryPool<byte>.Shared;
            this.PauseWriterThreshold = pauseWriterThreshold;
            this.ResumeWriterThreshold = resumeWriterThreshold;
            this.MinimumSegmentSize = minimumSegmentSize == -1 ? DefaultMinimumSegmentSize : minimumSegmentSize;
            this.TargetBytes = defaultTargetBytes;
        }

        // 편의를 위한 디폴트 옵션
        public static MadlineOptions Default { get; } = new();

        // 미리 만들어서 Madline 생성 시 대입됨
        public long PauseWriterThreshold { get; }
        public long ResumeWriterThreshold { get; }
        public int MinimumSegmentSize { get; }
        public int TargetBytes { get; }
        public MemoryPool<byte> Pool { get; }
    }
}
