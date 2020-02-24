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
    public class ServersController : Controller
    {
        private readonly GAContext _context;

        public ServersController(GAContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Index(int? page, String searchString, string sortOrder, string sortOrderOld, int OS, int ServerUser, int Enabled, Boolean registroApagado)
        {
            try
            {
                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;

                var servers = _context.Server.Include(x => x.OS).Include(x => x.ServerUser).AsQueryable();

                if (!string.IsNullOrEmpty(searchString))
                {
                    servers = servers.Where(x => x.Name.Contains(searchString));
                }

                if (OS > 0)
                {
                    servers = servers.Where(x => x.OSId == OS);
                }

                if (ServerUser > 0)
                {
                    servers = servers.Where(x => x.ServerUserId == ServerUser);
                }

                if (Enabled > 0)
                {
                    servers = servers.Where(x => x.Enable == Convert.ToBoolean(Enabled));
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
                    case "Servidor":
                        servers = servers.OrderBy(x => x.Name);
                        break;
                    case "ServidorDesc":
                        servers = servers.OrderByDescending(x => x.Name);
                        break;
                    case "OS":
                        servers = servers.OrderBy(x => x.OS.Name);
                        break;
                    case "OSDesc":
                        servers = servers.OrderByDescending(x => x.OS.Name);
                        break;
                    case "ServerUser":
                        servers = servers.OrderBy(x => x.ServerUser.Name);
                        break;
                    case "ServerUserDesc":
                        servers = servers.OrderByDescending(x => x.ServerUser.Name);
                        break;
                    case "User":
                        servers = servers.OrderBy(x => x.User);
                        break;
                    case "UserDesc":
                        servers = servers.OrderByDescending(x => x.User);
                        break;

                    default:
                        servers = servers.OrderBy(x => x.Name);
                        break;
                }

                ViewBag.OS = new SelectList(_context.OS.OrderBy(x => x.Name), "Id", "Name", OS);
                ViewBag.ServerUser = new SelectList(_context.ServerUser.OrderBy(x => x.Name), "Id", "Name", ServerUser);
                ViewBag.Enabled = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "1", Name = "Sim" }, new { ID = "2", Name = "Não" }, }, "ID", "Name", Enabled);
                ViewBag.Servers = await servers.ToPagedListAsync(pageNumber, itensPages);

                if (registroApagado) {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }

                return View(servers);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Server", "Erro ao pesquisar servidores pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var server = await _context.Server.Include(x => x.OS).Include(x => x.ServerUser).FirstOrDefaultAsync(m => m.Id == id);
                if (server == null)
                {
                    return NotFound();
                }
                GALibrary.GALogs.SaveLog("Server", "Vizualização de servidor " + server.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View(server);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Server", "Erro ao vizualizar detalhes do servidor com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }
        }

        [Authorize(Policy = "Cadastro")]
        public IActionResult Create()
        {
            try
            {
                ViewBag.OS = new SelectList(_context.OS.OrderBy(x => x.Name).ToList(), "Id", "Name");
                ViewBag.ServerUser = new SelectList(_context.ServerUser.OrderBy(x => x.Name).ToList(), "Id", "Name");
                GALibrary.GALogs.SaveLog("Server", "Inicio do cadastro de servidor pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Server", "Erro ao iniciar cadastro de servidor pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }



        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,ServerUserId,OSId,Date,User")] Server server)
        {

            try
            {
                server.Date = DateTime.Now;
                server.User = User.Identity.Name;
                server.Enable = true;

                if (ModelState.IsValid)
                {
                    _context.Add(server);
                    await _context.SaveChangesAsync();

                    GALibrary.GALogs.SaveLog("Server", "Fim do cadastro do servidor " + server.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));

                }

                ViewBag.OS = new SelectList(_context.OS.OrderBy(x => x.Name).ToList(), "Id", "Name");
                ViewBag.ServerUser = new SelectList(_context.ServerUser.OrderBy(x => x.Name).ToList(), "Id", "Name");

                return View(server);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Server", "Erro ao cadastrar servidor pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var server = await _context.Server.FindAsync(id);
                if (server == null)
                {
                    return NotFound();
                }

                ViewBag.OS = new SelectList(_context.OS.OrderBy(x => x.Name).ToList(), "Id", "Name", server.OSId);
                ViewBag.ServerUser = new SelectList(_context.ServerUser.OrderBy(x => x.Name).ToList(), "Id", "Name", server.ServerUser);
                ViewBag.Enable = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", server.Enable);

                GALibrary.GALogs.SaveLog("Server", "Inicio da edicao do servidor " + server.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(server);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Server", "Erro ao iniciar edicao do servidor com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ServerUserId,OSId,Date,User,Enable")] Server server)
        {
            if (id != server.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    server.Date = DateTime.Now;
                    server.User = User.Identity.Name;

                    _context.Update(server);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }

                ViewBag.OS = new SelectList(_context.OS.OrderBy(x => x.Name).ToList(), "Id", "Name", server.OSId);
                ViewBag.ServerUser = new SelectList(_context.ServerUser.OrderBy(x => x.Name).ToList(), "Id", "Name", server.ServerUser);
                ViewBag.Enable = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", server.Enable);

                GALibrary.GALogs.SaveLog("Server", "Fim da edicao do servidor " + server.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(server);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Server", "Erro ao editar servidor " + server.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var server = await _context.Server.FindAsync(id);
                _context.Server.Remove(server);

                GALibrary.GALogs.SaveLog("Server", "Servidor " + server.Name + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                await _context.SaveChangesAsync();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Server", "Erro ao remover servidor com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }


            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "Cadastro")]
        private bool ServerExists(int id)
        {
            return _context.Server.Any(e => e.Id == id);
        }
    }
}
