using Microsoft.AspNetCore.Http.HttpResults;
using Prepapp.Domain.Questions;
using Prepapp.Domain.Review;

namespace Prepapp.Web.Features.Review;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/review").WithTags("Review");

        group.MapGet("/due", (ReviewService service, Category? category, CancellationToken ct, int limit = 20) =>
            service.GetDueAsync(limit, category, ct));

        group.MapPost("/{cardId:int}/rate", async Task<Results<Ok<RateResult>, NotFound, ValidationProblem>> (
            int cardId, RateRequest request, ReviewService service, CancellationToken ct) =>
        {
            if (!Enum.IsDefined(request.Rating))
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["rating"] = [$"Rating must be one of: {string.Join(", ", Enum.GetNames<Rating>())}."],
                });

            return await service.RateAsync(cardId, request.Rating, ct) is { } result
                ? TypedResults.Ok(result)
                : TypedResults.NotFound();
        });

        return routes;
    }
}
