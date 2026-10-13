using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prepapp.Infrastructure.Data;

namespace Prepapp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Prep")
            ?? throw new InvalidOperationException("Connection string 'Prep' is not configured.");

        // Blazor Server components outlive a request, so they use the factory; endpoints use the scoped context.
        services.AddDbContextFactory<PrepDbContext>(o => o.UseSqlite(connectionString));
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
