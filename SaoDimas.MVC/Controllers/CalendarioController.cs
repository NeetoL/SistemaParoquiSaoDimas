using Microsoft.AspNetCore.Mvc;
namespace SaoDimas.MVC.Controllers;
public sealed class CalendarioController : Controller
{
    public IActionResult Index() => View();
}
