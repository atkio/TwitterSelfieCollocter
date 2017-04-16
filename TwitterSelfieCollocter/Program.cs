using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TwitterSelfieCollocter
{
    class Program
    {
        static void Main(string[] args)
        {
            DebugLogger.Instance.W("SearchList > START");
            SelfieTweetFunc.Instance.searchList();
            DebugLogger.Instance.W("SearchList > END");

            Thread.Sleep(180*1000);

            DebugLogger.Instance.W("SelfieFacerecognizer > START");
            SelfieFacerecognizer.Instance.checkALL();
             DebugLogger.Instance.W("SelfieFacerecognizer > END");
        }
    }

    class DebugLogger
    {
        private static volatile DebugLogger instance;
        private static object syncRoot = new Object();

        public static DebugLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            if (File.Exists("DEBUG"))
                                instance = new DebugLogger(true);
                            else
                                instance = new DebugLogger();
                        }
                    }
                }

                return instance;
            }
        }

        private DebugLogger()
        {

        }

        private DebugLogger(bool debugmode)
        {

            W = outputreal;

        }

        public Action<string> W = outputfake;

        static void outputfake(string s)
        {

        }

        static void outputreal(string s)
        {
            Console.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff") + ":" + s);
        }
    }
}
