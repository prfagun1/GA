using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GALibrary.Models;
using Microsoft.AspNetCore.Authorization;
using X.PagedList;
using Newtonsoft.Json;

namespace GA.Controllers
{
    public class ProceduresController : Controller
    {
        private readonly GAContext _context;
        private readonly SelectList tiposAtualizacoes = new SelectList(new[]    {
                                                        new { ID = "3", Name = "Comandos" },
                                                        new { ID = "4", Name = "Serviço iniciar" },
                                                        new { ID = "5", Name = "Serviço parar" },
                                                    }, "ID", "Name", 0);

        public ProceduresController(GAContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Index(int? page, Boolean? procedureEnabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado)
        {
            try
            {
                var procedure = _context.Procedure.AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;

                if (!string.IsNullOrEmpty(searchString))
                {
                    procedure = procedure.Where(x => x.Name.Contains(searchString));
                }

                if (procedureEnabled != null)
                {
                    procedure = procedure.Where(x => x.Enable == Convert.ToBoolean(procedureEnabled));
                }


                if (sortOrder != null && sortOrderOld != null && sortOrder == sortOrderOld && !sortOrder.EndsWith("Desc") && pageNumber == 1)
                {
                    sortOrder += "Desc";
                }

                ViewBag.sortOrder = sortOrder;
                ViewBag.sortOrderOld = sortOrder;
                ViewBag.searchString = searchString;


                switch (sortOrder)
                {
                    case "Nome":
                        procedure = procedure.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        procedure = procedure.OrderByDescending(x => x.Name);
                        break;
                    case "Ativo":
                        procedure = procedure.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        procedure = procedure.OrderByDescending(x => x.Enable);
                        break;

                    default:
                        procedure = procedure.OrderBy(x => x.Name);
                        break;
                }

                ViewBag.procedure = await procedure.ToPagedListAsync(pageNumber, itensPages);
                ViewBag.procedureEnabled = new SelectList(new[] { new { ID = "", Name = "Todos" }, new { ID = "true", Name = "Sim" }, new { ID = "false", Name = "Não" }, }, "ID", "Name", procedureEnabled);

                GALibrary.GALogs.SaveLog("Procedure", "Pesquisa de procedimentos realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }

                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Procedure", "Erro ao pesquisar procedimentos pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Details(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            try
            {

                var procedure = await _context.Procedure.Include(x => x.ProcedureSteps).FirstOrDefaultAsync(m => m.Id == id);
                if (procedure == null)
                {
                    return NotFound();
                }

                ViewBag.StepResult = this.GetProcedureStepsString(procedure.ProcedureSteps.OrderBy(x => x.Order).ToList());


                return View(procedure);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Procedure", "Erro ao vizualizar detalhes do procedimento com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }
        }


        [Authorize(Policy = "Cadastro")]
        private String GetProcedureStepsString(List<ProcedureSteps> steps)
        {
            String stepResult = "";

            foreach (ProcedureSteps step in steps)
            {
                int order = step.Order + 1;
                try
                {
                    switch (step.Type)
                    {
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
                    }
                }
                catch
                {
                    stepResult += order + " - Processo apagado do banco" + "<br />";
                }

            }

            if (stepResult.Length > 6)
            {
                return stepResult.Substring(0, stepResult.Length - 6);
            }

            return stepResult;
        }

        [Authorize(Policy = "Cadastro")]
        public IActionResult Create()
        {
            try
            {
                ViewBag.updateType = tiposAtualizacoes;
                ViewBag.updateCommands = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateServices = new SelectList(String.Empty, "Text", "Value");

                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Procedure", "Erro ao iniciar cadastro de procedimento pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Enable,User,Date")] Procedure procedure, String[] updateProcessSelected)
        {
            try
            {
                Boolean valid = true;

                procedure.Date = DateTime.Now;
                procedure.User = User.Identity.Name;
                procedure.Enable = true;

                if (updateProcessSelected == null || updateProcessSelected.Length == 0)
                {
                    ModelState.AddModelError("ProcedureSteps", "É preciso cadastrar ao menos um processo.");
                    valid = false;
                }

                if (ModelState.IsValid)
                {
                    _context.Add(procedure);
                    await _context.SaveChangesAsync();
                    _context.Entry(procedure).GetDatabaseValues();

                    GALibrary.GALogs.SaveLog("Procedure", "Fim do cadastro do procedimento " + procedure.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                    try
                    {
                        await this.AtualizaSteps(updateProcessSelected, procedure.Id);
                        GALibrary.GALogs.SaveLog("ProcedureSteps", "Passos do procedimento " + procedure.Name + " gravados com sucesso pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                    }
                    catch (Exception erro)
                    {
                        GALibrary.GALogs.SaveLog("ProcedureSteps", "Erro ao gravar os passos do procedimento " + procedure.Name + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                        ModelState.AddModelError("", "Erro ao salvar etapas, edite esta atualização ou crie ela novamente");
                        valid = false;
                    }

                    if (valid) return RedirectToAction("Index");
                }
                return View(procedure);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Procedure", "Erro ao cadastrar procedimento pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        public async Task AtualizaSteps(String[] updateProcessSelected, int procedureId)
        {
            int i = 0;

            List<ProcedureSteps> procedureStepsList = new List<ProcedureSteps>();

            foreach (String process in updateProcessSelected)
            {
                String[] option = process.Split('-');

                int type = Convert.ToInt32(option[0].Trim());
                int processId = Convert.ToInt32(option[1].Trim());

                ProcedureSteps procedureStep = new ProcedureSteps();
                procedureStep.ProcessId = processId;
                procedureStep.Order = i;
                procedureStep.Type = type;
                procedureStep.ProcedureID = procedureId;

                i++;

                procedureStepsList.Add(procedureStep);
            }

            await _context.ProcedureSteps.AddRangeAsync(procedureStepsList);
            await _context.SaveChangesAsync();
        }

        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {

                var procedure = await _context.Procedure.Include(x => x.ProcedureSteps).FirstOrDefaultAsync(m => m.Id == id);
                if (procedure == null)
                {
                    return NotFound();
                }

                ViewBag.updateType = tiposAtualizacoes;
                ViewBag.updateCommands = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateServices = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateProcessSelected = new MultiSelectList(SetProcedureProcessSelected(procedure), "Text", "Value");
                ViewBag.updateEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", procedure.Enable);

                return View(procedure);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Procedure", "Erro ao iniciar edicao do procedimento com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Cadastro")]
        private List<SelectListItem> SetProcedureProcessSelected(Procedure procedure)
        {

            List<SelectListItem> items = new List<SelectListItem>();
            foreach (ProcedureSteps step in procedure.ProcedureSteps.OrderBy(x => x.Order))
            {
                String type = "";
                String processName = "";

                switch (step.Type)
                {
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


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Enable,User,Date")] Procedure procedure, String[] updateProcessSelected)
        {
            if (id != procedure.Id)
            {
                return NotFound();
            }

            try
            {
                procedure.Date = DateTime.Now;
                procedure.User = User.Identity.Name;

                if (ModelState.IsValid)
                {

                    _context.ProcedureSteps.RemoveRange(_context.ProcedureSteps.Where(x => x.ProcedureID == procedure.Id));
                    await _context.SaveChangesAsync();
                    await this.AtualizaSteps(updateProcessSelected, procedure.Id);

                    _context.Update(procedure);
                    await _context.SaveChangesAsync();
                    GALibrary.GALogs.SaveLog("Procedure", "Fim da edicao da atualizacao do procedimento " + procedure.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                    _context.Update(procedure);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index)); 
                }

                ViewBag.updateType = tiposAtualizacoes;
                ViewBag.updateCommands = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateServices = new SelectList(String.Empty, "Text", "Value");
                ViewBag.updateProcessSelected = new MultiSelectList(SetProcedureProcessSelected(procedure), "Text", "Value");
                ViewBag.updateEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", procedure.Enable);

                return View(procedure);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Procedure", "Erro ao editar procedimento " + procedure.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var procedure = await _context.Procedure
                .FirstOrDefaultAsync(m => m.Id == id);
                if (procedure == null)
                {
                    return NotFound();
                }

                _context.Procedure.Remove(procedure);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Procedure", "Erro ao apagar procedimento com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

        }

        [Authorize(Policy = "Cadastro")]
        private bool ProcedureExists(int id)
        {
            return _context.Procedure.Any(e => e.Id == id);
        }


        [Authorize(Policy = "Cadastro")]
        public ActionResult LoadDropDownList(int id)
        {
            //Carrega os mesmos dados para ServiceStart e ServiceStop
            if (id == 5) id = 4;
            String usuario = User.Identity.Name;

            switch (id)
            {
                case 3:
                    return Json(new SelectList(_context.Command.Where(x => x.Enable && (x.Type == 1 || x.Type == 3)).ToList().OrderBy(x => x.Name), "Id", "Name"), new JsonSerializerSettings());
                case 4:
                    return Json(new SelectList(_context.Service.Where(x => x.Enable).ToList().OrderBy(x => x.Name), "Id", "Name"), new JsonSerializerSettings());
            }
            return View();
        }
    }
}
