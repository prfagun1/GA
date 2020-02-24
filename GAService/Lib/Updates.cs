using GALibrary.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;


namespace GAService.Lib
{
    class Updates
    {

        //private static Models.GAContext context = new Models.GAContext();

        public static List<UpdateGA> GetUpdates()
        {
            Models.GAContext context = new Models.GAContext();
            DateTime now = Convert.ToDateTime(DateTime.Now.ToString("g"));
            //Com o fluxo de aprovação
            //return context.UpdateGA.Include(x => x.Application).Where(x => x.Status == 0 && DateTime.Compare(x.Schedule.GetValueOrDefault(), now) == 0 && (x.Approved == true || x.Application.EnvironmentId == 1) && !x.Template).ToList();

            //Sem o fluxo de aprovação
            return context.UpdateGA.Include(x => x.Application).Where(x => x.Status == 0 && DateTime.Compare(x.Schedule.GetValueOrDefault(), now) == 0 && !x.Template).ToList();
        }


        public static List<ProcedureSchedule> GetProcedureSchedules()
        {
            DateTime now = Convert.ToDateTime(DateTime.Now.ToString("g"));
            Models.GAContext context = new Models.GAContext();

            return context.ProcedureSchedule.Include(x => x.Procedure).Where(x => DateTime.Compare(x.Schedule, now) == 0).ToList();

        }


        public static UpdateGA UpdateApplication(int updateId)
        {
            Models.GAContext context = new Models.GAContext();

            GALibrary.Models.Parameter parameter = context.Parameter.FirstOrDefault();

            var steps = context.UpdateSteps.Where(x => x.UpdateId == updateId).ToList().OrderBy(x => x.Order);
            UpdateGA update = context.UpdateGA.First(x => x.Id == updateId);

            //Atualiza status para atualizando (3)
            update.Status = 3;
            context.Entry(update).State = EntityState.Modified;
            context.SaveChanges();

            foreach (UpdateSteps step in steps)
            {

                switch (step.Type)
                {
                    case 1:
                        GALibrary.GALogs.SaveLogUpdate(update, "FilesDelete", "Inicio da remocao de arquivos", 2, parameter);
                        UpdateRemoveFiles(update, step.ProcessId);
                        GALibrary.GALogs.SaveLogUpdate(update, "FilesDelete", "Fim da remocao de arquivos", 2, parameter);
                        break;
                    case 2:
                        GALibrary.GALogs.SaveLogUpdate(update, "Files", "Inicio da copia de arquivos", 2, parameter);
                        UpdateCopyFiles(update, step.ProcessId);
                        GALibrary.GALogs.SaveLogUpdate(update, "Files", "Fim da copia de arquivos", 2, parameter);
                        break;
                    case 3:
                        GALibrary.GALogs.SaveLogUpdate(update, "Command", "Inicio da execucao do comando", 2, parameter);
                        UpdateCommand(update, null, step.ProcessId);
                        GALibrary.GALogs.SaveLogUpdate(update, "Command", "Fim da execucao do comando", 2, parameter);
                        break;
                    case 4:
                        GALibrary.GALogs.SaveLogUpdate(update, "ServiceStart", "Inicio do start de servico", 2, parameter);
                        UpdateServiceStart(update, null, step.ProcessId);
                        GALibrary.GALogs.SaveLogUpdate(update, "ServiceStart", "Fim do start de servico", 2, parameter);
                        break;
                    case 5:
                        GALibrary.GALogs.SaveLogUpdate(update, "ServiceStop", "Inicio da parada de servico", 2, parameter);
                        UpdateServiceStop(update, null, step.ProcessId);
                        GALibrary.GALogs.SaveLogUpdate(update, "ServiceStop", "Fim da parada de servico", 2, parameter);
                        break;
                    case 6:
                        GALibrary.GALogs.SaveLogUpdate(update, "SQL", "Inicio da execucao SQL", 2, parameter);
                        UpdateSQL(update, step.ProcessId);
                        GALibrary.GALogs.SaveLogUpdate(update, "SQL", "Fim da execucao SQL", 2, parameter);
                        break;

                }

            }

            GALibrary.GAFiles.CompressFolder(update, parameter);


            try
            {
                string log;
                using (System.IO.StreamReader reader = new System.IO.StreamReader(GALibrary.GALogs.GetUpdateLog(update, parameter)))
                {
                    log = reader.ReadToEnd();
                }

                if (log.Contains(" - Error - "))
                {
                    update.Status = 2;
                }
                else
                {
                    update.Status = 1;
                }
            }
            catch (Exception erro) {
                update.Status = 1;
                GALibrary.GALogs.SaveLogUpdate(update, "Status", "Erro ao salvar status: " + erro.ToString(), 1, parameter);
            }

            context.Entry(update).State = EntityState.Modified;
            context.SaveChanges();

            return update;   
        }


