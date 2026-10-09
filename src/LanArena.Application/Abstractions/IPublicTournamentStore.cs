using LanArena.Application.Features.PublicSite;

namespace LanArena.Application.Abstractions;

public interface IPublicTournamentStore
{
    Task<PublicTournamentSnapshot> GetSnapshot(CancellationToken cancellationToken = default);
    Task<string> RegisterCod4Team(RegisterCod4TeamRequest request, CancellationToken cancellationToken = default);
    Task<string> RegisterNfsRacer(RegisterNfsRacerRequest request, CancellationToken cancellationToken = default);
}
