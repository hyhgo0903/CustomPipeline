using System.Data.Common;

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

        public static Random r = new Random();

        public static ReadOnlyMemory<byte> CreateMessage(byte[] body)
        {
            var bodyLength = body.Length;
            var header = new Header(bodyLength, false, false);
            var headerBytes = header.Span.ToArray();
            var array = new byte[bodyLength + 2];
            Array.Copy(headerBytes, array, headerBytes.Length);
            Array.Copy(body, 0, array, 2, bodyLength);

            return new ReadOnlyMemory<byte>(array);
        }
        public static ReadOnlyMemory<byte> CreateMessageWithRandomBody(int bodyLength)
        {
            var header = new Header(bodyLength, false, false);
            var headerBytes = header.Span.ToArray();
            var body = new byte[bodyLength];
            // 임의의 정보를 갖는 바디를 채워준다.
            for (var i = 0; i < body.Length; ++i)
            {
                body[i] = (byte)r.Next(16);
            }
            var array = new byte[bodyLength + 2];
            Array.Copy(headerBytes, array, headerBytes.Length);
            Array.Copy(body, 0, array, headerBytes.Length, body.Length);

            return new ReadOnlyMemory<byte>(array);
        }
        public static int GetBodyLengthFromMessage(ReadOnlySequence<byte> source)
        {
            var num = (int) source.Length - 2;
            return num;
        }
        public static byte[] GetBodyFromMessage(ReadOnlySequence<byte> source)
        {
            return source.Slice(2, source.Length-2).ToArray();
        }

        public void Dispose()
        {
            this.MadWriter.CompleteWriter();
            this.MadReader.CompleteReader();
            this.pool.Dispose();
        }
    }
}
