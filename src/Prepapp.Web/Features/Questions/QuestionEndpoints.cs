using Microsoft.AspNetCore.Http.HttpResults;
using Prepapp.Domain.Questions;

namespace Prepapp.Web.Features.Questions;

public static class QuestionEndpoints
{
    public static IEndpointRouteBuilder MapQuestionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/questions").WithTags("Questions");

        group.MapGet("/", (QuestionService service, Category? category, int? difficulty, string? topic, CancellationToken ct) =>
            service.ListAsync(new QuestionFilter(category, difficulty, topic), ct));

        group.MapGet("/{id:int}", async Task<Results<Ok<QuestionDto>, NotFound>> (int id, QuestionService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } question ? TypedResults.Ok(question) : TypedResults.NotFound());

        group.MapPost("/", async (NewQuestionRequest request, QuestionService service, CancellationToken ct) =>
        {
            var question = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/questions/{question.Id}", question);
        });

        group.MapPost("/import", (List<NewQuestionRequest> requests, QuestionService service, CancellationToken ct) =>
            service.ImportAsync(requests, ct));

        return routes;
    }
}
