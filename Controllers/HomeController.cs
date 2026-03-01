using Microsoft.AspNetCore.Mvc;

namespace MiniLibrary.Controllers;

public class HomeController : Controller
{
    public IActionResult Error() => View();
}
