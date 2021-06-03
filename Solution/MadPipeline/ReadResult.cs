using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadPipeline
{
    // isCanceled 필요하면 그 때 부활
    
    /// <summary>
    /// <see cref="MadlineReader.TryRead"/> 호출의 결과입니다.
    /// </summary>
    public readonly struct ReadResult
    {
        internal readonly ReadOnlySequence<byte> ResultBuffer;
        internal readonly ResultFlags ResultFlags;

        /// <summary>
        /// Creates a new instance of <see cref="IsCompleted"/> flags.
        /// </summary>
        public ReadResult(ReadOnlySequence<byte> buffer, bool isCompleted)
        {
            ResultBuffer = buffer;
            ResultFlags = ResultFlags.None;

            if (isCompleted)
            {
                ResultFlags |= ResultFlags.Completed;
            }
        }

        /// <summary>
        /// 읽은 <see cref="ReadOnlySequence{Byte}"/>입니다.
        /// </summary>
        public ReadOnlySequence<byte> Buffer => ResultBuffer;
        
        /// <summary>
        /// <see cref="MadlineReader"/> 완료시 참이 반환됩니다.
        /// </summary>
        public bool IsCompleted => (ResultFlags & ResultFlags.Completed) != 0;
    }

    [Flags]
    internal enum ResultFlags : byte
    {
        None = 0x0,
        Completed = 0x1
    }
}