        public static String UpdateRemoveFiles(UpdateGA update, int fileDeleteId)
        {
            Models.GAContext context = new Models.GAContext();

            Parameter parameter = context.Parameter.FirstOrDefault();
            FileDelete filedelete = context.FileDelete.First(x => x.Id == fileDeleteId);
            List<FileDeleteFolder> fileDeleteFolders = context.FileDeleteFolder.Where(x => x.FileDeleteId == fileDeleteId).ToList();
            var pathToDelete = filedelete.FilesDirectory.Replace("\n", "").Split('\r');
            String log = "";

            try
            {
                GALibrary.GALogs.SaveLogUpdate(update, "FilesDelete", "Procedimento - " + filedelete.Name, 2, parameter);

                foreach (FileDeleteFolder fileDeleteFolder in fileDeleteFolders)
                {
                    String pathDestination = context.Folder.First(x => x.Id == fileDeleteFolder.FolderId).Path;
                    if (!pathDestination.EndsWith("\\")) pathDestination += "\\";
                    for (int i = 0; i < pathToDelete.Length; i++)
                    {
                        if (pathToDelete[i].Trim() == "") continue;
                        String path = pathDestination + pathToDelete[i];
                        try
                        {
                            //1 = Directory
                            //2 = File
                            if (System.IO.File.GetAttributes(path).HasFlag(System.IO.FileAttributes.Directory))
                            {

                                UpdateDeleteFilesBackup(update, filedelete, path, pathDestination, fileDeleteFolder.FolderId, 1);
                                System.IO.Directory.Delete(path, true);
                            }
                            else
                            {

                                UpdateDeleteFilesBackup(update, filedelete, path, pathDestination, fileDeleteFolder.FolderId, 2);
                                System.IO.File.Delete(path);
                            }

                        }
                        catch
                        {
                            GALibrary.GALogs.SaveLogUpdate(update, "FileDelete", "Arquivo/Pasta - " + path + " não foi encontrado.", 1, parameter);
                            log += path + " não foi encontrado" + System.Environment.NewLine;
                        }
                    }
                }
            }
            catch (Exception error)
            {
                log = error.ToString();
                GALibrary.GALogs.SaveLogUpdate(update, "FileDelete", "Arquivo/Pasta - " + filedelete.Id + " - " + log, 1, parameter);
            }

            return log;
        }


        /*
Type:
    1 = Directory
    2 = File
*/
        private static String UpdateDeleteFilesBackup(UpdateGA update, FileDelete filedelete, String path, String pathFolder, int folderId, int type)
        {
            Models.GAContext context = new Models.GAContext();
            GALibrary.Models.Parameter parameter = context.Parameter.FirstOrDefault();

            String pathBackup = context.Parameter.First().PathBackup;
            String pathBackupDirectory = "";
            String log = "";
            String pathZip = "";

            try
            {
                if (!pathBackup.EndsWith("\\")) pathBackup += "\\";
                pathZip = pathBackup + update.Schedule.GetValueOrDefault().Date.Year.ToString().PadLeft(4, '0') + "\\" + update.Schedule.GetValueOrDefault().Date.Month.ToString().PadLeft(2, '0') + "\\" + update.Id + "\\" + filedelete.Id + "\\";
                pathBackup += update.Schedule.GetValueOrDefault().Date.Year.ToString().PadLeft(4, '0') + "\\" + update.Schedule.GetValueOrDefault().Date.Month.ToString().PadLeft(2, '0') + "\\" + update.Id + "\\Arquivos apagados - " + filedelete.Id + "\\Pasta - " + folderId + "\\";

                pathBackupDirectory = pathBackup.Substring(0, pathBackup.LastIndexOf("\\"));
                if (!pathBackupDirectory.EndsWith("\\")) pathBackupDirectory += "\\";

                if (!System.IO.Directory.Exists(pathBackupDirectory)) System.IO.Directory.CreateDirectory(pathBackupDirectory);

                //Delete folder
                if (type == 1)
                {
                    String pathDestination = pathBackupDirectory + path.Replace(pathFolder, "");
                    CopyFolder(new System.IO.DirectoryInfo(path), new System.IO.DirectoryInfo(pathDestination));
                }
                //Delete file
                if (type == 2)
                {
                    String fileName = path.Substring(path.LastIndexOf("\\") + 1);
                    String fileDestination = pathBackupDirectory + path.Replace(pathFolder, "");
                    String directory = fileDestination.Substring(0, fileDestination.LastIndexOf("\\"));
                    if (!System.IO.Directory.Exists(directory)) System.IO.Directory.CreateDirectory(directory);
                    System.IO.File.Copy(path, fileDestination, true);
                }

                GALibrary.GALogs.SaveLogUpdate(update, "FilesDelete", "Backup do arquivo " + path + " para " + pathBackup + " realizado.", 2, parameter);
            }
            catch (Exception error)
            {
                log = error.ToString();
                GALibrary.GALogs.SaveLogUpdate(update, "FilesDelete", "Backup do arquivo " + path + " para " + pathBackup + " não realizado. Erro: " + log, 1, parameter);
            }

            return log;
        }

