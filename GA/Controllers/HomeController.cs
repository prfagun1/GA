using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GALibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace GA.Controllers
{
    public class HomeController : Controller
    {

        private readonly GAContext _context;

        public HomeController(GAContext context)
        {
            _context = context;
        }

        [Authorize(Policy = "Vizualização")]
        public IActionResult Index()
        {

            DateTime day1 = DateTime.Today.AddDays(1);
            DateTime day7 = DateTime.Today.AddDays(7);
            DateTime day30 = DateTime.Today.AddDays(30);

            DateTime day1N = DateTime.Today.AddDays(-1);
            DateTime day7N = DateTime.Today.AddDays(-7);
            DateTime day30N = DateTime.Today.AddDays(-30);


            ViewBag.ScheduledDays1 = _context.UpdateGA.Where(x => x.Status == 0 && x.Schedule <= day1).Count();
            ViewBag.ScheduledDays7 = _context.UpdateGA.Where(x => x.Status == 0 && x.Schedule <= day7).Count();
            ViewBag.ScheduledDays30 = _context.UpdateGA.Where(x => x.Status == 0 && x.Schedule <= day30).Count();

            ViewBag.DoneDays1 = _context.UpdateGA.Where(x => x.Status > 0 && x.Schedule >= day1N).Count();
            ViewBag.DoneDays7 = _context.UpdateGA.Where(x => x.Status > 0 && x.Schedule >= day7N).Count();
            ViewBag.DoneDays30 = _context.UpdateGA.Where(x => x.Status > 0 && x.Schedule >= day30N).Count();


            ViewBag.UpdateQuantity = _context.UpdateGA.Where(x => x.Status == 0).Count();
            ViewBag.UpdateQuantityDone = _context.UpdateGA.Where(x => x.Status > 0).Count();

            List<UpdateGA> updateList = _context.UpdateGA.OrderByDescending(x => x.Date).Take(3).ToList();
            ViewBag.UpdateList = updateList;


            List<UpdateGA> updateListUser = _context.UpdateGA.Where(x => x.User == User.Identity.Name).OrderByDescending(x => x.Date).Take(3).ToList();
            ViewBag.UpdateListUser = updateListUser;

            ViewBag.Database = _context.DatabaseGA.Count();
            ViewBag.Application = _context.Application.Count();
            ViewBag.Server = _context.Server.Count();

            return View();
        }

        [Authorize( Policy = "Vizualização")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
