using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Threading;
using System.Globalization;
using System.Data.Odbc;

namespace ConsoleAppServer
{
    public class Logic
    {
        private static object fileLock = new Object();

        public TcpClient client;

        //создание и передача модели в бд
        Model model = new Model();

        public static readonly string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        public static string fileLogOriginMessage = Path.Combine(pathToLog, string.Format("OriginMessage_{0}_{1:dd.MM.yyy}.log", AppDomain.CurrentDomain.FriendlyName, DateTime.Now));
        public static string fileLogParseMessage = Path.Combine(pathToLog, string.Format("ParseMessage_{0}_{1:dd.MM.yyy}.log", AppDomain.CurrentDomain.FriendlyName, DateTime.Now));
        public static string fileLogQuerry = Path.Combine(pathToLog, string.Format("Querry_{0}_{1:dd.MM.yyy}.log", AppDomain.CurrentDomain.FriendlyName, DateTime.Now));
        public int _count, _rows;

        public static List<string> Goods = new List<string> { "LEU", "NIT", "URO", "PRO", "BLO", "KET", "BIL", "GLU" };
        public static List<string> GoodsFrom2 = new List<string> { "pH", "SG" };

        public Logic(TcpClient tcpClient)
        {
            client = tcpClient;
        }

        public void HandleClientComm(object client)
        {
            StringBuilder builder = new StringBuilder();
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;
                //builder = null;

                try
                {
                    do
                    {
                        bytesRead = clientStream.Read(message, 0, message.Length);
                        builder.Append(Encoding.UTF8.GetString(message, 0, bytesRead));
                        //Console.WriteLine("\n");
                    }
                    while (clientStream.DataAvailable);
                }
                catch (SocketException e)
                {
                    //Console.WriteLine(e);
                    Console.WriteLine("Клиент отключился");
                    //a socket error has occured
                    break;
                }
                catch (IOException e)
                {
                    //Console.WriteLine(e);
                    Console.WriteLine("Клиент отключился{IOException}!");
                    //a socket error has occured
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine(e.Message);
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }


                //message has successfully been received
                ASCIIEncoding encoder = new ASCIIEncoding();
                System.Diagnostics.Debug.WriteLine(encoder.GetString(message, 0, bytesRead));

                string _message = builder.ToString();
                Console.WriteLine($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")}-{((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString()}\n{new string('*', 50)}\n{_message}\n{new string('*', 50)}\n");
                Logger.WriteText($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")}-remoteIP:{((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString()}\n{new string('*', 50)}\n");
                Logger.WriteText(_message);


                if (!string.IsNullOrEmpty(_message))
                {
                    LogicLayer.RemoveModelAll();
                    ParseMessage(_message);
                }
            }

            tcpClient.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_message"></param>
        private void ParseMessage(string _message)
        {
            string BarCode = "n/a";

            File.AppendAllText(fileLogOriginMessage, $"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")}\n{new string('*', 50)}\n{_message}\n{new string('*', 50)}\n");

            string[] stringSeparatorsLines = new string[] { "\r"/*, "\n"*/ };
            string[] lines = _message.Split(stringSeparatorsLines, StringSplitOptions.None);
            _count = 0;

            //if (!string.IsNullOrEmpty(message))
            //{
            foreach (string s in lines)
            {
                string[] stringSeparatorsRows = new string[] { "|", ":"/*, "+", "-"*/, " " };
                string[] rows = s.Split(stringSeparatorsRows, StringSplitOptions.RemoveEmptyEntries);
                _rows = 0;

                if (s.Contains("ID"))
                    _count = 0;

                //Console.WriteLine();

                if ((_count == 0) && (rows.Length > 2))
                {
                    if (!string.IsNullOrEmpty(rows[2]))
                    {
                        BarCode = rows[2].Trim(new char[] { '^', 'M', 'R' });
                        //LogicLayer.RemoveModelAll();
                    }
                    else
                        BarCode = "n/a";
                }
                else if ((_count == 0) && (rows.Length < 3))
                {
                    BarCode = "n/a";
                    //LogicLayer.RemoveModelAll();
                }

                if (rows.Length > 3)
                {
                    if (Goods.Contains(rows[1]))
                        //add items to model
                        LogicLayer.AddModel(new Model(code: BarCode, goods: rows[1], vidUnits: rows[2], value01: rows[3]));
                }
                else if ((rows.Length <= 3) && (rows.Length >= 2))
                {
                    if (GoodsFrom2.Contains(rows[1]))
                        LogicLayer.AddModel(new Model(code: BarCode, goods: rows[1], value01: rows[2]));
                }
                //}

                //lock (fileLock)
                //{
                //    using (StreamWriter myStream = new StreamWriter(fileLogParseMessage, true))
                //    {
                //        myStream.Write($"\n");
                //    }
                //}

                _count++;
            }

