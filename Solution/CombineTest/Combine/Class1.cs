namespace PipePerformanceTest
{
    using System;
    using System.IO;
    using System.Buffers;
    using MadPipeline;

    class MadPipeTester
    {
        private Madline madline;
        private IMadlineWriter madWriter;
        private IMadlineReader madReader;

        private bool writeSet = false;
        private bool readSet = false;

        private int writeCount = 0;
        private int readCount = 0;

        public MadPipeTester()
        {
            var options = new MadlineOptions();
            this.madline = new Madline(options);
            this.madWriter = this.madline;
            this.madReader = this.madline;
        }

        public Memory<byte> GetWriterMemory(int bytes)
        {
            return this.madWriter.GetMemory(bytes);
        }

        public void Advance(int bytes)
        {
            this.madWriter.Advance(bytes);
            this.madWriter.Flush();
        }

        // 반드시 청크 형식으로 된 것만..
        public void Read(FileStream fileStream, int bytes)
        {
            var resultInt = this.madReader.TryRead(out var result);
            if (resultInt > 0)
            {
                this.ProcessCopy(fileStream, in result, bytes);
                if (resultInt == 1)
                {
                    // 아직 읽을 게 남은 경우이므로 다시 읽기 시도
                    this.Read(fileStream, 0);
                }
            }
            else
            {
                this.madReader.DoRead().Then(
                    readResult =>
                    {
                        this.ProcessCopy(fileStream, in readResult, bytes);
                    });
            }
        }
        public void ProcessCopy(FileStream fileStream, in ReadOnlySequence<byte> result, int bytes)
        {
            var remains = bytes;
            foreach (var segment in result)
            {
                var length = segment.ToArray().Length;
                if (remains > length)
                {
                    remains -= length;
                    fileStream.Write(segment.ToArray(), 0, length);
                    continue;
                }
                else
                {
                    fileStream.Write(segment.ToArray(), 0, remains);
                    break;
                }
            }

            this.madReader.AdvanceTo(result.End);
        }

        public void CompleteWriter()
        {
            //this.madWriter.CompleteWriter();
        }
        public void CompleteReader()
        {
            //this.madReader.CompleteReader();
        }
    }
}
