using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppServer
{
    class Program
    {
        public static int port;// порт для прослушивания подключений
        public static IPAddress LocalipAddr;
        public static string RemoteipAddr;
        private static TcpListener tcpListener;
        private static Thread listenThread;


        public static string VerApp = $"{typeof(Program).Assembly.GetName().Version.ToString()}";
        public static readonly string path = Environment.CurrentDirectory;
        public static readonly string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /* Возвращает хэндл (указатель) нашего окна IntPtr hWnd*/
        [DllImport("User32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /* Устанавливаем окно по его указателю в нужное место */
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        /* Получаем крайние точки окна */
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        static void Main(string[] args)
        {
            if (!Directory.Exists(pathToLog))
            {
                Directory.CreateDirectory(pathToLog);
            }

            #region key
            if (!LicensyaCheck())
                Close();
            #endregion

            #region read ini
            try
            {
                //Создание объекта, для работы с файлом
                INIManager manager = new INIManager(path + @"\set.ini");
                //Получить значение по ключу name из секции main
                port = int.Parse(manager.GetPrivateString("workstation", "Port"));
                LocalipAddr = IPAddress.Parse(manager.GetPrivateString("workstation", "LocalIp"));
                RemoteipAddr = manager.GetPrivateString("workstation", "RemoteIp");

                Console.WriteLine("Port - " + port);
                Console.Title += $":[LocalIp - {LocalipAddr}]:[Port - {port}]";

                File.AppendAllText(pathToLog + @"\appEx.log", $"{DateTime.Now.ToString("dd.MM.yyyy HH: mm:ss.fff")} start app  - port:" + port + "\n");
                //Записать значение по ключу age в секции main
                // manager.WritePrivateString("main", "age", "21");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ini не прочтен" + ex.Message);
                Logger.WriteLog(ex.Message, 0, "Program Main ini не прочтен");
            }
            #endregion

            //IPAddress ipAddr = IPAddress.Parse("127.0.0.1");

            //tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener = new TcpListener(LocalipAddr, port);
            listenThread = new Thread(new ThreadStart(ListenForClients));
            listenThread.Start();

            /* Получили указатель на нашу консоль */
            var hWnd = FindWindow(null, Console.Title);
            var wndRect = new RECT();
            /* Получили ее размеры */
            GetWindowRect(hWnd, out wndRect);
            var cWidth = wndRect.Right - wndRect.Left;
            var cHeight = wndRect.Bottom - wndRect.Top;
            /* Флаг - означает что при установке позиции окна размер не менялся */
            var SWP_NOSIZE = 0x1;
            /* Окна выше остальных */
            var HWND_TOPMOST = -1;
            var Width = 1366;
            var Height = 768;
            /* Установка окна в нужное место */
            SetWindowPos(hWnd, HWND_TOPMOST, Width / 2 - cWidth / 5, Height / 2 - cHeight / 5, 0, 0, SWP_NOSIZE);
        }
        private static void ListenForClients()
        {
            tcpListener.Start();

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = tcpListener.AcceptTcpClient();

                //потом включить. надо хоть что то получить
                //if (RemoteipAddr == (((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()))
                //{
                //create a thread to handle communication
                //with connected client
                Logic clientObject = new Logic(client); //use dll
                Thread clientThread = new Thread(new ParameterizedThreadStart(clientObject.HandleClientComm));
                clientThread.Start(client);
                // }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool LicensyaCheck()
        {
            try
            {
                string keyfile = @"keyfile.dat";
                string sysSyrialize = @"sysSyrialize.dat";

                string curFile = !File.Exists(keyfile) ? !File.Exists(sysSyrialize) ? null : sysSyrialize : keyfile;

                //if (File.Exists(keyfile))
                //    curFile = keyfile;
                //else if (File.Exists(sysSyrialize))
                //    curFile = sysSyrialize;
                //else
                //    curFile = null;

                if (!string.IsNullOrEmpty(curFile))
                {
                    CryptoClass crypto = new CryptoClass();
                    //if (!crypto.Form_LoadTrue()) Close();

                    string date = crypto.GetDecodeKey(curFile).Substring(crypto.GetDecodeKey(curFile).IndexOf("|") + 1);

                    //Logger.WriteLog(date, 0, "res == 0");

                    if (DateTime.Parse(date).AddDays(1) <= DateTime.Now)
                    {
                        if (File.Exists(curFile))
                            File.Delete(curFile);

                        Logger.WriteLog("start", 0, "key == 0");
                        return false;
                    }
                    else
                    {
                        Console.Title = $"UrineStripReader40_Listener:[{VerApp}.......{date}]";
                        Logger.WriteLog("start", 0, "key == 1");
                        return true;
                    }
                }
                else return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Logger.WriteLog(ex.Message, 0, "key == 0");
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void Close()
        {
            //allow main to run off
            Environment.Exit(0);
        }

    }
}