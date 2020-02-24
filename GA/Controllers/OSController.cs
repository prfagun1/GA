using GALibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

namespace GA.Controllers
{
    public class OSController : Controller
    {
        private readonly GAContext _context;

        public OSController(GAContext context)
        {
            _context = context;
        }


        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Index(int? page, bool? OSEnabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado)
        {
            try
            {
                var os = _context.OS.AsQueryable();

                int itensPages = 5;
                var pageNumber = page ?? 1;


                if (!string.IsNullOrEmpty(searchString))
                {
                    os = os.Where(x => x.Name.Contains(searchString));
                }

                if (OSEnabled != null)
                {
                    os = os.Where(x => x.Enable == Convert.ToBoolean(OSEnabled));
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
                        os = os.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        os = os.OrderByDescending(x => x.Name);
                        break;
                    case "Ativo":
                        os = os.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        os = os.OrderByDescending(x => x.Enable);
                        break;
                    default:
                        os = os.OrderBy(x => x.Name);
                        break;
                }

                ViewBag.os = await os.ToPagedListAsync(pageNumber, itensPages);
                ViewBag.OSEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", OSEnabled);

                GALibrary.GALogs.SaveLog("OS", "Pesquisa de servidor realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());


                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }


                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("OS", "Erro ao pesquisar sistemas operacionais pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var os = await _context.OS.FirstOrDefaultAsync(m => m.Id == id);
                if (os == null)
                {
                    return NotFound();
                }
                GALibrary.GALogs.SaveLog("OS", "Vizualização do sistema operacional " + os.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View(os);
            }
            catch (Exception erro) {
                GALibrary.GALogs.SaveLog("OS", "Erro ao vizualizar detalhes do sistema operacional com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

        }


        [Authorize(Policy = "Cadastro")]
        public IActionResult Create()
        {
            try
            {
                GALibrary.GALogs.SaveLog("OS", "Inicio do cadastro de um sistema operacional pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("OS", "Erro ao iniciar cadastro de sistemas operacionais pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,AccessCommand,User,Date,Enable")] OS os)
        {
            try
            {
                os.Date = DateTime.Now;
                os.User = User.Identity.Name;
                os.Enable = true;

                if (ModelState.IsValid)
                {
                    _context.Add(os);
                    await _context.SaveChangesAsync();

                    GALibrary.GALogs.SaveLog("OS", "Fim do cadastro do sistema operacional " + os.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                    return RedirectToAction(nameof(Index));

                }
                return View(os);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("OS", "Erro ao cadastrar sistema operacional pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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

                var os = await _context.OS.FindAsync(id);
                if (os == null)
                {
                    return NotFound();
                }

                GALibrary.GALogs.SaveLog("OS", "Inicio da edicao do sistema operacional " + os.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                ViewBag.OSEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", os.Enable);

                return View(os);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("OS", "Erro ao iniciar edicao do sistema operacional com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,AccessCommand,User,Date,Enable")] OS os)
        {
            if (id != os.Id)
            {
                return NotFound();
            }

            try
            {
                os.Date = DateTime.Now;
                os.User = User.Identity.Name;

                if (ModelState.IsValid)
                {

                    _context.Update(os);
                    await _context.SaveChangesAsync();


                    GALibrary.GALogs.SaveLog("OS", "Fim da edicao do sistema operacional " + os.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.OSEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", os.Enable);

                return View(os);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("OS", "Erro ao editar sistema operacional " + os.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var os = await _context.OS.FirstOrDefaultAsync(m => m.Id == id);
                if (os == null)
                {
                    return NotFound();
                }

                _context.OS.Remove(os);
                await _context.SaveChangesAsync();

                GALibrary.GALogs.SaveLog("OS", "Sistema operacional " + os.Name + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return RedirectToAction(nameof(Index));
            }
            catch (Exception erro) {
                GALibrary.GALogs.SaveLog("OS", "Erro ao remover sistema operacional com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

        }


        [Authorize(Policy = "Cadastro")]
        private bool OSExists(int id)
        {
            return _context.OS.Any(e => e.Id == id);
        }
    }
}
