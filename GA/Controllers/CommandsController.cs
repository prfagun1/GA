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

namespace GA.Controllers
{
    public class CommandsController : Controller
    {
        private readonly GAContext _context;

        public CommandsController(GAContext context)
        {
            _context = context;
        }


        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Index(int? page, int commandEnabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado, int commandApplicationId, int commandServerId, int type)
        {
            try
            {
                var command = _context.Command.Include(c => c.Application).Include(c => c.Server).AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;

                if (!string.IsNullOrEmpty(searchString))
                {
                    command = command.Where(x => x.Name.Contains(searchString));
                }

                if (commandEnabled > 0)
                {
                    command = command.Where(x => x.Enable == Convert.ToBoolean(commandEnabled));
                }

                if (commandApplicationId > 0)
                {
                    command = command.Where(x => x.ApplicationId == commandApplicationId);
                }

                if (commandServerId > 0)
                {
                    command = command.Where(x => x.ServerId == commandServerId);
                }

                if (type > 0)
                {
                    command = command.Where(x => x.Type == type);
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
                        command = command.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        command = command.OrderByDescending(x => x.Name);
                        break;
                    case "Aplicacao":
                        command = command.OrderBy(x => x.Application.Name);
                        break;
                    case "AplicacaoDesc":
                        command = command.OrderByDescending(x => x.Application.Name);
                        break;
                    case "Servidor":
                        command = command.OrderBy(x => x.Server.Name);
                        break;
                    case "ServidorDesc":
                        command = command.OrderByDescending(x => x.Server.Name);
                        break;
                    case "Tipo":
                        command = command.OrderBy(x => x.Type);
                        break;
                    case "TipoDesc":
                        command = command.OrderByDescending(x => x.Type);
                        break;
                    case "Ativo":
                        command = command.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        command = command.OrderByDescending(x => x.Enable);
                        break;

                    default:
                        command = command.OrderBy(x => x.Name);
                        break;
                }

                ViewBag.command = await command.ToPagedListAsync(pageNumber, itensPages);
                ViewBag.commandApplicationId = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", commandApplicationId);
                ViewBag.commandServerId = new SelectList(_context.Server.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", commandServerId);
                ViewBag.commandEnabled = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "true", Name = "Sim" }, new { ID = "false", Name = "Não" }, }, "ID", "Name", commandEnabled);
                ViewBag.type = new SelectList(new[] { new { ID = "0", Name = "Sem filtro" }, new { ID = "1", Name = "Todos" }, new { ID = "2", Name = "Atualizações" }, new { ID = "3", Name = "Procedimentos" }, }, "ID", "Name");

                GALibrary.GALogs.SaveLog("Command", "Pesquisa de comando realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }

                return View();

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Command", "Erro ao pesquisar comandos pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var command = await _context.Command
                    .Include(c => c.Application)
                    .Include(c => c.Server)
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (command == null)
                {
                    return NotFound();
                }

                GALibrary.GALogs.SaveLog("Command", "Vizualização de detalhes do comando " + command.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View(command);

            }
            catch (Exception erro) {
                GALibrary.GALogs.SaveLog("Command", "Erro ao vizualizar detalhes do comando com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }
        }

        [Authorize(Policy = "Cadastro")]
        public IActionResult Create()
        {
            try
            {
                ViewBag.commandApplicationId = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name");
                ViewBag.commandServerId = new SelectList(_context.Server.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name");
                ViewBag.type = new SelectList(new[] { new { ID = "1", Name = "Todos" }, new { ID = "2", Name = "Atualizações" }, new { ID = "3", Name = "Procedimentos" }, }, "ID", "Name");

                GALibrary.GALogs.SaveLog("Command", "Inicio do cadastro do comando pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Command", "Erro ao iniciar cadastro de comando pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,ServerId,CommandText,ApplicationId,User,Date,Enable,Type")] Command command, int ServerId, int ApplicationId)
        {
            try
            {
                command.Date = DateTime.Now;
                command.User = User.Identity.Name;
                command.Enable = true;
                command.ApplicationId = ApplicationId;
                command.ServerId = ServerId;

                if (ModelState.IsValid)
                {
                    _context.Add(command);
                    await _context.SaveChangesAsync();
                    GALibrary.GALogs.SaveLog("Command", "Fim do cadastro do comando " + command.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.commandApplicationId = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", command.ApplicationId);
                ViewBag.commandServerId = new SelectList(_context.Server.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", command.ServerId);
                ViewBag.type = new SelectList(new[] { new { ID = "1", Name = "Todos" }, new { ID = "2", Name = "Atualizações" }, new { ID = "3", Name = "Procedimentos" }, }, "ID", "Name", command.Type);

                return View(command);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Command", "Erro ao cadastrar comando pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
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
                var command = await _context.Command.FindAsync(id);
                if (command == null)
                {
                    return NotFound();
                }

                ViewBag.commandApplicationId = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", command.ApplicationId);
                ViewBag.commandServerId = new SelectList(_context.Server.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", command.ServerId);
                ViewBag.commandEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", command.Enable);
                ViewBag.type = new SelectList(new[] { new { ID = "1", Name = "Todos" }, new { ID = "2", Name = "Atualizações" }, new { ID = "3", Name = "Procedimentos" }, }, "ID", "Name", command.Type);

                GALibrary.GALogs.SaveLog("Command", "Inicio da edicao do comando " + command.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(command);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Command", "Erro ao iniciar edicao do comando com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ServerId,CommandText,ApplicationId,User,Date,Enable,Type")] Command command, int ServerId, int ApplicationId)
        {
            if (id != command.Id)
            {
                return NotFound();
            }

            try
            {
                command.Date = DateTime.Now;
                command.User = User.Identity.Name;

                if (ModelState.IsValid)
                {
                    _context.Update(command);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.commandApplicationId = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", command.ApplicationId);
                ViewBag.commandServerId = new SelectList(_context.Server.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", command.ServerId);
                ViewBag.commandEnabled = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "true", Name = "Sim" }, new { ID = "false", Name = "Não" }, }, "ID", "Name", command.Enable);
                ViewBag.type = new SelectList(new[] { new { ID = "1", Name = "Todos" }, new { ID = "2", Name = "Atualizações" }, new { ID = "3", Name = "Procedimentos" }, }, "ID", "Name", command.Type);

                GALibrary.GALogs.SaveLog("Command", "Fim da edicao do comando " + command.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(command);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Command", "Erro ao editar aplicacao " + command.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                var command = await _context.Command.Include(c => c.Application).Include(c => c.Server).FirstOrDefaultAsync(m => m.Id == id);
                if (command == null)
                {
                    return NotFound();
                }

                _context.Command.Remove(command);
                await _context.SaveChangesAsync();
                GALibrary.GALogs.SaveLog("Command", "Comando " + command.Name + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

            }
            catch (Exception erro) {
                GALibrary.GALogs.SaveLog("Command", "Erro ao remover comando com " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
            }
            return RedirectToAction(nameof(Index));

        }

        [Authorize(Policy = "Cadastro")]
        private bool CommandExists(int id)
        {
            return _context.Command.Any(e => e.Id == id);
        }
    }
}
