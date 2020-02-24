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
using System.Text;
using System.IO;

namespace GA.Controllers
{
    public class ProcedureSchedulesController : Controller
    {
        private readonly GAContext _context;

        public ProcedureSchedulesController(GAContext context)
        {
            _context = context;
        }


        [Authorize(Policy = "Atualização")]
        public async Task<IActionResult> Index(int? page, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado, String dataFinal, String dataInicial, String criadoPor, int? procedureScheduleStatus)
        {

            try
            {
                var procedureSchedule = _context.ProcedureSchedule.Include(x => x.Procedure).AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;


                if (!string.IsNullOrEmpty(searchString))
                {
                    procedureSchedule = procedureSchedule.Where(x => x.Procedure.Name.Contains(searchString));
                }

                if (!string.IsNullOrEmpty(criadoPor))
                {
                    procedureSchedule = procedureSchedule.Where(x => x.User.Contains(criadoPor));
                }


                if (dataInicial != null)
                {
                    procedureSchedule = procedureSchedule.Where(x => x.Schedule >= Convert.ToDateTime(dataInicial));
                }

                if (dataFinal != null)
                {
                    procedureSchedule = procedureSchedule.Where(x => x.Schedule <= Convert.ToDateTime(dataFinal).AddHours(23).AddMinutes(59).AddSeconds(59));
                }

                if (procedureScheduleStatus != null)
                {
                    procedureSchedule = procedureSchedule.Where(x => x.Status == procedureScheduleStatus);
                }
            

                if (sortOrder != null && sortOrderOld != null && sortOrder == sortOrderOld && !sortOrder.EndsWith("Desc") && pageNumber == 1)
                {
                    sortOrder += "Desc";
                }

                ViewBag.sortOrder = sortOrder;
                ViewBag.sortOrderOld = sortOrder;
                ViewBag.searchString = searchString;
                ViewBag.criadoPor = criadoPor;
                ViewBag.dataInicial = dataInicial;
                ViewBag.dataFinal = dataFinal;


                switch (sortOrder)
                {
                    case "Nome":
                        procedureSchedule = procedureSchedule.OrderBy(x => x.Procedure.Name);
                        break;
                    case "NomeDesc":
                        procedureSchedule = procedureSchedule.OrderByDescending(x => x.Procedure.Name);
                        break;
                    case "Usuario":
                        procedureSchedule = procedureSchedule.OrderBy(x => x.User);
                        break;
                    case "UsuarioDesc":
                        procedureSchedule = procedureSchedule.OrderByDescending(x => x.User);
                        break;
                    case "Data":
                        procedureSchedule = procedureSchedule.OrderBy(x => x.Schedule);
                        break;
                    case "DataDesc":
                        procedureSchedule = procedureSchedule.OrderByDescending(x => x.Schedule);
                        break;
                    case "Status":
                        procedureSchedule = procedureSchedule.OrderBy(x => x.Status);
                        break;
                    case "StatusDesc":
                        procedureSchedule = procedureSchedule.OrderByDescending(x => x.Status);
                        break;
                    default:
                        procedureSchedule = procedureSchedule.OrderByDescending(x => x.Schedule);
                        break;
                }

                ViewBag.procedureSchedule = await procedureSchedule.ToPagedListAsync(pageNumber, itensPages);
                ViewBag.procedureScheduleStatus = new SelectList(new[] { new { ID = "", Name = "Todos" }, new { ID = "0", Name = "Pendente" }, new { ID = "1", Name = "Realizada com sucesso" }, new { ID = "2", Name = "Realizada com erros" }, }, "ID", "Name", procedureScheduleStatus);

                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Pesquisa de procedimentos agendados realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());


                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }


                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Erro ao pesquisar procedimentos agendados pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }



        [Authorize(Policy = "Atualização")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var procedureSchedule = await _context.ProcedureSchedule.Include(p => p.Procedure).FirstOrDefaultAsync(m => m.Id == id);
                if (procedureSchedule == null)
                {
                    return NotFound();
                }

                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Vizualização do agendamento com ID " + id + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View(procedureSchedule);
            }
            catch (Exception erro) {
                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Erro ao vizualizar detalhes do agendamento com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

            
        }