        public static void CopyFolder(System.IO.DirectoryInfo source, System.IO.DirectoryInfo target)
        {
            foreach (System.IO.DirectoryInfo dir in source.GetDirectories())
                CopyFolder(dir, target.CreateSubdirectory(dir.Name));
            foreach (System.IO.FileInfo file in source.GetFiles())
                file.CopyTo(System.IO.Path.Combine(target.FullName, file.Name));
        }


        public static String UpdateCopyFiles(UpdateGA update, int fileId)
        {
            Models.GAContext context = new Models.GAContext();

            GALibrary.Models.Parameter parameter = context.Parameter.FirstOrDefault();

            File file = context.File.First(x => x.Id == fileId);
            List<FileFolder> filefolders = context.FileFolder.Where(x => x.FileId == fileId).ToList();

            String logCopy = "";
            String pathUpdate = context.Parameter.First().PathUpdate;
            String pathZip = "";
            Boolean errorFound = false;

            // this.SaveLogUpdate(update, "Files", "Inicio da cópia de arquivos", 2);

            if (!pathUpdate.EndsWith("\\")) pathUpdate += "\\";
            pathUpdate += "Files" + "\\" + file.Date.Year.ToString().PadLeft(4, '0') + "\\" + file.Date.Month.ToString().PadLeft(2, '0') + "\\" + file.Id + "\\";

            pathZip = pathUpdate + "extracted";

            try
            {
                String[] fileZip = System.IO.Directory.GetFiles(pathUpdate);

                //Apaga o conteúdo extraido caso já exista:
                if (System.IO.Directory.Exists(pathZip)) System.IO.Directory.Delete(pathZip, true);

                //Somente busca 1 arquivo da pasta, pois o sistema não permite mais que um
                ZipFile.ExtractToDirectory(fileZip[0], pathZip);

                GALibrary.GALogs.SaveLogUpdate(update, "Files", "Procedimento - " + file.Name, 2, parameter);
                GALibrary.GALogs.SaveLogUpdate(update, "Files", "Arquivos descompactados na pasta " + pathZip, 2, parameter);

                //Copia arquivos
                try
                {
                    foreach (FileFolder filefolder in filefolders)
                    {
                        String pathDestination = context.Folder.First(x => x.Id == filefolder.FolderId).Path;
                        if (!pathDestination.EndsWith("\\")) pathDestination += "\\";


                        foreach (string dirPath in System.IO.Directory.GetDirectories(pathZip, "*", System.IO.SearchOption.AllDirectories))
                        {
                            System.IO.Directory.CreateDirectory(dirPath.Replace(pathZip, pathDestination));
                        }

                        String[] allFiles = System.IO.Directory.GetFiles(pathZip, "*.*", System.IO.SearchOption.AllDirectories);
                        //Copy all the files & Replaces any files with the same name
                        foreach (string newPath in allFiles)
                        {
                            try
                            {
                                //Faz backup do arquivo
                                logCopy += UpdateCopyFilesBackup(update, fileId, newPath.Replace(pathZip, pathDestination), pathDestination, filefolder.FolderId);
                                if (logCopy != "")
                                {
                                    GALibrary.GALogs.SaveLogUpdate(update, "Files", "Erro ao fazer backup do arquivo " + pathZip + " para " + pathDestination + ": " + logCopy, 1, parameter);
                                    if (System.IO.Directory.Exists(pathZip)) System.IO.Directory.Delete(pathZip, true);
                                    return logCopy;
                                }
                                //Só copia se fez backup
                                String caminhoCopia = newPath.Replace(pathZip, pathDestination);
                                System.IO.File.Copy(newPath, newPath.Replace(pathZip, pathDestination), true);
                                GALibrary.GALogs.SaveLogUpdate(update, "Files", "Copiado arquivo " + newPath + " para " + caminhoCopia, 2, parameter);

                                //Salva histórico dos arquivos
                                try
                                {
                                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(caminhoCopia);
                                    FileHistory filehistory = new FileHistory();
                                    filehistory.FileName = fileInfo.Name;
                                    filehistory.Folder = fileInfo.FullName;
                                    filehistory.FileId = fileId;
                                    filehistory.Size = fileInfo.Length;
                                    filehistory.Date = fileInfo.LastWriteTime;
                                    filehistory.UpdateId = update.Id;
                                    context.FileHistory.Add(filehistory);
                                    context.SaveChanges();
                                }
                                catch { }

                            }
                            catch (Exception error)
                            {
                                GALibrary.GALogs.SaveLogUpdate(update, "Files", "Erro ao copiar arquivo " + newPath + " para " + newPath.Replace(pathZip, pathDestination) + ": " + error.ToString(), 1, parameter);
                                logCopy += error.ToString();
                                errorFound = true;
                            }
                        }

                    }
                }
                catch (Exception error)
                {
                    GALibrary.GALogs.SaveLogUpdate(update, "Files", error.ToString(), 1, parameter);
                    errorFound = true;
                }


            }
            catch (Exception error)
            {
                GALibrary.GALogs.SaveLogUpdate(update, "Files", error.ToString(), 1, parameter);
                errorFound = true;
            }

            //Remove pasta descompactada
            if (System.IO.Directory.Exists(pathZip)) System.IO.Directory.Delete(pathZip, true);

            if (errorFound)
            {
                GALibrary.GALogs.SaveLogUpdate(update, "Files", logCopy, 1, parameter);
            }

            return null;
        }

