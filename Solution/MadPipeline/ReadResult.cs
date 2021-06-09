namespace MadPipeline
{
    using System.Buffers;
    
    
    public readonly struct ReadResult
    {
        internal readonly ReadOnlySequence<byte> ResultBuffer;

        /// <summary>
        /// Creates a new instance of <see cref="IsCompleted"/> flags.
        /// </summary>
        public ReadResult(ReadOnlySequence<byte> buffer, bool isCompleted)
        {
            ResultBuffer = buffer;
            this.IsCompleted = isCompleted;
        }

        /// <summary>
        /// 읽은 <see cref="ReadOnlySequence{Byte}"/>입니다.
        /// </summary>
        public ReadOnlySequence<byte> Buffer => ResultBuffer;
        
        public bool IsCompleted { get; }
    }
}