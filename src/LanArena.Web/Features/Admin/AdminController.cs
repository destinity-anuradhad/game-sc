using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using LanArena.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanArena.Web.Features.Admin;

[Route("admin")]
public sealed class AdminController(TournamentDbContext db, IConfiguration configuration, IWebHostEnvironment environment) : Controller
{
    private const long MaxTeamLogoBytes = 2 * 1024 * 1024;
    [AllowAnonymous, HttpGet("login")]
    public IActionResult Login() => View("~/Views/Admin/Login.cshtml");

    [AllowAnonymous, HttpPost("login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string password, CancellationToken cancellationToken)
    {
        var expected = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            ViewData["Error"] = "Admin password is not configured. Set Admin:Password in user-secrets or the Admin__Password environment variable.";
            return View("~/Views/Admin/Login.cshtml");
        }
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(password ?? ""));
        if (!CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
        {
            ViewData["Error"] = "Incorrect password.";
            return View("~/Views/Admin/Login.cshtml");
        }
        var identity = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Tournament Admin"), new Claim(ClaimTypes.Role, "Admin")], CookieAuthenticationDefaults.AuthenticationScheme));
        db.AdminAuditLogs.Add(CreateAudit("Sign in", "Admin", "admin", "Tournament Admin", new { Authenticated = false }, new { Authenticated = true }));
        await db.SaveChangesAsync(cancellationToken);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, identity, new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
        return RedirectToAction(nameof(Index));
    }

    [Authorize, HttpPost("logout"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        db.AdminAuditLogs.Add(CreateAudit("Sign out", "Admin", "admin", User.Identity?.Name ?? "Tournament Admin", new { Authenticated = true }, new { Authenticated = false }));
        await db.SaveChangesAsync();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new AdminDashboardModel(
            await db.Teams.Include(x => x.Players).OrderBy(x => x.Name).ToListAsync(cancellationToken),
            await db.NfsRacers.OrderBy(x => x.Code).ToListAsync(cancellationToken));
        return View("~/Views/Admin/Index.cshtml", model);
    }

    [Authorize, HttpGet("activity")]
    public async Task<IActionResult> Activity(CancellationToken cancellationToken)
    {
        var logs = await db.AdminAuditLogs.AsNoTracking().OrderByDescending(x => x.OccurredAt).Take(500).ToListAsync(cancellationToken);
        return View("~/Views/Admin/Activity.cshtml", logs);
    }

    [Authorize, HttpPost("registration/{kind}/{id}/status"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRegistrationStatus(string kind, string id, string status, CancellationToken cancellationToken)
    {
        if (status is not ("Approved" or "Rejected" or "Pending" or "Suspended" or "Disqualified")) return BadRequest();
        if (kind == "cod4")
        {
            var team = await db.Teams.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (team is null) return NotFound();
            var oldStatus = team.Status;
            team.Status = status;
            db.AdminAuditLogs.Add(CreateAudit("Update status", "COD4 team", team.Id, team.Name, new { Status = oldStatus }, new { Status = status }));
        }
        else if (kind == "nfs")
        {
            var racer = await db.NfsRacers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (racer is null) return NotFound();
            var oldStatus = racer.Status;
            racer.Status = status;
            db.AdminAuditLogs.Add(CreateAudit("Update status", "NFS racer", racer.Id, racer.Nickname, new { Status = oldStatus }, new { Status = status }));
        }
        else return BadRequest();
        await db.SaveChangesAsync(cancellationToken);
        return Redirect(kind == "nfs" ? "/admin#nfs" : "/admin#cod4");
    }

    [Authorize, HttpPost("registration/cod4/{id}/edit"), ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxTeamLogoBytes + 128 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxTeamLogoBytes + 128 * 1024)]
    public async Task<IActionResult> EditTeam(string id, [FromForm] AdminTeamEditInput input, CancellationToken cancellationToken)
    {
        var team = await db.Teams.Include(x => x.Players).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (team is null) return NotFound();
        if (!ModelState.IsValid) return AdminEditError("cod4", "Check the team details and ensure names, EPFs, and contact fields are valid.");

        var name = input.Name.Trim();
        var shortName = input.ShortName.Trim();
        var duplicates = await db.Teams.AsNoTracking().AnyAsync(x => x.TournamentId == team.TournamentId && x.Id != team.Id && (x.Name == name || x.ShortName == shortName), cancellationToken);
        if (duplicates) return AdminEditError("cod4", "That team name or short name is already being used by another team.");
        if (input.Players.Count != team.Players.Count || input.Players.Select(x => x.Id).Distinct().Count() != team.Players.Count || input.Players.Any(x => team.Players.All(p => p.Id != x.Id)))
            return AdminEditError("cod4", "The roster changed while the form was open. Reload the page and try again.");
        if (input.Players.Any(x => x.Role is not ("Main" or "Substitute")))
            return AdminEditError("cod4", "Player roles must be Main or Substitute.");

        var playerNicknames = input.Players.Select(x => x.Nickname.Trim()).ToArray();
        var epfNumbers = input.Players.Select(x => x.EpfNo.Trim()).ToArray();
        var guids = input.Players.Select(x => (x.Guid ?? "").Trim()).Where(x => x.Length > 0).ToArray();
        if (playerNicknames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != playerNicknames.Length || epfNumbers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != epfNumbers.Length || guids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != guids.Length)
            return AdminEditError("cod4", "Nicknames, EPF numbers, and GUIDs must be unique within the roster.");

        var existingPlayers = await db.TeamPlayers.AsNoTracking().Where(x => x.TournamentId == team.TournamentId && x.TeamId != team.Id).ToListAsync(cancellationToken);
        if (playerNicknames.Any(value => existingPlayers.Any(x => string.Equals(x.Nickname, value, StringComparison.OrdinalIgnoreCase))) ||
            epfNumbers.Any(value => existingPlayers.Any(x => string.Equals(x.EpfNo, value, StringComparison.OrdinalIgnoreCase))) ||
            guids.Any(value => existingPlayers.Any(x => string.Equals(x.Guid, value, StringComparison.OrdinalIgnoreCase))))
            return AdminEditError("cod4", "A nickname, EPF number, or GUID is already assigned to another team.");

        string? newLogoPath = null;
        if (input.Logo is { Length: > 0 } logo)
        {
            if (logo.Length > MaxTeamLogoBytes) return AdminEditError("cod4", "The team logo must be no larger than 2 MB.");
            var extension = await GetImageExtensionAsync(logo, cancellationToken);
            if (extension is null) return AdminEditError("cod4", "Upload a valid JPG or PNG team logo.");
            var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
            var uploadDirectory = Path.Combine(webRoot, "uploads", "team-logos");
            Directory.CreateDirectory(uploadDirectory);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var destinationPath = Path.Combine(uploadDirectory, fileName);
            await using (var destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                await logo.CopyToAsync(destination, cancellationToken);
            newLogoPath = $"/uploads/team-logos/{fileName}";
        }

        var previousLogoPath = team.LogoPath;
        var previousName = team.Name;
        var before = new
        {
            team.Name, team.ShortName, team.Captain, team.Phone, team.Email, team.LogoPath,
            team.Played, team.Wins, team.Losses, team.ScoreDifference, team.Points,
            Players = team.Players.Select(x => new { x.Role, x.RealName, x.Nickname, x.EpfNo, x.Guid }).ToArray()
        };
        team.Name = name;
        team.ShortName = shortName;
        team.Captain = input.Captain.Trim();
        team.Phone = input.Phone.Trim();
        team.Email = NullIfWhiteSpace(input.Email);
        team.Played = input.Played;
        team.Wins = input.Wins;
        team.Losses = input.Losses;
        team.ScoreDifference = input.ScoreDifference;
        team.Points = input.Points;
        if (newLogoPath is not null) team.LogoPath = newLogoPath;
        else if (input.RemoveLogo) team.LogoPath = null;

        var playersById = team.Players.ToDictionary(x => x.Id);
        foreach (var editedPlayer in input.Players)
        {
            var player = playersById[editedPlayer.Id];
            player.RealName = editedPlayer.RealName.Trim();
            player.Nickname = editedPlayer.Nickname.Trim();
            player.EpfNo = editedPlayer.EpfNo.Trim();
            player.Guid = (editedPlayer.Guid ?? "").Trim();
            player.Role = editedPlayer.Role;
        }

        if (!string.Equals(previousName, team.Name, StringComparison.Ordinal))
        {
            var matches = await db.CodMatches.Where(x => x.TournamentId == team.TournamentId && (x.TeamA == previousName || x.TeamB == previousName)).ToListAsync(cancellationToken);
            foreach (var match in matches)
            {
                if (match.TeamA == previousName) match.TeamA = team.Name;
                if (match.TeamB == previousName) match.TeamB = team.Name;
            }
        }

        var after = new
        {
            team.Name, team.ShortName, team.Captain, team.Phone, team.Email, team.LogoPath,
            team.Played, team.Wins, team.Losses, team.ScoreDifference, team.Points,
            Players = team.Players.Select(x => new { x.Role, x.RealName, x.Nickname, x.EpfNo, x.Guid }).ToArray()
        };
        db.AdminAuditLogs.Add(CreateAudit("Edit registration", "COD4 team", team.Id, team.Name, before, after));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (newLogoPath is not null) DeleteUploadedLogo(newLogoPath);
            return AdminEditError("cod4", "The update conflicts with another registration. Check team name, short name, EPF numbers, and GUIDs.");
        }

        if (previousLogoPath != team.LogoPath && previousLogoPath is not null) DeleteUploadedLogo(previousLogoPath);
        TempData["AdminNotice"] = $"{team.Name} was updated.";
        return Redirect("/admin#cod4");
    }

    [Authorize, HttpPost("registration/nfs/{id}/edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRacer(string id, [FromForm] AdminRacerEditInput input, CancellationToken cancellationToken)
    {
        var racer = await db.NfsRacers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (racer is null) return NotFound();
        if (!ModelState.IsValid) return AdminEditError("nfs", "Check the racer name, nickname, phone, and statistics.");
        var code = input.Code.Trim();
        var nickname = input.Nickname.Trim();
        var duplicate = await db.NfsRacers.AsNoTracking().AnyAsync(x => x.TournamentId == racer.TournamentId && x.Id != racer.Id && (x.Code == code || x.Nickname == nickname), cancellationToken);
        if (duplicate) return AdminEditError("nfs", "That racer code or nickname is already being used by another racer.");

        var previousNickname = racer.Nickname;
        var before = new { racer.Code, racer.Nickname, racer.FullName, racer.Phone, racer.Email, racer.StudentId, racer.Races, racer.Wins, racer.SecondPlaces, racer.ThirdPlaces, racer.Points };
        racer.Code = code;
        racer.Nickname = nickname;
        racer.FullName = input.FullName.Trim();
        racer.Phone = input.Phone.Trim();
        racer.Email = NullIfWhiteSpace(input.Email);
        racer.StudentId = NullIfWhiteSpace(input.StudentId);
        racer.Races = input.Races;
        racer.Wins = input.Wins;
        racer.SecondPlaces = input.SecondPlaces;
        racer.ThirdPlaces = input.ThirdPlaces;
        racer.Points = input.Points;
        if (!string.Equals(previousNickname, racer.Nickname, StringComparison.Ordinal))
        {
            var heats = await db.NfsHeats.Where(x => x.TournamentId == racer.TournamentId).ToListAsync(cancellationToken);
            foreach (var heat in heats)
            {
                var heatRacers = JsonSerializer.Deserialize<string[]>(heat.RacersJson) ?? [];
                if (!heatRacers.Contains(previousNickname, StringComparer.Ordinal)) continue;
                heat.RacersJson = JsonSerializer.Serialize(heatRacers.Select(x => x == previousNickname ? racer.Nickname : x));
            }
        }
        var after = new { racer.Code, racer.Nickname, racer.FullName, racer.Phone, racer.Email, racer.StudentId, racer.Races, racer.Wins, racer.SecondPlaces, racer.ThirdPlaces, racer.Points };
        db.AdminAuditLogs.Add(CreateAudit("Edit registration", "NFS racer", racer.Id, racer.Nickname, before, after));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return AdminEditError("nfs", "The update conflicts with another racer code or nickname.");
        }
        TempData["AdminNotice"] = $"{racer.Nickname} was updated.";
        return Redirect("/admin#nfs");
    }

    private IActionResult AdminEditError(string kind, string message)
    {
        TempData["AdminError"] = message;
        return Redirect(kind == "nfs" ? "/admin#nfs" : "/admin#cod4");
    }

    private AdminAuditLogEntity CreateAudit(string action, string entityType, string entityId, string entityName, object before, object after)
    {
        var address = HttpContext.Connection.RemoteIpAddress;
        var clientIp = address is null ? "unknown" : address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
        var localAddress = HttpContext.Connection.LocalIpAddress;
        var requestedHost = HttpContext.Request.Host.Host;
        var serverIp = System.Net.IPAddress.TryParse(requestedHost, out var requestHostAddress)
            ? requestHostAddress.ToString()
            : localAddress is null ? "unknown" : localAddress.IsIPv4MappedToIPv6 ? localAddress.MapToIPv4().ToString() : localAddress.ToString();
        return new AdminAuditLogEntity
        {
            OccurredAt = DateTimeOffset.UtcNow,
            AdminUser = User.Identity?.Name ?? "Tournament Admin",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            ClientIp = clientIp,
            ServerIp = serverIp,
            DetailsJson = JsonSerializer.Serialize(new { Before = before, After = after }, new JsonSerializerOptions { WriteIndented = true })
        };
    }

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void DeleteUploadedLogo(string logoPath)
    {
        const string prefix = "/uploads/team-logos/";
        if (!logoPath.StartsWith(prefix, StringComparison.Ordinal)) return;
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var path = Path.Combine(webRoot, "uploads", "team-logos", Path.GetFileName(logoPath));
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }

    private static async Task<string?> GetImageExtensionAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png") return null;
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        var isPng = bytesRead == 8 && header.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var isJpeg = bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        return (extension == ".png" && isPng) || (extension is ".jpg" or ".jpeg" && isJpeg) ? extension : null;
    }

}

public sealed record AdminDashboardModel(IReadOnlyList<TeamEntity> Teams, IReadOnlyList<NfsRacerEntity> Racers);
