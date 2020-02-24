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
    public class ApplicationsController : Controller
    {
        private readonly GAContext _context;

        public ApplicationsController(GAContext context)
        {
            _context = context;
        }


        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Index(int? page, int applicationEnabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado, String nomeAquivo, int applicationEnvironmentId)
        {
            try
            {
                var application = _context.Application.Include(f => f.Environment).AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;

                if (!string.IsNullOrEmpty(searchString))
                {
                    application = application.Where(x => x.Name.Contains(searchString));
                }

                if (applicationEnabled > 0)
                {
                    application = application.Where(x => x.Enable == Convert.ToBoolean(applicationEnabled));
                }

                if (applicationEnvironmentId > 0)
                {
                    application = application.Where(x => x.EnvironmentId == applicationEnvironmentId);
                }

                if (sortOrder != null && sortOrderOld != null && sortOrder == sortOrderOld && !sortOrder.EndsWith("Desc") && pageNumber == 1)
                {
                    sortOrder += "Desc";
                }

                switch (sortOrder)
                {
                    case "Nome":
                        application = application.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        application = application.OrderByDescending(x => x.Name);
                        break;
                    case "Ambiente":
                        application = application.OrderBy(x => x.Environment.Name);
                        break;
                    case "AmbienteDesc":
                        application = application.OrderByDescending(x => x.Environment.Name);
                        break;
                    case "Ativo":
                        application = application.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        application = application.OrderByDescending(x => x.Enable);
                        break;
                    default:
                        application = application.OrderBy(x => x.Name);
                        break;
                }

                ViewBag.sortOrder = sortOrder;
                ViewBag.sortOrderOld = sortOrder;
                ViewBag.searchString = searchString;
                ViewBag.applicationEnvironmentId = applicationEnvironmentId;

                ViewBag.application = await application.ToPagedListAsync(pageNumber, itensPages);
                ViewBag.applicationEnvironmentId = new SelectList(_context.Environment.OrderBy(x => x.Name), "Id", "Name", applicationEnvironmentId);
                ViewBag.applicationEnabled = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "1", Name = "Sim" }, new { ID = "2", Name = "Não" }, }, "ID", "Name", applicationEnabled);

                GALibrary.GALogs.SaveLog("Application", "Pesquisa de aplicacao realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }

                return View();
            }
            catch (Exception erro) {
                GALibrary.GALogs.SaveLog("Application", "Erro ao pesquisar aplicacoes pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var application = await _context.Application.Include(a => a.Environment).Include(x => x.Folder).FirstOrDefaultAsync(m => m.Id == id);

                if (application == null)
                {
                    return NotFound();
                }

                GALibrary.GALogs.SaveLog("Application", "Consulta de detalhes da aplicacao " + application.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View(application);
            }
            catch(Exception erro) {
                GALibrary.GALogs.SaveLog("Application", "Erro ao ver detalhes da aplicacao com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        public IActionResult Create()
        {
            try
            {
                ViewBag.EnvironmentId = new SelectList(_context.Environment.OrderBy(x => x.Name), "Id", "Name");
                ViewBag.ApplicationEnabled = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "1", Name = "Sim" }, new { ID = "2", Name = "Não" }, }, "ID", "Name");
                ViewBag.FolderList = new MultiSelectList(String.Empty, "Text", "Value");

                GALibrary.GALogs.SaveLog("Application", "Inicio do cadastro de aplicacao pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Application", "Erro ao iniciar cadastro de aplicacao pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,EnvironmentId,Enable")] Application application, String[] FolderList)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    application.Enable = true;

                    _context.Add(application);
                    await _context.SaveChangesAsync();
                    _context.Entry(application).GetDatabaseValues();

                    List<Folder> folderList = new List<Folder>();
                    foreach (String folderLine in FolderList)
                    {
                        Folder folder = new Folder();
                        folder.ApplicationId = application.Id;
                        folder.Path = folderLine;
                        folderList.Add(folder);
                    }
                    _context.Folder.AddRange(folderList);
                    await _context.SaveChangesAsync();

                    GALibrary.GALogs.SaveLog("Application", "Fim do cadastro da aplicacao " + application.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));

                }

                ViewBag.EnvironmentId = new SelectList(_context.Environment.OrderBy(x => x.Name), "Id", "Name", application.EnvironmentId);
                ViewBag.ApplicationEnabled = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "true", Name = "Sim" }, new { ID = "false", Name = "Não" }, }, "ID", "Name", application.Enable);
                ViewBag.FolderList = new MultiSelectList(FolderList);
                return View(application);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Application", "Erro ao cadastrar aplicacao pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var application = await _context.Application.FindAsync(id);
                if (application == null)
                {
                    return NotFound();
                }

                ViewBag.EnvironmentId = new SelectList(_context.Environment.OrderBy(x => x.Name), "Id", "Name", application.EnvironmentId);
                ViewBag.ApplicationEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", application.Enable);
                ViewBag.FolderList = new MultiSelectList(_context.Folder.Where(x => x.ApplicationId == id), "Path", "Path");


                GALibrary.GALogs.SaveLog("Application", "Inicio da edicao da aplicacao " + application.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(application);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Application", "Erro ao iniciar edicao da aplicacao com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,EnvironmentId,Enable")] Application application, String[] FolderList)
        {
            if (id != application.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    _context.Update(application);
                    await _context.SaveChangesAsync();

                    //Salva os diretórios que foram alterados
                    List<Folder> folderSavedList = _context.Folder.Where(x => x.ApplicationId == application.Id).ToList();

                    if (FolderList != null)
                    {
                        List<Folder> lista = new List<Folder>();
                        _context.Application.Attach(application);
                        _context.Entry(application).Collection(x => x.Folder).Load();

                        foreach (String path in FolderList)
                        {
                            Folder folder = folderSavedList.Where(x => x.Path == path).FirstOrDefault();
                            if (folder == null)
                            {
                                folder = new Folder();
                                folder.Path = path;
                                folder.ApplicationId = application.Id;
                            }
                            lista.Add(folder);
                        }

                        var apagar = application.Folder.Except(lista).ToList();
                        var novos = lista.Except(application.Folder).ToList();

                        if (novos.Count != 0)
                        {
                            foreach (Folder folder in novos)
                            {
                                _context.Folder.Add(folder);
                            }
                        }

                        if (apagar.Count != 0)
                        {
                            foreach (Folder folder in apagar)
                            {
                                _context.Folder.Remove(folder);
                            }
                        }

                    }
                    else
                    {
                        foreach (Folder folder in _context.Folder.ToList())
                        {
                            _context.Folder.Remove(folder);
                        }
                    }

                    await _context.SaveChangesAsync();
                    GALibrary.GALogs.SaveLog("Application", "Fim da edicao da aplicacao " + application.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.EnvironmentId = new SelectList(_context.Environment.OrderBy(x => x.Name), "Id", "Name", application.EnvironmentId);
                ViewBag.ApplicationEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", application.Enable);
                ViewBag.FolderList = new MultiSelectList(FolderList);

                return View(application);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Application", "Erro ao editar aplicacao " + application.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var application = await _context.Application.Include(a => a.Environment).FirstOrDefaultAsync(m => m.Id == id);

                if (application == null)
                {
                    return NotFound();
                }
                else
                {

                    application = await _context.Application.FindAsync(id);
                    _context.Application.Remove(application);
                    await _context.SaveChangesAsync();

                    GALibrary.GALogs.SaveLog("Application", "Aplicacao " + application.Name + " removida pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                    return RedirectToAction(nameof(Index));
                }
            }
            catch(Exception erro) {
                GALibrary.GALogs.SaveLog("Application", "Erro ao remover aplicacao com id " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return RedirectToAction(nameof(Index));
            }

        }

        [Authorize(Policy = "Cadastro")]
        private bool ApplicationExists(int id)
        {
            return _context.Application.Any(e => e.Id == id);
        }
    }
}
