using GALibrary.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GAService
{
    class Program
    {


        static void Main(string[] args)
        {

            //UpdateGA update =  context.UpdateGA.First(x => x.Id == 348);
            //CallSendMail(update);

            
            while (true)
            {
                try
                {
                    ThreadPool.QueueUserWorkItem(ExecutaUpdate);
                    ThreadPool.QueueUserWorkItem(ExecutaProcedure);
                    ThreadPool.QueueUserWorkItem(RemoveArquivosAntigos);
                    //ThreadPool.QueueUserWorkItem(VerificaAtualizacaoComRDM);
                }
                catch(Exception erro) {
                    String currentDirectory = System.IO.Directory.GetCurrentDirectory();
                    System.IO.File.AppendAllText(currentDirectory + "\\error.log", DateTime.Now.ToShortTimeString() + " - " + erro.ToString() + System.Environment.NewLine);
                }
                Thread.Sleep(60000);
            }
            
            
        }

        /*
        static void VerificaAtualizacaoComRDM(Object stateInfo) {
            try
            {
                Lib.Updates.VerificaAtualizacaoComRDM();
            }
            catch (Exception erro)
            {
                String currentDirectory = System.IO.Directory.GetCurrentDirectory();
                System.IO.File.AppendAllText(currentDirectory + "\\error.log", DateTime.Now.ToShortTimeString() + " - " + erro.ToString() + System.Environment.NewLine);
            }
        }
        */


//As 02:00 executa a rotina que remove arquivos antigos
        static void RemoveArquivosAntigos(Object stateInfo) {
            if (DateTime.Now.Hour == 2 && DateTime.Now.Minute == 0)
            {
                try
                {
                    Lib.Updates.ApagaArquivosAntigos();
                }
                catch (Exception erro)
                {
                    String currentDirectory = System.IO.Directory.GetCurrentDirectory();
                    System.IO.File.AppendAllText(currentDirectory + "\\error.log", DateTime.Now.ToShortTimeString() + " - " + erro.ToString() + System.Environment.NewLine);
                }
            }
        }


        static void ExecutaProcedure(Object stateInfo)
        {
            List<ProcedureSchedule> procedures = Lib.Updates.GetProcedureSchedules();

            foreach (ProcedureSchedule procedure in procedures)
            {
                try
                {
                    ThreadPool.QueueUserWorkItem(ExecutaProcedureTask, procedure);
                }
                catch (Exception erro)
                {
                    String currentDirectory = System.IO.Directory.GetCurrentDirectory();
                    System.IO.File.AppendAllText(currentDirectory + "\\error.log", DateTime.Now.ToShortTimeString() + " - " + erro.ToString() + System.Environment.NewLine);
                }
            }
        }

        static void ExecutaProcedureTask(Object stateInfo)
        {
            Models.GAContext context = new Models.GAContext();
            Parameter parameter = context.Parameter.FirstOrDefault();

            ProcedureSchedule procedureSchedule = (ProcedureSchedule)stateInfo;
            GALibrary.GALogs.SaveLogProcedure(procedureSchedule, "Start", "Inicio do procedimento", 2, parameter);
            Lib.Updates.ExecuteProcedure(procedureSchedule);
        }


        static void ExecutaUpdate(Object stateInfo)
        {
            List<UpdateGA> updates = Lib.Updates.GetUpdates();

            foreach (UpdateGA update in updates)
            {
                try
                {
                    Console.WriteLine("Executando atualização: " + update.Name);
                    ThreadPool.QueueUserWorkItem(ExecutaUpdateTask, update);
                }
                catch (Exception erro)
                {
                    String currentDirectory = System.IO.Directory.GetCurrentDirectory();
                    System.IO.File.AppendAllText(currentDirectory + "\\error.log", DateTime.Now.ToShortTimeString() + " - " + erro.ToString() + System.Environment.NewLine);
                }

            }
        }

        static void ExecutaUpdateTask(Object stateInfo)
        {
            Models.GAContext context = new Models.GAContext();
            Parameter parameter = context.Parameter.FirstOrDefault();

            UpdateGA update = (UpdateGA)stateInfo;
            GALibrary.GALogs.SaveLogUpdate(update, "Start", "Inicio da atualização", 2, parameter);
            UpdateGA updateStatus = Lib.Updates.UpdateApplication(update.Id);

            if (update.AlertUser)
            {
                CallSendMail(updateStatus);
            }

        }


        static void CallSendMail(UpdateGA update) {
            Models.GAContext context = new Models.GAContext();

            //Busca configuração de E-mails e LDAP
            var builder = new ConfigurationBuilder().SetBasePath(System.IO.Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfigurationRoot configuration = builder.Build();

            var emailSettings = new EmailSettings();
            configuration.GetSection("EmailSettings").Bind(emailSettings);

            var ldap = new Ldap();
            configuration.GetSection("Ldap").Bind(ldap);

            //Busca parametros da aplicação
            Parameter parameter = context.Parameter.FirstOrDefault();

            //Envia e-mail
            GALibrary.GAMail.SendMail(null, 1, update, emailSettings, parameter, "", ldap);
        }
   
    }
}