        [Authorize(Policy = "Atualização")]
        public IActionResult Create()
        {
            try
            {
                ViewBag.procedureId = new SelectList(_context.Procedure.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name");
                ViewBag.procedureScheduleStatus = new SelectList(new[] { new { ID = "", Name = "Todos" }, new { ID = "0", Name = "Pendente" }, new { ID = "1", Name = "Realizada com sucesso" }, new { ID = "2", Name = "Realizada com erros" }, }, "ID", "Name");

                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Inicio de um agendamento do procedimento pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Erro ao iniciar cadastro de agendamento de procedimento usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Atualização")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ProcedureID,Schedule,Status,User,Date")] ProcedureSchedule procedureSchedule)
        {

            try
            {
                procedureSchedule.Date = DateTime.Now;
                procedureSchedule.User = User.Identity.Name;
                procedureSchedule.Status = 0;

                if (ModelState.IsValid)
                {

                    _context.Add(procedureSchedule);
                    await _context.SaveChangesAsync();
                    GALibrary.GALogs.SaveLog("ProcedureSchedule", "Fim do cadastro do agendamento do procedimento com ID " + procedureSchedule.ProcedureID + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));

                }

                ViewBag.procedureId = new SelectList(_context.Procedure.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", procedureSchedule.ProcedureID);
                ViewBag.procedureScheduleStatus = new SelectList(new[] { new { ID = "", Name = "Todos" }, new { ID = "0", Name = "Pendente" }, new { ID = "1", Name = "Realizada com sucesso" }, new { ID = "2", Name = "Realizada com erros" }, }, "ID", "Name", procedureSchedule.Status);

                return View(procedureSchedule);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Erro ao cadastrar agendamento do procedimento pelo usuário " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var procedureSchedule = await _context.ProcedureSchedule.Include(x => x.Procedure).FirstOrDefaultAsync(x => x.Id == id);
                if (procedureSchedule == null)
                {
                    return NotFound();
                }


                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Inicio da edicao do agendamento do procedimento " + procedureSchedule.Procedure.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                ViewBag.procedureId = new SelectList(_context.Procedure.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", procedureSchedule.ProcedureID);
                ViewBag.procedureScheduleStatus = new SelectList(new[] { new { ID = "", Name = "Todos" }, new { ID = "0", Name = "Pendente" }, new { ID = "1", Name = "Realizada com sucesso" }, new { ID = "2", Name = "Realizada com erros" }, }, "ID", "Name", procedureSchedule.Status);

                return View(procedureSchedule);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Erro ao iniciar edicao do agendamento de procedimento com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Atualização")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProcedureID,Schedule,Status,User,Date")] ProcedureSchedule procedureSchedule)
        {
            if (id != procedureSchedule.Id)
            {
                return NotFound();
            }

            try
            {
                procedureSchedule.Date = DateTime.Now;
                procedureSchedule.User = User.Identity.Name;
                procedureSchedule.Status = 0;

                if (ModelState.IsValid)
                {
                    _context.Update(procedureSchedule);
                    await _context.SaveChangesAsync();

                    GALibrary.GALogs.SaveLog("ProcedureSchedule", "Fim da edicao do agendamento do procedimento " + procedureSchedule.Id + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.procedureId = new SelectList(_context.Procedure.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", procedureSchedule.ProcedureID);
                ViewBag.procedureScheduleStatus = new SelectList(new[] { new { ID = "", Name = "Todos" }, new { ID = "0", Name = "Pendente" }, new { ID = "1", Name = "Realizada com sucesso" }, new { ID = "2", Name = "Realizada com erros" }, }, "ID", "Name", procedureSchedule.Status);

                return View(procedureSchedule);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Erro ao editar agendamento do procedimento " + procedureSchedule.Procedure.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Atualização")]
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var procedureSchedule = await _context.ProcedureSchedule.Include(p => p.Procedure).FirstOrDefaultAsync(m => m.Id == id);
                if (procedureSchedule == null)
                {
                    return NotFound();
                }

                _context.ProcedureSchedule.Remove(procedureSchedule);
                await _context.SaveChangesAsync();

                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Agendamento do procedimento " + procedureSchedule.Procedure.Name + " que ia ser realizado às " + procedureSchedule.Schedule.ToString() + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return RedirectToAction(nameof(Index));
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Erro ao remover agendamento do procedimento com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }
        }

        [Authorize(Policy = "Atualização")]
        private bool ProcedureScheduleExists(int id)
        {
            return _context.ProcedureSchedule.Any(e => e.Id == id);
        }


        [Authorize(Policy = "Atualização")]
        public ActionResult DownloadLog(int procedureScheduleID)
        {

            var procedureSchedule = _context.ProcedureSchedule.Include(p => p.Procedure).FirstOrDefault(m => m.Id == procedureScheduleID);
            var content_type = "text/plain";

            try
            {
                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Realizado download do arquivo de log " + procedureSchedule.Id + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return File(Lib.GAClass.GetProcedureScheduleLog(procedureSchedule), content_type, procedureScheduleID.ToString() + ".log");
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ProcedureSchedule", "Erro ao fazer download arquivo com " + procedureScheduleID + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());

                byte[] byteArray = Encoding.UTF8.GetBytes(erro.ToString());
                Stream stream = new MemoryStream(byteArray);
                return File(stream, content_type, "Erro.txt");
            }

        }

    }
}
