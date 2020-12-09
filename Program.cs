using CarModelSdk_NetCore;
using Sql_NetFramework;
using System;
using System.Configuration;
using System.Threading;

namespace Sql_NetCore
{
    class Program
    {
        private static string _myName = "";

        static void Main(string[] args)
        {
            // получим имя диапазона для этого экземпляра программы
            if (args.Length >= 2
                && args[0] == "-name" )
                    _myName = args[1].PadRight(4);

            MyParams myParams = new MyParams();
            myParams.Print();

            //////////////////////
            // Работа с данными //
            //////////////////////

            AutomatReco automat = new AutomatReco(_myName, myParams);

            AutomatReco.Log(" ", 1);
            AutomatReco.Log("CURRENT DIRECTORY:", 1);
            string curDir = Environment.CurrentDirectory;
            AutomatReco.Log(curDir, 1);
            Int32.TryParse(myParams.Value("LogLevel"), out int logLevel);
            AutomatReco.Log("LogLevel = " + logLevel + " (0-1-2)", 1);
            Int32.TryParse(myParams.Value("LogToDisk"), out int logToDisk);
            AutomatReco.Log("LogToDisk = " + logToDisk, 1);

            AutomatReco.Log(" ", 1);
            Int32.TryParse(myParams.Value("UseMultyProc"), out int useMultyProc);
            AutomatReco.Log("UseMultyProc = " + useMultyProc, 1);
            Int32.TryParse(myParams.Value("IDsProcSpanLim"), out int idsProcSpanLim);
            if (useMultyProc > 0)
            {
                Int32.TryParse(myParams.Value("ProcCount"), out int procCount);
                if (procCount < 1) procCount = 1;
                if (procCount > 10) procCount = 10;
                AutomatReco.Log("ProcCount  = " + procCount, 1);
                AutomatReco.Log("IDsProcSpanLim = " + idsProcSpanLim, 1);
                string procHubName = myParams.Value("ProcHubName");
                AutomatReco.Log("ProcHubName  = '" + procHubName + "'", 1);
            }

            AutomatReco.Log(" ", 1);
            Int32.TryParse(myParams.Value("IDsBathSize"), out int idsBathSize);
            AutomatReco.Log("IDsBathSize = " + idsBathSize, 1);
            Int32.TryParse(myParams.Value("IDsSaveStep"), out int idsSaveStep);
            AutomatReco.Log("IDsSaveStep = " + idsSaveStep, 1);

            AutomatReco.Log(" ", 1);
            Int32.TryParse(myParams.Value("UseCycle"), out int useCycle);
            AutomatReco.Log("UseCycle = " + useCycle, 1);
            Int32.TryParse(myParams.Value("ForseToday"), out int forseToday);
            AutomatReco.Log("ForseToday = " + forseToday, 1);
            Int32.TryParse(myParams.Value("RecognizeMode"), out int mode);
            AutomatReco.Log("Mode = " + mode.ToString() + " (" + ((mode & 1) > 0 ? "Mark" : "-") + ", " + ((mode & 2) > 0 ? "Type" : "-") + ")", 1);
            Int32.TryParse(myParams.Value("RecognizeBorder"), out int recognizeBorder);
            AutomatReco.Log("RecognizeBorder = " + recognizeBorder, 1);

            AutomatReco.Log(" ", 1);
            AutomatReco.Log("FILTER OPTIONS:", 1);
            Int32.TryParse(myParams.Value("UseDarkFilter"), out int useDarkFilter);
            AutomatReco.Log("UseDarkFilter = " + useDarkFilter, 1);
            if (useDarkFilter > 0)
            {
                Int32.TryParse(myParams.Value("Latitude_deg"), out int latitude_deg);
                AutomatReco.Log("Latitude_deg = " + latitude_deg, 1);
                Int32.TryParse(myParams.Value("OffsetDayTime_min"), out int offsetDayTime_min);
                AutomatReco.Log("OffsetDayTime_min = " + offsetDayTime_min, 1);
                Int32.TryParse(myParams.Value("OffsetTimeZone_min"), out int offsetTimeZone_min);
                AutomatReco.Log("OffsetTimeZone_min = " + offsetTimeZone_min, 1);
            }

            AutomatReco.Log(" ", 1);
            AutomatReco.Log("WAITING OPTIONS:", 1);
            Int32.TryParse(myParams.Value("UsePauses"), out int useStepWaits);
            AutomatReco.Log("UsePauses = " + useStepWaits, 1);
            if (useStepWaits > 0)
            {
                Int32.TryParse(myParams.Value("IDsBathPause_ms"), out int idsBathWait_ms);
                AutomatReco.Log("IDsBathPause_ms = " + idsBathWait_ms, 1);
                Int32.TryParse(myParams.Value("OneFilePause_ms"), out int oneFileWait_ms);
                AutomatReco.Log("OneFilePause_ms = " + oneFileWait_ms, 1);
            }

            AutomatReco.Log(" ", 1);
            AutomatReco.Log("DISK USE OPTIONS:", 1);
            Int32.TryParse(myParams.Value("FilesToDisk"), out int filesToDisk);
            AutomatReco.Log("FilesToDisk = " + filesToDisk, 1);
            if (filesToDisk > 0)
            {
                string dirIn = myParams.Value("DirIn");
                AutomatReco.Log("DirIn = " + dirIn, 1);
                string dirOut = myParams.Value("DirOut");
                AutomatReco.Log("DirOut = " + dirOut, 1);
                Int32.TryParse(myParams.Value("SaveOps_SaveRecs"), out int saveOps_SaveRecs);
                AutomatReco.Log("SaveOps_SaveRecs = " + saveOps_SaveRecs, 1);
                Int32.TryParse(myParams.Value("SaveOps_SaveUnrecs"), out int saveOps_SaveUnrecs);
                AutomatReco.Log("SaveOps_SaveUnrecs = " + saveOps_SaveUnrecs, 1);
            }

            automat.RemoveDirectories();

            int cnt = 0;
            int cntIDs = 0;
            do
            {
                cnt++;

                int idsCnt = automat.RecoPhotosByBath();

                if (idsCnt <= 0)
                    // данных больше нет
                    break;

                cntIDs += idsCnt;

                // очистка мусора
                GC.Collect();
                GC.WaitForPendingFinalizers();
                myParams.ReadAll();
            }
            while (true /*useCycle > 0*/);

            automat.CloseOldIDsSpan(_myName);

            return;
        }

    }
}
