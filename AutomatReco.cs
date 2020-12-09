using CarModelSdk_NetCore;
using System;
using System.IO;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Configuration;
using Sql_NetFramework.SqlConn;
using System.Collections.Generic;
using Sql_NetCore;
using System.Text;
using System.Linq;
using Emgu.CV;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace Sql_NetFramework
{
    class AutomatReco
    {
        private static string _myName = "";

        public static Dictionary<string, string> CarTypes = new Dictionary<string, string>()
        {
            {"car", "Легковой автомобиль" },
            {"truck", "Грузовой автомобиль" },
            {"bus lite", "Микроавтобус" },
            {"bus", "Автобус" },
            {"bus hard", "Автобус" },
            {"motorcycle", "Мотоцикл" },
            {"front", "вид спереди" },
            {"back", "вид сзади" },
        };

        public static MyParams myParams;

        static SqlConnection conn;

        static int cntPrep = 0;
        static int cntReco = 0;
        static int cntSave = 0;
        static int cntLock = 0;

        private CarModelSdk sdk;

        public static void Log(string text, int level=2)
        {
            Int32.TryParse(myParams.Value("LogToDisk"), out int logToDisk);
            Int32.TryParse(myParams.Value("LogLevel"), out int logLevel);

            if (level <= logLevel)
            {
                string str = (string.IsNullOrEmpty(_myName) ? "" : (_myName + " - ")) 
                    + (text.Length > 1 ? (DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + text) : "");

                if (logToDisk > 0)
                {
                    bool isOk = false;

                    while (!isOk)
                    {
                        TextWriter writer;
                        if (File.Exists("LOG.txt"))
                            writer = File.AppendText("LOG.txt");
                        else
                            writer = File.CreateText("LOG.txt");

                        try
                        {
                            writer.WriteLine(str);

                            isOk = true;
                        }
                        catch (Exception e)
                        {
                            isOk = false;
                            Console.Write("!");
                        }
                        finally
                        {
                            writer.Close();
                        }
                    }
                }
                Console.WriteLine(str);
            }
        }

        public AutomatReco(string myName, MyParams mpr)
        {
            _myName = myName;
            myParams = mpr;

            //Log(" ", 1);
            //Log("START PROGRAM ", 0);

            RestoreConnection();

            RestoreAutomatReco();
        }

        public static void  RestoreConnection()
        {
            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            conn = DBUtils.GetDBConnection();
            try
            {
                conn.Open();

                if (conn.State == System.Data.ConnectionState.Open)
                {
                    Log(new string("Connection OK"), 1);
                }
                else
                    Log(new string("Connection is not established!"), 0);
            }
            catch (Exception e)
            {
                if (markExceptions>0)  Console.WriteLine(new string("Connection Error: " + e));
                if (markExceptions>1)  Console.WriteLine(" -> -> " + e.StackTrace);
            }
            finally
            {
                //Console.WriteLine(" ");
            }
        }

        public void RestoreAutomatReco()
        {
            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            {
                Int32.TryParse(myParams.Value("FilesToDisk"), out int filesToDisk);
                string dirIn = (filesToDisk > 0) ? myParams.Value("DirIn") : "";
                string dirOut = (filesToDisk > 0) ? myParams.Value("DirOut") : "";

                Int32.TryParse(myParams.Value("IDsProcSpanLim"), out int idsProcSpanLim);
                Int32.TryParse(myParams.Value("RecognizeMode"), out int mode);
                Int32.TryParse(myParams.Value("RecognizeBorder"), out int recognizeBorder);
                Int32.TryParse(myParams.Value("Latitude_deg"), out int latitude_deg);
                Int32.TryParse(myParams.Value("OffsetDayTime_min"), out int offsetDayTime_min);
                Int32.TryParse(myParams.Value("OffsetTimeZone_min"), out int offsetTimeZone_min);

                Int32.TryParse(myParams.Value("OutName_FileName"), out int outName_FileName);
                Int32.TryParse(myParams.Value("OutName_DateTime"), out int outName_DateTime);
                Int32.TryParse(myParams.Value("OutName_Name"), out int outName_Name);
                Int32.TryParse(myParams.Value("OutName_Mark"), out int outName_Mark);
                Int32.TryParse(myParams.Value("OutName_Modl"), out int outName_Modl);
                Int32.TryParse(myParams.Value("OutName_Genr"), out int outName_Genr);
                Int32.TryParse(myParams.Value("OutName_MarkProp"), out int outName_MarkProp);
                Int32.TryParse(myParams.Value("OutName_Type"), out int outName_Type);
                Int32.TryParse(myParams.Value("OutName_Side"), out int outName_Side);
                Int32.TryParse(myParams.Value("OutName_TypeProp"), out int outName_TypeProp);
                Int32.TryParse(myParams.Value("OutName_FileNam2"), out int outName_FileNam2);

                Int32.TryParse(myParams.Value("SaveOps_MarkRect"), out int saveOps_MarkRect);
                Int32.TryParse(myParams.Value("SaveOps_TypeRect"), out int saveOps_TypeRect);
                Int32.TryParse(myParams.Value("SaveOps_SaveRecs"), out int saveOps_SaveRecs);
                Int32.TryParse(myParams.Value("SaveOps_SaveUnrecs"), out int saveOps_SaveUnrecs);
                Int32.TryParse(myParams.Value("SaveOps_RemoveOld"), out int saveOps_RemoveOld);
                Int32.TryParse(myParams.Value("SaveOps_RenameNew"), out int saveOps_RenameNew);

                try
                {
                    int res0 = CarModelSdk.SetRecoCountLim(
                        idsProcSpanLim
                        );

                    int res1 = CarModelSdk.SetRecognizeMode(
                        (RecognizeMode) mode
                        );

                    int res91 = CarModelSdk.SetRecognizeOptions(
                        recognizeBorder,
                        latitude_deg,
                        offsetDayTime_min,
                        offsetTimeZone_min
                        );

                    sdk = new CarModelSdk();

                    int testInt = CarModelSdk.Test(12345);
                    Log(new string("testInt 12345 = " + testInt), 1);
                    Log(" ", 2);

                    int res6 = CarModelSdk.GetRecoCarMarkAddress(out long addrNum1);
                    Log("RecoCarMarkAddress = " + addrNum1, 1);
                    int res7 = CarModelSdk.GetRecoCarTypeAddress(out long addrNum2);
                    Log("RecoCarTypeAddress = " + addrNum2, 1);

                    int res13 = CarModelSdk.SetFileDirectoryIn(dirIn);
                    int res14 = CarModelSdk.SetFileDirectoryOut(dirOut);

                    int res93 = CarModelSdk.SetOutFileNamesFormat(
                        outName_FileName,
                        outName_DateTime,
                        outName_Name,
                        outName_Mark,
                        outName_Modl,
                        outName_Genr,
                        outName_MarkProp,
                        outName_Type,
                        outName_Side,
                        outName_TypeProp,
                        outName_FileNam2);

                    int res94 = CarModelSdk.SetSaveOptions(
                        filesToDisk,
                        saveOps_MarkRect,
                        saveOps_TypeRect,
                        saveOps_SaveRecs,
                        saveOps_SaveUnrecs,
                        saveOps_RemoveOld,
                        saveOps_RenameNew);
                }
                catch (Exception e)
                {
                    if (markExceptions > 0) Console.WriteLine(new string("Restore AutomatReco Error: " + e));
                    if (markExceptions > 1) Console.WriteLine(" -> -> " + e.StackTrace);
                }
                finally
                {
                    //Console.WriteLine(" -> -> finally ");
                }
            }
        }

        private static string MyTrim(byte[] arr, int from, int size)
        {
            // Console.Write(" MyTrim() -> ");
            string res = string.Empty;
            bool start = false;

            if (arr != null && arr.Length > 0)
            {
                int cnt = 0;
                for (int i = from; i < from + size; i++)
                {
                    // Console.Write(" " + arr[i].ToString());

                    if (arr[i] == (byte)'\'')
                    {
                        start = true;
                        res += '\"';
                        cnt++;
                    }
                    else if (arr[i] >= (byte)32 && arr[i] <= (byte)255)
                    {
                        start = true;
                        res += (char)(arr[i]);
                        cnt++;
                    }
                    else
                    {
                        if (start && i > 5)
                            break;
                    }
                }
            }
            //Console.WriteLine();
            //Console.WriteLine(res);

            return res;
        }


        // НАГРУЗОЧНОЕ ТЕСТИРОВАНИЕ ЧЕРЕЗ ДИСК
        public void RemoveDirectories()
        {
            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            try
            {
                Int32.TryParse(myParams.Value("FilesToDisk"), out int filesToDisk);

                if (filesToDisk <= 0)
                    return;

                string dirIn = (filesToDisk > 0) ? myParams.Value("DirIn") : "";
                string dirOut = (filesToDisk > 0) ? myParams.Value("DirOut") : "";
                Int32.TryParse(myParams.Value("DirIn_recreate"), out int dirIn_recreate);
                Int32.TryParse(myParams.Value("DirOut_recreate"), out int dirOut_recreate);

                //Console.WriteLine("RemoveDirectories() ");

                if (filesToDisk > 0
                    && dirIn_recreate > 0)
                {
                    if (System.IO.Directory.Exists(dirIn))
                        Directory.Delete(dirIn, true);
                }

                if (filesToDisk > 0
                    && dirOut_recreate > 0)
                {
                    if (System.IO.Directory.Exists(dirOut))
                        Directory.Delete(dirOut, true);
                }
            }
            catch (Exception e)
            {
                if (markExceptions > 0)  Log(new string("Remove directories error: " + e));
            }
            finally
            {
                //Console.WriteLine("finally");
            }
        }

        public static void CreateDirectories()
        {
            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            try
            {
                Int32.TryParse(myParams.Value("FilesToDisk"), out int filesToDisk);

                if (filesToDisk <= 0)
                    return;

                string dirIn = (filesToDisk > 0) ? myParams.Value("DirIn") : "";
                string dirOut = (filesToDisk > 0) ? myParams.Value("DirOut") : "";
                Int32.TryParse(myParams.Value("DirIn_recreate"), out int dirIn_recreate);
                Int32.TryParse(myParams.Value("DirOut_recreate"), out int dirOut_recreate);

                //Console.WriteLine("CreateDirectories() ");


                if (filesToDisk > 0
                    && dirIn != string.Empty)
                {
                    if (!System.IO.Directory.Exists(dirIn))
                    {
                        Directory.CreateDirectory(dirIn);
                    }
                }

                if (filesToDisk > 0
                    && dirOut != string.Empty)
                {
                    if (!System.IO.Directory.Exists(dirOut))
                    {
                        Directory.CreateDirectory(dirOut);
                    }
                }
            }
            catch (Exception e)
            {
                if (markExceptions > 0)  Log(new string("Create directories error: " + e));
            }
            finally
            {
                //Console.WriteLine("finally");
            }
        }

        private void Wait (int msec)
        {
            
            {
                Int32.TryParse(myParams.Value("UsePauses"), out int useStepWaits);

                if (useStepWaits > 0)
                {
                    Log("PAUSE  " + msec + "  мс. ", 1);
                    System.Threading.Thread.Sleep(msec);
                }
            }
        }



        public static string readParamFromDB(string valName, string valDefault)
        {
            Log("saveParamToDB: " + valName + " (default " + valDefault + ")", 1);

            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            if (conn == null)
                RestoreConnection();

            string valValue = valDefault;   // "01.01.2020 00:00:01";

            try
            {
                if (conn == null)
                    RestoreConnection();

                string sql = myParams.Value("PARAMS_SqlRead");
                sql = sql
                    .Replace("##Name##", valName);

                // Создать объект Command
                SqlCommand cmd = new SqlCommand
                {
                    Connection = conn,
                    CommandText = sql,
                    CommandTimeout = 1000
                };

                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            valValue = reader.GetString(0);
                        }
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                if (markExceptions > 0)  Log(new string("Reading '" + valName + "' from [dbo].[TSInfo_RecoStatus] error: " + e));
            }
            finally
            {
                //Console.WriteLine("finally");
                // Закрыть соединение.
            }

            Log("Read param from DB: " + valName + " >> '" + valValue + "', from table [dbo].[TSInfo_RecoStatus]" + " - OK", 2);
            return valValue;
        }

        public static int saveParamToDB(string valName, string valValue)
        {
            Log("saveParamToDB: " + valName + " = " + valValue, 1);

            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            if (conn == null)
                RestoreConnection();

            string sql = myParams.Value("PARAMS_SqlSave");
            sql = sql
                .Replace("##Name##", valName)
                .Replace("##Value##", valValue);

            int resIns = -1;

            Log("sql: " + sql, 2);

            try
            {
                if (conn == null)
                    RestoreConnection();

                SqlCommand command = new SqlCommand(null, conn);
                command.CommandText = sql;

                command.Prepare();
                resIns = command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                if (markExceptions > 0)  Log(new string("Saving '" + valName + "' = '" + valValue + "' to [dbo].[TSInfo_RecoStatus] error: " + e));
                if (markExceptions > 1)  Log(" -> -> " + e.StackTrace);
            }
            finally
            {
                //Console.WriteLine(" -> -> finally "); 
            }

            Log("Save param to DB: " + valName + " << '" + valValue + "', in table [dbo].[TSInfo_RecoStatus]" + " - OK", 1);

            return resIns;
        }

        private int updateParamInDB(string valName, string valValue)
        {
            Log("updateParamInDB: " + valName + " = " + valValue, 1);

            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            if (conn == null)
                RestoreConnection();

            string sql = myParams.Value("PARAMS_SqlUpdate");
            sql = sql
                .Replace("##Name##", valName)
                .Replace("##Value##", valValue);

            int resIns = -1;

            try
            {
                if (conn == null)
                    RestoreConnection();

                SqlCommand command = new SqlCommand(null, conn);
                command.CommandText = sql;

                command.Prepare();
                resIns = command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                if (markExceptions > 0)  Log(new string("Saving '" + valName + "' = '" + valValue + "' to [dbo].[TSInfo_RecoStatus] error: " + e));
                if (markExceptions > 1)  Log(" -> -> " + e.StackTrace);
            }
            finally
            {
                //Console.WriteLine(" -> -> finally "); 
            }

            Log("Save param to DB: " + valName + " << '" + valValue + "'", 2);

            return resIns;
        }

        private int deleteParamFromDB(string valName)
        {
            Log("deleteParamFromDB: " + valName, 1);

            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            if (conn == null)
                RestoreConnection();

            string sql = myParams.Value("PARAMS_SqlDelete");
            sql = sql
                .Replace("##Name##", valName);

            int resIns = -1;

            Log("sql: " + sql, 1);
            //Console.ReadLine()/*;*/

            try
            {
                if (conn == null)
                    RestoreConnection();

                SqlCommand command = new SqlCommand(null, conn);
                command.CommandText = sql;

                command.Prepare();
                resIns = command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                if (markExceptions > 0)  Log(new string("Deleting param '" + valName + "' from [dbo].[TSInfo_RecoStatus] error: " + e));
                if (markExceptions > 1)  Log(" -> -> " + e.StackTrace);
            }
            finally
            {
                //Console.WriteLine(" -> -> finally "); 
            }

            //Log("Delete param from DB: " + valName + " - OK", 1);
            //Console.ReadLine();

            return resIns;
        }


        private int updateLastDate(string newLastDate)
        {
            string curLastDate = readParamFromDB("LastDate", "");

            if (newLastDate != curLastDate)
                saveParamToDB("LastDate " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"), curLastDate);

            return saveParamToDB("LastDate", newLastDate);
        }


        private int incrementParamInDB(string valName, int incVal)
        {
            Log("incrementParamInDB: " + valName + " += " + incVal, 1);

            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            if (conn == null)
                RestoreConnection();

            string sql = myParams.Value("PARAMS_SqlIncrement");
            sql = sql
                .Replace("##Name##", valName)
                .Replace("##IncVal##", incVal.ToString());

            int resIns = -1;

            Log("sql: " + sql, 1);
            //Console.ReadLine()/*;*/

            try
            {
                if (conn == null)
                    RestoreConnection();

                SqlCommand command = new SqlCommand(null, conn);
                command.CommandText = sql;

                command.Prepare();
                resIns = command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                if (markExceptions > 0) Log(new string("Incremint param '" + valName + "' in [dbo].[TSInfo_RecoStatus] error: " + e));
                if (markExceptions > 1) Log(" -> -> " + e.StackTrace);
            }
            finally
            {
                //Console.WriteLine(" -> -> finally "); 
            }

            //Log("Delete param from DB: " + valName + " - OK", 1);
            //Console.ReadLine();

            return resIns;
        }



        private DateTime StringToDate000(string str)
        {
            int yer = Int32.Parse(str.Substring(0, 4));
            int mon = Int32.Parse(str.Substring(5, 2));
            int day = Int32.Parse(str.Substring(8, 2));

            return new DateTime(yer, mon, day, 0, 0, 0);
        }

        private DateTime StringToDateTime(string str)
        {
            int yer = Int32.Parse(str.Substring(0, 4));
            int mon = Int32.Parse(str.Substring(5, 2));
            int day = Int32.Parse(str.Substring(8, 2));

            int hor = Int32.Parse(str.Substring(11, 2));
            int min = Int32.Parse(str.Substring(14, 2));
            int sec = Int32.Parse(str.Substring(17, 2));

            return new DateTime(yer, mon, day, hor, min, sec);
        }

        private string DateTimeToString(DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private DateTime getNextDateTime(string curDateString, out DateTime strtDT, out DateTime stopDT, out bool isToday)
        {
            Int32.TryParse(myParams.Value("UseDarkFilter"), out int useDarkFilter);
            Int32.TryParse(myParams.Value("Latitude_deg"), out int latitude_deg);
            Int32.TryParse(myParams.Value("OffsetDayTime_min"), out int offsetDayTime_min);
            Int32.TryParse(myParams.Value("OffsetTimeZone_min"), out int offsetTimeZone_min);

            Int32.TryParse(myParams.Value("ForseToday"), out int forseToday);

            var todayDT = DateTime.Today;
            strtDT = StringToDate000(curDateString);
            isToday = strtDT >= todayDT;

            // если уже начался новый световой день - форсим переход на текущую дату
            if ( (strtDT > todayDT)
                || (forseToday > 0 && strtDT < todayDT) )
            {
                // получим время рассвета для текущей даты
                int today_yer = todayDT.Year;
                int today_mon = todayDT.Month;
                int today_day = todayDT.Day;
                int todaySunrize_mins = CarModelSdk.CalcSunrize_mins(todayDT.Month, todayDT.Day);
                int todaySunrize_hr = todaySunrize_mins / 60;
                int todaySunrize_mn = todaySunrize_mins % 60;
                int todaySunrize_sc = 0;
                var todaySunrizeDT = new DateTime ( today_yer, 
                                                    today_mon, 
                                                    today_day, 
                                                    todaySunrize_hr,
                                                    todaySunrize_mn, 
                                                    todaySunrize_sc)
                                    .AddMinutes(15);

                //Console.WriteLine("DateTime.Now = " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                //Console.WriteLine("todaySunrizeDT = " + todaySunrizeDT.ToString("yyyy-MM-dd HH:mm:ss"));
                // форсим переход на текущую дату
                if (DateTime.Now >= todaySunrizeDT)
                {
                    strtDT = todayDT;

                    int res = updateLastDate(todayDT.ToString("yyyy-MM-dd"));
                    //int res = updateParamInDB("LastDate", todayDT.ToString("yyyy-MM-dd"));
                    int res1 = updateParamInDB("LastID", "0");
                    Log("FORSE TODAY:  todayDT = " + todayDT.ToString("yyyy-MM-dd HH:mm:ss"), 1);
                }
            }

            int yer = strtDT.Year;
            int mon = strtDT.Month;
            int day = strtDT.Day;

            stopDT = strtDT.AddDays(1);

            // если активен фильтр по тёмному времени суток - поправим границы диапазона
            if (useDarkFilter > 0)
            {
                // получим время рассвета
                int sunrize_mins = CarModelSdk.CalcSunrize_mins(mon, day);
                int sunrize_hr = sunrize_mins / 60;
                int sunrize_mn = sunrize_mins % 60;
                int sunrize_sc = 0;

                strtDT = new DateTime(strtDT.Year, strtDT.Month, strtDT.Day, sunrize_hr, sunrize_mn, sunrize_sc);

                // получим время заката
                int sunset_mins = CarModelSdk.CalcSunset_mins(mon, day);
                if (sunset_mins < 1440)
                {
                    int sunset_hr = sunset_mins / 60;
                    int sunset_mn = sunset_mins % 60;
                    int sunset_sc = 0;
                    stopDT = new DateTime(strtDT.Year, strtDT.Month, strtDT.Day, sunset_hr, sunset_mn, sunset_sc);
                }
                else
                {
                    stopDT = new DateTime(stopDT.Year, stopDT.Month, stopDT.Day, 0, 0, 0);
                }
                // Console.WriteLine("stopDT = " + stopDT.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            int resFrom = updateParamInDB("DateTimeFrom", strtDT.ToString("yyyy-MM-dd HH:mm:ss"));
            int resTo = updateParamInDB("DateTimeTo", stopDT.ToString("yyyy-MM-dd HH:mm:ss"));

            return stopDT;
        }

        public int RecoPhotosByBath()
        {
            TimeSpan ts_Prepare = new TimeSpan();
            TimeSpan ts_Process = new TimeSpan();
            TimeSpan ts_Pauses = new TimeSpan();

            Int32.TryParse(myParams.Value("IDsBathSize"), out int idsBathSize);
            Int32.TryParse(myParams.Value("IDsBathPause_ms"), out int idsBathPause_ms);
            Int32.TryParse(myParams.Value("NoFilesPause_ms"), out int noFilesPause_ms);
            Int32.TryParse(myParams.Value("RecognizeBorder_prs"), out int recognizeBorder);

            Stopwatch stopWatch_prepare = new Stopwatch();
            stopWatch_prepare.Start(); 
            int cnt = PreparePhotoBath(
                out List<PhotoStruct> photoList, 
                out DateTime strtDT, out DateTime stopDT, 
                out bool isToday);

            stopWatch_prepare.Stop();
            ts_Prepare = stopWatch_prepare.Elapsed;

            DateTime StartDT = DateTime.Now;
            DateTime StatoDT = DateTime.Now.AddHours(-4);
            string strDate = StatoDT.ToString("yyyy-MM-dd") + ((isToday && StatoDT.Hour >= 15) ? " вечер" : "");

            //////////////////////
            // Подготовка полей //
            //////////////////////

            Int32.TryParse(AutomatReco.readParamFromDB("CountPrep " + strDate, "0"), out int countPrep);
            Int32.TryParse(AutomatReco.readParamFromDB("CountReco " + strDate, "0"), out int countReco);
            Int32.TryParse(AutomatReco.readParamFromDB("CountSave " + strDate, "0"), out int countSave);
            //Int32.TryParse(AutomatReco.readParamFromDB("CountLock " + strDate, "0"), out int countLock);

            if (countPrep <= 0)
                AutomatReco.saveParamToDB("CountPrep " + strDate, "0");
            if (countReco <= 0)
                AutomatReco.saveParamToDB("CountReco " + strDate, "0");
            if (countSave <= 0)
                AutomatReco.saveParamToDB("CountSave " + strDate, "0");
            //if (countLock <= 0)
            //    AutomatReco.saveParamToDB("CountLock " + strDate, "0");

            //////////////////////
            // Обработка данных //
            //////////////////////

            // проверка на пустой список
            if (photoList.Count > 0)
            {
                // обработка батча
                ProcessPhotoBath(photoList,
                    ref ts_Process, ref ts_Pauses);

                // пауза
                if (idsBathPause_ms > 0)
                {
                    Stopwatch sw_waits = new Stopwatch();
                    sw_waits.Start();
                        Wait(idsBathPause_ms);
                    sw_waits.Stop();
                    ts_Pauses += sw_waits.Elapsed;
                }
            }
            else
            {
                return 0;
            }

            Log("" + cntPrep.ToString().PadLeft(5)
              + " - " + cntReco.ToString().PadLeft(5)
              + " - " + (DateTime.Now - StartDT).Duration().TotalSeconds.ToString("0.00").PadLeft(8)
              + " - " + ((DateTime.Now - StartDT).Duration().TotalSeconds/60).ToString("0.00").PadLeft(6)
              + " - " + ts_Prepare.TotalSeconds.ToString("0.00").PadLeft(6)
              + " - " + ts_Process.TotalSeconds.ToString("0.00").PadLeft(7)
              + " - " + ts_Pauses.TotalSeconds.ToString("0.00").PadLeft(7)
              , 0);

            /////////////////////
            // Инкремент полей //
            /////////////////////

            incrementParamInDB("CountPrep " + strDate, cntPrep);
            incrementParamInDB("CountReco " + strDate, cntReco);
            incrementParamInDB("CountSave " + strDate, cntSave);
            //incrementParamInDB("CountLock " + strDate, cntLock);

            cntPrep = 0;
            cntReco = 0;
            cntSave = 0;
            cntLock = 0;

            return photoList.Count;
        }
        
        
        private int PreparePhotoBath(out List<PhotoStruct> photoListImg, 
            out DateTime strtDT_out, out DateTime stopDT_out, out bool isToday_out)
        {
            // Console.WriteLine("PreparePhotoBath()");
            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            Int32.TryParse(myParams.Value("UseMultyProc"), out int useMultyProc);
            Int32.TryParse(myParams.Value("IDsBathSize"), out int idsBathSize);
            Int32.TryParse(myParams.Value("IDsBathSize_today"), out int idsBathSize_today);
            Int32.TryParse(myParams.Value("TodayTimeLag_ms"), out int todayTimeLag_ms);
            string sqlReadIDs = myParams.Value("SqlReadIDs");

            int cnt = 0;
            List<PhotoStruct> photoList = new List<PhotoStruct>();
                        
            DateTime strtDT = DateTime.Today;
            DateTime stopDT = DateTime.Today;
            bool isToday = true;
            string idLast = "0";

            {
                string dateTime_From =    readParamFromDB(_myName + "_DateTime_From", "-");
                string dateTime_To   =    readParamFromDB(_myName + "_DateTime_To",   "-");
                long idFrom = Int64.Parse(readParamFromDB(_myName + "_ID_From", "-1"));
                long idTo   = Int64.Parse(readParamFromDB(_myName + "_ID_To",   "-1"));

                if (dateTime_From != "-"
                    && dateTime_To != "-"
                    && idFrom >= 0
                    && idTo >= 0)
                {
                    // диапазон выделен - выберем пачку IDs
                    strtDT = StringToDateTime(dateTime_From);
                    stopDT = StringToDateTime(dateTime_To);
                    idLast = (idFrom -1).ToString() + " and info.id <= " + idTo.ToString();
                }
                else
                {
                    photoListImg = new List<PhotoStruct>();
                    strtDT_out = strtDT;
                    stopDT_out = stopDT;
                    isToday_out = isToday;
                    return 0;
                }
            }

            isToday = strtDT >= DateTime.Today;

            if (conn == null)
                RestoreConnection();

            try
            {
                Log("[[ PREPARE ... ]]   time interval   " + DateTimeToString(stopDT) + " - " + DateTimeToString(stopDT), 2);

                sqlReadIDs = sqlReadIDs
                    .Replace("##idsBathSize##", isToday ? idsBathSize_today.ToString() : idsBathSize.ToString())
                    //.Replace("##idsBathSize##", idsBathSize.ToString())
                    .Replace("##dateTimeFrom##", DateTimeToString(strtDT))
                    .Replace("##dateTimeTo##", DateTimeToString(stopDT))
                    .Replace("##lastId##", idLast);

                Log(sqlReadIDs, 2);

                // Создать объект Command
                SqlCommand cmd = new SqlCommand
                {
                    Connection = conn,
                    CommandText = sqlReadIDs,
                    CommandTimeout = 36000
                };

                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {

                        while (reader.Read())
                        {
                            // IDs
                            var targetInfoID = reader.GetInt64("TargetInfoID");
                            var dateFix = reader.GetDateTime("DateFix");
                            string fileName = targetInfoID + " - " + reader.GetDateTime(1).ToString("u").Substring(0, 19).Replace(":", ".");

                            photoList.Add(new PhotoStruct() { TargetInfoID = targetInfoID, DateFix = dateFix, TargetImageID = -1, ImgFileName = fileName });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (markExceptions > 0)  Console.WriteLine(new string("Reading IDs from DB Error: " + e));
            }
            finally
            {
                // Закрыть соединение.
                //conn.Close();
                // Разрушить объект, освободить ресурс.
                //conn.Dispose();
                //conn = null;
            }

            photoListImg = new List<PhotoStruct>();
            for (int i=0; i<photoList.Count; i++)
            {
                Log("i = " + i, 2);

                var rec = photoList[i];
                byte[] buf = null;


                Stopwatch sw_queryImage = new Stopwatch();
                sw_queryImage.Start();
                int resImg = QueryImageByteArr(rec.TargetInfoID, ref buf, out long bufLen);


                PhotoStruct recImg = new PhotoStruct()
                {
                    TargetInfoID = rec.TargetInfoID,
                    DateFix = rec.DateFix,
                    TargetImageID = -1,
                    ImgFileName = rec.ImgFileName,
                    buf = buf,
                    bufLen = bufLen
                };

                photoListImg.Add(recImg);
            }

            cntPrep = photoList.Count;

            strtDT_out = strtDT;
            stopDT_out = stopDT;
            isToday_out = isToday;

            Log("cntAll = " + cntPrep, 2);

            return cntPrep;
        }
        
        public int ProcessPhotoBath(List<PhotoStruct> photoList,
            ref TimeSpan ts_Process, ref TimeSpan ts_Pauses)
        {
            //Console.WriteLine("ProcessPhotoBath()");            
            Int32.TryParse(myParams.Value("UseMultyProc"), out int useMultyProc);
            Int32.TryParse(myParams.Value("IDsSaveStep"), out int idsSaveStep);
            Int32.TryParse(myParams.Value("OneFilePause_ms"), out int oneFilePause_ms);

            // сбросим счётчики распознавания
            cntPrep = 0;
            cntReco = 0;
            cntSave = 0;
            cntLock = 0;
            long lastId = 0;

            for (int i = 0; i < photoList.Count; i++)
            {
                var rec = photoList[i];
                lastId = rec.TargetInfoID;
                int resRec = 0;

                //Console.WriteLine("CreateDirectories()");
                CreateDirectories();

                CarDataStruct answer = new CarDataStruct();
                string fname = rec.TargetInfoID.ToString();

                //Console.WriteLine("RecognizeImageByteArr()");
                if (rec.bufLen > 1000)
                {
                    Stopwatch sw_reco = new Stopwatch();
                    sw_reco.Start();
                    resRec = CarModelSdk.RecognizeImageByteArr(out answer, rec.buf, rec.bufLen, rec.ImgFileName);
                    sw_reco.Stop();
                    ts_Process += sw_reco.Elapsed;
                }
                else 
                    continue;

                //Console.WriteLine("MyTrim(answer.Name, 0, 250)");
                string resName = MyTrim(answer.Name, 0, 250);

                if (resRec > 0)
                {
                    cntReco++;

                    var resSaveToDB = SaveResultRec(rec, true, answer);

                    if (oneFilePause_ms > 0)
                    {
                        Stopwatch sw_pause = new Stopwatch();
                        sw_pause.Start();
                        Wait(oneFilePause_ms);
                        sw_pause.Stop();
                        ts_Pauses += sw_pause.Elapsed;
                    }
                }

                // запомним lastId
                int ii = i + 1;
                if (ii % idsSaveStep == 0
                    && ii > 0)
                {
                    long idFrom = lastId + 1;
                    updateParamInDB(_myName + "_ID_From", idFrom.ToString());
                    //saveParamToDB(_myName + "_ID_From", idFrom.ToString());
                }
            }

            cntPrep = photoList.Count;

            // запомним lastId
            long idFromRes = lastId + 1;
            updateParamInDB(_myName + "_ID_From", idFromRes.ToString());
            //saveParamToDB(_myName + "_ID_From", idFromRes.ToString());

            return cntReco;
        }



        public int QueryImage(long id, ref Emgu.CV.Mat img)
        {
            Log("QueryImage ( " + id + " )", 2);

            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            int res = 0;

            if (conn == null)
                RestoreConnection();

            try
            {
                string sqlReadImage = myParams.Value("SqlReadImage");

                sqlReadImage = sqlReadImage
                    .Replace("##ID##", id.ToString());

                Log(sqlReadImage, 2);

                // Создать объект Command
                SqlCommand cmd = new SqlCommand
                {
                    Connection = conn,
                    CommandText = sqlReadImage,
                    CommandTimeout = 36000
                };

                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            // read image from DB
                            MemoryStream memory = new MemoryStream();

                            long startIndex = 0;
                            const int ChunkSize = 256;

                            while (true)
                            {
                                byte[] buffer = new byte[ChunkSize];
                                long retrievedBytes = reader.GetBytes(0, startIndex, buffer, 0, ChunkSize);
                                memory.Write(buffer, 0, (int)retrievedBytes);
                                startIndex += retrievedBytes;

                                if (retrievedBytes != ChunkSize)
                                    break;
                            }

                            byte[] imgInMem = memory.ToArray();
                            memory.Dispose();

                            CvInvoke.Imdecode(imgInMem, Emgu.CV.CvEnum.ImreadModes.AnyColor, img);
                            int w = img.Width;
                            int h = img.Height;

                            res = 1;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (markExceptions > 0)  Console.WriteLine(new string("Reading Image from DB Error: " + e));
                if (markExceptions > 1)  Console.WriteLine(" -> -> " + e.StackTrace);
                res = 0;
            }
            finally
            {
                // Закрыть соединение.
                //conn.Close();
                // Разрушить объект, освободить ресурс.
                //conn.Dispose();
                //conn = null;
            }

            return res;
        }

        public int QueryImageByteArr(long id, ref byte[] imgInMem, out long bufLen)
        {
            Log("QueryImageByteArr ( " + id + " )", 2);

            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            int res = 0;
            bufLen = 0;

            if (conn == null)
                RestoreConnection();

            try
            {
                string sqlReadImage = myParams.Value("SqlReadImage");

                sqlReadImage = sqlReadImage
                    .Replace("##ID##", id.ToString());

                Log(sqlReadImage, 2);

                // Создать объект Command
                SqlCommand cmd = new SqlCommand
                {
                    Connection = conn,
                    CommandText = sqlReadImage,
                    CommandTimeout = 36000
                };

                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        if (reader.Read())
                        {
                            // read image from DB
                            MemoryStream memory = new MemoryStream();

                            long startIndex = 0;
                            const int ChunkSize = 256;
                            bufLen = 0;

                            while (true)
                            {
                                byte[] buffer = new byte[ChunkSize];
                                long retrievedBytes = reader.GetBytes(0, startIndex, buffer, 0, ChunkSize);
                                memory.Write(buffer, 0, (int)retrievedBytes);
                                startIndex += retrievedBytes;

                                bufLen += retrievedBytes;

                                if (retrievedBytes != ChunkSize)
                                    break;
                            }
                            imgInMem = memory.ToArray();
                            memory.Dispose();
                        }
                    }
                    res = 1;
                }
            }
            catch (Exception e)
            {
                if (markExceptions > 0)  Console.WriteLine(new string("Reading Image from DB Error: " + e));
                if (markExceptions > 1)  Console.WriteLine(" -> -> " + e.StackTrace);
                res = 0;
            }
            finally
            {
                // Закрыть соединение.
                //conn.Close();
                // Разрушить объект, освободить ресурс.
                //conn.Dispose();
                //conn = null;
            }

            return res;
        }

        private static int SaveResultRec(PhotoStruct rec, bool success, CarDataStruct answer)
        {
            if (!success)
                return 0;

            Int32.TryParse(myParams.Value("MarkExceptions"), out int markExceptions);

            int resIns = -1;

            Int32.TryParse(myParams.Value("RecognizeBorder"), out int recognizeBorder);
            Int32.TryParse(myParams.Value("MarkSaveID"), out int markSaveID);
            Int32.TryParse(myParams.Value("MarkLockID"), out int markLockID);

            string str1 = MyTrim(answer.MarkProbStr, 0, 10);
            int probMark =
                str1.Length > 0
                ? Int32.Parse(str1)
                : 0;
            string str2 = MyTrim(answer.TypeProbStr, 0, 10);
            int probType =
                str2.Length > 0
                ? Int32.Parse(str2)
                : 0;

            if ((probMark >= recognizeBorder)
                || (probType >= recognizeBorder))
            {
                string sqlSave = myParams.Value("SqlSaveImageRes");

                string mark = MyTrim(answer.Mark, 0, 50);
                string modl = MyTrim(answer.Modl, 0, 50);
                string probMarkStr = probMark.ToString();
                string type = MyTrim(answer.Type, 0, 50);
                string side = MyTrim(answer.Side, 0, 50);
                string probTypeStr = probType.ToString();

                string brand = mark + ", " + modl /*+ " " + MyTrim(ans.Generation, 0, 250)*/;
                if (brand == ",  ") brand = string.Empty;

                if (CarTypes.Keys.Contains(type))
                    type = CarTypes[type];

                if (CarTypes.Keys.Contains(side))
                    side = CarTypes[side];

                sqlSave = sqlSave
                    .Replace("##ID##", rec.TargetInfoID.ToString());

                if (probMark >= recognizeBorder)
                    sqlSave = sqlSave
                        .Replace("##brand##", "'" + brand + "'")
                        .Replace("##brandProb##", "'" + probMarkStr + "'");
                else
                    sqlSave = sqlSave
                        .Replace("##brand##", "''")
                        .Replace("##brandProb##", "''");

                if (probType >= recognizeBorder) 
                    sqlSave = sqlSave
                        .Replace("##type##", "'" + type + "'")
                        .Replace("##typeProb##", "'" + probTypeStr + "'");
                else
                    sqlSave = sqlSave
                        .Replace("##type##", "''")
                        .Replace("##typeProb##", "''");

                // Console.WriteLine("sqlSave = '" + sqlSave + "'");

                Log(sqlSave, 2);

                bool ok = false;

                try
                {
                    if (conn == null)
                        RestoreConnection();

                    SqlCommand command = new SqlCommand(null, conn);
                    command.CommandText = sqlSave;

                    command.Prepare();
                    resIns = command.ExecuteNonQuery();

                    cntSave++;
                    ok = true;
                }
                catch (Exception e)
                {
                    cntLock++;
                    ok = false;
                }
                finally
                {
                    // Console.WriteLine(" -> -> finally ");
                }

                if (ok)
                {
                    if (markSaveID > 0)
                        Console.Write("+ " + (markSaveID > 1 ? (rec.TargetInfoID.ToString() + ", ") : ""));
                }
                else
                {
                    if (markLockID > 0)
                        Console.Write("X " + (markSaveID > 1 ? (rec.TargetInfoID.ToString() + ", ") : ""));
                }
            }

            return resIns;
        }

        public bool CloseOldIDsSpan(string procName)
        {
            //Console.WriteLine("CloseOldIDsSpan (" + procName + ")");

            deleteParamFromDB(procName + "_Span");
            deleteParamFromDB(procName + "_DateTime_From");
            deleteParamFromDB(procName + "_DateTime_To");
            deleteParamFromDB(procName + "_ID_From");
            deleteParamFromDB(procName + "_ID_To");
            return true;
        }
    }
}
