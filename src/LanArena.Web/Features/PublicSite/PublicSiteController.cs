using LanArena.Application.Features.PublicSite.GetPublicTournament;
using Microsoft.AspNetCore.Mvc;

namespace LanArena.Web.Features.PublicSite;

[Route("")]
public sealed class PublicSiteController(GetPublicTournamentHandler getTournament) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View("~/Views/PublicSite/Index.cshtml", await getTournament.Handle(cancellationToken));
    [HttpGet("schedule")]
    public async Task<IActionResult> Schedule(CancellationToken cancellationToken) =>
        View("~/Views/PublicSite/Schedule.cshtml", await getTournament.Handle(cancellationToken));
    [HttpGet("teams")]
    public async Task<IActionResult> Teams(CancellationToken cancellationToken) =>
        View("~/Views/PublicSite/Teams.cshtml", await getTournament.Handle(cancellationToken));
    [HttpGet("racers")]
    public async Task<IActionResult> Racers(CancellationToken cancellationToken) =>
        View("~/Views/PublicSite/Racers.cshtml", await getTournament.Handle(cancellationToken));
    [HttpGet("rules")]
    public IActionResult Rules() => View("~/Views/PublicSite/Rules.cshtml");
    [HttpGet("leaderboard/cod4")]
    public async Task<IActionResult> Cod4Leaderboard(CancellationToken cancellationToken) =>
        View("~/Views/PublicSite/Cod4Leaderboard.cshtml", await getTournament.Handle(cancellationToken));
    [HttpGet("leaderboard/nfs")]
    public async Task<IActionResult> NfsLeaderboard(CancellationToken cancellationToken) =>
        View("~/Views/PublicSite/NfsLeaderboard.cshtml", await getTournament.Handle(cancellationToken));

    [HttpGet("error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View("~/Views/Shared/Error.cshtml");
}
