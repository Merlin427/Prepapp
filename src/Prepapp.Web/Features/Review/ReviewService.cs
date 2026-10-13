using Microsoft.EntityFrameworkCore;
using Prepapp.Domain.Questions;
using Prepapp.Domain.Review;
using Prepapp.Infrastructure.Data;

namespace Prepapp.Web.Features.Review;

public record DueCardDto(
    int CardId,
    int QuestionId,
    Category Category,
    string Topic,
    int Difficulty,
    string Prompt,
    string ModelAnswer,
    IReadOnlyList<string> KeyPoints,
    DateTime DueAt,
    int Repetitions);

public record RateRequest(Rating Rating);

public record RateResult(int CardId, DateTime DueAt, int IntervalDays, double EaseFactor);

public class ReviewService(IDbContextFactory<PrepDbContext> dbFactory, TimeProvider time)
{
    public const int MaxDueLimit = 100;

    public async Task<List<DueCardDto>> GetDueAsync(int limit, Category? category = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = time.GetUtcNow().UtcDateTime;

        var query = db.ReviewCards.AsNoTracking().Where(c => c.DueAt <= now);
        if (category is { } cat)
            query = query.Where(c => c.Question.Category == cat);

        return await query
            .OrderBy(c => c.DueAt).ThenBy(c => c.Id)
            .Take(Math.Clamp(limit, 1, MaxDueLimit))
            .Select(c => new DueCardDto(
                c.Id, c.QuestionId, c.Question.Category, c.Question.Topic, c.Question.Difficulty,
                c.Question.Prompt, c.Question.ModelAnswer, c.Question.KeyPoints, c.DueAt, c.Repetitions))
            .ToListAsync(ct);
    }

    public async Task<RateResult?> RateAsync(int cardId, Rating rating, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var card = await db.ReviewCards.FirstOrDefaultAsync(c => c.Id == cardId, ct);
        if (card is null)
            return null;

        card.Rate(rating, time.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);
        return new RateResult(card.Id, card.DueAt, card.IntervalDays, card.EaseFactor);
    }
}
