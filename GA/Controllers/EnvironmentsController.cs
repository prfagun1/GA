using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GALibrary.Models;
using X.PagedList;
using Microsoft.AspNetCore.Authorization;

namespace GA.Controllers
{
    public class EnvironmentsController : Controller
    {
        private readonly GAContext _context;

        public EnvironmentsController(GAContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Index(int? page, bool? EnvironmentGAEnabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado)
        {
            try
            {
                var environment = _context.Environment.AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;


                if (!string.IsNullOrEmpty(searchString))
                {
                    environment = environment.Where(x => x.Name.Contains(searchString));
                }

                if (EnvironmentGAEnabled != null)
                {
                    environment = environment.Where(x => x.Enable == Convert.ToBoolean(EnvironmentGAEnabled));
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
                        environment = environment.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        environment = environment.OrderByDescending(x => x.Name);
                        break;
                    case "Ativo":
                        environment = environment.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        environment = environment.OrderByDescending(x => x.Enable);
                        break;
                    default:
                        environment = environment.OrderBy(x => x.Name);
                        break;
                }

                ViewBag.environment = await environment.ToPagedListAsync(pageNumber, itensPages);
                ViewBag.EnvironmentEnabled = new SelectList(new[] { new { ID = "", Name = "Todos" }, new { ID = "true", Name = "Sim" }, new { ID = "false", Name = "Não" }, }, "ID", "Name", EnvironmentGAEnabled);

                GALibrary.GALogs.SaveLog("Environment", "Pesquisa de ambiente realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());


                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }


                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Environment", "Erro ao pesquisar ambientes pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var environment = await _context.Environment
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (environment == null)
                {
                    return NotFound();
                }

                GALibrary.GALogs.SaveLog("Environment", "Vizualização de ambiente " + environment.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(environment);
            }
            catch (Exception erro){
                GALibrary.GALogs.SaveLog("Environment", "Erro ao vizualizar detalhes do ambiente com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }
        }

        [Authorize(Policy = "Cadastro")]
        public IActionResult Create()
        {
            try
            {
                GALibrary.GALogs.SaveLog("Environment", "Inicio do cadastro de ambiente pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Environment", "Erro ao iniciar cadastro de ambiente pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Enable")] GALibrary.Models.Environment environment)
        {
            environment.Enable = true;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(environment);
                    await _context.SaveChangesAsync();

                    GALibrary.GALogs.SaveLog("Environment", "Fim do cadastro do ambiente " + environment.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                    return RedirectToAction(nameof(Index));
                }
                catch(Exception erro) {
                    GALibrary.GALogs.SaveLog("Environment", "Erro ao cadastrar ambiente pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                }
            }
            return View(environment);
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
                var environment = await _context.Environment.FindAsync(id);
                if (environment == null)
                {
                    return NotFound();
                }

                ViewBag.EnvironmentEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", environment.Enable);

                GALibrary.GALogs.SaveLog("Environment", "Inicio da edicao do ambiente " + environment.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(environment);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Environment", "Erro ao iniciar edicao do ambiente com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Enable")] GALibrary.Models.Environment environment)
        {
            if (id != environment.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    _context.Update(environment);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.EnvironmentEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", environment.Enable);

                GALibrary.GALogs.SaveLog("Environment", "Fim da edicao do ambiente " + environment.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(environment);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Environment", "Erro ao editar ambiente " + environment.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var environment = await _context.Environment.FirstOrDefaultAsync(m => m.Id == id);
                if (environment == null)
                {
                    return NotFound();
                }

                _context.Environment.Remove(environment);
                await _context.SaveChangesAsync();

                GALibrary.GALogs.SaveLog("Environment", "Ambiente " + environment.Name + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return RedirectToAction(nameof(Index));

            }
            catch (Exception erro) {
                GALibrary.GALogs.SaveLog("Environment", "Erro ao remover ambiente com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

        }

        [Authorize(Policy = "Cadastro")]
        private bool EnvironmentExists(int id)
        {
            return _context.Environment.Any(e => e.Id == id);
        }
    }
}
