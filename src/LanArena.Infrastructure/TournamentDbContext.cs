using Microsoft.EntityFrameworkCore;

namespace LanArena.Infrastructure;

public sealed class TournamentDbContext(DbContextOptions<TournamentDbContext> options) : DbContext(options)
{
    public DbSet<TournamentEntity> Tournaments => Set<TournamentEntity>();
    public DbSet<SiteConfigEntity> SiteConfigs => Set<SiteConfigEntity>();
    public DbSet<TeamEntity> Teams => Set<TeamEntity>();
    public DbSet<TeamPlayerEntity> TeamPlayers => Set<TeamPlayerEntity>();
    public DbSet<NfsRacerEntity> NfsRacers => Set<NfsRacerEntity>();
    public DbSet<CodMatchEntity> CodMatches => Set<CodMatchEntity>();
    public DbSet<NfsHeatEntity> NfsHeats => Set<NfsHeatEntity>();
    public DbSet<AdminAuditLogEntity> AdminAuditLogs => Set<AdminAuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TournamentEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<SiteConfigEntity>().ToTable("SiteConfig").HasKey(x => x.Id);
        modelBuilder.Entity<SiteConfigEntity>().Property(x => x.Id).ValueGeneratedNever();
        modelBuilder.Entity<SiteConfigEntity>().Property(x => x.TournamentDate).HasMaxLength(30);
        modelBuilder.Entity<SiteConfigEntity>().Property(x => x.TournamentLocation).HasMaxLength(200);
        modelBuilder.Entity<TeamEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<TeamEntity>().Property(x => x.Name).HasMaxLength(100);
        modelBuilder.Entity<TeamEntity>().Property(x => x.ShortName).HasMaxLength(4);
        modelBuilder.Entity<TeamEntity>().Property(x => x.LogoPath).HasMaxLength(260);
        modelBuilder.Entity<TeamEntity>().HasIndex(x => new { x.TournamentId, x.Name }).IsUnique();
        modelBuilder.Entity<TeamEntity>().HasIndex(x => new { x.TournamentId, x.ShortName }).IsUnique();
        modelBuilder.Entity<TeamPlayerEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<TeamPlayerEntity>().Property(x => x.Nickname).HasMaxLength(24);
        modelBuilder.Entity<TeamPlayerEntity>().Property(x => x.RealName).HasMaxLength(80);
        modelBuilder.Entity<TeamPlayerEntity>().Property(x => x.Guid).HasMaxLength(64);
        modelBuilder.Entity<TeamPlayerEntity>().Property(x => x.EpfNo).HasMaxLength(24);
        modelBuilder.Entity<TeamPlayerEntity>().HasIndex(x => new { x.TournamentId, x.EpfNo }).IsUnique().HasFilter("[EpfNo] <> ''");
        modelBuilder.Entity<TeamPlayerEntity>().HasIndex(x => new { x.TournamentId, x.Guid }).IsUnique().HasFilter("[Guid] <> ''");
        modelBuilder.Entity<TeamPlayerEntity>().HasOne<TeamEntity>().WithMany(x => x.Players).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<NfsRacerEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<NfsRacerEntity>().Property(x => x.Code).HasMaxLength(12);
        modelBuilder.Entity<NfsRacerEntity>().Property(x => x.Nickname).HasMaxLength(24);
        modelBuilder.Entity<NfsRacerEntity>().HasIndex(x => new { x.TournamentId, x.Code }).IsUnique();
        modelBuilder.Entity<NfsRacerEntity>().HasIndex(x => new { x.TournamentId, x.Nickname }).IsUnique();
        modelBuilder.Entity<CodMatchEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<NfsHeatEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<AdminAuditLogEntity>().ToTable("AdminAuditLogs").HasKey(x => x.Id);
        modelBuilder.Entity<AdminAuditLogEntity>().Property(x => x.Id).UseIdentityColumn();
        modelBuilder.Entity<AdminAuditLogEntity>().Property(x => x.AdminUser).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<AdminAuditLogEntity>().Property(x => x.Action).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<AdminAuditLogEntity>().Property(x => x.EntityType).HasMaxLength(40).IsRequired();
        modelBuilder.Entity<AdminAuditLogEntity>().Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<AdminAuditLogEntity>().Property(x => x.EntityName).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<AdminAuditLogEntity>().Property(x => x.ClientIp).HasMaxLength(45).IsRequired();
        modelBuilder.Entity<AdminAuditLogEntity>().Property(x => x.ServerIp).HasMaxLength(45).IsRequired();
        modelBuilder.Entity<AdminAuditLogEntity>().Property(x => x.DetailsJson).IsRequired();
        modelBuilder.Entity<AdminAuditLogEntity>().HasIndex(x => x.OccurredAt);
    }
}

public sealed class TournamentEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "Scienter Welfare Team";
    public DateOnly EventDate { get; set; } = new(2026, 10, 7);
    public string Venue { get; set; } = "Colombo, Sri Lanka";
    public string RegistrationDeadline { get; set; } = "07 October · 17:00";
    public int NextRacerNumber { get; set; } = 1;
}

public sealed class SiteConfigEntity
{
    public int Id { get; set; } = 1;
    public DateTimeOffset RegistrationEndsAt { get; set; } = new(2026, 10, 12, 12, 0, 0, TimeSpan.FromMinutes(330));
    public string TournamentDate { get; set; } = "2026-10-07";
    public string TournamentLocation { get; set; } = "Colombo, Sri Lanka";
}

public sealed class TeamEntity
{
    public string Id { get; set; } = "";
    public int TournamentId { get; set; }
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public string Captain { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string? LogoPath { get; set; }
    public string Status { get; set; } = "Pending";
    public int Played { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int ScoreDifference { get; set; }
    public int Points { get; set; }
    public List<TeamPlayerEntity> Players { get; set; } = [];
}

public sealed class TeamPlayerEntity
{
    public int Id { get; set; }
    public string TeamId { get; set; } = "";
    public int TournamentId { get; set; }
    public string Nickname { get; set; } = "";
    public string RealName { get; set; } = "";
    public string Guid { get; set; } = "";
    public string EpfNo { get; set; } = "";
    public string Role { get; set; } = "Main";
}

public sealed class NfsRacerEntity
{
    public string Id { get; set; } = "";
    public int TournamentId { get; set; }
    public string Code { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string? StudentId { get; set; }
    public string Status { get; set; } = "Pending";
    public int Races { get; set; }
    public int Wins { get; set; }
    public int SecondPlaces { get; set; }
    public int ThirdPlaces { get; set; }
    public int Points { get; set; }
}

public sealed class CodMatchEntity
{
    public string Id { get; set; } = "";
    public int TournamentId { get; set; }
    public string TeamA { get; set; } = "";
    public string TeamB { get; set; } = "";
    public string Time { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Map { get; set; } = "";
    public string Server { get; set; } = "";
    public string Status { get; set; } = "Scheduled";
    public int? ScoreA { get; set; }
    public int? ScoreB { get; set; }
}

public sealed class NfsHeatEntity
{
    public string Id { get; set; } = "";
    public int TournamentId { get; set; }
    public string Type { get; set; } = "";
    public string Track { get; set; } = "";
    public string Time { get; set; } = "";
    public string Status { get; set; } = "Scheduled";
    public string RacersJson { get; set; } = "[]";
}

public sealed class AdminAuditLogEntity
{
    public int Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string AdminUser { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string ClientIp { get; set; } = "";
    public string ServerIp { get; set; } = "";
    public string DetailsJson { get; set; } = "{}";
}
