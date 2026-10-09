using LanArena.Application.Features.PublicSite.RegisterNfsRacer;
using Microsoft.AspNetCore.Mvc;

namespace LanArena.Web.Features.Registration;

[Route("register/nfs")]
public sealed class NfsRegistrationController(RegisterNfsRacerHandler registerRacer) : Controller
{
    [HttpGet("")]
    public IActionResult Create() => View("~/Views/Registration/Nfs.cshtml", new NfsRegistrationInput());

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NfsRegistrationInput input, CancellationToken cancellationToken)
    {
        if (!input.AcceptRules) ModelState.AddModelError(nameof(input.AcceptRules), "Accept the tournament rules to continue.");
        if (!ModelState.IsValid) return View("~/Views/Registration/Nfs.cshtml", input);
        try
        {
            var reference = await registerRacer.Handle(new(input.FullName, input.Nickname, input.Phone, input.Email, input.StudentId), cancellationToken);
            return View("~/Views/Registration/Confirmation.cshtml", new RegistrationConfirmation(reference, input.Nickname, "Need for Speed: Most Wanted 2005"));
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("~/Views/Registration/Nfs.cshtml", input);
        }
    }
}
