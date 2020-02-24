using GALibrary.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GA.Controllers
{
    public class AccountController : Controller
    {

        private readonly IOptions<Ldap> config;
        private readonly GAContext db;

        public AccountController(IOptions<Ldap> config, GAContext context)
        {
            this.config = config;
            this.db = context;
        }

        public IActionResult Index() => View(config.Value);

    public ActionResult Login()
        {
            return this.View();
        }

        [AllowAnonymous]
        public ActionResult AccessDenied()
        {
            return this.View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginData model, string returnUrl)
        {
            List<Claim> claims = new List<Claim>();
            var listaGrupos = db.PermissionGroup.ToList();

            if (ModelState.IsValid)
            {

                claims = Lib.AD.ValidaAcesso(config.Value, model, listaGrupos);

                if (claims == null)
                {
                    ModelState.AddModelError("", "Usuário ou senha inválida");
                    return this.View(model);
                }

                // Create the identity from the user info
                
                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, model.Username));
                identity.AddClaim(new Claim(ClaimTypes.Name, model.Username));


                identity.AddClaims(claims);

                // Authenticate using the identity
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = model.RememberMe });

                return this.RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "O usuario e a senha não podem estar em branco");
                return this.View(model);
            }
        }

        public async Task<IActionResult> LogOff()
        {
            await HttpContext.SignOutAsync();
            return this.RedirectToAction("Login", "Account");
        }
    }
}