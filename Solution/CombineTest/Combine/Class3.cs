using System;
using System.IO;

namespace PipePerformanceTest
{
    class Program
    {
        static void Main()
        {
            var testApp = new PipePerformanceTest();

            testApp.InitializeTargetPipe(PipeBrand.MAD);

            testApp.RunFileCopy();

            if (testApp.CheckFile())
            {
                Console.WriteLine("copy succeed");
            }
            else
            {
                Console.WriteLine("copy failed");
            }

            testApp.DumpResult();
        }
    }
}