        private static String UpdateCopyFilesBackup(UpdateGA update, int fileId, String fileBackup, String pathOrigin, int folderId)
        {
            Models.GAContext context = new Models.GAContext();
            GALibrary.Models.Parameter parameter = context.Parameter.FirstOrDefault();

            String pathBackup = context.Parameter.First().PathBackup;
            File file = context.File.First(x => x.Id == fileId);
            String pathBackupDirectory = "";

            try
            {
                if (!pathBackup.EndsWith("\\")) pathBackup += "\\";
                pathBackup += update.Schedule.GetValueOrDefault().Date.Year.ToString().PadLeft(4, '0') + "\\" + update.Schedule.GetValueOrDefault().Date.Month.ToString().PadLeft(2, '0') + "\\" + update.Id + "\\Arquivos copiados - " + file.Id + "\\Pasta - " + folderId + "\\";

                pathBackup += fileBackup.Replace(pathOrigin, "");
                pathBackup = pathBackup.Replace("\\\\", "\\");
                fileBackup = "\\" + fileBackup.Replace("\\\\", "\\");

                pathBackupDirectory = pathBackup.Substring(0, pathBackup.LastIndexOf("\\"));
                if (!System.IO.Directory.Exists(pathBackupDirectory)) System.IO.Directory.CreateDirectory(pathBackupDirectory);

                if (System.IO.File.Exists(fileBackup))
                {
                    System.IO.File.Copy(fileBackup, pathBackup, true);
                    GALibrary.GALogs.SaveLogUpdate(update, "Files", "Backup do arquivo " + fileBackup + " para " + pathBackup + " realizado.", 2, parameter);
                }
                else
                {
                    GALibrary.GALogs.SaveLogUpdate(update, "Files", "Backup do arquivo " + fileBackup + " não é necessário porque é um arquivo novo.", 2, parameter);
                }

            }
            catch (Exception error)
            {
                return error.ToString();
            }

            return null;
        }


        public static String UpdateCommand(UpdateGA update, ProcedureSchedule procedureschedule, int commandId)
        {
            Models.GAContext context = new Models.GAContext();
            GALibrary.Models.Parameter parameter = context.Parameter.FirstOrDefault();

            Command command = context.Command.First(x => x.Id == commandId);
            Server server = context.Server.Include(x => x.ServerUser).First(x => x.Id == command.ServerId);


            OS os = context.OS.First(x => x.Id == server.OSId);

            String result = "";
            String comandoCompleto = os.AccessCommand;

            comandoCompleto = comandoCompleto.Replace("nomeDoServidor", server.Name);
            comandoCompleto = comandoCompleto.Replace("usuarioDeAcesso", GALibrary.GACrypto.Base64Decode(server.ServerUser.ServerUsername));
            comandoCompleto = comandoCompleto.Replace("senhaDeAcesso", GALibrary.GACrypto.Base64Decode(server.ServerUser.ServerPassword));
            comandoCompleto = comandoCompleto.Replace("comandoParaExecutar", command.CommandText);

            try
            {
                if (update == null)
                {
                    GALibrary.GALogs.SaveLogProcedure(procedureschedule, "Command", "Comando - " + command.Name + " - " + result, 2, parameter);
                }
                else
                {
                    GALibrary.GALogs.SaveLogUpdate(update, "Command", "Procedimento - " + command.Name, 2, parameter);
                }

                System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + comandoCompleto);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.CreateNoWindow = true;
                procStartInfo.UseShellExecute = false;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();

                result = proc.StandardOutput.ReadToEnd();

                if (update != null)
                {
                    GALibrary.GALogs.SaveLogUpdate(update, "Command", "Comando - " + command.Name + " - " + result, 2, parameter);
                    GALibrary.GALogs.SaveLogUpdate(update, "Command", "Comando - " + command.CommandText + " - " + result, 2, parameter);
                }
                else
                {
                    GALibrary.GALogs.SaveLogProcedure(procedureschedule, "Command", "Comando - " + command.Name + " - " + result, 2, parameter);
                    GALibrary.GALogs.SaveLogProcedure(procedureschedule, "Command", "Comando - " + command.CommandText + " - " + result, 2, parameter);
                }
            }
            catch (Exception error)
            {
                if (update != null)
                {
                    GALibrary.GALogs.SaveLogUpdate(update, "Command", "Comando - " + command.Name + " - " + error.ToString(), 1, parameter);
                    GALibrary.GALogs.SaveLogUpdate(update, "Command", "Comando - " + command.CommandText + " - " + error.ToString(), 1, parameter);
                }
                else
                {
                    GALibrary.GALogs.SaveLogProcedure(procedureschedule, "Command", "Comando - " + command.Name + " - " + error.ToString(), 1, parameter);
                    GALibrary.GALogs.SaveLogProcedure(procedureschedule, "Command", "Comando - " + command.CommandText + " - " + error.ToString(), 1, parameter);
                }

                return error.ToString();
            }

