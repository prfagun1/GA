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
    public class DatabasesGAController : Controller
    {
        private readonly GAContext _context;

        public DatabasesGAController(GAContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Index(int? page, bool? DatabaseGAEnabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado, int DatabaseGAApplicationId, int DatabaseGADatabaseConnectionId)
        {
            try
            {
                var databaseGA = _context.DatabaseGA.Include(d => d.Application).Include(d => d.DatabaseConnection).AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;


                if (!string.IsNullOrEmpty(searchString))
                {
                    databaseGA = databaseGA.Where(x => x.Name.Contains(searchString));
                }

                if (DatabaseGAEnabled != null)
                {
                    databaseGA = databaseGA.Where(x => x.Enable == Convert.ToBoolean(DatabaseGAEnabled));
                }

                if (DatabaseGAApplicationId > 0)
                {
                    databaseGA = databaseGA.Where(x => x.ApplicationId == DatabaseGAApplicationId);
                }

                if (DatabaseGADatabaseConnectionId > 0)
                {
                    databaseGA = databaseGA.Where(x => x.DatabaseConnectionId == DatabaseGADatabaseConnectionId);
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
                        databaseGA = databaseGA.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        databaseGA = databaseGA.OrderByDescending(x => x.Name);
                        break;
                    case "Aplicacao":
                        databaseGA = databaseGA.OrderBy(x => x.Application.Name);
                        break;
                    case "AplicacaoDesc":
                        databaseGA = databaseGA.OrderByDescending(x => x.Application.Name);
                        break;
                    case "ConexaoBanco":
                        databaseGA = databaseGA.OrderBy(x => x.DatabaseConnection.Name);
                        break;
                    case "ConexaoBancoDesc":
                        databaseGA = databaseGA.OrderByDescending(x => x.DatabaseConnection.Name);
                        break;
                    case "Ativo":
                        databaseGA = databaseGA.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        databaseGA = databaseGA.OrderByDescending(x => x.Enable);
                        break;

                    default:
                        databaseGA = databaseGA.OrderBy(x => x.Name);
                        break;
                }

                ViewBag.databaseGA = await databaseGA.ToPagedListAsync(pageNumber, itensPages);

                ViewBag.DatabaseGAApplicationId = new SelectList(_context.Application.OrderBy(x => x.Name), "Id", "Name", DatabaseGAApplicationId);
                ViewBag.DatabaseGADatabaseConnectionId = new SelectList(_context.DatabaseConnection.OrderBy(x => x.Name), "Id", "Name", DatabaseGADatabaseConnectionId);
                ViewBag.DatabaseGAEnabled = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "true", Name = "Sim" }, new { ID = "false", Name = "Não" }, }, "ID", "Name", DatabaseGAEnabled);

                GALibrary.GALogs.SaveLog("DatabaseGA", "Pesquisa de aplicacao realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());


                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }


                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("DatabaseGA", "Erro ao pesquisar bancos de dados pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var databaseGA = await _context.DatabaseGA.Include(d => d.Application).Include(d => d.DatabaseConnection).FirstOrDefaultAsync(m => m.Id == id);
                if (databaseGA == null)
                {
                    GALibrary.GALogs.SaveLog("DatabaseGA", "Vizualização de detalhes do banco " + databaseGA.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return NotFound();
                }

                return View(databaseGA);
            }
            catch (Exception erro) {
                GALibrary.GALogs.SaveLog("DatabaseGA", "Erro ao vizualizar detalhes do banco com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }
        }


        [Authorize(Policy = "Cadastro")]
        public IActionResult Create()
        {
            try
            {
                ViewBag.DatabaseGAApplicationId = new SelectList(_context.Application.OrderBy(x => x.Name), "Id", "Name");
                ViewBag.DatabaseGADatabaseConnectionId = new SelectList(_context.DatabaseConnection.OrderBy(x => x.Name), "Id", "Name");

                GALibrary.GALogs.SaveLog("DatabaseGA", "Inicio do cadastro de banco de dados pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("DatabaseGA", "Erro ao iniciar cadastro de banco de dados pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,DatabaseName,Port,DatabaseUser,DatabasePassword,Server,DatabaseConnectionId,ApplicationId,Enable,User,Date")] DatabaseGA databaseGA, int DatabaseGAApplicationId, int DatabaseGADatabaseConnectionId)
        {
            try
            {
                databaseGA.Date = DateTime.Now;
                databaseGA.User = User.Identity.Name;
                databaseGA.Enable = true;

                if (ModelState.IsValid)
                {

                    databaseGA.DatabasePassword = GALibrary.GACrypto.Base64Encode(databaseGA.DatabasePassword);
                    databaseGA.DatabaseUser = GALibrary.GACrypto.Base64Encode(databaseGA.DatabaseUser);

                    _context.Add(databaseGA);
                    await _context.SaveChangesAsync();

                    GALibrary.GALogs.SaveLog("DatabaseGA", "Fim do cadastro do banco de dados " + databaseGA.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                    return RedirectToAction(nameof(Index));
                }

                ViewBag.DatabaseGAApplicationId = new SelectList(_context.Application.OrderBy(x => x.Name), "Id", "Name", DatabaseGAApplicationId);
                ViewBag.DatabaseGADatabaseConnectionId = new SelectList(_context.DatabaseConnection.OrderBy(x => x.Name), "Id", "Name", DatabaseGADatabaseConnectionId);
                return View(databaseGA);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("DatabaseGA", "Erro ao cadastrar banco de dados pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var databaseGA = await _context.DatabaseGA.FindAsync(id);
                if (databaseGA == null)
                {
                    return NotFound();
                }

                databaseGA.DatabaseUser = GALibrary.GACrypto.Base64Decode(databaseGA.DatabaseUser);
                databaseGA.DatabasePassword = GALibrary.GACrypto.Base64Decode(databaseGA.DatabasePassword);

                ViewBag.DatabaseGAApplicationId = new SelectList(_context.Application.OrderBy(x => x.Name), "Id", "Name", databaseGA.ApplicationId);
                ViewBag.DatabaseGADatabaseConnectionId = new SelectList(_context.DatabaseConnection.OrderBy(x => x.Name), "Id", "Name", databaseGA.DatabaseConnectionId);
                ViewBag.DatabaseGAEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", databaseGA.Enable);

                GALibrary.GALogs.SaveLog("DatabaseGA", "Inicio da edicao do banco de dados " + databaseGA.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(databaseGA);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("DatabaseGA", "Erro ao iniciar edicao do banco de dados com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,DatabaseName,Port,DatabaseUser,DatabasePassword,Server,DatabaseConnectionId,ApplicationId,Enable,User,Date")] DatabaseGA databaseGA)
        {
            if (id != databaseGA.Id)
            {
                return NotFound();
            }

            try
            {
                databaseGA.Date = DateTime.Now;
                databaseGA.User = User.Identity.Name;

                if (databaseGA.DatabaseUser != null) {
                    databaseGA.DatabaseUser = GALibrary.GACrypto.Base64Encode(databaseGA.DatabaseUser);
                }
                if (databaseGA.DatabasePassword != null)
                {
                    databaseGA.DatabasePassword = GALibrary.GACrypto.Base64Encode(databaseGA.DatabasePassword);
                }
                else {
                    String databaseGAPassword = _context.DatabaseGA.AsNoTracking().First(x => x.Id == id).DatabasePassword.Clone().ToString();
                    databaseGA.DatabasePassword = databaseGAPassword;
                }

                if (ModelState.IsValid)
                {
                    _context.Update(databaseGA);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.DatabaseGAApplicationId = new SelectList(_context.Application.OrderBy(x => x.Name), "Id", "Name", databaseGA.ApplicationId);
                ViewBag.DatabaseGADatabaseConnectionId = new SelectList(_context.DatabaseConnection.OrderBy(x => x.Name), "Id", "Name", databaseGA.DatabaseConnectionId);
                ViewBag.DatabaseGAEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", databaseGA.Enable);

                GALibrary.GALogs.SaveLog("DatabaseGA", "Fim da edicao do banco de dados " + databaseGA.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(databaseGA);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("DatabaseGA", "Erro ao editar banco de dados " + databaseGA.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> Delete(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var databaseGA = await _context.DatabaseGA.Include(d => d.Application).Include(d => d.DatabaseConnection).FirstOrDefaultAsync(m => m.Id == id);
                if (databaseGA == null)
                {
                    return NotFound();
                }

                _context.DatabaseGA.Remove(databaseGA);
                await _context.SaveChangesAsync();

                GALibrary.GALogs.SaveLog("DatabaseGA", "Banco de dados " + databaseGA.Name + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return RedirectToAction(nameof(Index));
            }
            catch (Exception erro) {
                GALibrary.GALogs.SaveLog("DatabaseGA", "Erro ao remover banco de dados com " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

        }

        [Authorize(Policy = "Cadastro")]
        private bool DatabaseGAExists(int id)
        {
            return _context.DatabaseGA.Any(e => e.Id == id);
        }
    }
}
