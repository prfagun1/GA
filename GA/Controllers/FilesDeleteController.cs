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
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace GA.Controllers
{
    public class FilesDeleteController : Controller
    {
        private readonly GAContext _context;

        public FilesDeleteController(GAContext context)
        {
            _context = context;
        }


        [Authorize(Policy = "Vizualização")]
        public async Task<IActionResult> Index(int? page, int Application, int Enabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado, String nomeAquivo, String dataFinal, String dataInicial, String criadoPor)
        {
            try
            {
                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;

                var filesDelete = _context.FileDelete.Include(f => f.Application).AsQueryable();

                if (!string.IsNullOrEmpty(searchString))
                {
                    filesDelete = filesDelete.Where(x => x.Name.Contains(searchString));
                }

                if (Application > 0)
                {
                    filesDelete = filesDelete.Where(x => x.ApplicationId == Application);
                }

                if (!string.IsNullOrEmpty(nomeAquivo))
                {
                    filesDelete = filesDelete.Where(x => x.FilesDirectory == nomeAquivo);
                }

                if (!string.IsNullOrEmpty(criadoPor))
                {
                    filesDelete = filesDelete.Where(x => x.User.Contains(criadoPor));
                }

                if (Enabled > 0)
                {
                    filesDelete = filesDelete.Where(x => x.Enable == Convert.ToBoolean(Enabled));
                }

                if (dataInicial != null)
                {
                    filesDelete = filesDelete.Where(x => x.Date >= Convert.ToDateTime(dataInicial));
                }

                if (dataFinal != null)
                {
                    filesDelete = filesDelete.Where(x => x.Date <= Convert.ToDateTime(dataFinal).AddHours(23).AddMinutes(59).AddSeconds(59));
                }

                if (sortOrder != null && sortOrderOld != null && sortOrder == sortOrderOld && !sortOrder.EndsWith("Desc") && pageNumber == 1)
                {
                    sortOrder += "Desc";
                }


                ViewBag.sortOrder = sortOrder;
                ViewBag.sortOrderOld = sortOrder;
                ViewBag.searchString = searchString;
                ViewBag.nomeAquivo = nomeAquivo;
                ViewBag.dataFinal = dataFinal;
                ViewBag.dataInicial = dataInicial;
                ViewBag.criadoPor = criadoPor;

                switch (sortOrder)
                {
                    case "Aplicacao":
                        filesDelete = filesDelete.OrderBy(x => x.Name);
                        break;
                    case "AplicacaoDesc":
                        filesDelete = filesDelete.OrderByDescending(x => x.Name);
                        break;
                    case "Nome":
                        filesDelete = filesDelete.OrderBy(x => x.Application.Name);
                        break;
                    case "NomeDesc":
                        filesDelete = filesDelete.OrderByDescending(x => x.Application.Name);
                        break;
                    case "CriadoPor":
                        filesDelete = filesDelete.OrderBy(x => x.User);
                        break;
                    case "CriadoPorDesc":
                        filesDelete = filesDelete.OrderByDescending(x => x.User);
                        break;
                    case "CriadoEm":
                        filesDelete = filesDelete.OrderBy(x => x.Date);
                        break;
                    case "CriadoEmDesc":
                        filesDelete = filesDelete.OrderByDescending(x => x.Date);
                        break;
                    case "Ativo":
                        filesDelete = filesDelete.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        filesDelete = filesDelete.OrderByDescending(x => x.Enable);
                        break;


                    default:
                        filesDelete = filesDelete.OrderByDescending(x => x.Date);
                        break;
                }

                GALibrary.GALogs.SaveLog("FileDelete", "Pesquisa de arquivos para apagar realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }

                ViewBag.Application = new SelectList(_context.Application.OrderBy(x => x.Name), "Id", "Name", Application);
                ViewBag.Enabled = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "1", Name = "Sim" }, new { ID = "2", Name = "Não" }, }, "ID", "Name", Enabled);
                ViewBag.FilesDelete = await filesDelete.ToPagedListAsync(pageNumber, itensPages);

                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("FileDelete", "Erro ao pesquisar arquivos para apagar pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Vizualização")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var fileDelete = await _context.FileDelete.Include(f => f.Application).FirstOrDefaultAsync(m => m.Id == id);

                var folderIDs = _context.FileDeleteFolder.Where(x => x.FileDeleteId == id).Select(x => x.FolderId);
                ViewBag.FolderIDs = _context.Folder.Where(x => folderIDs.Contains(x.Id)).Select(x => x.Path);

                if (fileDelete == null)
                {
                    return NotFound();
                }

                GALibrary.GALogs.SaveLog("FileDelete", "Vizualização de detalhes do arquivo para apagar " + fileDelete.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(fileDelete);
            }
            catch (Exception erro) {
                GALibrary.GALogs.SaveLog("FileDelete", "Erro ao vizualizar detalhes do arquivo para apagar com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

        }


        [Authorize(Policy = "Atualização")]
        public IActionResult Create(int? updateApplicationId)
        {
            try
            {
                CreateInternal(updateApplicationId);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("FileDelete", "Erro ao iniciar cadastro de arquivos para apagar pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
            return View();
        }

        [Authorize(Policy = "Atualização")]
        public IActionResult CreatePartialView(int? updateApplicationId)
        {
            try
            {
                CreateInternal(updateApplicationId);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("FileDelete", "Erro ao iniciar cadastro de arquivos para apagar pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
            return PartialView();
        }


        [Authorize(Policy = "Atualização")]
        public void CreateInternal(int? updateApplicationId) {
            if (updateApplicationId != null)
            {
                ViewBag.ApplicationIdFileDelete = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", updateApplicationId);
                ViewBag.Empty = false;
            }
            else
            {
                ViewBag.Empty = true;
                ViewBag.ApplicationIdFileDelete = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name");
            }

            GALibrary.GALogs.SaveLog("FileDelete", "Inicio do cadastro de arquivos para apagar pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

            ViewBag.FileDeleteFolder = new MultiSelectList("", "Id", "Path");
            ViewBag.EnabledFileDelete = new SelectList(new[] { new { ID = "true", Name = "Sim" }, new { ID = "false", Name = "Não" }, }, "ID", "Name");

        }


        [Authorize(Policy = "Atualização")]
        public JsonResult LoadDropDownList(int applicationID)
        {
            var sa = new JsonSerializerSettings();
            var folder = _context.Folder.Where(x => x.ApplicationId == applicationID).OrderBy(x => x.Path).ToList();
            return Json(folder, sa);

        }


        [Authorize(Policy = "Atualização")]
        [HttpPost]
        public async Task<ActionResult> SalvaDados()
        {
            String status = "1";

            try
            {
                FileDelete fileDelete = new FileDelete();

                String name = Request.Form["fileDeleteName"];
                String applicationId = Request.Form["fileDeleteApplicationId"];
                String filesDirectory = Request.Form["filesDeleteDirectory"];
                String folders = Request.Form["fileDeleteFolders"];

                fileDelete.Name = name;
                fileDelete.ApplicationId = Convert.ToInt32(applicationId);
                fileDelete.Date = DateTime.Now;
                fileDelete.User = User.Identity.Name;
                fileDelete.Enable = true;
                fileDelete.FilesDirectory = Regex.Replace(filesDirectory, "(?<!\r)\n", "\r\n");

                if (ModelState.IsValid)
                {
                    _context.Add(fileDelete);
                    await _context.SaveChangesAsync();
                    _context.Entry(fileDelete).GetDatabaseValues();

                    List<FileDeleteFolder> fileDeleteFolderList = new List<FileDeleteFolder>();
                    foreach (String folder in folders.Trim().Split(" "))
                    {
                        FileDeleteFolder fileDeleteFolder = new FileDeleteFolder();
                        fileDeleteFolder.ApplicationId = fileDelete.ApplicationId;
                        fileDeleteFolder.FolderId = Convert.ToInt32(folder);
                        fileDeleteFolder.FileDeleteId = fileDelete.Id;
                        fileDeleteFolderList.Add(fileDeleteFolder);
                    }
                    _context.FileDeleteFolder.AddRange(fileDeleteFolderList);
                    await _context.SaveChangesAsync();

                    GALibrary.GALogs.SaveLog("FileDelete", "Fim do cadastro de arquivos para remover" + fileDelete.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                }
            }
            catch(Exception erro) {
                GALibrary.GALogs.SaveLog("FileDelete", "Erro ao cadastrar arquivo para remover pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                status = erro.ToString();
            }

            return Json(new { status = status });
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
                var fileDelete = await _context.FileDelete.FindAsync(id);
                if (fileDelete == null)
                {
                    return NotFound();
                }

                var folderIDs = _context.FileDeleteFolder.Where(x => x.FileDeleteId == id).Select(x => x.FolderId);

                ViewBag.FileDeleteFolderLeft = new MultiSelectList(_context.Folder.Where(x => x.ApplicationId == fileDelete.ApplicationId && !folderIDs.Contains(x.Id)), "Id", "Path");
                ViewBag.FileDeleteFolderRight = new MultiSelectList(_context.Folder.Where(x => folderIDs.Contains(x.Id)), "Id", "Path");

                ViewBag.ApplicationIdFileDelete = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", fileDelete.ApplicationId);
                ViewBag.EnabledFileDelete = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", fileDelete.Enable);

                GALibrary.GALogs.SaveLog("FileDelete", "Inicio da edicao do arquivo para remover " + fileDelete.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(fileDelete);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("FileDelete", "Erro ao iniciar edicao do arquivo para remover com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Atualização")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ApplicationId,FilesDirectory,User,Date,Enable")] FileDelete fileDelete, int[] FileDeleteFolderRight)
        {
            Boolean valida = true;

            if (id != fileDelete.Id)
            {
                return NotFound();
            }

            try
            {

                fileDelete.Date = DateTime.Now;
                fileDelete.User = User.Identity.Name;

                if (FileDeleteFolderRight.Length == 0)
                {
                    valida = false;
                    ViewBag.ErroPastas = "É necessário selecionar ao menos uma pasta, o status original foi restaurado";
                }

                if (ModelState.IsValid && valida)
                {
                    fileDelete.FilesDirectory = Regex.Replace(fileDelete.FilesDirectory, "(?<!\r)\n", "\r\n");
                    _context.Update(fileDelete);
                    await _context.SaveChangesAsync();

                    List<FileDeleteFolder> folderSavedList = _context.FileDeleteFolder.Where(x => x.FileDeleteId == fileDelete.Id).ToList();

                    if (FileDeleteFolderRight != null)
                    {
                        List<FileDeleteFolder> lista = new List<FileDeleteFolder>();
                        _context.FileDelete.Attach(fileDelete);
                        _context.Entry(fileDelete).Collection(x => x.FileDeleteFolder).Load();


                        foreach (int idFolder in FileDeleteFolderRight)
                        {
                            FileDeleteFolder fileDeletefolders = folderSavedList.Where(x => x.FolderId == idFolder).FirstOrDefault();
                            if (fileDeletefolders == null)
                            {
                                fileDeletefolders = new FileDeleteFolder();
                                fileDeletefolders.ApplicationId = fileDelete.ApplicationId;
                                fileDeletefolders.FileDeleteId = fileDelete.Id;
                                fileDeletefolders.FolderId = idFolder;
                            }
                            lista.Add(fileDeletefolders);
                        }

                        var apagar = fileDelete.FileDeleteFolder.Except(lista).ToList();
                        var novos = lista.Except(fileDelete.FileDeleteFolder).ToList();

                        if (novos.Count != 0)
                        {
                            foreach (FileDeleteFolder fileDeleteFolders in novos)
                            {
                                _context.FileDeleteFolder.Add(fileDeleteFolders);
                            }
                        }

                        if (apagar.Count != 0)
                        {
                            foreach (FileDeleteFolder fileDeleteFolders in apagar)
                            {
                                _context.FileDeleteFolder.Remove(fileDeleteFolders);
                            }
                        }
                        _context.SaveChanges();
                    }

                    GALibrary.GALogs.SaveLog("FileDelete", "Fim da edicao do arquivo para apagar " + fileDelete.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));
                }


                var folderIDs = _context.FileDeleteFolder.Where(x => x.FileDeleteId == id).Select(x => x.FolderId);

                ViewBag.FileDeleteFolderLeft = new MultiSelectList(_context.Folder.Where(x => x.ApplicationId == fileDelete.ApplicationId && !folderIDs.Contains(x.Id)), "Id", "Path");
                ViewBag.FileDeleteFolderRight = new MultiSelectList(_context.Folder.Where(x => folderIDs.Contains(x.Id)), "Id", "Path");

                ViewBag.ApplicationIdFileDelete = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", fileDelete.ApplicationId);
                ViewBag.EnabledFileDelete = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", fileDelete.Enable);


                return View(fileDelete);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("FileDelete", "Erro ao editar arquivo para apagar com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var fileDelete = await _context.FileDelete.FindAsync(id);
                _context.FileDelete.Remove(fileDelete);
                await _context.SaveChangesAsync();

                GALibrary.GALogs.SaveLog("FileDelete", "Arquivo para apagar " + fileDelete.Name + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
            }
            catch(Exception erro)
            {
                GALibrary.GALogs.SaveLog("FileDelete", "Erro ao remover arquivo para apagar com " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
            }


            return RedirectToAction(nameof(Index));

        }

        [Authorize(Policy = "Atualização")]
        private bool FileDeleteExists(int id)
        {
            return _context.FileDelete.Any(e => e.Id == id);
        }
    }
}
