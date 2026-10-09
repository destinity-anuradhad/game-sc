using LanArena.Application.Abstractions;

namespace LanArena.Application.Features.PublicSite.GetPublicTournament;

public sealed class GetPublicTournamentHandler(IPublicTournamentStore store)
{
    public Task<PublicTournamentSnapshot> Handle(CancellationToken cancellationToken = default) =>
        store.GetSnapshot(cancellationToken);
}
