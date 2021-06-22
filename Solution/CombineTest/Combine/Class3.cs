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

            //testApp.RunFileCopy();
            testApp.RunFileCopy("../../../testTxt.txt");

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