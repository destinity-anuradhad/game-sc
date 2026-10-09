namespace LanArena.Application.Features.PublicSite;

public sealed record PublicTournamentSnapshot(
    string Name,
    DateOnly EventDate,
    string Venue,
    DateTimeOffset RegistrationEndsAt,
    IReadOnlyList<PublicTeam> Teams,
    IReadOnlyList<PublicRacer> Racers,
    IReadOnlyList<PublicCodMatch> Matches,
    IReadOnlyList<PublicNfsHeat> Heats);

public sealed record PublicTeam(string Name, string ShortName, string? LogoPath, string Captain, IReadOnlyList<string> Players, string Status, int Played, int Wins, int Losses, int ScoreDifference, int Points);
public sealed record PublicRacer(string Code, string Nickname, string FullName, int Races, int Wins, int SecondPlaces, int ThirdPlaces, int Points, string Status);
public sealed record PublicCodMatch(string Id, string TeamA, string TeamB, string Time, string Mode, string Map, string Server, string Status, int? ScoreA, int? ScoreB);
public sealed record PublicNfsHeat(string Id, string Type, string Track, string Time, string Status, IReadOnlyList<string> Racers);

public sealed record RegisterCod4TeamRequest(
    string TeamName,
    string ShortName,
    string CaptainName,
    string Phone,
    string? Email,
    string? LogoPath,
    string MainPlayerOne,
    string MainPlayerTwo,
    string MainPlayerThree,
    string MainPlayerFour,
    string MainPlayerFive,
    string MainPlayerOneRealName,
    string MainPlayerTwoRealName,
    string MainPlayerThreeRealName,
    string MainPlayerFourRealName,
    string MainPlayerFiveRealName,
    string EpfNoOne,
    string EpfNoTwo,
    string EpfNoThree,
    string EpfNoFour,
    string EpfNoFive,
    string GuidOne,
    string GuidTwo,
    string GuidThree,
    string GuidFour,
    string GuidFive,
    string? Substitute,
    string? SubstituteRealName,
    string? SubstituteEpfNo,
    string? SubstituteGuid);

public sealed record RegisterNfsRacerRequest(
    string FullName,
    string Nickname,
    string Phone,
    string? Email,
    string? StudentId);
