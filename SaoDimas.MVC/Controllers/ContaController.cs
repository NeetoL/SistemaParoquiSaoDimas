using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SaoDimas.Aplicacao.Services.Interface;
using SaoDimas.MVC.Models;
namespace SaoDimas.MVC.Controllers;
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ContaController(IUsuariosAplicacao usuarios) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return Voltar(returnUrl);
        return View(new LoginViewModel { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    }

    [AllowAnonymous, HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel formulario)
    {
        ArgumentNullException.ThrowIfNull(formulario);
        if (!ModelState.IsValid) return View(formulario);
        var usuario = await usuarios.AutenticarAsync(formulario.Usuario, formulario.Senha, HttpContext.RequestAborted);
        if (usuario is null)
        {
            ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos.");
            formulario.Senha = string.Empty;
            ModelState.Remove(nameof(formulario.Senha));
            return View(formulario);
        }
        var identidade = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
             new Claim(ClaimTypes.Name, usuario.Login),
             new Claim(ClaimTypes.Role, usuario.Perfil), new Claim("versao", usuario.Versao.ToString(System.Globalization.CultureInfo.InvariantCulture))], CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidade), new AuthenticationProperties { IsPersistent = false });
        return Voltar(formulario.ReturnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet] public IActionResult AcessoNegado() { Response.StatusCode = 403; return View("~/Views/Shared/Estado.cshtml", 403); }
    [HttpGet] public async Task<IActionResult> Perfil(CancellationToken ct) => View((await usuarios.ListarAsync(ct)).Single(u=>u.Id.ToString()==User.FindFirstValue(ClaimTypes.NameIdentifier)));
    [HttpGet] public IActionResult Senha() => View();
    [HttpPost] public async Task<IActionResult> Senha(string atual,string nova) {
        var resultado=await usuarios.AlterarSenhaAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),atual??"",nova??"",HttpContext.RequestAborted);
        if(!resultado.Sucesso){ModelState.AddModelError("",resultado.Mensagem);return View();}
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);return RedirectToAction(nameof(Login));
    }
    private IActionResult Voltar(string? returnUrl) => Url.IsLocalUrl(returnUrl)
        ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
}
