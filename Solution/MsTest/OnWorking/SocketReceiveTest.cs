//using System.Net;

//namespace Tests
//{
//    using MadPipeline;
//    using Microsoft.VisualStudio.TestTools.UnitTesting;
//    using System.Net.Sockets;

//    [TestClass]
//    public sealed class SocketReceiveTest
//    {
//        private readonly Socket socket;
//        private readonly SocketAsyncEventArgs receiveArgs;
//        private readonly Madline madline;

//        public SocketReceiveTest()
//        {
//            this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
//            this.socket.Bind(new IPEndPoint(IPAddress.Loopback, 12333));
//            this.receiveArgs = new SocketAsyncEventArgs();
//            var options = new MadlineOptions();
//            this.madline = new Madline(options);
//        }

//        public void ReceiveProcess()
//        {
//            var memory = this.madline.GetMemory(1);
//            this.receiveArgs.SetBuffer(memory);
//        }

//        public void ProcessReceive1()
//        {
//            var memory = this.pipeline.GetMemory(1);
//            this.receiveArgs.SetBuffer = memory;
//            var received = this.socket.ReceiveAsync(this.receiveArgs);
//            // 대충 소켓으로부터 받는 코드
//            var signal = this.pipeline.Advance(received);
//            signal.OnComplete(() =>
//            {
//                this.ProcessReceive();
//            })
//        }
//        public void ProcessReceive2()
//        {
//            var memory = this.pipeline.GetMemory(1);
//            this.receiveArgs.SetBuffer = memory;
//            var received = this.socket.ReceiveAsync(this.receiveArgs);
//            // 대충 소켓으로부터 받는 코드
//            if (this.pipeline.TryAdvance(received) == false)
//            {
//                this.pipeline.Advance(received)
//                    .OnComplete(() =>
//                    {
//                        this.ProcessReceive2();
//                    })
//            }
//            else
//            {
//                this.ProcessReceive2();
//            }
//        }
//        public void ProcessSend()
//        {
//            if (this.pipeline.TryRead(var result, size))
//            {
//                this.SendToSocket(result.Buffer);
//            }
//            else
//            {
//                this.pipeline.DoRead(size)
//                    .Then(result =>
//                    {
//                        this.SendToSocket(result.Buffer);
//                    })
//            }
//        }
//        // result 로 pipe가 완료되었는지 캔슬되었는지 등등 검사 후 
//        //스트림 내용 그대로 소켓에 Send 하는 작업
//        public void SendToSocket(Buffer buffer)
//        {
//            ....this.pipeline.AdvanceTo(result.Buffer.End);
//            this.ProcessSend();
//        }
//    }
//}