            return null;

        }


        public static String UpdateServiceStart(UpdateGA update, ProcedureSchedule procedureschedule, int serviceId)
        {
            Models.GAContext context = new Models.GAContext();
            GALibrary.Models.Parameter parameter = context.Parameter.FirstOrDefault();

            Service service = context.Service.First(x => x.Id == serviceId);
            Server server = context.Server.Include(x => x.ServerUser).First(x => x.Id == service.ServerId);
            OS os = context.OS.First(x => x.Id == server.OSId);


            String result = "";
            String comandoCompleto = os.AccessCommand;
            comandoCompleto = comandoCompleto.Replace("nomeDoServidor", server.Name);
            comandoCompleto = comandoCompleto.Replace("usuarioDeAcesso", GALibrary.GACrypto.Base64Decode(server.ServerUser.ServerUsername));
            comandoCompleto = comandoCompleto.Replace("senhaDeAcesso", GALibrary.GACrypto.Base64Decode(server.ServerUser.ServerPassword));
            comandoCompleto = comandoCompleto.Replace("comandoParaExecutar", service.CommandStart);

            try
            {

                if (update != null)
                {
                    GALibrary.GALogs.SaveLogUpdate(update, "ServiceStart", "Procedimento - " + service.Name, 2, parameter);
                }
                else
                {
                    GALibrary.GALogs.SaveLogProcedure(procedureschedule, "ServiceStart - " + service.Name, "Iniciando servico", 2, parameter);
                }
                

                System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + comandoCompleto);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.CreateNoWindow = true;
                procStartInfo.UseShellExecute = false;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();

                result = proc.StandardOutput.ReadToEnd();

                if (update != null)
                {
                    GALibrary.GALogs.SaveLogUpdate(update, "ServiceStart - " + service.Name, "Retorno: " + result, 2, parameter);
                }
                else
                {
                    GALibrary.GALogs.SaveLogProcedure(procedureschedule, "ServiceStart - " + service.Name, "Retorno: " + result, 2, parameter);
                }
            }
            catch (Exception error)
            {
                if (update != null)
                {
                    GALibrary.GALogs.SaveLogUpdate(update, "ServiceStart - " + service.Name, error.ToString(), 1, parameter);
                }
                else
                {
                    GALibrary.GALogs.SaveLogProcedure(procedureschedule, "ServiceStart - " + service.Name, error.ToString(), 1, parameter);
                }
                return error.ToString();
            }

