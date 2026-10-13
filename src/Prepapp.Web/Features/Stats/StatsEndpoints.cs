namespace Prepapp.Web.Features.Stats;

public static class StatsEndpoints
{
    public static IEndpointRouteBuilder MapStatsEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGroup("/api/stats").WithTags("Stats")
            .MapGet("/overview", (StatsService service, CancellationToken ct) => service.GetOverviewAsync(ct));

        return routes;
    }
}
