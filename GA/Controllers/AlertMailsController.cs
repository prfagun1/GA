using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using X.PagedList;
using GALibrary.Models;

namespace GA.Controllers
{
    public class AlertMailsController : Controller
    {
        private readonly GAContext _context;

        public AlertMailsController(GAContext context)
        {
            _context = context;
        }



        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Index(int? page, int alertMailEnabled, String nome, string sortOrder, string sortOrderOld, Boolean registroApagado, String email, int alertMailPadrao)
        {
            try
            {

                var alertMail = _context.AlertMail.AsQueryable();

                int itensPages = _context.Parameter.FirstOrDefault().ItensPage ?? 10;
                var pageNumber = page ?? 1;

                if (!string.IsNullOrEmpty(nome))
                {
                    alertMail = alertMail.Where(x => x.Name.Contains(nome));
                }

                if (!string.IsNullOrEmpty(email))
                {
                    alertMail = alertMail.Where(x => x.Email.Contains(email));
                }

                if (alertMailEnabled > 0)
                {
                    alertMail = alertMail.Where(x => x.Enable == Convert.ToBoolean(alertMailEnabled));
                }

                if (alertMailPadrao > 0)
                {
                    alertMail = alertMail.Where(x => x.Default == Convert.ToBoolean(alertMailPadrao));
                }

                if (sortOrder != null && sortOrderOld != null && sortOrder == sortOrderOld && !sortOrder.EndsWith("Desc") && pageNumber == 1)
                {
                    sortOrder += "Desc";
                }

                switch (sortOrder)
                {
                    case "Nome":
                        alertMail = alertMail.OrderBy(x => x.Name);
                        break;
                    case "NomeDesc":
                        alertMail = alertMail.OrderByDescending(x => x.Name);
                        break;
                    case "Email":
                        alertMail = alertMail.OrderBy(x => x.Email);
                        break;
                    case "EmailDesc":
                        alertMail = alertMail.OrderByDescending(x => x.Email);
                        break;
                    case "Padrao":
                        alertMail = alertMail.OrderBy(x => x.Default);
                        break;
                    case "AmbienteDesc":
                        alertMail = alertMail.OrderByDescending(x => x.Default);
                        break;
                    case "Ativo":
                        alertMail = alertMail.OrderBy(x => x.Enable);
                        break;
                    case "AtivoDesc":
                        alertMail = alertMail.OrderByDescending(x => x.Enable);
                        break;
                    default:
                        alertMail = alertMail.OrderBy(x => x.Default).OrderBy(x => x.Name);
                        break;
                }

                ViewBag.sortOrder = sortOrder;
                ViewBag.sortOrderOld = sortOrder;
                ViewBag.nome = nome;
                ViewBag.email = email;

                ViewBag.alertMail = await alertMail.ToPagedListAsync(pageNumber, itensPages);
                ViewBag.alertMailEnabled = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "1", Name = "Sim" }, new { ID = "2", Name = "Não" }, }, "ID", "Name", alertMailEnabled);

                ViewBag.alertMailPadrao = new SelectList(new[] { new { ID = "0", Name = "Todos" }, new { ID = "1", Name = "Sim" }, new { ID = "2", Name = "Não" }, }, "ID", "Name", alertMailPadrao);

                GALibrary.GALogs.SaveLog("Application", "Pesquisa de aplicacao realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                if (registroApagado)
                {
                    ViewBag.RegistroApagado = "<p>Registro apagado com sucesso </p>";
                }

                return View();
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("AlertMail", "Erro ao pesquisar emails pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }

        }


        [Authorize(Policy = "Cadastro")]
        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Create([Bind("Id,Name,Email,Default,Enable")] AlertMail alertMail)
        {
            if (ModelState.IsValid)
            {
                if (alertMail.Default) {
                    IQueryable<AlertMail> alertMails = _context.AlertMail;
                    foreach (AlertMail mail in alertMails) {
                        mail.Default = false;
                    }
                }

                alertMail.Enable = true;
                _context.Add(alertMail);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(alertMail);
        }

        [Authorize(Policy = "Cadastro")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var alertMail = await _context.AlertMail.FindAsync(id);
            if (alertMail == null)
            {
                return NotFound();
            }

            ViewBag.AlertMailsEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", alertMail.Enable);
            return View(alertMail);
        }


        [Authorize(Policy = "Cadastro")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Default,Enable")] AlertMail alertMail)
        {
            
            if (id != alertMail.Id)
            {
                return NotFound();
            }

            IQueryable<AlertMail> alertMailDefault = _context.AlertMail.Where(x => x.Default && x.Id != alertMail.Id);

            if(alertMailDefault.Count() == 0 && !alertMail.Default){
                ModelState.AddModelError("Default", "É preciso ter ao menos um e-mail como padrão.");
            }


            if (alertMail.Default)
            {
                IQueryable<AlertMail> alertMails = _context.AlertMail.Where(x => x.Id != alertMail.Id);
                foreach (AlertMail mail in alertMails)
                {
                    mail.Default = false;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(alertMail);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AlertMailExists(alertMail.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AlertMailsEnabled = new SelectList(new[] { new { ID = true, Name = "Sim" }, new { ID = false, Name = "Não" }, }, "ID", "Name", alertMail.Enable);

            return View(alertMail);
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
                var alertEmail = await _context.AlertMail.FirstOrDefaultAsync(m => m.Id == id);

                if (alertEmail == null)
                {
                    return NotFound();
                }
                else
                {

                    alertEmail = await _context.AlertMail.FindAsync(id);
                    _context.AlertMail.Remove(alertEmail);
                    await _context.SaveChangesAsync();

                    GALibrary.GALogs.SaveLog("AlertMail", "E-mail " + alertEmail.Name + " removido pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("AlertMail", "Erro ao remover e-mail com id " + id + " pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return RedirectToAction(nameof(Index));
            }
        }



        private bool AlertMailExists(int id)
        {
            return _context.AlertMail.Any(e => e.Id == id);
        }
    }
}
