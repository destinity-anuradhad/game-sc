using LanArena.Application.Features.PublicSite.RegisterCod4Team;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace LanArena.Web.Features.Registration;

[Route("register/cod4")]
public sealed class Cod4RegistrationController(RegisterCod4TeamHandler registerTeam, IWebHostEnvironment environment) : Controller
{
    private const long MaxTeamLogoBytes = 2 * 1024 * 1024;

    [HttpGet("")]
    public IActionResult Create() => View("~/Views/Registration/Cod4.cshtml", new Cod4RegistrationInput());

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxTeamLogoBytes + 128 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxTeamLogoBytes + 128 * 1024)]
    public async Task<IActionResult> Create(Cod4RegistrationInput input, CancellationToken cancellationToken)
    {
        if (!input.AcceptRules) ModelState.AddModelError(nameof(input.AcceptRules), "Accept the tournament rules to continue.");
        string? logoExtension = null;
        if (input.TeamLogo is { FileName.Length: > 0 } logo)
        {
            if (logo.Length is <= 0 or > MaxTeamLogoBytes)
                ModelState.AddModelError(nameof(input.TeamLogo), "The team logo must be no larger than 2 MB.");
            else
            {
                logoExtension = await GetImageExtensionAsync(logo, cancellationToken);
                if (logoExtension is null)
                    ModelState.AddModelError(nameof(input.TeamLogo), "Upload a valid JPG or PNG image.");
            }
        }
        if (!ModelState.IsValid) return View("~/Views/Registration/Cod4.cshtml", input);

        string? savedLogoPath = null;
        string? savedLogoUrl = null;
        var registrationSaved = false;
        try
        {
            if (logoExtension is not null && input.TeamLogo is not null)
            {
                var fileName = $"{Guid.NewGuid():N}{logoExtension}";
                var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
                var uploadDirectory = Path.Combine(webRoot, "uploads", "team-logos");
                Directory.CreateDirectory(uploadDirectory);
                savedLogoPath = Path.Combine(uploadDirectory, fileName);
                await using var destination = new FileStream(savedLogoPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                await input.TeamLogo.CopyToAsync(destination, cancellationToken);
                savedLogoUrl = $"/uploads/team-logos/{fileName}";
            }

            var reference = await registerTeam.Handle(new(input.TeamName, input.ShortName, input.CaptainName, input.Phone, input.Email,
                savedLogoUrl,
                input.MainPlayerOne, input.MainPlayerTwo, input.MainPlayerThree, input.MainPlayerFour, input.MainPlayerFive,
                input.MainPlayerOneRealName, input.MainPlayerTwoRealName, input.MainPlayerThreeRealName, input.MainPlayerFourRealName, input.MainPlayerFiveRealName,
                input.EpfNoOne, input.EpfNoTwo, input.EpfNoThree, input.EpfNoFour, input.EpfNoFive,
                input.GuidOne, input.GuidTwo, input.GuidThree, input.GuidFour, input.GuidFive, input.Substitute, input.SubstituteRealName, input.SubstituteEpfNo, input.SubstituteGuid), cancellationToken);
            registrationSaved = true;
            return View("~/Views/Registration/Confirmation.cshtml", new RegistrationConfirmation(reference, input.TeamName, "Call of Duty 4"));
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("~/Views/Registration/Cod4.cshtml", input);
        }
        finally
        {
            if (!registrationSaved && savedLogoPath is not null && System.IO.File.Exists(savedLogoPath))
                System.IO.File.Delete(savedLogoPath);
        }
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
