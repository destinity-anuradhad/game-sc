using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LanArena.Web.Features.Admin;

public sealed class AdminTeamEditInput
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(4, MinimumLength = 2), RegularExpression("^[A-Za-z0-9]{2,4}$")] public string ShortName { get; set; } = "";
    [Required, StringLength(80)] public string Captain { get; set; } = "";
    [Required, Phone, StringLength(30)] public string Phone { get; set; } = "";
    [EmailAddress, StringLength(120)] public string? Email { get; set; }
    public IFormFile? Logo { get; set; }
    public bool RemoveLogo { get; set; }
    [Range(0, int.MaxValue)] public int Played { get; set; }
    [Range(0, int.MaxValue)] public int Wins { get; set; }
    [Range(0, int.MaxValue)] public int Losses { get; set; }
    public int ScoreDifference { get; set; }
    [Range(0, int.MaxValue)] public int Points { get; set; }
    public List<AdminTeamPlayerEditInput> Players { get; set; } = [];
}

public sealed class AdminTeamPlayerEditInput
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string RealName { get; set; } = "";
    [Required, StringLength(24)] public string Nickname { get; set; } = "";
    [Required, StringLength(24), RegularExpression("^[A-Za-z0-9-]+$")] public string EpfNo { get; set; } = "";
    [StringLength(64)] public string? Guid { get; set; }
    [Required] public string Role { get; set; } = "Main";
}

public sealed class AdminRacerEditInput
{
    [Required, StringLength(12)] public string Code { get; set; } = "";
    [Required, StringLength(24)] public string Nickname { get; set; } = "";
    [Required, StringLength(80)] public string FullName { get; set; } = "";
    [Required, Phone, StringLength(30)] public string Phone { get; set; } = "";
    [EmailAddress, StringLength(120)] public string? Email { get; set; }
    [StringLength(40)] public string? StudentId { get; set; }
    [Range(0, int.MaxValue)] public int Races { get; set; }
    [Range(0, int.MaxValue)] public int Wins { get; set; }
    [Range(0, int.MaxValue)] public int SecondPlaces { get; set; }
    [Range(0, int.MaxValue)] public int ThirdPlaces { get; set; }
    [Range(0, int.MaxValue)] public int Points { get; set; }
}
