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
    public class FileHistoriesController : Controller
    {
        private readonly GAContext _context;

        public FileHistoriesController(GAContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "Vizualização")]
        public async Task<IActionResult> Index(int? page, string sortOrder, string sortOrderOld, String nomeAquivo, int ApplicationIdFileHistory, String criadoPor, String dataFinal, String dataInicial, int? demanda)
        {
            try
            {
                var fileHistory = _context.FileHistory.Include(f => f.File).Include(u => u.File.Application).Include(t => t.Update).AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;

                if (!string.IsNullOrEmpty(nomeAquivo))
                {
                    fileHistory = fileHistory.Where(x => x.FileName.Contains(nomeAquivo));
                }

                if (!string.IsNullOrEmpty(criadoPor))
                {
                    fileHistory = fileHistory.Where(x => x.File.User.Contains(criadoPor));
                }
                if (ApplicationIdFileHistory > 0)
                {
                    fileHistory = fileHistory.Where(x => x.File.ApplicationId == ApplicationIdFileHistory);
                }

                if (dataInicial != null)
                {
                    fileHistory = fileHistory.Where(x => x.Date >= Convert.ToDateTime(dataInicial));
                }

                if (dataFinal != null)
                {
                    fileHistory = fileHistory.Where(x => x.Date <= Convert.ToDateTime(dataFinal).AddHours(23).AddMinutes(59).AddSeconds(59));
                }

                if (demanda != null)
                {
                    fileHistory = fileHistory.Where(x => x.Update.Demanda == demanda );
                }

                if (sortOrder!= null && sortOrderOld != null && sortOrder == sortOrderOld && !sortOrder.EndsWith("Desc") && pageNumber == 1)
                {
                    sortOrder += "Desc";
                }


                ViewBag.sortOrder = sortOrder;
                ViewBag.sortOrderOld = sortOrder;
                ViewBag.nomeAquivo = nomeAquivo;
                ViewBag.dataFinal = dataFinal;
                ViewBag.dataInicial = dataInicial;
                ViewBag.criadoPor = criadoPor;
                ViewBag.demanda = demanda;


                switch (sortOrder)
                {
                    case "Aplicacao":
                        fileHistory = fileHistory.OrderBy(x => x.File.Application.Name);
                        break;
                    case "AplicacaoDesc":
                        fileHistory = fileHistory.OrderByDescending(x => x.File.Application.Name);
                        break;
                    case "Atualicacao":
                        fileHistory = fileHistory.OrderBy(x => x.UpdateId);
                        break;
                    case "AtualicacaoDesc":
                        fileHistory = fileHistory.OrderByDescending(x => x.UpdateId);
                        break;
                    case "Data":
                        fileHistory = fileHistory.OrderBy(x => x.Date);
                        break;
                    case "DataDesc":
                        fileHistory = fileHistory.OrderByDescending(x => x.Date);
                        break;
                    case "Arquivo":
                        fileHistory = fileHistory.OrderBy(x => x.FileName);
                        break;
                    case "ArquivoDesc":
                        fileHistory = fileHistory.OrderByDescending(x => x.FileName);
                        break;
                    case "Usuario":
                        fileHistory = fileHistory.OrderBy(x => x.File.User);
                        break;
                    case "UsuarioDesc":
                        fileHistory = fileHistory.OrderByDescending(x => x.File.User);
                        break;
                    case "Demanda":
                        fileHistory = fileHistory.OrderBy(x => x.Update.Demanda);
                        break;
                    case "DemandaDesc":
                        fileHistory = fileHistory.OrderByDescending(x => x.Update.Demanda);
                        break;
                    case "Tamanho":
                        fileHistory = fileHistory.OrderBy(x => x.Size);
                        break;
                    case "TamanhoDesc":
                        fileHistory = fileHistory.OrderByDescending(x => x.Size);
                        break;
                    default:
                        fileHistory = fileHistory.OrderByDescending(x => x.Date);
                        break;
                }

                ViewBag.fileHistory = await fileHistory.ToPagedListAsync(pageNumber, itensPages);
                ViewBag.ApplicationIdFileHistory = new SelectList(_context.Application.Where(x => x.Enable).OrderBy(x => x.Name), "Id", "Name", ApplicationIdFileHistory);

                GALibrary.GALogs.SaveLog("FileHistory", "Listagem de atualizações realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());


                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("FileHistory", "Erro ao pesquisar atualizações pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
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
                var fileHistory = await _context.FileHistory.Include(f => f.Update).FirstOrDefaultAsync(m => m.FileId == id);
                if (fileHistory == null)
                {
                    return NotFound();
                }

                return View(fileHistory);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("FileHistory", "Erro ao vizualizar detalhes de atualizações com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }
        }

        [Authorize(Policy = "Vizualização")]
        private bool FileHistoryExists(int id)
        {
            return _context.FileHistory.Any(e => e.FileId == id);
        }
    }
}
