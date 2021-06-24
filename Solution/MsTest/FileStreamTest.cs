namespace Tests
{
    using MadPipeline;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Buffers;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    [TestClass]
    public sealed class FileStreamTests
    {
        private readonly Madline madline;
        private readonly IMadlineWriter madWriter;
        private readonly IMadlineReader madReader;

        private long TargetBytes { get; set; }

        private const string srcFileName = "../../../../TestFrom.txt";
        private const string destFileName = "../../../../TestTo.txt";
        private FileStream srcFile;
        private FileStream destFile;

        private long writeRemainBytes;
        private long readRemainBytes;

        public FileStreamTests()
        {
            // 기존의 Threshold 가진 madline으로 진행
            var malineOptions = new MadlineOptions(null, 20000000000000, 10000000000000);
            this.madline = new Madline(malineOptions);
            //this.madline = new Madline();

            this.madWriter = this.madline;
            this.madReader = this.madline;

            this.srcFile = new FileStream(srcFileName, FileMode.Open);
            this.destFile = new FileStream(destFileName, FileMode.Create);
            this.TargetBytes = this.srcFile.Length;
            this.readRemainBytes = this.TargetBytes;
            this.writeRemainBytes = this.TargetBytes;
        }


        // 데이터를 읽도록 시도하고, 실패한 경우 예약

        public void StartWrite()
        {
            while (this.writeRemainBytes > 0)
            {
                if (this.madline.State.IsWritingPaused == false)
                {
                    this.WriteProcess();
                }
            }
        }

        public void StartRead()
        {
            do
            {
                if (this.madline.State.IsReadingPaused == false)
                {
                    this.ReadProcess();
                }
            } while (this.readRemainBytes > 0);
        }

        public void WriteProcess()
        {
            if (this.madline.WriteCheck())
            {
                var memory = this.madWriter.GetMemory(4096);
                var advanceBytes = srcFile.Read(memory.Span);
                this.writeRemainBytes -= advanceBytes;
                this.madWriter.Advance(advanceBytes);
                this.madWriter.Flush();
            }
            else
            {
                this.madWriter.WriteSignal().OnCompleted(
                    () =>
                    {
                        this.WriteProcess();
                    });
            }

        }
        // WriteProcess()를 통해 기록된 것을 읽고 구문분석
        public void ReadProcess()
        {
            if (this.madReader.TryRead(out var result, 0))
            {
                this.SendToFile(in result);
            }
            else
            {
                this.madReader.DoRead().Then(
                    readResult =>
                    {
                        this.SendToFile(in readResult);
                    });
            }
        }

        public void SendToFile(in ReadOnlySequence<byte> result)
        {
            var remains = result.Length;
            foreach (var segment in result)
            {
                var length = segment.ToArray().Length;
                this.readRemainBytes -= length;
                if (remains > length)
                {
                    this.destFile.Write(segment.ToArray(), 0, length);
                    continue;
                }
                else
                {
                    this.destFile.Write(segment.ToArray(), 0, (int) remains);
                    break;
                }
            }
            this.madReader.AdvanceTo(result.End);
        }
        
        [TestMethod]
        public void FileStreamTest()
        {
            var writeThread = new Thread(this.StartWrite);
            var readThread = new Thread(this.StartRead);
            readThread.Start();
            writeThread.Start();
            writeThread.Join();
            readThread.Join();
            Assert.AreEqual(this.readRemainBytes, this.writeRemainBytes);

            this.srcFile.Close();
            this.destFile.Close();

        }
    }
}