using LanArena.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanArena.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLanArenaInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<TournamentDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<SqlPublicTournamentStore>();
        services.AddScoped<IPublicTournamentStore>(provider => provider.GetRequiredService<SqlPublicTournamentStore>());
        return services;
    }
}
