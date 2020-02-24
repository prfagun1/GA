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
    public class ServicesController : Controller
    {
        private readonly GAContext _context;

        public ServicesController(GAContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Index(int? page, bool? serviceEnabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado, int? serviceServerId, int? serviceApplicationId)
        {

            try
            {
                var service = _context.Service.Include(x => x.Server).Include(x => x.Application).AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;

                if (!string.IsNullOrEmpty(searchString))
                {
                    service = service.Where(x => x.Name.Contains(searchString));
                }

                if (serviceEnabled != null)
                {
                    service = service.Where(x => x.Enable == Convert.ToBoolean(serviceEnabled));
                }

                if (serviceServerId != null)
                {
                    service = service.Where(x => x.ServerId == serviceServerId);
                }

                if (serviceApplicationId != null)
                {
                    service = service.Where(x => x.ApplicationId == serviceApplicationId);
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
                        service = service.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        service = service.OrderByDescending(x => x.Name);
                        break;
                    case "Servidor":
                        service = service.OrderBy(x => x.Server.Name);
                        break;
                    case "ServidorDesc":
                        service = service.OrderByDescending(x => x.Server.Name);
                        break;
                    case "Aplicacao":
                        service = service.OrderBy(x => x.Application.Name);
                        break;
                    case "AplicacaoDesc":
                        service = service.OrderByDescending(x => x.Application.Name);
                        break;
                    case "Ativo":
                        service = service.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        service = service.OrderByDescending(x => x.Enable);
                        break;
                    default:
                        service = service.OrderBy(x => x.Name);
                        break;
                }

                ViewBag.service = await service.ToPagedListAsync(pageNumber, itensPages);

                ViewBag.serviceEnabled = new SelectList(new[] { new { ID = "", Name = "Todos" }, new { ID = "true", Name = "Sim" }, new { ID = "false", Name = "Não" }, }, "ID", "Name", serviceEnabled);
                ViewBag.serviceServerId = new SelectList(_context.Server.OrderBy(x => x.Name), "Id", "Name", serviceServerId);
                ViewBag.serviceApplicationId = new SelectList(_context.Application.OrderBy(x => x.Name), "Id", "Name", serviceApplicationId);

                GALibrary.GALogs.SaveLog("Service", "Pesquisa de servicos realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());


                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }


                return View();

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Service", "Erro ao pesquisar servicos pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var service = await _context.Service.Include(s => s.Application).Include(s => s.Server).FirstOrDefaultAsync(m => m.Id == id);
                if (service == null)
                {
                    return NotFound();
                }
                GALibrary.GALogs.SaveLog("Service", "Vizualizacao do servico " + service.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View(service);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Service", "Erro ao vizualizar detalhes do servico com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }
        }

        [Authorize(Policy = "Cadastro")]
        public IActionResult Create()
        {
            try
            {
                ViewBag.serviceServerId = new SelectList(_context.Server.OrderBy(x => x.Name), "Id", "Name");
                ViewBag.serviceApplicationId = new SelectList(_context.Application.OrderBy(x => x.Name), "Id", "Name");
                GALibrary.GALogs.SaveLog("Service", "Inicio do cadastro de servico pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Service", "Erro ao iniciar cadastro de servico pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,ApplicationId,ServerId,CommandStart,CommandStop,User,Date,Enable")] Service service)
        {
            try
            {
                service.Date = DateTime.Now;
                service.User = User.Identity.Name;
                service.Enable = true;

                if (ModelState.IsValid)
                {
                    _context.Add(service);
                    await _context.SaveChangesAsync();
                    GALibrary.GALogs.SaveLog("Service", "Fim do cadastro do servico " + service.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));
                }
                ViewBag.serviceServerId = new SelectList(_context.Server.OrderBy(x => x.Name), "Id", "Name", service.ServerId);
                ViewBag.serviceApplicationId = new SelectList(_context.Application.OrderBy(x => x.Name), "Id", "Name", service.ApplicationId);

                return View(service);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Service", "Erro ao cadastrar servico pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var service = await _context.Service.FindAsync(id);
                if (service == null)
                {
                    return NotFound();
                }

                ViewBag.serviceEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", service.Enable);
                ViewBag.serviceServerId = new SelectList(_context.Server.OrderBy(x => x.Name), "Id", "Name", service.ServerId);
                ViewBag.serviceApplicationId = new SelectList(_context.Application.OrderBy(x => x.Name), "Id", "Name", service.ApplicationId);

                GALibrary.GALogs.SaveLog("Service", "Inicio da edicao do servico " + service.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(service);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Service", "Erro ao iniciar edicao do servico com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ApplicationId,ServerId,CommandStart,CommandStop,User,Date,Enable")] Service service)
        {
            if (id != service.Id)
            {
                return NotFound();
            }

            try
            {

                service.Date = DateTime.Now;
                service.User = User.Identity.Name;

                if (ModelState.IsValid)
                {
                    _context.Update(service);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.serviceEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", service.Enable);
                ViewBag.serviceServerId = new SelectList(_context.Server.OrderBy(x => x.Name), "Id", "Name", service.ServerId);
                ViewBag.serviceApplicationId = new SelectList(_context.Application.OrderBy(x => x.Name), "Id", "Name", service.ApplicationId);

                GALibrary.GALogs.SaveLog("Service", "Fim da edicao do servico " + service.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(service);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Service", "Erro ao editar servico " + service.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var service = await _context.Service.Include(s => s.Application).Include(s => s.Server).FirstOrDefaultAsync(m => m.Id == id);

                if (service == null)
                {
                    return NotFound();
                }

                _context.Service.Remove(service);
                await _context.SaveChangesAsync();
                GALibrary.GALogs.SaveLog("Service", "Servico " + service.Name + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return RedirectToAction(nameof(Index));
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Service", "Erro ao remover servico com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

        }

        [Authorize(Policy = "Cadastro")]
        private bool ServiceExists(int id)
        {
            return _context.Service.Any(e => e.Id == id);
        }
    }
}
