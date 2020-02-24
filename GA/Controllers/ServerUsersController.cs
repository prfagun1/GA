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
    public class ServerUsersController : Controller
    {
        private readonly GAContext _context;

        public ServerUsersController(GAContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Index(int? page, bool? ServerUserEnabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado)
        {
            try
            {
                var serverUser = _context.ServerUser.AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;

                if (!string.IsNullOrEmpty(searchString))
                {
                    serverUser = serverUser.Where(x => x.Name.Contains(searchString));
                }

                if (ServerUserEnabled != null)
                {
                    serverUser = serverUser.Where(x => x.Enable == Convert.ToBoolean(ServerUserEnabled));
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
                        serverUser = serverUser.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        serverUser = serverUser.OrderByDescending(x => x.Name);
                        break;
                    case "Ativo":
                        serverUser = serverUser.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        serverUser = serverUser.OrderByDescending(x => x.Enable);
                        break;
                    default:
                        serverUser = serverUser.OrderBy(x => x.Name);
                        break;
                }

                ViewBag.serverUser = await serverUser.ToPagedListAsync(pageNumber, itensPages);
                ViewBag.ServerUserEnabled = new SelectList(new[] { new { ID = "", Name = "Todos" }, new { ID = "true", Name = "Sim" }, new { ID = "false", Name = "Não" }, }, "ID", "Name", ServerUserEnabled);

                GALibrary.GALogs.SaveLog("ServerUser", "Pesquisa de usuario de servidor realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }

                return View();

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ServerUser", "Erro ao pesquisar usuarios de servidor pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var serverUser = await _context.ServerUser.FirstOrDefaultAsync(m => m.Id == id);
                if (serverUser == null)
                {
                    return NotFound();
                }
                GALibrary.GALogs.SaveLog("ServerUser", "Vizualizacao de usuario " + serverUser.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View(serverUser);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ServerUser", "Erro ao vizualizar detalhes do usuario com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

        }

        [Authorize(Policy = "Cadastro")]
        public IActionResult Create()
        {
            try
            {
                GALibrary.GALogs.SaveLog("ServerUser", "Inicio do cadastro de usuario pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View();

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ServerUser", "Erro ao iniciar cadastro de usuario de servidor pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,ServerUsername,ServerPassword,User,Date,Enable")] ServerUser serverUser)
        {

            try
            {
                serverUser.Date = DateTime.Now;
                serverUser.User = User.Identity.Name;
                serverUser.Enable = true;

                if (ModelState.IsValid)
                {
                    serverUser.ServerPassword = GALibrary.GACrypto.Base64Encode(serverUser.ServerPassword);
                    serverUser.ServerUsername = GALibrary.GACrypto.Base64Encode(serverUser.ServerUsername);

                    _context.Add(serverUser);
                    await _context.SaveChangesAsync();

                    GALibrary.GALogs.SaveLog("ServerUser", "Fim do cadastro do usuario de servidor " + serverUser.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));

                }
                return View(serverUser);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ServerUser", "Erro ao cadastrar usuario de servidor pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var serverUser = await _context.ServerUser.FindAsync(id);
                if (serverUser == null)
                {
                    return NotFound();
                }

                serverUser.ServerUsername = GALibrary.GACrypto.Base64Decode(serverUser.ServerUsername);
                ViewBag.ServerUserEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", serverUser.Enable);

                GALibrary.GALogs.SaveLog("ServerUser", "Inicio da edicao do usuario " + serverUser.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(serverUser);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ServerUser", "Erro ao iniciar edicao do usuario de servidor com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ServerUsername,ServerPassword,User,Date,Enable")] ServerUser serverUser)
        {
            if (id != serverUser.Id)
            {
                return NotFound();
            }

            try
            {
                serverUser.Date = DateTime.Now;
                serverUser.User = User.Identity.Name;

                if (serverUser.ServerUsername != null)
                {
                    serverUser.ServerUsername = GALibrary.GACrypto.Base64Encode(serverUser.ServerUsername);
                }
                if (serverUser.ServerPassword != null)
                {
                    serverUser.ServerPassword = GALibrary.GACrypto.Base64Encode(serverUser.ServerPassword);
                }
                else
                {
                    String serverUserPassword = _context.ServerUser.AsNoTracking().First(x => x.Id == id).ServerPassword.Clone().ToString();
                    serverUser.ServerPassword = serverUserPassword;
                }

                if (ModelState.IsValid)
                {
                    _context.Update(serverUser);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.ServerUserEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", serverUser.Enable);

                GALibrary.GALogs.SaveLog("ServerUser", "Fim da edicao do usuario " + serverUser.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(serverUser);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ServerUser", "Erro ao editar usuario " + serverUser.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var serverUser = await _context.ServerUser.FirstOrDefaultAsync(m => m.Id == id);
                if (serverUser == null)
                {
                    return NotFound();
                }

                _context.ServerUser.Remove(serverUser);
                await _context.SaveChangesAsync();

                GALibrary.GALogs.SaveLog("ServerUser", "Usuario " + serverUser.Name + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return RedirectToAction(nameof(Index));
            }

            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("ServerUser", "Erro ao remover usuario com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

        }

        [Authorize(Policy = "Cadastro")]
        private bool ServerUserExists(int id)
        {
            return _context.ServerUser.Any(e => e.Id == id);
        }
    }
}
