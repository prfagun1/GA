using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using File = System.IO.File;

namespace GA.Lib
{
    public class GAClass
    {

        public static async Task<bool> SaveFiles(IFormFileCollection files, int id, String step, int type, GALibrary.Models.Parameter parameter)
        {
            foreach (IFormFile file in files)
            {
                string fname;

                try
                {
                    string[] testfiles = file.FileName.Split(new char[] { '\\' });
                    fname = testfiles[testfiles.Length - 1];
                }
                catch
                {
                    fname = file.FileName;
                }

                string erro = await SaveFile(id, file, fname, type);

                if (erro == null)
                {
                    GALibrary.GALogs.SaveLog(step, "Arquivos salvos com sucesso para o ID - " + id, 2, GALibrary.Models.DB.Context.Parameter.FirstOrDefault());
                }
                else
                {
                    GALibrary.GALogs.SaveLog(step, "Erro ao salvar arquivos para o ID - " + id + " - " + erro, 1, GALibrary.Models.DB.Context.Parameter.FirstOrDefault());
                    return false;
                }

            }

            //Caso sejam arquivos SQL compacta a pasta
            if (type == 1) GALibrary.GAFiles.CompressSQLFiles(id, step, parameter);

            return true;
        }



        private static async Task<String> SaveFile(int id, IFormFile fileData, String fileName, int type)
        {
            String folderUpdate = GALibrary.Models.DB.Context.Parameter.First().PathUpdate;
            String updateType;
            if (type == 1) updateType = "SQL"; else updateType = "Files";
            DateTime today = DateTime.Now;

            try
            {
                if (!folderUpdate.EndsWith("\\")) folderUpdate += "\\";
                folderUpdate += updateType + "\\" + today.Year.ToString().PadLeft(4, '0') + "\\" + today.Month.ToString().PadLeft(2, '0') + "\\" + id + "\\";

                Directory.CreateDirectory(folderUpdate);

                    using (var fileStream = new FileStream(folderUpdate + fileName, FileMode.Create))
                    {
                        await fileData.CopyToAsync(fileStream);
                    }
                
            }
            catch (Exception error)
            {
                return error.ToString();
            }

            return null;
        }


        public static Stream GetFile(int fileId) {
            GALibrary.Models.File file = GALibrary.Models.DB.Context.File.Find(fileId);
            String path = GALibrary.Models.DB.Context.Parameter.First().PathUpdate;
            if (!path.EndsWith("\\")) path += "\\";
            path += "Files" + "\\" + file.Date.Year.ToString().PadLeft(4, '0') + "\\" + file.Date.Month.ToString().PadLeft(2, '0') + "\\" + file.Id + "\\";
            Stream fimeStream = new FileStream(path + file.FileName, FileMode.Open);

            return fimeStream;
        }


        public static Stream GetSQLFile(int sqlId)
        {
            GALibrary.Models.SQL sql = GALibrary.Models.DB.Context.SQL.Find(sqlId);
            String path = GALibrary.Models.DB.Context.Parameter.First().PathUpdate;
            if (!path.EndsWith("\\")) path += "\\";
            path += "SQL" + "\\" + sql.Date.Year.ToString().PadLeft(4, '0') + "\\" + sql.Date.Month.ToString().PadLeft(2, '0') + "\\" + sql.Id + "\\";
            Stream fimeStream = new FileStream(path + "sql.zip", FileMode.Open);
            return fimeStream;
        }

        public static Stream GetProcedureScheduleLog(GALibrary.Models.ProcedureSchedule procedureSchedule)
        {
           String path = GALibrary.Models.DB.Context.Parameter.First().PathLog;
            if (!path.EndsWith("\\")) path += "\\";
            path += "Procedures\\" + "GA-Procedure" + "-" + procedureSchedule.Schedule.Year.ToString().PadLeft(4, '0') + "-" + procedureSchedule.Schedule.Month.ToString().PadLeft(2, '0') + "-" + procedureSchedule.Schedule.Day.ToString().PadLeft(2, '0') + "-" + procedureSchedule.Procedure.Name + "-" + procedureSchedule.Id + ".log";
            return new FileStream(path, FileMode.Open);
        }


        public static Stream GetUpdateBackup(GALibrary.Models.UpdateGA update)
        {
            String path = GALibrary.Models.DB.Context.Parameter.First().PathBackup;
            if (!path.EndsWith("\\")) path += "\\";
            path += update.Schedule.GetValueOrDefault().Year.ToString().PadLeft(4, '0') + "\\" + update.Schedule.GetValueOrDefault().Month.ToString().PadLeft(2, '0') + "\\" + update.Id + "\\Backup.zip";
            return new FileStream(path, FileMode.Open);
        }


    }
}
