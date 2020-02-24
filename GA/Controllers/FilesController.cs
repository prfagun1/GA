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
using Newtonsoft.Json;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace GA.Controllers
{
    public class FilesController : Controller
    {
        private readonly GAContext _context;

        public FilesController(GAContext context)
        {
            _context = context;
        }


        [Authorize(Policy = "Vizualização")]
        public async Task<IActionResult> Index(int? page, int fileEnabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado, String nomeAquivo, int fileApplicationId, String criadoPor, String dataFinal, String dataInicial)
        {
            try
            {
                var file = _context.File.Include(f => f.Application).AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;

                if (!string.IsNullOrEmpty(searchString))
                {
                    file = file.Where(x => x.Name.Contains(searchString));
                }

                if (!string.IsNullOrEmpty(nomeAquivo))
                {
                    file = file.Where(x => x.FileName.Contains(nomeAquivo));
                }

                if (!string.IsNullOrEmpty(criadoPor))
                {
                    file = file.Where(x => x.User.Contains(criadoPor));
                }

                if (fileEnabled > 0)
                {
                    file = file.Where(x => x.Enable == Convert.ToBoolean(fileEnabled));
                }

                if (fileApplicationId > 0)
                {
                    file = file.Where(x => x.ApplicationId == fileApplicationId);
                }

                if (dataInicial != null)
                {
                    file = file.Where(x => x.Date >= Convert.ToDateTime(dataInicial));
                }

                if (dataFinal != null)
                {
                    file = file.Where(x => x.Date <= Convert.ToDateTime(dataFinal).AddHours(23).AddMinutes(59).AddSeconds(59));
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
                    case "Nome":
                        file = file.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        file = file.OrderByDescending(x => x.Name);
                        break;
                    case "Aplicacao":
                        file = file.OrderBy(x => x.Application.Name);
                        break;
                    case "AplicacaoDesc":
                        file = file.OrderByDescending(x => x.Application.Name);
                        break;
                    case "Data":
                        file = file.OrderBy(x => x.Date);
                        break;
                    case "DataDesc":
                        file = file.OrderByDescending(x => x.Date);
                        break;
                    case "Arquivo":
                        file = file.OrderBy(x => x.FileName);
                        break;
                    case "ArquivoDesc":
                        file = file.OrderByDescending(x => x.FileName);
                        break;
                    case "Usuario":
                        file = file.OrderBy(x => x.User);
                        break;
                    case "UsuarioDesc":
                        file = file.OrderByDescending(x => x.User);
                        break;
                    case "Ativo":
                        file = file.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        file = file.OrderByDescending(x => x.Enable);
                        break;

                    default:
                        file = file.OrderByDescending(x => x.Date);
                        break;
                }

                ViewBag.file = await file.ToPagedListAsync(pageNumber, itensPages);
                ViewBag.fileApplicationId = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", fileApplicationId);
                ViewBag.fileEnabled = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "1", Name = "Sim" }, new { ID = "2", Name = "Não" }, }, "ID", "Name", fileEnabled);

                GALibrary.GALogs.SaveLog("File", "Pesquisa de arquivos realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }

                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("File", "Erro ao pesquisar arquivos pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var file = await _context.File.Include(f => f.Application).FirstOrDefaultAsync(m => m.Id == id);

                var folderIDs = _context.FileFolder.Where(x => x.FileId == id).Select(x => x.FolderId);
                ViewBag.FolderIDs = _context.Folder.Where(x => folderIDs.Contains(x.Id)).Select(x => x.Path);

                if (file == null)
                {
                    return NotFound();
                }

                GALibrary.GALogs.SaveLog("File", "Vizualizacao dos detalhes do arquivo " + file.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View(file);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("File", "Erro ao vizualizar detalhes do arquivo com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }
        }

        [Authorize(Policy = "Atualização")]
        public IActionResult Create(int? updateApplicationId)
        {
            try
            {
                CreateInternal(updateApplicationId);
                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Environment", "Erro ao iniciar cadastro do arquivo pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Atualização")]
        public IActionResult CreatePartialView(int? updateApplicationId)
        {
            try
            {
                CreateInternal(updateApplicationId);
                return PartialView();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Environment", "Erro ao iniciar cadastro do arquivo pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Atualização")]
        public void CreateInternal(int? updateApplicationId)
        {
            if (updateApplicationId != null)
            {
                ViewBag.fileApplicationId = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", updateApplicationId);
                ViewBag.Empty = false;
            }
            else
            {
                ViewBag.Empty = true;
                ViewBag.fileApplicationId = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name");
            }

            GALibrary.GALogs.SaveLog("File", "Inicio do cadastro de arquivos pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

            ViewBag.FileFolder = new MultiSelectList("", "Id", "Path");
            ViewBag.EnabledFile = new SelectList(new[] { new { ID = "true", Name = "Sim" }, new { ID = "false", Name = "Não" }, }, "ID", "Name");
        }


        [Authorize(Policy = "Atualização")]
        [HttpPost]
        public async Task<ActionResult> SalvaDados()
        {
            String status = "1";
            try
            {
                String name = Request.Form["fileName"];
                int fileApplicationId = Convert.ToInt32(Request.Form["fileApplicationId"]);


                String folders = Request.Form["fileFolders"];
                String fileName = Request.Form["fileName"];
                String fileFileName = Request.Form["fileFileName"];

                IFormFileCollection files = Request.Form.Files;

                GALibrary.Models.File file = new GALibrary.Models.File();

                file.ApplicationId = fileApplicationId;
                file.Date = DateTime.Now;
                file.User = User.Identity.Name;
                file.Enable = true;
                file.FileName = fileFileName;
                file.FilesRemoved = false;
                file.Name = name;
                

                _context.File.Add(file);
                await _context.SaveChangesAsync();
                _context.Entry(file).GetDatabaseValues();

                List<FileFolder> fileFolderList = new List<FileFolder>();

                foreach (String folder in folders.TrimEnd().Split(' '))
                {
                    FileFolder fileFolder = new FileFolder();
                    fileFolder.ApplicationId = fileApplicationId;
                    fileFolder.FolderId = Convert.ToInt32(folder);
                    fileFolder.FileId = file.Id;

                    fileFolderList.Add(fileFolder);
                }

                _context.FileFolder.AddRange(fileFolderList);
                await _context.SaveChangesAsync();

                await Lib.GAClass.SaveFiles(files, file.Id, "File", 2, _context.Parameter.FirstOrDefault());

                GALibrary.GALogs.SaveLog("File", "Fim do cadastro de arquivos " + file.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
            }
            catch(Exception erro) {
                GALibrary.GALogs.SaveLog("File", "Erro ao cadastrar arquivo pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var file = await _context.File.FindAsync(id);
                if (file == null)
                {
                    return NotFound();
                }

                var folderIDs = _context.FileFolder.Where(x => x.FileId == id).Select(x => x.FolderId);

                ViewBag.FileFolderEsquerda = new MultiSelectList(_context.Folder.Where(x => x.ApplicationId == file.ApplicationId && !folderIDs.Contains(x.Id)), "Id", "Path");
                ViewBag.FileFolderDireita = new MultiSelectList(_context.Folder.Where(x => folderIDs.Contains(x.Id)), "Id", "Path");

                ViewBag.EnabledFile = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", file.Enable);
                ViewBag.ApplicationIdFile = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", file.ApplicationId);

                GALibrary.GALogs.SaveLog("File", "Inicio da edicao do arquivo " + file.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(file);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("File", "Erro ao iniciar edicao do arquivo com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Atualização")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ApplicationId,User,Date,FileName,Enable,FilesRemoved")] GALibrary.Models.File file, int[] FileFolder, int ApplicationIdFile, IFormFileCollection FilesFiles, String FilesNameOld)
        {
            Boolean valida = true;
            Parameter parameter = _context.Parameter.FirstOrDefault();

            if (id != file.Id)
            {
                return NotFound();
            }

            try
            {

                file.Date = DateTime.Now;
                file.User = User.Identity.Name;
                file.ApplicationId = ApplicationIdFile;

                if (FileFolder.Length == 0) {
                    valida = false;
                    ViewBag.ErroPastas = "É necessário selecionar ao menos uma pasta, o status original foi restaurado";
                }

                if (ModelState.IsValid && valida)
                {

                    if (FilesFiles.Count > 0) {
                        try
                        {
                            GALibrary.GAFiles.DeleteFiles(file.Id, file.Date, 2, parameter);
                            await Lib.GAClass.SaveFiles(FilesFiles, file.Id, "File", 2, _context.Parameter.FirstOrDefault());
                            String fileName = FilesFiles[0].FileName;
                            file.FileName = fileName.Substring(fileName.LastIndexOf("\\") + 1);

                            GALibrary.GALogs.SaveLog("File", "Novos arquivos salvos pelo usuario " + User.Identity.Name + " ao editar " + file.Name, 2, _context.Parameter.FirstOrDefault());
                        }
                        catch(Exception erro) {
                            GALibrary.GALogs.SaveLog("File", "Erro ao salvar novos arquivos pelo usuario " + User.Identity.Name + " ao editar o " + file.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                            valida = false;
                        }

                    }

                    if (valida)
                    {
                        _context.Update(file);
                        await _context.SaveChangesAsync();


                        List<FileFolder> folderSavedList = _context.FileFolder.Where(x => x.FileId == file.Id).ToList();

                        if (FileFolder != null)
                        {
                            List<FileFolder> lista = new List<FileFolder>();
                            _context.File.Attach(file);
                            _context.Entry(file).Collection(x => x.FileFolder).Load();


                            foreach (int idFolder in FileFolder)
                            {
                                FileFolder filefolders = folderSavedList.Where(x => x.FolderId == idFolder).FirstOrDefault();
                                if (filefolders == null)
                                {
                                    filefolders = new FileFolder();
                                    filefolders.ApplicationId = file.ApplicationId;
                                    filefolders.FileId = file.Id;
                                    filefolders.FolderId = idFolder;
                                }
                                lista.Add(filefolders);

                            }

                            var apagar = file.FileFolder.Except(lista).ToList();
                            var novos = lista.Except(file.FileFolder).ToList();

                            if (novos.Count != 0)
                            {
                                foreach (FileFolder filefolders in novos)
                                {
                                    _context.FileFolder.Add(filefolders);
                                }
                            }

                            if (apagar.Count != 0)
                            {
                                foreach (FileFolder filefolders in apagar)
                                {
                                    _context.FileFolder.Remove(filefolders);
                                }
                            }

                        }
                        else
                        {
                            foreach (FileFolder filefolders in _context.FileFolder.ToList())
                            {
                                _context.FileFolder.Remove(filefolders);
                            }
                        }

                        _context.SaveChanges();
                        
                    }
                    

                    GALibrary.GALogs.SaveLog("File", "Fim da edicao do arquivo " + file.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));
                }

                var folderIDs = _context.FileFolder.Where(x => x.FileId == id).Select(x => x.FolderId);

                ViewBag.FileFolderEsquerda = new MultiSelectList(_context.Folder.Where(x => x.ApplicationId == file.ApplicationId && !folderIDs.Contains(x.Id)), "Id", "Path");
                ViewBag.FileFolderDireita = new MultiSelectList(_context.Folder.Where(x => folderIDs.Contains(x.Id)), "Id", "Path");

                ViewBag.EnabledFile = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", file.Enable);
                ViewBag.ApplicationIdFile = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", file.ApplicationId);

                return View(file);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("File", "Erro ao editar arquivo com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Atualização")]
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                var file = await _context.File.Include(c => c.Application).FirstOrDefaultAsync(m => m.Id == id);
                if (file == null)
                {
                    return NotFound();
                }

                _context.File.Remove(file);
                await _context.SaveChangesAsync();

                Parameter parameter = _context.Parameter.FirstOrDefault();

                GALibrary.GAFiles.DeleteFiles(file.Id, file.Date, 2, parameter);

                GALibrary.GALogs.SaveLog("File", "Arquivo " + file.Name + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("File", "Erro ao remover arquivo com " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
            }
            return RedirectToAction(nameof(Index));
        }


        [Authorize(Policy = "Atualização")]
        public JsonResult LoadDropDownList(int applicationID)
        {
            var sa = new JsonSerializerSettings();
            var folder = _context.Folder.Where(x => x.ApplicationId == applicationID).OrderBy(x => x.Path).ToList();
            return Json(folder, sa);

        }


        [Authorize(Policy = "Atualização")]
        private bool FileExists(int id)
        {
            return _context.File.Any(e => e.Id == id);
        }


        [Authorize(Policy = "Atualização")]
        public ActionResult DownloadFile(int fileId)
        {

            GALibrary.Models.File file = _context.File.Find(fileId);
            var content_type = "application/zip";

            try
            {
                GALibrary.GALogs.SaveLog("File", "Realizado download do arquivo " + file.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return File(Lib.GAClass.GetFile(fileId), content_type, file.FileName);
            }
            catch(Exception erro) {
                GALibrary.GALogs.SaveLog("File", "Erro ao fazer download arquivo com " + fileId + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());

                content_type = "text/txt";
                byte[] byteArray = Encoding.UTF8.GetBytes(erro.ToString());
                Stream stream = new MemoryStream(byteArray);

                return File(stream, content_type, "Erro.txt");
            }

        }
    }
}