            foreach (Model spw in LogicLayer.GetModel)
            {
                //Console.ForegroundColor = spw.Code != "n/a" ? ConsoleColor.Green : ConsoleColor.Red;
                //Console.WriteLine($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")} - [BarCode] - {spw.Code}");
                //Console.ForegroundColor = ConsoleColor.White;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")} {spw.Code} \t {spw.Goods} \t {spw.Value01}");
                File.AppendAllText(fileLogParseMessage, $"\n{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")} \t {spw.Code} \t {spw.Goods} \t {spw.Value01}");

                if (spw.Code != "n/a")
                {
                    string query = QueryGetG(spw)?.Query; // получить запрос
                    if (!string.IsNullOrEmpty(query) && query != "Query NULL")
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(query);
                        File.AppendAllText(fileLogQuerry, $"\n{new string('*', 12)}-START:[{spw.Code}]-{new string('*', 12)}\n");
                        File.AppendAllText(fileLogQuerry, $"\n[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")}] \t {spw.Code} \t {spw.Goods} \t {spw.Value01}");
                        File.AppendAllText(fileLogQuerry, $"\n[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")}]{new string('*', 12)}-[{spw.Code}]-{new string('*', 12)}");
                        File.AppendAllText(fileLogQuerry, $"\n{query}");
                        File.AppendAllText(fileLogQuerry, $"\n{new string('*', 12)}-END-{new string('*', 12)}\n");
                        //Console.ForegroundColor = ConsoleColor.White;

                        //if (!string.IsNullOrEmpty(query) && query != "Query NULL") UpdateRowBdThread(spw); //!!! 
                    }
                    else
                        Console.WriteLine(query);
                }
            }
        }

        /// <summary>
        /// получить Запрос
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static QueryModel QueryGetG(Model model)
        {
            QueryModel query = null;

            if ((Goods.Contains(model.Goods)) && (IsFloat(model.Value01)))
            {
                query = (new QueryModel()
                {
                    //Type = model.Type,
                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                            "d.result = '" + GetValue(ParseFloat(model.Value01), model.Goods) + "', \n" +
                            "d.result_text = '" + GetValue(ParseFloat(model.Value01), model.Goods) + "', \n" +
                            "d.hardware_date_updated = current_timestamp, " +
                            "d.hardware_info = ('UrineStripReader40') \n" +
                            "where ID = (select R.ID  \n" +
                                         "from JOR_CHECKS_DT D  \n" +
                                         "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
                                         "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
                                         "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
                                         "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
                                         "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
                                         "and (R.CODE_NAME = cast(('" + GetGoods(model.Goods) + "') as MIDDLE_NAME))  \n" +
                                         "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                });

                model.Query = query.Query;
            }
            else if ((Goods.Contains(model.Goods)) && (!IsFloat(model.Value01)))
            {
                query = (new QueryModel()
                {
                    //Type = model.Type,
                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                           // "d.result = '" + GetValueString(model.Value01) + "', \n" +
                            "d.result_text = '" + GetValueString(model.Value01) + "', \n" +
                            "d.hardware_date_updated = current_timestamp, " +
                            "d.hardware_info = ('UrineStripReader40') \n" +
                            "where ID = (select R.ID  \n" +
                                         "from JOR_CHECKS_DT D  \n" +
                                         "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
                                         "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
                                         "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
                                         "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
                                         "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
                                         "and (R.CODE_NAME = cast(('" + GetGoods(model.Goods) + "') as MIDDLE_NAME))  \n" +
                                         "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                });

                model.Query = query.Query;
            }
            else if ((GoodsFrom2.Contains(model.Goods)) && (IsFloat(model.Value01)))
            {
                query = (new QueryModel()
                {
                    //Type = model.Type,
                    Query = "update jor_results_dt d set d.IS_OUT_OF_NORM = 0, \n" +
                            "d.result = '" + GetValue(ParseFloat(model.Value01), model.Goods) + "', \n" +
                            "d.result_text = '" + GetValue(ParseFloat(model.Value01), model.Goods) + "', \n" +
                            "d.hardware_date_updated = current_timestamp, " +
                            "d.hardware_info = ('UrineStripReader40') \n" +
                            "where ID = (select R.ID  \n" +
                                         "from JOR_CHECKS_DT D  \n" +
                                         "inner join JOR_CHECKS C on C.ID = D.HD_ID  \n" +
                                         "inner join JOR_RESULTS_DT R on R.HD_ID = D.ID  \n" +
                                         "left join DIC_NO_OPPORT_TO_RES N on N.ID = D.DIC_NO_OPPORT_TO_RES_ID  \n" +
                                         "where (R.HD_ID = D.ID) and(D.DATE_DONE is null) and(D.IS_REFUSE = 0)  \n" +
                                         "and (D.BULB_NUM_CODE = cast('" + model.Code + "' as NAME))  \n" +
                                         "and (R.CODE_NAME = cast(('" + GetGoods(model.Goods) + "') as MIDDLE_NAME))  \n" +
                                         "and((D.DIC_NO_OPPORT_TO_RES_ID is null) or(N.IS_IN_WORK = 1)))"
                });

                model.Query = query.Query;
            }
            else
            {
                query = (new QueryModel()
                { /*Type = "Query NULL",*/ Query = "Query NULL" });
            }

            return query;
        }

        private static string GetValueString(string value)
        {
            return value == "neg" ? " не обнаружено" : " обнаружено";
        }

        /// <summary>
        /// округление определенных товаров
        /// ver from 20200727 from nadejda
        /// </summary>
        /// <param name="v"></param>
        /// <param name="goods"></param>
        /// <returns></returns>
        private static double GetValue(float v, string goods)
        {
            if ((Goods.Contains(goods)) || (GoodsFrom2.Contains(goods)))
            {
                if (goods == "LYM%") return Math.Round(v);
                //if (goods == "NE%") return Math.Round(v);
                //if (goods == "MO%") return Math.Round(v);
                //if (goods == "EO%") return Math.Round(v);
                //if (goods == "BA%") return Math.Round(v);
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                return Double.Parse(v.ToString());
            }
            else return 0;
        }

        /// <summary>
        /// замена короткого кода
        /// </summary>
        /// <param name="goods"></param>
        /// <returns></returns>
        private static string GetGoods(string goods)
        {
            if ((Goods.Contains(goods)) || (GoodsFrom2.Contains(goods)))
            {
                //if (goods == "LY%") goods = "LYM%";
                //if (goods == "NE%") goods = "GRA%";
                //if (goods == "MO%") goods = "MON%";
                //if (goods == "EO%") goods = "EOZ%";
                //if (goods == "BA%") goods = "BAZ%";
                //if (goods == "RDWc") goods = "RDW";
                //if (goods == "PDWc") goods = "PDW";

                //if (goods == "GRAN#") goods = "GRA%";
                if (goods == "RDW-SD") goods = "RDW";
                return goods;
            }
            else return "n/a";
        }

        /// <summary>
        /// получить флоат с точкой
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static float ParseFloat(string val)
        {
            return float.Parse(val, NumberStyles.Any, new CultureInfo("en-US"));
        }

        /// <summary>
        /// проверка на флоат
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static bool IsFloat(string val)
        {
            return float.TryParse(val, NumberStyles.Any, new CultureInfo("en-US"), out _);
        }

        /// <summary>
        ///  метод запускающий метод изменения данных в бд
        /// </summary>
        /// <param name="query"></param>
        static void UpdateRowBdThread(Model query)
        {
            //Console.WriteLine("Начало метода myThread");
            Thread myThread = new Thread(new ParameterizedThreadStart(UpdateRowBdThread));
            myThread.Start(query);
            // Console.WriteLine("Конец метода myThread");
        }

        /// <summary>
        /// внесение данных в бд
        /// </summary>
        /// <param name="query"></param>
        private static void UpdateRowBdThread(object queryObj)
        {
            Model query = (Model)queryObj;
            string queryBd = QueryGetG(query)?.Query;

            if (!string.IsNullOrEmpty(queryBd))
            {
                OdbcCommand command = new OdbcCommand(queryBd);
                using (OdbcConnection connection = new OdbcConnection(GetValueIni("Connection", "dbname")))
                {
                    command.Connection = connection;
                    try
                    {
                        connection.Open();
                        int res = command.ExecuteNonQuery();

                        if (res == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"\n{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")} Update count: {res}, BarCode: {query.Code}, Goods: {query.Goods}, VidUnits: {query.VidUnits}, Value1: {query.Value01}");
                            Logger.WriteLog($"\n{queryBd}", 0, "res == 0");
                            Logger.WriteLog($"Update count: {res}, BarCode: {query.Code}, Goods: {query.Goods}, VidUnits: {query.VidUnits}, Value1: {query.Value01}", 0, "res == 0");

                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"\n{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")} Update count: {res}, BarCode: {query.Code}, Goods: {query.Goods}, TypeGoods: {query.VidUnits}, Value1: {query.Value01}");
                            Logger.WriteLog($"\n{queryBd}", 1, "res == 1");
                            Logger.WriteLog($"Update count: {res}, BarCode: {query.Code}, Goods: {query.Goods}, VidUnits: {query.VidUnits}, Value1: {query.Value01}", 1, "res == 1");
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"UpdateRow: update fail: {ex.Message}\n", "ex");
                        //Log.WriteLog(ex.Message);
                        Logger.WriteLog($"\n{ex.Message}", 0, "res == 0");
                    }
                    // The connection is automatically closed at 
                    // the end of the Using block.
                }
            }
        }

        /// <summary>
        /// Получить значение по секции и ключу
        /// </summary>
        /// <param name="Section"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        private static string GetValueIni(string Section, string Key)
        {
            try
            {
                var pathIni = new Uri(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).LocalPath;
                //Создание объекта, для работы с файлом
                INIManager manager = new INIManager(pathIni + @"\set.ini");
                //Получить значение по секции и ключу
                return manager.GetPrivateString(Section, Key);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(ex.Message, 0, "GetValueIni");
                return "";
            }
        }

    }
}
