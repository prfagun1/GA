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
    public class PermissionGroupsController : Controller
    {
        private readonly GAContext _context;

        public PermissionGroupsController(GAContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "Administração")]
        private SelectList GetPermissionNames(int? selected)
        {
            return new SelectList(new[] {
                new { ID = "0", Name = "Administração" },
                new { ID = "2", Name = "Aprovação" },
                new { ID = "3", Name = "Atualização" },
                new { ID = "1", Name = "Cadastro" },
                new { ID = "4", Name = "Vizualização" },
            }, "ID", "Name", selected);
        }


        [Authorize(Policy = "Administração")]
        public async Task<IActionResult> Index(int? page, bool? PermissionGroupsEnabled, String searchString, string sortOrder, string sortOrderOld, Boolean registroApagado, int? AccessType)
        {
            try
            {
                var permissionGroup = _context.PermissionGroup.AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;


                if (!string.IsNullOrEmpty(searchString))
                {
                    permissionGroup = permissionGroup.Where(x => x.Name.Contains(searchString));
                }

                if (PermissionGroupsEnabled != null)
                {
                    permissionGroup = permissionGroup.Where(x => x.Enable == Convert.ToBoolean(PermissionGroupsEnabled));
                }

                if (AccessType != null)
                {
                    permissionGroup = permissionGroup.Where(x => x.AccessType == AccessType);
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
                        permissionGroup = permissionGroup.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        permissionGroup = permissionGroup.OrderByDescending(x => x.Name);
                        break;
                    case "Ativo":
                        permissionGroup = permissionGroup.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        permissionGroup = permissionGroup.OrderByDescending(x => x.Enable);
                        break;
                    case "Permissao":
                        permissionGroup = permissionGroup.OrderBy(x => x.AccessType);
                        break;
                    case "PermissaoDesc":
                        permissionGroup = permissionGroup.OrderByDescending(x => x.AccessType);
                        break;
                    default:
                        permissionGroup = permissionGroup.OrderBy(x => x.Name);
                        break;
                }

                ViewBag.permissionGroup = await permissionGroup.ToPagedListAsync(pageNumber, itensPages);
                ViewBag.PermissionGroupEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", PermissionGroupsEnabled);
                ViewBag.AccessType = this.GetPermissionNames(AccessType);

                GALibrary.GALogs.SaveLog("PermissionGroup", "Pesquisa de grupo de permissao realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());


                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }


                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("PermissionGroup", "Erro ao pesquisar grupos de permissao pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }


        [Authorize(Policy = "Administração")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var permissionGroup = await _context.PermissionGroup.FirstOrDefaultAsync(m => m.Id == id);
                if (permissionGroup == null)
                {
                    return NotFound();
                }

                GALibrary.GALogs.SaveLog("PermissionGroup", "Vizualização do grupo de permissao " + permissionGroup.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View(permissionGroup);
            }
            catch(Exception erro)
            {
                GALibrary.GALogs.SaveLog("PermissionGroup", "Erro ao vizualizar detalhes do grupo de permissao com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

        }

        [Authorize(Policy = "Administração")]
        public IActionResult Create()
        {
            try
            {
                ViewBag.AccessType = this.GetPermissionNames(null);
                GALibrary.GALogs.SaveLog("PermissionGroup", "Inicio do cadastro de um grupo de permissao pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("PermissionGroup", "Erro ao iniciar cadastro de grupos de permissao pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Administração")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Domain,AccessType,Enable,User,Date")] PermissionGroup permissionGroup)
        {
            try
            {
                permissionGroup.Date = DateTime.Now;
                permissionGroup.User = User.Identity.Name;
                permissionGroup.Enable = true;

                if (ModelState.IsValid)
                {
                    _context.Add(permissionGroup);
                    await _context.SaveChangesAsync();
                    GALibrary.GALogs.SaveLog("PermissionGroup", "Fim do cadastro do grupo de permissao " + permissionGroup.Name + " pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.AccessType = this.GetPermissionNames(permissionGroup.AccessType);
                return View(permissionGroup);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("PermissionGroup", "Erro ao cadastrar grupo de permissao pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Administração")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var permissionGroup = await _context.PermissionGroup.FindAsync(id);
                if (permissionGroup == null)
                {
                    return NotFound();
                }

                GALibrary.GALogs.SaveLog("PermissionGroup", "Inicio da edicao do grupo de permissao " + permissionGroup.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                ViewBag.AccessType = this.GetPermissionNames(permissionGroup.AccessType);
                ViewBag.PermissionGroupEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", permissionGroup.Enable);

                return View(permissionGroup);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("PermissionGroup", "Erro ao iniciar edicao do grupo de permissao com id " + id + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Administração")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Domain,AccessType,Enable,User,Date")] PermissionGroup permissionGroup)
        {
            if (id != permissionGroup.Id)
            {
                return NotFound();
            }

            try
            {
                permissionGroup.Date = DateTime.Now;
                permissionGroup.User = User.Identity.Name;

                if (ModelState.IsValid)
                {
                    _context.Update(permissionGroup);
                    await _context.SaveChangesAsync();
                    GALibrary.GALogs.SaveLog("PermissionGroup", "Fim da edicao do grupo de permissao " + permissionGroup.Name + " realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.AccessType = this.GetPermissionNames(permissionGroup.AccessType);
                ViewBag.PermissionGroupEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", permissionGroup.Enable);

                return View(permissionGroup);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("PermissionGroup", "Erro ao editar grupo de permissao " + permissionGroup.Name + " pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Administração")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var permissionGroup = await _context.PermissionGroup.FirstOrDefaultAsync(m => m.Id == id);
                if (permissionGroup == null)
                {
                    return NotFound();
                }

                _context.PermissionGroup.Remove(permissionGroup);
                await _context.SaveChangesAsync();

                GALibrary.GALogs.SaveLog("PermissionGroup", "Grupo de permissao " + permissionGroup.Name + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return RedirectToAction(nameof(Index));
            }
            catch(Exception erro)
            {
                GALibrary.GALogs.SaveLog("PermissionGroup", "Erro ao remover grupo de permissao com ID " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
            }

        }


        [Authorize(Policy = "Administração")]
        private bool PermissionGroupExists(int id)
        {
            return _context.PermissionGroup.Any(e => e.Id == id);
        }
    }
}
