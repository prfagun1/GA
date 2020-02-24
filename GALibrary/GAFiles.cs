using GALibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using File = System.IO.File;

namespace GALibrary
{
    public class GAFiles
    {

        //Compacta a pasta do backup
        public static String CompressFolder(UpdateGA update, Parameter parameter)
        {

            String pathBackup = parameter.PathBackup;
            if (!pathBackup.EndsWith("\\")) pathBackup += "\\";

            pathBackup += update.Schedule.GetValueOrDefault().Date.Year.ToString().PadLeft(4, '0') + "\\" + update.Schedule.GetValueOrDefault().Date.Month.ToString().PadLeft(2, '0') + "\\" + update.Id + "\\";
            String name = "Backup.zip";

            String pathZip = pathBackup;

            if (!System.IO.Directory.Exists(pathBackup)) return null;

            if (System.IO.File.Exists(pathZip + "\\" + name)) System.IO.File.Delete(pathZip + "\\" + name);

            try
            {

                Guid guid = Guid.NewGuid();
                String arquivoTemporario = parameter.PathTemp + "\\" + guid + ".zip";
                ZipFile.CreateFromDirectory(pathZip, arquivoTemporario);
                System.IO.File.Copy(arquivoTemporario, pathZip + "backup.zip", true);
                System.IO.File.Delete(arquivoTemporario);

                foreach (String folder in System.IO.Directory.GetDirectories(pathZip))
                {
                    System.IO.Directory.Delete(folder, true);
                }

                GALogs.SaveLog("Compress", "Arquivos compactados com sucesso para o ID - " + update.Id, 2, parameter);

            }
            catch (Exception error)
            {
                GALogs.SaveLog("Compress", "Erro ao compactar arquivos para o update id - " + update.Id + " - " + error, 1, parameter);
                return error.ToString();
            }
            return null;
        }



        public static String CompressSQLFiles(int id, String step, Models.Parameter parameter)
        {

            String folder = parameter.PathUpdate;
            String folderTemp = parameter.PathTemp;
            DateTime today = DateTime.Now;

            try
            {
                if (!Directory.Exists(folderTemp))
                {
                    Directory.CreateDirectory(folderTemp);
                }

                if (!folder.EndsWith("\\")) folder += "\\";
                folder += "SQL\\" + today.Year.ToString().PadLeft(4, '0') + "\\" + today.Month.ToString().PadLeft(2, '0') + "\\" + id + "\\";


                DirectoryInfo f = new DirectoryInfo(folder);
                FileInfo[] a = f.GetFiles();

                Guid guid = Guid.NewGuid();

                String arquivoTemporario = folderTemp + "\\" + guid + ".zip";
                ZipFile.CreateFromDirectory(folder, arquivoTemporario);
                File.Copy(arquivoTemporario, folder + "sql.zip", true);
                File.Delete(arquivoTemporario);

                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i].Name.ToUpper().EndsWith(".SQL"))
                        a[i].Delete();
                }


                GALibrary.GALogs.SaveLog(step, "Arquivos compactados com sucesso para o ID - " + id, 2, parameter);

            }
            catch (Exception error)
            {
                GALibrary.GALogs.SaveLog(step, "Erro ao compactar arquivos para o ID - " + id + " - " + error, 1, parameter);
                return error.ToString();
            }
            return null;
        }

        public static void DeleteFiles(int id, DateTime idDate, int type, Models.Parameter parameter)
        {
            String folderUpdate = parameter.PathUpdate;
            String updateType;

            if (type == 1) updateType = "SQL"; else updateType = "Files";


            if (!folderUpdate.EndsWith("\\")) folderUpdate += "\\";
            folderUpdate += updateType + "\\" + idDate.Year.ToString().PadLeft(4, '0') + "\\" + idDate.Month.ToString().PadLeft(2, '0') + "\\" + id;

            if (Directory.Exists(folderUpdate))
            {
                Directory.Delete(folderUpdate, true);
            }
        }


//Buscar encoding do arquivo de texto
        public static Encoding GetEncoding(string filename)
        {
            using (var reader = new StreamReader(filename, Encoding.Default, true))
            {
                if (reader.Peek() >= 0) // you need this!
                    reader.Read();

                return reader.CurrentEncoding;
            }
        }
    }
}
