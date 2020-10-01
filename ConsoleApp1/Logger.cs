using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppServer
{
    public class Logger
    {
        private static BlockingCollection<string> _blockingCollection;
        private static string _filename = $"Log\\Logger-{DateTime.Now:dd.MM.yyy}.txt";
        private static Task _task;

        static Logger()
        {
            if (!Directory.Exists("Log"))
            {
                Directory.CreateDirectory("Log");
            }

            _blockingCollection = new BlockingCollection<string>();

            _task = Task.Factory.StartNew(() =>
            {
                using (var streamWriter = new StreamWriter(_filename, true, Encoding.UTF8))
                {
                    streamWriter.AutoFlush = true;

                    foreach (var s in _blockingCollection.GetConsumingEnumerable())
                        streamWriter.WriteLine(s);
                }
            },
            TaskCreationOptions.LongRunning);
        }

        public static void WriteLog(string action, int errorCode, string errorDiscription)
        {
            _blockingCollection.Add($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")} action: {action}, code: {errorCode.ToString()}, description: { errorDiscription} ");
        }

        public static void WriteText(string errorDiscription)
        {
            _blockingCollection.Add($"\n{ errorDiscription} ");
        }

        public static void Flush()
        {
            _blockingCollection.CompleteAdding();
            _task.Wait();
        }
    }
}
