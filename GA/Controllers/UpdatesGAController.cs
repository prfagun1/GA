using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GALibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using X.PagedList;
using System.Text;
using System.IO;
using Microsoft.Extensions.Options;

namespace GA.Controllers
{
    public class UpdatesGAController : Controller
    {
        private readonly GAContext _context;
        private readonly IOptions<EmailSettings> emailSettings;
        private readonly IOptions<Ldap> ldap;

        private readonly SelectList tiposAtualizacoes = new SelectList(new[]    {
                                                        new { ID = "1", Name = "Arquivos para apagar" },
                                                        new { ID = "2", Name = "Arquivos para copiar" },
                                                        new { ID = "3", Name = "Comandos" },
                                                        new { ID = "4", Name = "Serviço iniciar" },
                                                        new { ID = "5", Name = "Serviço parar" },
                                                        new { ID = "6", Name = "SQL" },
                                                    }, "ID", "Name", 0);



        public UpdatesGAController(GAContext context, IOptions<EmailSettings> emailSettings, IOptions<Ldap> ldap)
        {
            _context = context;
            this.emailSettings = emailSettings;
            this.ldap = ldap;

        }


        [Authorize(Policy = "Vizualização")]
        public async Task<IActionResult> Index(int? page, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado, int? updateApplicationId, int? updateStatus, String updateCriadoPor, String dataFinal, String dataInicial, Boolean? updateApproved, int? updateDemanda, int? updateTipo, int? updateTemplate)
        {

            try
            {
                var update = _context.UpdateGA.Include(x => x.Application).AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;


                if (!string.IsNullOrEmpty(searchString))
                {
                    update = update.Where(x => x.Name.Contains(searchString));
                }

                if (updateDemanda != null)
                {
                    update = update.Where(x => x.Demanda == updateDemanda);
                }

                if (updateTipo != null && updateTipo >= 0)
                {
                    update = update.Where(x => x.Manual == Convert.ToBoolean(updateTipo));
                }

                if (updateTemplate != null && updateTemplate >= 0)
                {
                    update = update.Where(x => x.Template == Convert.ToBoolean(updateTemplate));
                }

                if (!string.IsNullOrEmpty(updateCriadoPor))
                {
                    update = update.Where(x => x.User.Contains(updateCriadoPor));
                }

                if (updateApplicationId != null)
                {
                    update = update.Where(x => x.ApplicationId == updateApplicationId);
                }

                if (dataInicial != null)
                {
                    update = update.Where(x => x.Schedule >= Convert.ToDateTime(dataInicial));
                }

                if (dataFinal != null)
                {
                    update = update.Where(x => x.Schedule <= Convert.ToDateTime(dataFinal).AddHours(23).AddMinutes(59).AddSeconds(59));
                }

                if (updateStatus >=  0)
                {
                    update = update.Where(x => x.Status == updateStatus);
                }

                if (updateApproved != null)
                {
                    update = update.Where(x => x.Approved == updateApproved);
                }
            

                if (sortOrder != null && sortOrderOld != null && sortOrder == sortOrderOld && !sortOrder.EndsWith("Desc") && pageNumber == 1)
                {
                    sortOrder += "Desc";
                }

                ViewBag.sortOrder = sortOrder;
                ViewBag.sortOrderOld = sortOrder;
                ViewBag.searchString = searchString;
                ViewBag.updateCriadoPor = updateCriadoPor;
                ViewBag.updateDemanda = updateDemanda;
                ViewBag.updateApproved = updateApproved;
                ViewBag.dataFinal = dataFinal;
                ViewBag.dataInicial = dataInicial;




                switch (sortOrder)
                {
                    case "Demanda":
                        update = update.OrderBy(x => x.Demanda);
                        break;
                    case "DemandaDesc":
                        update = update.OrderByDescending(x => x.Demanda);
                        break;
                    case "Nome":
                        update = update.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        update = update.OrderByDescending(x => x.Name);
                        break;
                    case "Agendamento":
                        update = update.OrderBy(x => x.Schedule);
                        break;
                    case "AgendamentoDesc":
                        update = update.OrderByDescending(x => x.Schedule);
                        break;
                    case "Usuario":
                        update = update.OrderBy(x => x.User);
                        break;
                    case "UsuarioDesc":
                        update = update.OrderByDescending(x => x.User);
                        break;
                    case "Aplicacao":
                        update = update.OrderBy(x => x.Application.Name);
                        break;
                    case "AplicacaoDesc":
                        update = update.OrderByDescending(x => x.Application.Name);
                        break;
                    case "Tipo":
                        update = update.OrderBy(x => x.Manual);
                        break;
                    case "TipoDesc":
                        update = update.OrderByDescending(x => x.Manual);
                        break;
                    case "Status":
                        update = update.OrderBy(x => x.Status);
                        break;
                    case "StatusDesc":
                        update = update.OrderByDescending(x => x.Status);
                        break;
                    default:
                        update = update.OrderByDescending(x => x.Date);
                        break;
                }

                ViewBag.update = await update.ToPagedListAsync(pageNumber, itensPages);


                ViewBag.updateTipo = new SelectList(new[] { new { ID = "-1", Name = "Todos" }, new { ID = "0", Name = "Automático" }, new { ID = "1", Name = "Manual" }, }, "ID", "Name", updateTipo);
                ViewBag.updateTemplate = new SelectList(new[] { new { ID = "-1", Name = "Todos" }, new { ID = "0", Name = "Não" }, new { ID = "1", Name = "Sim" }, }, "ID", "Name", updateTemplate);
                ViewBag.updateStatus = new SelectList(new[] { new { ID = "-1", Name = "Todos" }, new { ID = "0", Name = "Pendente" }, new { ID = "1", Name = "Realizada com sucesso" }, new { ID = "2", Name = "Realizada com erros" }, new { ID = "4", Name = "Rascunho" }, }, "ID", "Name", updateStatus);
                ViewBag.updateApplicationId = new SelectList(_context.Application.OrderBy(x => x.Name), "Id", "Name", updateApplicationId);

                GALibrary.GALogs.SaveLog("Update", "Pesquisa de atualizacoes realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }

                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Update", "Erro ao pesquisar atualizacoes pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Vizualização")]
        public ActionResult LoadDropDownList(int id, int applicationID)
        {
            //Carrega os mesmos dados para ServiceStart e ServiceStop
            if (id == 5) id = 4;
            String usuario = User.Identity.Name;

            switch (id)
            {
                case 1:
                    return Json(new SelectList(_context.FileDelete.Where(x => x.Enable && x.ApplicationId == applicationID && x.User == usuario).ToList().OrderBy(x => x.Name), "Id", "Name"), new JsonSerializerSettings());
                case 2:
                    return Json(new SelectList(_context.File.Where(x => x.Enable && x.ApplicationId == applicationID && x.User == usuario).ToList().OrderBy(x => x.Name), "Id", "Name"), new JsonSerializerSettings());
                case 3:
                    return Json(new SelectList(_context.Command.Where(x => x.Enable && x.ApplicationId == applicationID && (x.Type == 1 || x.Type == 2)).ToList().OrderBy(x => x.Name), "Id", "Name"), new JsonSerializerSettings());
                case 4:
                    return Json(new SelectList(_context.Service.Where(x => x.Enable && x.ApplicationId == applicationID).ToList().OrderBy(x => x.Name), "Id", "Name"), new JsonSerializerSettings());
                case 6:
                    return Json(new SelectList(_context.SQL.Include(x => x.Database).Where(x => x.Enable && x.Database.ApplicationId == applicationID && x.User == usuario).OrderBy(x => x.Name), "Id", "Name"), new JsonSerializerSettings());
            }
            return View();
        }



        [Authorize(Policy = "Vizualização")]
        public ActionResult LoadTemplate(int templateId)
        {
            return Json(_context.UpdateGA.Where(x => x.Id == templateId), new JsonSerializerSettings());
        }



        [Authorize(Policy = "Vizualização")]
        public async Task<IActionResult> Details(int? id)
        {
            Boolean backup = true;

            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var updateGA = await _context.UpdateGA.Include(x => x.AlertMail).Include(x => x.UpdateSteps).Include(u => u.Application).Include(x => x.Application.Environment).FirstOrDefaultAsync(m => m.Id == id);
                if (updateGA == null)
                {
                    return NotFound();
                }

                ViewBag.StepResult = this.GetUpdateStepsString(updateGA.UpdateSteps.OrderBy(x => x.Order).ToList());

                String pathBackup = _context.Parameter.First().PathBackup;
                if (!pathBackup.EndsWith("\\")) pathBackup += "\\";


                //Verifica se existe backup
                pathBackup += updateGA.Schedule.GetValueOrDefault().Year.ToString().PadLeft(4, '0') + "\\" + updateGA.Schedule.GetValueOrDefault().Month.ToString().PadLeft(2, '0') + "\\" + updateGA.Id + "\\";
                String name = "Backup.zip";
                String pathZip = pathBackup;
                if (!System.IO.File.Exists(pathZip + name))
                {
                    backup = false;
                }

                ViewBag.Backup = backup;

                GALibrary.GALogs.SaveLog("Update", "Vizualizacao dos detalhes da atualizacao " + updateGA.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View(updateGA);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Update", "Erro ao vizualizar detalhes da atualicaxao com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }
        }


        [Authorize(Policy = "Vizualização")]
        private String GetUpdateStepsString(List<UpdateSteps> steps)
        {
            String stepResult = "";

            foreach (UpdateSteps step in steps)
            {
                int order = step.Order + 1;
                try
                {
                    switch (step.Type)
                    {
                        case 1:
                            FileDelete fileDelete = _context.FileDelete.Find(step.ProcessId);
                            stepResult += order + " - Arquivos para apagar - " + "<a href = \"/filesDelete/Details/" + fileDelete.Id + "\" >" + fileDelete.Name + "</a>" + "<br />";
                            break;
                        case 2:
                            GALibrary.Models.File file = _context.File.Find(step.ProcessId);
                            stepResult += order + " - Arquivos para copiar - " + "<a href = \"/files/Details/" + file.Id + "\" >" + file.Name + "</a>" + "<br />";
                            break;
                        case 3:
                            Command command = _context.Command.Find(step.ProcessId);
                            stepResult += order + " - Comandos - " + "<a href = \"/commands/Details/" + command.Id + "\" >" + command.Name + "</a>" + "<br />";
                            break;
                        case 4:
                            Service serviceStart = _context.Service.Find(step.ProcessId);
                            stepResult += order + " - Serviço iniciar - " + "<a href = \"/services/Details/" + serviceStart.Id + "\" >" + serviceStart.Name + "</a>" + "<br />";
                            break;
                        case 5:
                            Service serviceStop = _context.Service.Find(step.ProcessId);
                            stepResult += order + " - Serviço parar - " + "<a href = \"/services/Details/" + serviceStop.Id + "\" >" + serviceStop.Name + "</a>" + "<br />";
                            break;
                        case 6:
                            SQL sql = _context.SQL.Find(step.ProcessId);
                            stepResult += order + " - SQL - " + "<a href = \"/sqls/Details/" + sql.Id + "\" >" + sql.Name + "</a>" + "<br />";
                            break;
                    }
                }
                catch
                {
                    stepResult += order + " - Processo apagado do banco" + "<br />";
                }

            }

            if (stepResult.Length > 6) {
                return stepResult.Substring(0, stepResult.Length - 6);
            }

            return stepResult;
        }



        [Authorize(Policy = "Atualização")]
        public IActionResult Create()
        {

            try
            {
                ViewBag.updateApplicationId = new SelectList(_context.Application.Where(x => x.Enable && x.EnvironmentId == 1).OrderBy(x => x.Name), "Id", "Name");
                ViewBag.updateType = tiposAtualizacoes;
                ViewBag.updateFilesDelete = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateFiles = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateCommands = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateServices = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateSQLs = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateEnvironmentId = new SelectList(_context.Environment.Where(x => x.Enable == true).OrderBy(x => x.Name), "Id", "Name");
                ViewBag.updateProcessSelected = new MultiSelectList(String.Empty, "Text", "Value");
                ViewBag.updateTemplate = new SelectList(_context.UpdateGA.Where(x => x.Template).OrderBy(x => x.Name).OrderBy(x => x.Name), "Id", "Name");
                ViewBag.updateEmails = new SelectList(_context.AlertMail.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name",_context.AlertMail.Where(x => x.Default).First().Id);

                ViewBag.updateManual = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" } }, "ID", "Name", false);

                GALibrary.GALogs.SaveLog("Update", "Inicio do cadastro de uma atualizacao pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View();

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Update", "Erro ao iniciar cadastro de uma atualizacao pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Atualização")]
        public async Task AtualizaSteps(String[] updateProcessSelected, int updateGAId) {
            int i = 0;

            List<UpdateSteps> updateStepsList = new List<UpdateSteps>();

            foreach (String process in updateProcessSelected)
            {
                String[] option = process.Split('-');

                int type = Convert.ToInt32(option[0].Trim());
                int processId = Convert.ToInt32(option[1].Trim());

                UpdateSteps updateSteps = new UpdateSteps();
                updateSteps.Order = i;
                updateSteps.ProcessId = processId;
                updateSteps.Type = type;
                updateSteps.UpdateId = updateGAId;

                i++;

                //Caso seja anexado um arquivo para apagar, copiar ou um SQL desativa o mesmo para não aparecer mais na seleção.
                if (type == 1)
                {
                    FileDelete fileDelete = _context.FileDelete.Find(processId);
                    fileDelete.Enable = false;
                    _context.Update(fileDelete);
                    await _context.SaveChangesAsync();
                }
                if (type == 2)
                {
                    GALibrary.Models.File file = _context.File.Find(processId);
                    file.Enable = false;
                    _context.Update(file);
                    await _context.SaveChangesAsync();
                }

                if (type == 6)
                {
                    SQL sql = _context.SQL.Find(processId);
                    sql.Enable = false;
                    _context.Update(sql);
                    await _context.SaveChangesAsync();
                }

                updateStepsList.Add(updateSteps);
            }

            await _context.UpdateSteps.AddRangeAsync(updateStepsList);
            await _context.SaveChangesAsync();
        }


        [Authorize(Policy = "Atualização")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,ApplicationId,Enable,Schedule,Approved,AprovedDate,Status,Date,User,Demanda,FilesRemoved,Template,AlertmailId,AlertUser")] UpdateGA updateGA, String[] updateProcessSelected, int updateEnvironmentId, string button, int updateTemplate, int updateEmails, bool updateManual)
        {
            try
            {
                int tipo = 0;
                switch (button) {
                    case "Agendar a atualização":
                        tipo = 1;
                        break;
                    case "Salvar como rascunho":
                        tipo = 2;
                        break;
                    case "Salvar como template":
                        tipo = 3;
                        break;
                }


                Boolean valid = true;

                updateGA.Date = DateTime.Now;
                updateGA.User = User.Identity.Name;
                updateGA.Enable = true;
                updateGA.Approved = true;
                updateGA.Status = 0;
                updateGA.FilesRemoved = false;
                updateGA.AlertmailId = updateEmails;
                updateGA.Manual = Convert.ToBoolean(updateManual);


                if ((tipo == 1 || tipo == 3) && !updateManual ) {

                    if (updateProcessSelected == null || updateProcessSelected.Length == 0)
                    {
                        ModelState.AddModelError("UpdateSteps", "É preciso cadastrar ao menos um processo.");
                    }
                }

                if (tipo == 1)
                {
                    if (updateGA.Schedule < DateTime.Now || updateGA.Schedule == null)
                    {
                        ModelState.AddModelError("Schedule", "A data não pode estar em branco e precisa ser posterior ao horário atual.");
                    }

                    if (updateGA.Demanda == null)
                    {
                        ModelState.AddModelError("Demanda", "É preciso informar o número da demanda.");
                    }
                }

                if (tipo == 2) {
                    updateGA.Status = 4;
                }

                if (tipo == 3) {
                    updateGA.Template = true;
                }


                if (ModelState.IsValid)
                {
                    _context.Add(updateGA);
                    await _context.SaveChangesAsync();
                    _context.Entry(updateGA).GetDatabaseValues();
                    GALibrary.GALogs.SaveLog("Update", "Fim do cadastro da atualizacao " + updateGA.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                    try
                    {
                        await this.AtualizaSteps(updateProcessSelected, updateGA.Id);
                        GALibrary.GALogs.SaveLog("Updatesteps", "Passos da atualização " + updateGA.Name + " gravados com sucesso pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                    }
                    catch (Exception erro)
                    {
                        GALibrary.GALogs.SaveLog("Updatesteps", "Erro ao gravar os passos da atualização " + updateGA.Name + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                        ModelState.AddModelError("", "Erro ao salvar etapas, edite esta atualização ou crie ela novamente");
                        valid = false;
                    }

                    if (valid) {

                        if(tipo == 1 && updateEnvironmentId == 1) {

                            Application application = _context.Application.FirstOrDefault(x => x.Id == updateGA.ApplicationId);
                            Parameter parameter = _context.Parameter.FirstOrDefault();
                            String email = _context.AlertMail.First(x => x.Id == updateGA.AlertmailId).Email;

                            GALibrary.GAMail.SendMail(email, 2, updateGA, (EmailSettings)emailSettings.Value, parameter, application.Name, (Ldap)ldap.Value);
                        }

                        return RedirectToAction("Index");
                    }

                }

                ViewBag.updateType = tiposAtualizacoes;
                ViewBag.updateFilesDelete = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateFiles = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateCommands = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateServices = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateSQLs = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateTemplate = new SelectList(_context.UpdateGA.Where(x => x.Template).OrderBy(x => x.Name), "Id", "Name", updateTemplate);
                ViewBag.updateEmails = new SelectList(_context.AlertMail.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", updateEmails);

                ViewBag.updateManual = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" } }, "ID", "Name", Convert.ToBoolean(updateGA.Manual));

                ViewBag.updateApplicationId = new SelectList(_context.Application.Where(x => x.Enable && x.EnvironmentId == updateEnvironmentId).OrderBy(x => x.Name), "Id", "Name", updateGA.ApplicationId);
                ViewBag.updateEnvironmentId = new SelectList(_context.Environment.Where(x => x.Enable == true).OrderBy(x => x.Name), "Id", "Name", updateEnvironmentId);
                ViewBag.updateProcessSelected = new MultiSelectList(SetUpdateProcessSelectedString(updateProcessSelected), "Text", "Value");
                ViewBag.updateDemanda = updateGA.Demanda;
                ViewBag.updateSchedule = updateGA.Schedule;


                return View(updateGA);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Update", "Erro ao cadastrar atualizacao pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Atualização")]
        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var updateGA = await _context.UpdateGA.Include(x => x.Application).Include(x => x.UpdateSteps).Include(x => x.AlertMail).FirstOrDefaultAsync(x => x.Id == id);
                if (updateGA == null)
                {
                    return NotFound();
                }

                ViewBag.updateApplicationName = _context.Application.First(x => x.Id == updateGA.ApplicationId).Name;
                ViewBag.updateType = tiposAtualizacoes;
                ViewBag.updateFilesDelete = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateFiles = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateCommands = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateServices = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateSQLs = new SelectList(String.Empty, "Text", "Value");

                ViewBag.updateApplicationId = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", updateGA.ApplicationId);
                ViewBag.updateEnvironmentId = new SelectList(_context.Environment.Where(x => x.Enable == true).OrderBy(x => x.Name), "Id", "Name", updateGA.Application.EnvironmentId);
                ViewBag.updateProcessSelected = new MultiSelectList(SetUpdateProcessSelected(updateGA), "Text", "Value");

                ViewBag.updateEmails = new SelectList(_context.AlertMail.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", updateGA.AlertmailId);
                ViewBag.updateManual = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" } }, "ID", "Name", updateGA.Manual);
                ViewBag.updateSchedule = updateGA.Schedule;

                GALibrary.GALogs.SaveLog("Update", "Inicio da edicao da atualizacao " + updateGA.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View(updateGA);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Update", "Erro ao iniciar edicao da atualizacao com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Atualização")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ApplicationId,Enable,Schedule,Approved,AprovedDate,Status,Date,User,Demanda,FilesRemoved,Manual,Template,AlertmailId,AlertUser")] UpdateGA updateGA, String[] updateProcessSelected, int updateEnvironmentId, int updateApplicationId, string button, int updateTemplate, int updateEmails, bool updateManual)
        {

            if (id != updateGA.Id)
            {
                return NotFound();
            }

            try
            {

                int tipo = 0;
                switch (button)
                {
                    case "Agendar a atualização":
                        tipo = 1;
                        break;
                    case "Salvar como rascunho":
                        tipo = 2;
                        break;
                    case "Salvar como template":
                        tipo = 3;
                        break;
                }

                updateGA.Date = DateTime.Now;
                updateGA.User = User.Identity.Name;
                updateGA.Enable = true;
                updateGA.Approved = false;
                updateGA.Status = 0;
                updateGA.FilesRemoved = false;
                updateGA.ApplicationId = updateApplicationId;
                updateGA.AlertmailId = updateEmails;
                updateGA.Manual = Convert.ToBoolean(updateManual);


                if ((tipo == 1 || tipo == 3) && !updateManual)
                {

                    if (updateProcessSelected == null || updateProcessSelected.Length == 0)
                    {
                        ModelState.AddModelError("UpdateSteps", "É preciso cadastrar ao menos um processo.");
                    }
                }

                if (tipo == 1)
                {
                    if (updateGA.Schedule < DateTime.Now || updateGA.Schedule == null)
                    {
                        ModelState.AddModelError("Schedule", "A data não pode estar em branco e precisa ser posterior ao horário atual.");
                    }

                    if (updateGA.Demanda == null)
                    {
                        ModelState.AddModelError("Demanda", "É preciso informar o número da demanda.");
                    }
                }

                if (tipo == 2)
                {
                    updateGA.Status = 4;
                }

                if (tipo == 3)
                {
                    updateGA.Template = true;
                }


                if (ModelState.IsValid)
                {
                    _context.UpdateSteps.RemoveRange(_context.UpdateSteps.Where(x => x.UpdateId == updateGA.Id));
                    await _context.SaveChangesAsync();
                    await this.AtualizaSteps(updateProcessSelected, updateGA.Id);
 
                    _context.Update(updateGA);
                    await _context.SaveChangesAsync();
                    GALibrary.GALogs.SaveLog("Update", "Fim da edicao da atualizacao " + updateGA.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                    //Envia e-mail
                    if (tipo == 1 && updateEnvironmentId == 1)
                    {
                        Application application = _context.Application.FirstOrDefault(x => x.Id == updateGA.ApplicationId);
                        Parameter parameter = _context.Parameter.FirstOrDefault();
                        String email = _context.AlertMail.First(x => x.Id == updateGA.AlertmailId).Email;

                        GALibrary.GAMail.SendMail(email, 2, updateGA, (EmailSettings)emailSettings.Value, parameter, application.Name, (Ldap)ldap.Value);
                    }

                    return RedirectToAction(nameof(Index));
                }


                ViewBag.updateApplicationName = _context.Application.First(x => x.Id == updateGA.ApplicationId).Name;
                ViewBag.updateType = tiposAtualizacoes;
                ViewBag.updateFilesDelete = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateFiles = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateCommands = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateServices = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateSQLs = new SelectList(String.Empty, "Text", "Value");

                ViewBag.updateApplicationId = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", updateApplicationId);
                ViewBag.updateEnvironmentId = new SelectList(_context.Environment.Where(x => x.Enable == true).OrderBy(x => x.Name), "Id", "Name", updateEnvironmentId);
                ViewBag.updateProcessSelected = new MultiSelectList(SetUpdateProcessSelected(updateGA), "Text", "Value");

                ViewBag.updateEmails = new SelectList(_context.AlertMail.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", updateGA.AlertmailId);
                ViewBag.updateManual = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" } }, "ID", "Name", updateGA.Manual);

                return View(updateGA);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Update", "Erro ao editar a atualizacao " + updateGA.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Atualização")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {

                var updateGA = await _context.UpdateGA.FindAsync(id);

                if (updateGA == null)
                {
                    return NotFound();
                }

                _context.UpdateGA.Remove(updateGA);
                await _context.SaveChangesAsync();
                GALibrary.GALogs.SaveLog("OS", "Fim da edicao da atualizacao " + updateGA.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return RedirectToAction(nameof(Index));

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Update", "Erro ao editar atualizacao com ID " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Atualização")]
        private bool UpdateGAExists(int id)
        {
            return _context.UpdateGA.Any(e => e.Id == id);
        }


        [Authorize(Policy = "Atualização")]
        private List<SelectListItem> SetUpdateProcessSelected(UpdateGA update)
        {
            List<SelectListItem> items = new List<SelectListItem>();
            foreach (UpdateSteps step in update.UpdateSteps.OrderBy(x => x.Order))
            {
                String type = "";
                String processName = "";

                switch (step.Type)
                {
                    case 1:
                        type = "Arquivos para apagar";
                        processName = _context.FileDelete.Find(step.ProcessId).Name;
                        break;
                    case 2:
                        type = "Arquivos para copiar";
                        processName = _context.File.Find(step.ProcessId).Name;
                        break;
                    case 3:
                        type = "Comandos";
                        processName = _context.Command.Find(step.ProcessId).Name;
                        break;
                    case 4:
                        type = "Serviço iniciar";
                        processName = _context.Service.Find(step.ProcessId).Name;
                        break;
                    case 5:
                        type = "Serviço parar";
                        processName = _context.Service.Find(step.ProcessId).Name;
                        break;
                    case 6:
                        type = "SQL";
                        processName = _context.SQL.Find(step.ProcessId).Name;
                        break;
                }


                var item = new SelectListItem
                {
                    Value = type + " - " + processName,
                    Text = step.Type + " - " + step.ProcessId
                };

                items.Add(item);
            }

            return items;
        }


        [Authorize(Policy = "Atualização")]
        private List<SelectListItem> SetUpdateProcessSelectedString(String[] updateProcessSelected)
        {

            List<SelectListItem> items = new List<SelectListItem>();
            foreach (String step in updateProcessSelected)
            {
                String type = "";
                String processName = "";
                String[] linha = step.Split('-');
                int id = Convert.ToInt32(linha[1].Trim());

                switch (linha[0].Trim())
                {
                    case "1":
                        type = "Arquivos para apagar";
                        processName = _context.FileDelete.Find(id).Name;
                        break;
                    case "2":
                        type = "Arquivos para copiar";
                        processName = _context.File.Find(id).Name;
                        break;
                    case "3":
                        type = "Comandos";
                        processName = _context.Command.Find(id).Name;
                        break;
                    case "4":
                        type = "Serviço iniciar";
                        processName = _context.Service.Find(id).Name;
                        break;
                    case "5":
                        type = "Serviço parar";
                        processName = _context.Service.Find(id).Name;
                        break;
                    case "6":
                        type = "SQL";
                        processName = _context.SQL.Find(id).Name;
                        break;
                }


                var item = new SelectListItem
                {
                    Value = type + " - " + processName,
                    Text = linha[0] + " - " + id
                };

                items.Add(item);
            }

            return items;
        }

        [Authorize]
        [Authorize(Policy = "Atualização")]
        public ActionResult LoadDropDownListApplications(int ambiente)
        {
            if (ambiente > 1) ambiente = 2;
            return Json(new SelectList(_context.Application.Where(x => x.EnvironmentId == ambiente && x.Enable).OrderBy(x => x.Name), "Id", "Name"), new JsonSerializerSettings());
        }


        [Authorize(Policy = "Atualização")]
        public ActionResult DownloadLog(int updateId)
        {

            var update = _context.UpdateGA.FirstOrDefault(m => m.Id == updateId);
            var content_type = "text/plain";
            var parameter = _context.Parameter.FirstOrDefault();


            try
            {
                GALibrary.GALogs.SaveLog("Update", "Realizado download do arquivo de log da atualizacao " + update.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return File(GALibrary.GALogs.GetUpdateLog(update, parameter), content_type, updateId.ToString() + ".log");
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Update", "Erro ao fazer download arquivo com " + updateId + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());

                byte[] byteArray = Encoding.UTF8.GetBytes(erro.ToString());
                Stream stream = new MemoryStream(byteArray);
                return File(stream, content_type, "Erro.txt");
            }

        }


        [Authorize(Policy = "Vizualização")]
        public ActionResult DownloadFileBackup(int updateId)
        {

            var content_type = "application/zip";

            try
            {
                var update = _context.UpdateGA.FirstOrDefault(m => m.Id == updateId);
                GALibrary.GALogs.SaveLog("Update", "Realizado download do arquivo de backup da atualizacao " + update.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return File(Lib.GAClass.GetUpdateBackup(update), content_type, updateId.ToString() + ".zip");
            }
            catch (Exception erro)
            {
                content_type = "text/plain";
                GALibrary.GALogs.SaveLog("Update", "Erro ao fazer download do backup do update com id " + updateId + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());

                byte[] byteArray = Encoding.UTF8.GetBytes(erro.ToString());
                Stream stream = new MemoryStream(byteArray);
                return File(stream, content_type, "Erro.txt");
            }

        }


        [Authorize(Policy = "Vizualização")]
        public ActionResult GetUpdateProcessSelected(int templateId)
        {
            UpdateGA update = _context.UpdateGA.Include(x => x.UpdateSteps).Where(x => x.Id == templateId).First();
            var teste = SetUpdateProcessSelected(update);
            return Json(teste, new JsonSerializerSettings());
        }

    }
}
