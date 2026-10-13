using Microsoft.EntityFrameworkCore;
using Prepapp.Domain.Questions;
using Prepapp.Domain.Review;
using Prepapp.Infrastructure.Data;

namespace Prepapp.Web.Features.Stats;

/// <summary>Accuracy is the share of reviewed cards whose most recent rating was Good or Easy.</summary>
public record CategoryStatsDto(Category Category, int Questions, int Reviewed, int Due, double? Accuracy);

public record TopicStatsDto(Category Category, string Topic, int Reviewed, int Due, double Accuracy);

public record StatsOverviewDto(
    int TotalQuestions,
    int Due,
    int Reviewed,
    double? Accuracy,
    IReadOnlyList<CategoryStatsDto> Categories,
    IReadOnlyList<TopicStatsDto> WeakestTopics);

public class StatsService(IDbContextFactory<PrepDbContext> dbFactory, TimeProvider time)
{
    public const int WeakestTopicCount = 5;

    public async Task<StatsOverviewDto> GetOverviewAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = time.GetUtcNow().UtcDateTime;

        // One card per question, so this stays small; aggregate in memory.
        var cards = await db.ReviewCards.AsNoTracking()
            .Select(c => new CardRow(c.Question.Category, c.Question.Topic, c.LastRating, c.DueAt <= now))
            .ToListAsync(ct);

        var categories = Enum.GetValues<Category>()
            .Select(category =>
            {
                var rows = cards.Where(c => c.Category == category).ToList();
                return new CategoryStatsDto(category, rows.Count, rows.Count(r => r.Reviewed), rows.Count(r => r.IsDue), Accuracy(rows));
            })
            .ToList();

        var weakest = cards
            .Where(c => c.Reviewed)
            .GroupBy(c => (c.Category, c.Topic))
            .Select(g => new TopicStatsDto(
                g.Key.Category,
                g.Key.Topic,
                g.Count(),
                cards.Count(c => c.Category == g.Key.Category && c.Topic == g.Key.Topic && c.IsDue),
                Accuracy(g)!.Value))
            .OrderBy(t => t.Accuracy).ThenByDescending(t => t.Due).ThenBy(t => t.Topic)
            .Take(WeakestTopicCount)
            .ToList();

        return new StatsOverviewDto(
            cards.Count,
            cards.Count(c => c.IsDue),
            cards.Count(c => c.Reviewed),
            Accuracy(cards),
            categories,
            weakest);
    }

    private static double? Accuracy(IEnumerable<CardRow> rows)
    {
        var reviewed = rows.Where(r => r.Reviewed).ToList();
        if (reviewed.Count == 0)
            return null;
        return (double)reviewed.Count(r => r.LastRating is Rating.Good or Rating.Easy) / reviewed.Count;
    }

    private sealed record CardRow(Category Category, string Topic, Rating? LastRating, bool IsDue)
    {
        public bool Reviewed => LastRating is not null;
    }
}