            return null;
        }

        public static String UpdateServiceStop(UpdateGA update, ProcedureSchedule procedureschedule, int serviceId)
        {
            Models.GAContext context = new Models.GAContext();
            GALibrary.Models.Parameter parameter = context.Parameter.FirstOrDefault();

            Service service = context.Service.First(x => x.Id == serviceId);
            Server server = context.Server.Include(x => x.ServerUser).First(x => x.Id == service.ServerId);
            OS os = context.OS.First(x => x.Id == server.OSId);

            String result = "";
            String comandoCompleto = os.AccessCommand;
            comandoCompleto = comandoCompleto.Replace("nomeDoServidor", server.Name);
            comandoCompleto = comandoCompleto.Replace("usuarioDeAcesso", GALibrary.GACrypto.Base64Decode(server.ServerUser.ServerUsername));
            comandoCompleto = comandoCompleto.Replace("senhaDeAcesso", GALibrary.GACrypto.Base64Decode(server.ServerUser.ServerPassword));
            comandoCompleto = comandoCompleto.Replace("comandoParaExecutar", service.CommandStop);

            try
            {
                if (update != null)
                {
                    GALibrary.GALogs.SaveLogUpdate(update, "ServiceStop", "Procedimento - " + service.Name, 2, parameter);
                }
                else
                {
                    GALibrary.GALogs.SaveLogProcedure(procedureschedule, "ServiceStop - " + service.Name, "Parando servico", 2, parameter);
                }

                System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + comandoCompleto);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.CreateNoWindow = true;
                procStartInfo.UseShellExecute = false;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();

                result = proc.StandardOutput.ReadToEnd();

                if (update != null)
                {
                    GALibrary.GALogs.SaveLogUpdate(update, "ServiceStop - " + service.Name, "Retorno: " + result, 2, parameter);
                }
                else
                {
                    GALibrary.GALogs.SaveLogProcedure(procedureschedule, "ServiceStop - " + service.Name, "Retorno: " + result, 2, parameter);
                }
            }
            catch (Exception error)
            {
                if (update != null)
                {
                    GALibrary.GALogs.SaveLogUpdate(update, "ServiceStop  - " + service.Name, error.ToString(), 1, parameter);
                }
                else
                {
                    GALibrary.GALogs.SaveLogProcedure(procedureschedule, "ServiceStop  - " + service.Name, error.ToString(), 1, parameter);
                }
                return error.ToString();
            }

            return result;
        }


        public static String UpdateSQL(UpdateGA update, int sqlId)
        {
            Models.GAContext context = new Models.GAContext();
            GALibrary.Models.Parameter parameter = context.Parameter.FirstOrDefault();

            SQL sql = context.SQL.First(x => x.Id == sqlId);
            String log = null;

            GALibrary.GALogs.SaveLogUpdate(update, "SQL", "Procedimento - " + sql.Name, 2, parameter);

            //2 = Text
            //1 = File
            if (sql.Type == 2)
            {
                try
                {
                    log = UpdateSQLCommand(sql.SQLScript, sql);
                    GALibrary.GALogs.SaveLogUpdate(update, "SQL", "SQL - " + sql.SQLScript + " - " + log, 2, parameter);
                }
                catch (Exception error)
                {
                    log = error.ToString();
                    GALibrary.GALogs.SaveLogUpdate(update, "SQL", log, 1, parameter);
                    return log;
                }
            }
            else
            {
                try
                {
                    String pathUpdate = context.Parameter.First().PathUpdate;

                    if (!pathUpdate.EndsWith("\\")) pathUpdate += "\\" + "SQL" + "\\";
                    pathUpdate += sql.Date.Year.ToString().PadLeft(4, '0') + "\\" + sql.Date.Month.ToString().PadLeft(2, '0') + "\\" + sql.Id + "\\";


                    //descompactar arquivo

                    try
                    {
                        ZipFile.ExtractToDirectory(pathUpdate + "sql.zip", pathUpdate);
                    }
                    catch (Exception error)
                    {
                        log = error.ToString();
                        GALibrary.GALogs.SaveLogUpdate(update, "SQL", "SQL - Erro ao descompactar arquivo - " + log, 1, parameter);
                    }


                    String[] fileEntries = System.IO.Directory.GetFiles(pathUpdate, "*.sql");
                    Array.Sort(fileEntries);

                    //Busca todos os SQLs do diretório
                    foreach (string fileName in fileEntries)
                    {
                        try
                        {
                            //Busca o encode do arquivo para evitar erros de acentuação
//                            var encode =  GALibrary.GAFiles.GetEncoding(fileName);
                            String sqlScript = System.IO.File.ReadAllText(fileName, CodePagesEncodingProvider.Instance.GetEncoding(1252));

                            System.IO.FileInfo sqlScriptInfo = new System.IO.FileInfo(fileName);

                            log = "Script: " + sqlScript + " - " + UpdateSQLCommand(sqlScript, sql);
                            GALibrary.GALogs.SaveLogUpdate(update, "SQL", "SQL - " + sqlScriptInfo.Name + " - " + log, 2, parameter);
                        }
                        catch (Exception error)
                        {
                            log = error.ToString();
                            GALibrary.GALogs.SaveLogUpdate(update, "SQL", "SQL - " + fileName + " - " + log, 1, parameter);
                            return log;
                        }
                    }

                    //Apaga arquivos SQL após a atualização e deixa somente o zip
                    System.IO.DirectoryInfo f = new System.IO.DirectoryInfo(pathUpdate);
                    System.IO.FileInfo[] a = f.GetFiles();
                    for (int i = 0; i < a.Length; i++)
                    {
                        if (a[i].Name.ToUpper().EndsWith(".SQL"))
                            a[i].Delete();
                    }

                }
                catch (Exception error)
                {
                    log = error.ToString();
                    GALibrary.GALogs.SaveLogUpdate(update, "SQL", log, 1, parameter);
                    return log;
                }
            }

            return log;
        }

        private static String UpdateSQLCommand(String sqlScript, SQL sql)
        {
            Models.GAContext context = new Models.GAContext();

            String pathTemp = context.Parameter.First().PathTemp;
            DatabaseGA database = context.DatabaseGA.Include(x => x.DatabaseConnection).First(x => x.Id == sql.DatabaseId);
            String comandoCompleto = "";
            String result = "";
            Guid id = Guid.NewGuid();

            if (!System.IO.Directory.Exists(pathTemp)) System.IO.Directory.CreateDirectory(pathTemp);
            if (!pathTemp.EndsWith("\\")) pathTemp += "\\" + id.ToString() + ".sql";

            comandoCompleto = database.DatabaseConnection.SQLImportCommand;
            comandoCompleto = comandoCompleto.Replace("usuarioDeAcesso", GALibrary.GACrypto.Base64Decode(database.DatabaseUser));
            comandoCompleto = comandoCompleto.Replace("senhaDeAcesso", GALibrary.GACrypto.Base64Decode(database.DatabasePassword));
            comandoCompleto = comandoCompleto.Replace("enderecoServidor", database.Server);
            comandoCompleto = comandoCompleto.Replace("nomeBancoDeDados", database.DatabaseName);
            comandoCompleto = comandoCompleto.Replace("portaBancoDeDados", database.Port.ToString());

            comandoCompleto = comandoCompleto.Replace("SQLScript", pathTemp);

            try
            {

                //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                System.IO.File.AppendAllText(pathTemp, sqlScript, CodePagesEncodingProvider.Instance.GetEncoding(1252));

                //Ajusta o charset para que a acentuação funcione corretamente
                //Ajusta o formato da data
                result = database.DatabaseConnection.SQLImportCommand + System.Environment.NewLine;
                result += OSCommand("set NLS_DATE_FORMAT=DD/MM/RR && " + comandoCompleto);
                //http://possoajudar.unimed-ners.com.br/ti/_layouts/15/start.aspx#/Catlogo%20de%20Servios%20%20Viso%20TI/Banco%20de%20Dados%20Oracle%20-%20Caracteres%20de%20acentua%C3%A7%C3%A3o%20inv%C3%A1lidos.aspx

            }
            catch (Exception error)
            {
                result += System.Environment.NewLine + "Comando: " + database.DatabaseConnection.SQLImportCommand + " - " + error.ToString();
            }
            finally
            {
                if (System.IO.File.Exists(pathTemp))
                {
                    Thread.Sleep(2000);
                    System.IO.File.Delete(pathTemp);
                }
            }

            return result;

        }

        public static string OSCommand(String command)
        {
            System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/c \"" + command + "\"");
            //procStartInfo.StandardOutputEncoding = Encoding.UTF8;
            //procStartInfo.StandardErrorEncoding = Encoding.UTF8;
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;
            procStartInfo.CreateNoWindow = true;


            procStartInfo.UseShellExecute = false;
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();

            String result = "";
            result += proc.StandardOutput.ReadToEnd();
            result += proc.StandardError.ReadToEnd();

            return result;
        }



        public static void ApagaArquivosAntigos()
        {

            Models.GAContext context = new Models.GAContext();

            String folderUpdate = context.Parameter.First().PathUpdate;
            String folderBackup = context.Parameter.First().PathBackup;

            int retentionTime = context.Parameter.FirstOrDefault().RetentionTime.Value;
            DateTime dataExpurgo = DateTime.Now.AddDays(-retentionTime);

            //Busca os SQLs e Arquivos que são tempaltes para não apaga-los
            List<int> fileTemplate = context.UpdateSteps.Include(x => x.Update).Where(x => x.Type == 1 && x.Update.Template == true).Select(x => x.ProcessId).ToList();
            List<int> sqlTemplate = context.UpdateSteps.Include(x => x.Update).Where(x => x.Type == 2 && x.Update.Template == true).Select(x => x.ProcessId).ToList();

            List<UpdateGA> updateList = context.UpdateGA.Where(x => x.Date < dataExpurgo && x.FilesRemoved == false && x.Template == false).ToList();

            List<File> fileList = context.File.Where(x => x.Date < dataExpurgo && x.FilesRemoved == false && !fileTemplate.Contains(x.Id)).ToList();
            List<SQL> sqlList = context.SQL.Where(x => x.Date < dataExpurgo && x.FilesRemoved == false && !sqlTemplate.Contains(x.Id)).ToList();

            

            //Apaga backups
            foreach (UpdateGA update in updateList)
            {
                String pastaBackup = folderBackup + "\\" + update.Date.Year.ToString().PadLeft(4, '0') + "\\" + update.Date.Month.ToString().PadLeft(2, '0') + "\\" + update.Id;
                if (System.IO.Directory.Exists(pastaBackup)) System.IO.Directory.Delete(pastaBackup, true);

                update.FilesRemoved = true;
                context.Entry(update).State = EntityState.Modified;
                context.SaveChanges();
            }


            //apaga arquivos
            foreach (File file in fileList)
            {
                String pastaFiles = folderUpdate + "\\Files\\" + file.Date.Year.ToString().PadLeft(4, '0') + "\\" + file.Date.Month.ToString().PadLeft(2, '0') + "\\" + file.Id;
                if (System.IO.Directory.Exists(pastaFiles)) System.IO.Directory.Delete(pastaFiles, true);

                file.FilesRemoved = true;
                context.Entry(file).State = EntityState.Modified;
                context.SaveChanges();
            }

            //Apaga arquivos SQL
            foreach (SQL sql in sqlList)
            {
                String pastaSQL = folderUpdate + "\\SQL\\" + sql.Date.Year.ToString().PadLeft(4, '0') + "\\" + sql.Date.Month.ToString().PadLeft(2, '0') + "\\" + sql.Id;
                if (System.IO.Directory.Exists(pastaSQL)) System.IO.Directory.Delete(pastaSQL, true);

                sql.FilesRemoved = true;
                context.Entry(sql).State = EntityState.Modified;
                context.SaveChanges();
            }

        }


        /*
        public static void VerificaAtualizacaoComRDM()
        {

            //Lista todas atualizações pendentes de aprovação
            List<UpdateGA> updates = context.UpdateGA.Where(x => x.Approved == false).ToList();

            foreach (UpdateGA update in updates)
            {

                try
                {
                    Boolean aprovado = AprovaAtualizacaoComRDM(update);
                    if (aprovado)
                    {
                        update.Approved = true;
                        update.AprovedDate = DateTime.Now;
                        context.Entry(update).State = EntityState.Modified;
                        context.SaveChanges();
                        SaveLog("Busca RDM TraceGP", "RDM " + update.RDM + " configurada com status " + update.Approved, 2);
                    }
                }
                catch (Exception erro)
                {
                    SaveLog("Busca RDM TraceGP", "Erro ao buscar informações do TraceGP: " + erro.ToString(), 1);
                }
            }
        }
        */

        public static void SaveLog(String step, String message, int logLevel)
        {
            Models.GAContext context = new Models.GAContext();

            String logFolder = context.Parameter.First().PathLog;
            int logLevelDatabase = context.Parameter.First().LogLevelApplication;

            String logName = "";
            String logLevelName = "Message";

            if (logLevel == 1) logLevelName = "Error";

            DateTime today = DateTime.Now;
            logName = "GA-" + today.Year.ToString().PadLeft(4, '0') + "-" + today.Month.ToString().PadLeft(2, '0') + "-" + today.Day.ToString().PadLeft(2, '0') + ".log";

            try
            {
                if (!logFolder.EndsWith("\\")) logFolder += "\\";
                System.IO.Directory.CreateDirectory(logFolder);

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


        /*
        private static Boolean AprovaAtualizacaoComRDM(UpdateGA update)
        {

            var builder = new ConfigurationBuilder().SetBasePath(System.IO.Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfigurationRoot configuration = builder.Build();


            var traceGP = new Models.TraceGP();
            configuration.GetSection("TraceGP").Bind(traceGP);

            String stringConnection = traceGP.TraceGPConexao;
            String select = traceGP.TraceGPSelect;


            OracleConnection conn;
            OracleCommand cmd = null;
            conn = new OracleConnection(stringConnection);

            conn.Open();
            cmd = new OracleCommand(select, conn);
            cmd.Parameters.Clear();
            cmd.Parameters.Add(":RDM", update.RDM);

            OracleDataReader reader = cmd.ExecuteReader();
            reader.Read();

            if (!reader.HasRows)
            {
                return false;
            }

            int resultado = Convert.ToInt16(reader.GetValue(0).ToString());

            conn.Close();

            if (resultado == 87 || resultado == 60 || resultado == 76 || resultado == 33)
            {
                return true;
            }


            return false;
        }*/


        public static void ExecuteProcedure(ProcedureSchedule procedureSchedule)
        {
            Models.GAContext context = new Models.GAContext();

            GALibrary.Models.Parameter parameter = context.Parameter.FirstOrDefault();
            var steps = context.ProcedureSteps.Where(x => x.ProcedureID == procedureSchedule.ProcedureID).ToList().OrderBy(x => x.Order);

            //Atualiza status para atualizando (3)
            procedureSchedule.Status = 3;
            context.Entry(procedureSchedule).State = EntityState.Modified;
            context.SaveChanges();

            try
            {
                foreach (ProcedureSteps step in steps)
                {

                    switch (step.Type)
                    {
                        case 3:
                            GALibrary.GALogs.SaveLogProcedure(procedureSchedule, "Command", "Inicio da execucao do comando", 2, parameter);
                            UpdateCommand(null, procedureSchedule, step.ProcessId);
                            GALibrary.GALogs.SaveLogProcedure(procedureSchedule, "Command", "Fim da execucao do comando", 2, parameter);
                            break;
                        case 4:
                            GALibrary.GALogs.SaveLogProcedure(procedureSchedule, "ServiceStart", "Inicio do start de serviço", 2, parameter);
                            UpdateServiceStart(null, procedureSchedule, step.ProcessId);
                            GALibrary.GALogs.SaveLogProcedure(procedureSchedule, "ServiceStart", "Fim do start de serviço", 2, parameter);
                            break;
                        case 5:
                            GALibrary.GALogs.SaveLogProcedure(procedureSchedule, "ServiceStop", "Inicio da parada de serviço", 2, parameter);
                            UpdateServiceStop(null, procedureSchedule, step.ProcessId);
                            GALibrary.GALogs.SaveLogProcedure(procedureSchedule, "ServiceStop", "Fim da parada de serviço", 2, parameter);
                            break;

                    }

                }

                procedureSchedule.Status = 1;
                context.Entry(procedureSchedule).State = EntityState.Modified;
                context.SaveChanges();
            }

            catch
            {
                procedureSchedule.Status = 2;
                context.Entry(procedureSchedule).State = EntityState.Modified;
                context.SaveChanges();
            }
        }

    }
}
