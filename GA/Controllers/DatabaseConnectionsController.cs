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
    public class DatabaseConnectionsController : Controller
    {
        private readonly GAContext _context;

        public DatabaseConnectionsController(GAContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Index(int? page, bool? databaseConnectionEnabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado)
        {

            try
            {
                var databaseConnection = _context.DatabaseConnection.AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;


                if (!string.IsNullOrEmpty(searchString))
                {
                    databaseConnection = databaseConnection.Where(x => x.Name.Contains(searchString));
                }

                if (databaseConnectionEnabled != null)
                {
                    databaseConnection = databaseConnection.Where(x => x.Enable == databaseConnectionEnabled);
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
                        databaseConnection = databaseConnection.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        databaseConnection = databaseConnection.OrderByDescending(x => x.Name);
                        break;
                    case "Ativo":
                        databaseConnection = databaseConnection.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        databaseConnection = databaseConnection.OrderByDescending(x => x.Enable);
                        break;
                    default:
                        databaseConnection = databaseConnection.OrderBy(x => x.Name);
                        break;
                }


                ViewBag.databaseConnection = await databaseConnection.ToPagedListAsync(pageNumber, itensPages);
                ViewBag.databaseConnectionEnabled = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "true", Name = "Sim" }, new { ID = "false", Name = "Não" }, }, "ID", "Name", databaseConnectionEnabled);

                GALibrary.GALogs.SaveLog("DatabaseConnection", "Pesquisa de conexao com o banco realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }

                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("DatabaseConnection", "Erro ao pesquisar conexoes com o banco pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var databaseConnection = await _context.DatabaseConnection.FirstOrDefaultAsync(m => m.Id == id);
                if (databaseConnection == null)
                {
                    return NotFound();
                }

                GALibrary.GALogs.SaveLog("DatabaseConnection", "Consulta de detalhes da conexao com o banco " + databaseConnection.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(databaseConnection);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("DatabaseConnection", "Erro ao vizualizar detalhes da conexao com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }
        }


        [Authorize(Policy = "Cadastro")]
        public IActionResult Create()
        {
            try
            {
                GALibrary.GALogs.SaveLog("DatabaseConnection", "Inicio do cadastro de conexao com banco pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("DatabaseConnection", "Erro ao iniciar cadastro de conexao com banco pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,SQLImportCommand,Enable,User,Date")] DatabaseConnection databaseConnection)
        {
            try
            {
                databaseConnection.Date = DateTime.Now;
                databaseConnection.User = User.Identity.Name;
                databaseConnection.Enable = true;

                if (ModelState.IsValid)
                {
                    _context.Add(databaseConnection);
                    await _context.SaveChangesAsync();
                    GALibrary.GALogs.SaveLog("DatabaseConnection", "Fim do cadastro da conexao com banco " + databaseConnection.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));
                }

                return View(databaseConnection);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("DatabaseConnection", "Erro ao cadastrar conexao com o banco pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var databaseConnection = await _context.DatabaseConnection.FindAsync(id);
                if (databaseConnection == null)
                {
                    return NotFound();
                }

                ViewBag.DatabaseConnectionEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", databaseConnection.Enable);

                GALibrary.GALogs.SaveLog("DatabaseConnection", "Inicio da edicao da conexao com banco " + databaseConnection.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(databaseConnection);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("DatabaseConnection", "Erro ao iniciar edicao da conexao com o banco de id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,SQLImportCommand,Enable,User,Date")] DatabaseConnection databaseConnection)
        {
            if (id != databaseConnection.Id)
            {
                return NotFound();
            }

            try
            {
                databaseConnection.Date = DateTime.Now;
                databaseConnection.User = User.Identity.Name;

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(databaseConnection);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception erro)
                    {
                        if (!DatabaseConnectionExists(databaseConnection.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            GALibrary.GALogs.SaveLog("DatabaseConnection", "Erro ao editar conexao com banco " + databaseConnection.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.DatabaseConnectionEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", databaseConnection.Enable);

                GALibrary.GALogs.SaveLog("DatabaseConnection", "Fim da edicao da conexao com banco " + databaseConnection.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(databaseConnection);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("DatabaseConnection", "Erro ao editar conexao com banco " + databaseConnection.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var databaseConnection = await _context.DatabaseConnection.FirstOrDefaultAsync(m => m.Id == id);

                if (databaseConnection == null)
                {
                    return NotFound();
                }

                _context.DatabaseConnection.Remove(databaseConnection);
                await _context.SaveChangesAsync();

                GALibrary.GALogs.SaveLog("DatabaseConnection", "Conexao com banco " + databaseConnection.Name + " removida pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return RedirectToAction(nameof(Index));
            }
            catch (Exception erro) {
                GALibrary.GALogs.SaveLog("DatabaseConnection", "Erro ao remover conexao com banco com " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return RedirectToAction(nameof(Index));
            }

        }


        [Authorize(Policy = "Cadastro")]
        private bool DatabaseConnectionExists(int id)
        {
            return _context.DatabaseConnection.Any(e => e.Id == id);
        }
    }
}
