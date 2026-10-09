using System.Text.Json;
using LanArena.Application.Abstractions;
using LanArena.Application.Features.PublicSite;
using Microsoft.EntityFrameworkCore;

namespace LanArena.Infrastructure;

public sealed class SqlPublicTournamentStore(TournamentDbContext db) : IPublicTournamentStore
{
    public async Task InitializeAsync(string legacyFile, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("IF OBJECT_ID(N'dbo.SiteConfig', N'U') IS NULL BEGIN CREATE TABLE [dbo].[SiteConfig] ([Id] int NOT NULL CONSTRAINT [PK_SiteConfig] PRIMARY KEY, [RegistrationEndsAt] datetimeoffset NOT NULL, [TournamentDate] nvarchar(30) NOT NULL, [TournamentLocation] nvarchar(200) NOT NULL); END; IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types t ON c.user_type_id=t.user_type_id WHERE c.object_id=OBJECT_ID(N'dbo.SiteConfig') AND c.name=N'TournamentDate' AND t.name IN (N'date',N'datetime',N'datetime2')) BEGIN EXEC(N'ALTER TABLE dbo.SiteConfig ADD TournamentDateText nvarchar(30) NULL'); EXEC(N'UPDATE dbo.SiteConfig SET TournamentDateText=CONVERT(nvarchar(30),TournamentDate,23)'); EXEC(N'ALTER TABLE dbo.SiteConfig DROP COLUMN TournamentDate'); EXEC sp_rename N'dbo.SiteConfig.TournamentDateText', N'TournamentDate', N'COLUMN'; EXEC(N'ALTER TABLE dbo.SiteConfig ALTER COLUMN TournamentDate nvarchar(30) NOT NULL'); END", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("IF COL_LENGTH(N'dbo.TeamPlayers', N'RealName') IS NULL ALTER TABLE dbo.TeamPlayers ADD RealName nvarchar(80) NOT NULL CONSTRAINT DF_TeamPlayers_RealName DEFAULT N'' WITH VALUES;", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("IF COL_LENGTH(N'dbo.Teams', N'LogoPath') IS NULL ALTER TABLE dbo.Teams ADD LogoPath nvarchar(260) NULL;", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("IF OBJECT_ID(N'dbo.AdminAuditLogs', N'U') IS NULL BEGIN CREATE TABLE dbo.AdminAuditLogs (Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AdminAuditLogs PRIMARY KEY, OccurredAt datetimeoffset NOT NULL, AdminUser nvarchar(100) NOT NULL, Action nvarchar(80) NOT NULL, EntityType nvarchar(40) NOT NULL, EntityId nvarchar(100) NOT NULL, EntityName nvarchar(200) NOT NULL, ClientIp nvarchar(45) NOT NULL, ServerIp nvarchar(45) NOT NULL, DetailsJson nvarchar(max) NOT NULL); CREATE INDEX IX_AdminAuditLogs_OccurredAt ON dbo.AdminAuditLogs(OccurredAt DESC); END; IF COL_LENGTH(N'dbo.AdminAuditLogs', N'ServerIp') IS NULL ALTER TABLE dbo.AdminAuditLogs ADD ServerIp nvarchar(45) NOT NULL CONSTRAINT DF_AdminAuditLogs_ServerIp DEFAULT N'unknown' WITH VALUES;", cancellationToken);

        if (!await db.SiteConfigs.AnyAsync(cancellationToken))
        {
            db.SiteConfigs.Add(new SiteConfigEntity());
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PublicTournamentSnapshot> GetSnapshot(CancellationToken cancellationToken = default)
    {
        var tournament = await db.Tournaments.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var siteConfig = await db.SiteConfigs.AsNoTracking().FirstAsync(cancellationToken);
        var teams = await db.Teams.AsNoTracking().Include(x => x.Players).ToListAsync(cancellationToken);
        var racers = await db.NfsRacers.AsNoTracking().ToListAsync(cancellationToken);
        var matches = await db.CodMatches.AsNoTracking().ToListAsync(cancellationToken);
        var heats = await db.NfsHeats.AsNoTracking().ToListAsync(cancellationToken);
        var eventDate = ParseEventDate(siteConfig.TournamentDate, tournament?.EventDate ?? new DateOnly(2026, 10, 7));
        return new PublicTournamentSnapshot(tournament?.Name ?? "Battle Arena", eventDate, siteConfig.TournamentLocation, siteConfig.RegistrationEndsAt,
            teams.Select(team => new PublicTeam(team.Name, team.ShortName, team.LogoPath, team.Captain, team.Players.OrderBy(p => p.Role == "Substitute").Select(p => p.Nickname).ToArray(), team.Status, team.Played, team.Wins, team.Losses, team.ScoreDifference, team.Points)).ToArray(),
            racers.Select(racer => new PublicRacer(racer.Code, racer.Nickname, racer.FullName, racer.Races, racer.Wins, racer.SecondPlaces, racer.ThirdPlaces, racer.Points, racer.Status)).ToArray(),
            matches.Select(match => new PublicCodMatch(match.Id, match.TeamA, match.TeamB, match.Time, match.Mode, match.Map, match.Server, match.Status, match.ScoreA, match.ScoreB)).ToArray(),
            heats.Select(heat => new PublicNfsHeat(heat.Id, heat.Type, heat.Track, heat.Time, heat.Status, JsonSerializer.Deserialize<string[]>(heat.RacersJson) ?? [])).ToArray());
    }

    public async Task<string> RegisterCod4Team(RegisterCod4TeamRequest request, CancellationToken cancellationToken = default)
    {
        var tournament = await GetOrCreateTournamentAsync(cancellationToken);
        if (await db.Teams.AnyAsync(x => x.TournamentId == tournament.Id && (x.Name == request.TeamName || x.ShortName == request.ShortName), cancellationToken))
            throw new ArgumentException("That team name or short name is already registered.");
        var roster = new[]
        {
            (request.MainPlayerOne, request.MainPlayerOneRealName, request.GuidOne, request.EpfNoOne), (request.MainPlayerTwo, request.MainPlayerTwoRealName, request.GuidTwo, request.EpfNoTwo),
            (request.MainPlayerThree, request.MainPlayerThreeRealName, request.GuidThree, request.EpfNoThree), (request.MainPlayerFour, request.MainPlayerFourRealName, request.GuidFour, request.EpfNoFour),
            (request.MainPlayerFive, request.MainPlayerFiveRealName, request.GuidFive, request.EpfNoFive)
        };
        var players = roster.Select(x => new TeamPlayerEntity { TournamentId = tournament.Id, Nickname = x.Item1.Trim(), RealName = x.Item2.Trim(), Guid = x.Item3.Trim(), EpfNo = x.Item4.Trim(), Role = "Main" }).ToList();
        if (!string.IsNullOrWhiteSpace(request.Substitute)) players.Add(new TeamPlayerEntity { TournamentId = tournament.Id, Nickname = request.Substitute.Trim(), RealName = request.SubstituteRealName!.Trim(), Guid = request.SubstituteGuid!.Trim(), EpfNo = request.SubstituteEpfNo!.Trim(), Role = "Substitute" });
        var existingNames = await db.TeamPlayers.Where(x => x.TournamentId == tournament.Id).Select(x => x.Nickname).ToListAsync(cancellationToken);
        var existingGuids = await db.TeamPlayers.Where(x => x.TournamentId == tournament.Id).Select(x => x.Guid).ToListAsync(cancellationToken);
        var existingEpf = await db.TeamPlayers.Where(x => x.TournamentId == tournament.Id && x.EpfNo != "").Select(x => x.EpfNo).ToListAsync(cancellationToken);
        if (players.Any(x => existingNames.Contains(x.Nickname, StringComparer.OrdinalIgnoreCase))) throw new ArgumentException("A player nickname is already registered on another team.");
        if (players.Any(x => existingGuids.Contains(x.Guid, StringComparer.OrdinalIgnoreCase))) throw new ArgumentException("A COD4 GUID is already registered on another team.");
        if (players.Any(x => existingEpf.Contains(x.EpfNo, StringComparer.OrdinalIgnoreCase))) throw new ArgumentException("An EPF number is already registered on another team.");
        var reference = $"COD-{Guid.NewGuid():N}"[..11].ToUpperInvariant();
        db.Teams.Add(new TeamEntity { Id = reference, TournamentId = tournament.Id, Name = request.TeamName, ShortName = request.ShortName, Captain = request.CaptainName, Phone = request.Phone, Email = request.Email, LogoPath = request.LogoPath, Status = "Pending", Players = players });
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new ArgumentException("A matching team or player is already registered. Please check the team name, short name, GUIDs and EPF numbers."); }
        return reference;
    }

    public async Task<string> RegisterNfsRacer(RegisterNfsRacerRequest request, CancellationToken cancellationToken = default)
    {
        var tournament = await GetOrCreateTournamentAsync(cancellationToken);
        if (await db.NfsRacers.AnyAsync(x => x.TournamentId == tournament.Id && (x.Nickname == request.Nickname || x.Phone == request.Phone), cancellationToken))
            throw new ArgumentException("That racing nickname or phone number is already registered.");
        var code = $"NFS{tournament.NextRacerNumber++:000}";
        db.NfsRacers.Add(new NfsRacerEntity { Id = code, TournamentId = tournament.Id, Code = code, Nickname = request.Nickname, FullName = request.FullName, Phone = request.Phone, Email = request.Email, StudentId = request.StudentId, Status = "Pending" });
        await db.SaveChangesAsync(cancellationToken);
        return code;
    }

    private static DateOnly ParseEventDate(string? value, DateOnly fallback)
    {
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var parsed) ? parsed : fallback;
    }
    private async Task<TournamentEntity> GetOrCreateTournamentAsync(CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments.FirstOrDefaultAsync(cancellationToken);
        if (tournament is not null) return tournament;
        var config = await db.SiteConfigs.AsNoTracking().FirstAsync(cancellationToken);
        tournament = new TournamentEntity
        {
            Name = "Battle Arena",
            EventDate = ParseEventDate(config.TournamentDate, new DateOnly(2026, 10, 7)),
            Venue = config.TournamentLocation
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync(cancellationToken);
        return tournament;
    }

    private sealed class LegacyTournament
    {
        public string Name { get; set; } = "Scienter Welfare Team"; public DateOnly EventDate { get; set; } = new(2026, 10, 7);
        public string Venue { get; set; } = "Colombo, Sri Lanka"; public string RegistrationDeadline { get; set; } = "07 October · 17:00"; public int NextRacerNumber { get; set; } = 1;
        public List<LegacyTeam> Teams { get; set; } = []; public List<LegacyRacer> Racers { get; set; } = []; public List<LegacyMatch> Matches { get; set; } = []; public List<LegacyHeat> Heats { get; set; } = [];
    }
    private sealed class LegacyTeam
    {
        public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string ShortName { get; set; } = ""; public string Captain { get; set; } = ""; public string Phone { get; set; } = ""; public string? Email { get; set; }
        public string Status { get; set; } = "Pending"; public int Played { get; set; } public int Wins { get; set; } public int Losses { get; set; } public int ScoreDifference { get; set; } public int Points { get; set; } public List<LegacyPlayer> Players { get; set; } = [];
    }
    private sealed class LegacyPlayer { public string Nickname { get; set; } = ""; public string Role { get; set; } = "Main"; public string? Guid { get; set; } public string? EpfNo { get; set; } }
    private sealed class LegacyRacer
    {
        public string Code { get; set; } = ""; public string Nickname { get; set; } = ""; public string FullName { get; set; } = ""; public string Phone { get; set; } = ""; public string? Email { get; set; } public string? StudentId { get; set; }
        public string Status { get; set; } = "Pending"; public int Races { get; set; } public int Wins { get; set; } public int SecondPlaces { get; set; } public int ThirdPlaces { get; set; } public int Points { get; set; }
    }
    private sealed class LegacyMatch { public string Id { get; set; } = ""; public string TeamA { get; set; } = ""; public string TeamB { get; set; } = ""; public string Time { get; set; } = ""; public string Mode { get; set; } = ""; public string Map { get; set; } = ""; public string Server { get; set; } = ""; public string Status { get; set; } = ""; public int? ScoreA { get; set; } public int? ScoreB { get; set; } }
    private sealed class LegacyHeat { public string Id { get; set; } = ""; public string Type { get; set; } = ""; public string Track { get; set; } = ""; public string Time { get; set; } = ""; public string Status { get; set; } = ""; public List<string> Racers { get; set; } = []; }
}

