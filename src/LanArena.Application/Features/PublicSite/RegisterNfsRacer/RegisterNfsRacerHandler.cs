using LanArena.Application.Abstractions;

namespace LanArena.Application.Features.PublicSite.RegisterNfsRacer;

public sealed class RegisterNfsRacerHandler(IPublicTournamentStore store)
{
    public Task<string> Handle(RegisterNfsRacerRequest request, CancellationToken cancellationToken = default) =>
        store.RegisterNfsRacer(request with
        {
            FullName = request.FullName.Trim(),
            Nickname = request.Nickname.Trim(),
            Phone = request.Phone.Trim()
        }, cancellationToken);
}
