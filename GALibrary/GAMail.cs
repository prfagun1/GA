using System;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace GALibrary
{
    public class GAMail
    {
        //Envio de e-mail
        //Tipo:
        //1 = Envio de log para quem criou atualização;
        //2 = Envio de comunicado para grupo

        public static void SendMail(string toEmail, int type, GALibrary.Models.UpdateGA update, GALibrary.Models.EmailSettings emailSettings, GALibrary.Models.Parameter parameter, String aplicacao, Models.Ldap ldap)
        {
            try
            {
                string body = "";
                Models.UserAD userAD = GALibrary.GAAD.GetADUserData(ldap, update.User);


                if (toEmail == null)
                {
                    toEmail = userAD.Mail;
                }

                MailMessage mail = new MailMessage();

                body = SendMailBody(type, update, aplicacao, userAD.FullName);
                LinkedResource header = new LinkedResource(parameter.MailHeader);
                header.ContentId = "header";
                LinkedResource footer = new LinkedResource(parameter.MailFooter);
                footer.ContentId = "footer";


                mail.To.Add(new MailAddress(toEmail));
                AlternateView bodyImages = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);
                bodyImages.LinkedResources.Add(header);
                bodyImages.LinkedResources.Add(footer);
                mail.AlternateViews.Add(bodyImages);


                mail.Subject = $"GA - {aplicacao} - {update.Name}";
                mail.Body = body;
                mail.IsBodyHtml = true;
                mail.Priority = MailPriority.High;
                mail.From = new MailAddress(emailSettings.From);



                SmtpClient smtp = new SmtpClient(emailSettings.Server, emailSettings.Port);

                smtp.Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password);
                smtp.EnableSsl = false;
                smtp.Send(mail);
                smtp.Dispose();
                mail.Dispose();

                GALogs.SaveLog("Email", "email enviado com sucesso para " + toEmail, 2, parameter);

            }
            catch (Exception erro)
            {
                GALogs.SaveLog("Email", "Erro ao enviar e-mail a atualização - " + update.Name + " - " + erro, 1, parameter);
            }


        }


        //Type == 1 resultado da conclusão
        //Type == 2 comunicado
        private static String SendMailBody(int type, GALibrary.Models.UpdateGA update, String aplicacao, String userFullName)
        {

            String body = "<html>";
            body += $"   <body>";
            body += $"       <table width=689>";
            body += $"           <tr>";
            body += $"               <td>";
            body += $"                   <div align=\"center\">";
            body += $"                      <img src=cid:header alt=\"Header\"";
            body += "                       <br />&nbsp;";
            body += $"                      <h2> <p> <b> Atualização do sistema {aplicacao} </p> </b> </h2>";
            body += "                    </div>";

            if (type == 1)
            {
                String status;
                if (update.Status == 1)
                {
                    status = "sucesso";
                }
                else
                {
                    status = "erros";
                }

                body += $"<p>";
                body += $"A atualização {update.Name} foi concluída com {status}.";
                body += $"</p>";
                body += $"<p>";
                body += $"Acesse o link ao lado para mais detalhes: <a href=\"https://ga.unimednordesters.com.br/UpdatesGA/Details/{update.Id}\">https://ga.unimednordesters.com.br/UpdatesGA/Details/{update.Id}</a> ";
                body += $"</p>";
            }
            else
            {
                body += $"<p> &emsp; <b>Quando: </b>{update.Schedule.Value.ToShortDateString()} às {update.Schedule.Value.ToShortTimeString()}</p>";
                body += $"<p> &emsp; <b>Responsável: </b>{userFullName}</p>";
                if (update.Description == null)
                {
                    body += $"<p> &emsp; <b>Procedimento: </b> Não foi informado na atualização.</p>";
                }
                else
                {
                    body += $"<p> &emsp; <b>Procedimento: </b>{update.Description.Replace("\r\n", "<br />&emsp; ")}</p>";
                }
                body += $"</p>";
            }


            body += $"                   <p>&emsp;<p>";
            body += $"                   <div align=\"center\">";
            body += "                       <p> <img src=cid:footer alt=\"Footer\"> </p>";
            body += "                   </div>";
            body += "               </td>";
            body += "           </tr>";
            body += "       </table>";
            body += "   </body>";
            body += "</html>";

            return body;
        }


    }
}
