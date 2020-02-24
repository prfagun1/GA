using GALibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;

namespace GA.Controllers
{
    public class SQLsController : Controller
    {
        private readonly GAContext _context;

        public SQLsController(GAContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "Vizualização")]
        public async Task<IActionResult> Index(int? page, bool? sqlEnabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado, int? sqlDatabaseId, int? sqlType, String criadoPor, String dataFinal, String dataInicial)
        {

            try
            {
                var sql = _context.SQL.Include(x => x.Database).AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;

                if (!string.IsNullOrEmpty(searchString))
                {
                    sql = sql.Where(x => x.Name.Contains(searchString));
                }

                if (!string.IsNullOrEmpty(criadoPor))
                {
                    sql = sql.Where(x => x.User.Contains(criadoPor));
                }

                if (sqlEnabled != null)
                {
                    sql = sql.Where(x => x.Enable == sqlEnabled);
                }

                if (sqlDatabaseId != null)
                {
                    sql = sql.Where(x => x.DatabaseId == sqlDatabaseId);
                }

                if (dataInicial != null)
                {
                    sql = sql.Where(x => x.Date >= Convert.ToDateTime(dataInicial));
                }

                if (dataFinal != null)
                {
                    sql = sql.Where(x => x.Date <= Convert.ToDateTime(dataFinal).AddHours(23).AddMinutes(59).AddSeconds(59));
                }

                if (sqlType > 0)
                {
                    sql = sql.Where(x => x.Type == sqlType);
                }

                if (sortOrder != null && sortOrderOld != null && sortOrder == sortOrderOld && !sortOrder.EndsWith("Desc") && pageNumber == 1)
                {
                    sortOrder += "Desc";
                }

                ViewBag.sortOrder = sortOrder;
                ViewBag.sortOrderOld = sortOrder;
                ViewBag.searchString = searchString;
                ViewBag.criadoPor = criadoPor;
                ViewBag.dataFinal = dataFinal;
                ViewBag.dataInicial = dataInicial;

                switch (sortOrder)
                {
                    case "Nome":
                        sql = sql.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        sql = sql.OrderByDescending(x => x.Name);
                        break;
                    case "Banco":
                        sql = sql.OrderBy(x => x.Database.Name);
                        break;
                    case "BancoDesc":
                        sql = sql.OrderByDescending(x => x.Database.Name);
                        break;
                    case "Tipo":
                        sql = sql.OrderBy(x => x.Type);
                        break;
                    case "TipoDesc":
                        sql = sql.OrderByDescending(x => x.Type);
                        break;
                    case "Ativo":
                        sql = sql.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        sql = sql.OrderByDescending(x => x.Enable);
                        break;
                    case "Data":
                        sql = sql.OrderBy(x => x.Date);
                        break;
                    case "DataDesc":
                        sql = sql.OrderByDescending(x => x.Date);
                        break;
                    case "Usuario":
                        sql = sql.OrderBy(x => x.User);
                        break;
                    case "UsuarioDesc":
                        sql = sql.OrderByDescending(x => x.User);
                        break;
                    default:
                        sql = sql.OrderByDescending(x => x.Date);
                        break;
                }
                ViewBag.sql = await sql.ToPagedListAsync(pageNumber, itensPages);

                ViewBag.sqlEnabled = new SelectList(new[] { new { ID = "", Name = "Todos" }, new { ID = "true", Name = "Sim" }, new { ID = "false", Name = "Não" }, }, "ID", "Name", sqlEnabled);
                ViewBag.sqlType = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "1", Name = "Arquivo" }, new { ID = "2", Name = "Texto" }, }, "ID", "Name", sqlType);
                ViewBag.sqlDatabaseId = new SelectList(_context.DatabaseGA.OrderBy(x => x.Name), "Id", "Name", sqlDatabaseId);

                GALibrary.GALogs.SaveLog("SQL", "Pesquisa de scripts de banco realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }

                return View();


            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("SQL", "Erro ao pesquisar scripts de banco pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var sql = await _context.SQL.Include(s => s.Database).FirstOrDefaultAsync(m => m.Id == id);
                if (sql == null)
                {
                    return NotFound();
                }

                GALibrary.GALogs.SaveLog("SQL", "Vizualizacao dos detalhes do script de banco " + sql.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(sql);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("SQL", "Erro ao vizualizar detalhes do script de banco com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

        }


        [Authorize(Policy = "Atualização")]
        public IActionResult Create(int? updateApplicationId)
        {
            try
            {
                CreateInternal(updateApplicationId);
                GALibrary.GALogs.SaveLog("SQL", "Inicio do cadastro de um script SQL pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("SQL", "Erro ao iniciar cadastro de scripts de banco pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Atualização")]
        public IActionResult CreatePartialView(int? updateApplicationId)
        {
            try
            {
                CreateInternal(updateApplicationId);
                GALibrary.GALogs.SaveLog("SQL", "Inicio do cadastro de um script SQL pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return PartialView();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("SQL", "Erro ao iniciar cadastro de scripts de banco pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Atualização")]
        public void CreateInternal(int? updateApplicationId)
        {
            if (updateApplicationId == null)
            {
                ViewBag.Empty = false;
                ViewBag.sqlDatabaseId = new SelectList(_context.DatabaseGA.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name");
            }
            else
            {
                ViewBag.Empty = true;
                ViewBag.sqlDatabaseId = new SelectList(_context.DatabaseGA.Where(x => x.Enable && x.ApplicationId == updateApplicationId).OrderBy(x => x.Name), "Id", "Name");
            }

            GALibrary.GALogs.SaveLog("SQL", "Inicio do cadastro de scripts de banco " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
            ViewBag.sqlType = new SelectList(new[] { new { ID = "1", Name = "Arquivo" }, new { ID = "2", Name = "Texto" }, }, "ID", "Name");
        }


        [Authorize(Policy = "Atualização")]
        [HttpPost]
        public async Task<ActionResult> SalvaDados()
        {
            String status = "1";
            try
            {
                String name = Request.Form["sqlName"];
                String sqlScript = Request.Form["sqlScript"];
                int type = Convert.ToInt32(Request.Form["sqlType"]);
                int databaseId = Convert.ToInt32(Request.Form["sqlDatabaseId"]);
                IFormFileCollection files = Request.Form.Files;

                GALibrary.Models.SQL sql = new GALibrary.Models.SQL();

                sql.Name = name;
                sql.SQLScript = sqlScript;
                sql.Type = type;
                sql.DatabaseId = databaseId;
                sql.Date = DateTime.Now;
                sql.User = User.Identity.Name;
                sql.Enable = true;
                sql.FilesRemoved = false;


                _context.SQL.Add(sql);
                await _context.SaveChangesAsync();
                _context.Entry(sql).GetDatabaseValues();

                if (type == 1)
                {
                    await Lib.GAClass.SaveFiles(files, sql.Id, "SQL", 1, _context.Parameter.FirstOrDefault());
                }

                GALibrary.GALogs.SaveLog("SQL", "Fim do cadastro de scripts de banco " + sql.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("SQL", "Erro ao cadastrar scripts de banco pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var sql = await _context.SQL.Include(x => x.Database).FirstOrDefaultAsync(x => x.Id == id);
                if (sql == null)
                {
                    return NotFound();
                }

                ViewBag.sqlEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", sql.Enable);
                ViewBag.sqlType = new SelectList(new[] { new { ID = "1", Name = "Arquivo" }, new { ID = "2", Name = "Texto" }, }, "ID", "Name", sql.Type);
                ViewBag.sqlDatabaseId = new SelectList(_context.DatabaseGA.OrderBy(x => x.Name), "Id", "Name", sql.DatabaseId);

                GALibrary.GALogs.SaveLog("SQL", "Inicio da edicao do script de banco " + sql.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(sql);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("SQL", "Erro ao iniciar edicao do script de banco com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Atualização")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,User,Date,DatabaseId,Type,SQLScript,Enable,FilesRemoved")] SQL sql, IFormFileCollection sqlFilesUpload, int sqlDatabaseId, int sqlType)
        {
            Boolean valida = true;
            Parameter parameter = _context.Parameter.FirstOrDefault();

            if (id != sql.Id)
            {
                return NotFound();
            }

            try
            {

                sql.Date = DateTime.Now;
                sql.User = User.Identity.Name;
                sql.DatabaseId = sqlDatabaseId;
                sql.FilesRemoved = false;
                sql.Type = sqlType;

                if (ModelState.IsValid)
                {
                    if (sqlFilesUpload.Count > 0)
                    {
                        try
                        {
                            GALibrary.GAFiles.DeleteFiles(sql.Id, sql.Date, 1, parameter);
                            await Lib.GAClass.SaveFiles(sqlFilesUpload, sql.Id, "SQL", 1, _context.Parameter.FirstOrDefault());

                            GALibrary.GALogs.SaveLog("SQL", "Novos arquivos salvos pelo usuario " + User.Identity.Name + " ao editar " + sql.Name, 2, _context.Parameter.FirstOrDefault());
                        }
                        catch (Exception erro)
                        {
                            GALibrary.GALogs.SaveLog("SQL", "Erro ao salvar novos arquivos pelo usuario " + User.Identity.Name + " ao editar o " + sql.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                            valida = false;
                        }

                    }

                    if (valida) {
                        _context.Update(sql);
                        await _context.SaveChangesAsync();

                        GALibrary.GALogs.SaveLog("SQL", "Fim da edicao do script de banco " + sql.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                        return RedirectToAction(nameof(Index));
                    } 
                
                }

                ViewBag.sqlEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", sql.Enable);
                ViewBag.sqlType = new SelectList(new[] {  new { ID = "1", Name = "Arquivo" }, new { ID = "2", Name = "Texto" }, }, "ID", "Name", sql.Type);
                ViewBag.sqlDatabaseId = new SelectList(_context.DatabaseGA.OrderBy(x => x.Name), "Id", "Name", sql.DatabaseId);

                return View(sql);

            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("SQL", "Erro ao editar script de banco " + sql.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var sql = await _context.SQL.Include(s => s.Database).FirstOrDefaultAsync(m => m.Id == id);
                if (sql == null)
                {
                    return NotFound();
                }

                _context.SQL.Remove(sql);
                await _context.SaveChangesAsync();

                GALibrary.GALogs.SaveLog("SQL", "Script de banco " + sql.Name + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return RedirectToAction(nameof(Index));
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("SQL", "Erro ao remover script de banco com " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }
        }

        [Authorize(Policy = "Vizualização")]
        public ActionResult DownloadFile(int sqlId)
        {

            var content_type = "application/zip";

            try
            {
                GALibrary.GALogs.SaveLog("SQL", "Realizado download do arquivo com id " + sqlId + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return File(Lib.GAClass.GetSQLFile(sqlId), content_type, "sql.zip");
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("File", "Erro ao fazer download arquivo com id " + sqlId + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());

                content_type = "text/txt";
                byte[] byteArray = Encoding.UTF8.GetBytes(erro.ToString());
                Stream stream = new MemoryStream(byteArray);

                return File(stream, content_type, "Erro.txt");
            }

        }

        [Authorize(Policy = "Vizualização")]
        private bool SQLExists(int id)
        {
            return _context.SQL.Any(e => e.Id == id);
        }
    }
}
