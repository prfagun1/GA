using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GALibrary.Models;
using Microsoft.AspNetCore.Authorization;

namespace GA.Controllers
{
    public class ParametersController : Controller
    {
        private readonly GAContext _context;

        public ParametersController(GAContext context)
        {
            _context = context;
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
                var parameter = await _context.Parameter.FirstOrDefaultAsync(m => m.Id == id);
                if (parameter == null)
                {
                    return NotFound();
                }

                GALibrary.GALogs.SaveLog("Parameter", "Vizualização dos parametros da aplicacao realizado pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return View(parameter);
            }
            catch(Exception erro) {
                GALibrary.GALogs.SaveLog("Parameter", "Erro ao vizualizar parametros da aplicacao pelo usuario " + User.Identity.Name + " erro: " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return NotFound();
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
                var parameter = await _context.Parameter.FindAsync(id);
                if (parameter == null)
                {
                    return NotFound();
                }

                ViewBag.LogLevelApplication = new SelectList(new[] { new { ID = "1", Name = "Somente Erros" }, new { ID = "2", Name = "Debug" }, }, "ID", "Name", parameter.LogLevelApplication);
                ViewBag.LogLevelUpdate = new SelectList(new[] { new { ID = "1", Name = "Somente Erros" }, new { ID = "2", Name = "Debug" }, }, "ID", "Name", parameter.LogLevelUpdate);

                GALibrary.GALogs.SaveLog("Parameter", "Inicio da edicao dos parametros da aplicacao realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());

                return View(parameter);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Parameter", "Erro ao editar parametros pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Administração")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PathBackup,PathUpdate,PathLog,PathTemp,LogLevelApplication,LogLevelUpdate,User,Datetime,RetentionTime,ItensPage")] Parameter parameter)
        {
            if (id != parameter.Id)
            {
                return NotFound();
            }

            try
            {
                parameter.Datetime = DateTime.Now;
            parameter.User = User.Identity.Name;

            if (ModelState.IsValid)
            {

                    _context.Update(parameter);
                    await _context.SaveChangesAsync();


                GALibrary.GALogs.SaveLog("Parameter", "Fim da edicao dos parâmetros de ambiente realizada pelo usuario " + User.Identity.Name, 2, _context.Parameter.FirstOrDefault());
                return RedirectToAction("Details", new { id = 1 });
            }

            ViewBag.LogLevelApplication = new SelectList(new[] { new { ID = "1", Name = "Somente Erros" }, new { ID = "2", Name = "Debug" }, }, "ID", "Name", parameter.LogLevelApplication);
            ViewBag.LogLevelUpdate = new SelectList(new[] { new { ID = "1", Name = "Somente Erros" }, new { ID = "2", Name = "Debug" }, }, "ID", "Name", parameter.LogLevelUpdate);

            return View(parameter);
            }
            catch (Exception erro)
            {
                GALibrary.GALogs.SaveLog("Parameter", "Erro ao editar parametros pelo usuario " + User.Identity.Name + ": " + erro.ToString(), 1, _context.Parameter.FirstOrDefault());
                return View("~/Views/Shared/Error.cshtml");
            }
        }

        [Authorize(Policy = "Administração")]
        private bool ParameterExists(int id)
        {
            return _context.Parameter.Any(e => e.Id == id);
        }
    }
}
