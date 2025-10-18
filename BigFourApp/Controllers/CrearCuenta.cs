using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BigFourApp.Models;
using BigFourApp.Persistence;

namespace BigFourApp.Controllers;

public class CrearCuenta : Controller
{
    public IEventRepository _event;

    public CrearCuenta(IEventRepository evento)
    {
        _event = evento;
    }

    public IActionResult Create()
    {
        return View();
    }
    //public ActionResult Create(Usuarios usuarios)
    //{
    //    if (!ModelState.IsValid)
    //    {
    //        return View(usuarios);
    //    }

    //    Usuarios created = _articleRepository.Create(usuarios);

    //    return RedirectToAction(nameof(Index), new { id = created.Id_Usuario });
    //}
}