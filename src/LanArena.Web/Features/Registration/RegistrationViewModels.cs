using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LanArena.Web.Features.Registration;

public sealed class Cod4RegistrationInput
{
    [Required, StringLength(50)] public string TeamName { get; set; } = "";
    [Required, StringLength(4, MinimumLength = 2), RegularExpression("^[A-Za-z0-9]{2,4}$", ErrorMessage = "Use 2–4 letters or numbers for the team short name.")] public string ShortName { get; set; } = "";
    [Required, StringLength(80)] public string CaptainName { get; set; } = "";
    [Required, Phone, StringLength(30)] public string Phone { get; set; } = "";
    [EmailAddress, StringLength(120)] public string? Email { get; set; }
    public IFormFile? TeamLogo { get; set; }
    [Required, StringLength(80)] public string MainPlayerOneRealName { get; set; } = "";
    [Required, StringLength(80)] public string MainPlayerTwoRealName { get; set; } = "";
    [Required, StringLength(80)] public string MainPlayerThreeRealName { get; set; } = "";
    [Required, StringLength(80)] public string MainPlayerFourRealName { get; set; } = "";
    [Required, StringLength(80)] public string MainPlayerFiveRealName { get; set; } = "";
    [Required, StringLength(24)] public string MainPlayerOne { get; set; } = "";
    [Required, StringLength(24)] public string MainPlayerTwo { get; set; } = "";
    [Required, StringLength(24)] public string MainPlayerThree { get; set; } = "";
    [Required, StringLength(24)] public string MainPlayerFour { get; set; } = "";
    [Required, StringLength(24)] public string MainPlayerFive { get; set; } = "";
    [Required, StringLength(24), RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Use letters, numbers or hyphens for an EPF number.")] public string EpfNoOne { get; set; } = "";
    [Required, StringLength(24), RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Use letters, numbers or hyphens for an EPF number.")] public string EpfNoTwo { get; set; } = "";
    [Required, StringLength(24), RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Use letters, numbers or hyphens for an EPF number.")] public string EpfNoThree { get; set; } = "";
    [Required, StringLength(24), RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Use letters, numbers or hyphens for an EPF number.")] public string EpfNoFour { get; set; } = "";
    [Required, StringLength(24), RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Use letters, numbers or hyphens for an EPF number.")] public string EpfNoFive { get; set; } = "";
    [StringLength(64)] public string GuidOne { get; set; } = "";
    [StringLength(64)] public string GuidTwo { get; set; } = "";
    [StringLength(64)] public string GuidThree { get; set; } = "";
    [StringLength(64)] public string GuidFour { get; set; } = "";
    [StringLength(64)] public string GuidFive { get; set; } = "";
    [StringLength(24)] public string? Substitute { get; set; }
    [StringLength(80)] public string? SubstituteRealName { get; set; }
    [StringLength(24), RegularExpression("^$|^[A-Za-z0-9-]+$", ErrorMessage = "Use letters, numbers or hyphens for an EPF number.")] public string? SubstituteEpfNo { get; set; }
    [StringLength(64)] public string? SubstituteGuid { get; set; }
    public bool AcceptRules { get; set; }
}

public sealed class NfsRegistrationInput
{
    [Required, StringLength(80)] public string FullName { get; set; } = "";
    [Required, StringLength(24)] public string Nickname { get; set; } = "";
    [Required, Phone, StringLength(30)] public string Phone { get; set; } = "";
    [EmailAddress, StringLength(120)] public string? Email { get; set; }
    [StringLength(40)] public string? StudentId { get; set; }
    public bool AcceptRules { get; set; }
}

public sealed record RegistrationConfirmation(string Reference, string RegisteredName, string Game);
