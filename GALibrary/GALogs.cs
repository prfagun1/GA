using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GALibrary
{
    public class GALogs
    {

        //Log Level
        //    1 - only errors
        //    2 - all

        public static void SaveLog(String step, String message, int logLevel, Models.Parameter parameter)
        {
            String logFolder = parameter.PathLog;
            int logLevelDatabase = parameter.LogLevelApplication;
            String logName = "";
            String logLevelName = "Message";

            if (logLevel == 1) logLevelName = "Error";

            DateTime today = DateTime.Now;
            logName = "GA-" + today.Year.ToString().PadLeft(4, '0') + "-" + today.Month.ToString().PadLeft(2, '0') + "-" + today.Day.ToString().PadLeft(2, '0') + ".log";

            try
            {
                if (!logFolder.EndsWith("\\")) logFolder += "\\";
                Directory.CreateDirectory(logFolder);

                if (logLevel <= logLevelDatabase)
                {
                    System.IO.File.AppendAllText(logFolder + logName, DateTime.Now.ToString() + " - " + logLevelName + " - " + step + " - " + message + System.Environment.NewLine);
                }
            }

            catch (Exception error)
            {
                System.IO.File.AppendAllText("c:\\temp\\" + logName, DateTime.Now.ToString() + " - " + logLevelName + " - " + step + " - Erro ao abrir o arquivo de log: " + error.ToString() + System.Environment.NewLine);
            }
        }


        public static String SaveLogUpdate(Models.UpdateGA update, String process, String message, int logLevel, Models.Parameter parameter)
        {

            String logFolder = parameter.PathLog;
            int logLevelDatabase = parameter.LogLevelUpdate;
            String logName = "";
            String logLevelName = "Message";

            if (logLevel == 1) logLevelName = "Error";

            DateTime today = DateTime.Now;
            logName = "GA-Update-" + update.Schedule.GetValueOrDefault().Year.ToString().PadLeft(4, '0') + "-" + update.Schedule.GetValueOrDefault().Month.ToString().PadLeft(2, '0') + "-" + update.Schedule.GetValueOrDefault().Day.ToString().PadLeft(2, '0') + "-" + update.Name + "-" + update.Id + ".log";

            try
            {
                if (!logFolder.EndsWith("\\")) logFolder += "\\";
                logFolder += "Updates\\";
                System.IO.Directory.CreateDirectory(logFolder);

                if (logLevel <= logLevelDatabase)
                {
                    System.IO.File.AppendAllText(logFolder + logName, DateTime.Now.ToString() + " - " + logLevelName + " - " + process + " - " + message + System.Environment.NewLine);
                }
            }

            catch (Exception error)
            {
                return error.ToString();
            }

            return null;
        }


        public static System.IO.Stream GetUpdateLog(Models.UpdateGA update, Models.Parameter parameter)
        {
            String path = parameter.PathLog;
            if (!path.EndsWith("\\")) path += "\\";
            path += "Updates\\" + "GA-Update" + "-" + update.Schedule.GetValueOrDefault().Year.ToString().PadLeft(4, '0') + "-" + update.Schedule.GetValueOrDefault().Month.ToString().PadLeft(2, '0') + "-" + update.Schedule.GetValueOrDefault().Day.ToString().PadLeft(2, '0') + "-" + update.Name + "-" + update.Id + ".log";
            return new System.IO.FileStream(path, System.IO.FileMode.Open);
        }


        public static String SaveLogProcedure(Models.ProcedureSchedule procedureschedule, String process, String message, int logLevel, Models.Parameter parameter)
        {
            Models.GAContext context = new Models.GAContext();

            String logFolder = parameter.PathLog;
            int logLevelDatabase = parameter.LogLevelUpdate;
            String logName = "";
            String logLevelName = "Message";

            if (logLevel == 1) logLevelName = "Error";

            DateTime today = DateTime.Now;
            logName = "GA-Procedure-" + procedureschedule.Schedule.Year.ToString().PadLeft(4, '0') + "-" + procedureschedule.Schedule.Month.ToString().PadLeft(2, '0') + "-" + procedureschedule.Schedule.Day.ToString().PadLeft(2, '0') + "-" + procedureschedule.Procedure.Name + "-" + procedureschedule.Id + ".log";

            try
            {
                if (!logFolder.EndsWith("\\")) logFolder += "\\";
                logFolder += "Procedures\\";
                System.IO.Directory.CreateDirectory(logFolder);

                if (logLevel <= logLevelDatabase)
                {
                    System.IO.File.AppendAllText(logFolder + logName, DateTime.Now.ToString() + " - " + logLevelName + " - " + process + " - " + message + System.Environment.NewLine);
                }
            }

            catch (Exception error)
            {
                return error.ToString();
            }

            return null;
        }
    }
}
