using Microsoft.EntityFrameworkCore;
using Prepapp.Domain.Questions;
using Prepapp.Infrastructure.Data;

namespace Prepapp.Web.Features.Questions;

public class QuestionService(IDbContextFactory<PrepDbContext> dbFactory, TimeProvider time)
{
    public async Task<List<QuestionDto>> ListAsync(QuestionFilter filter, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var query = db.Questions.AsNoTracking();
        if (filter.Category is { } category)
            query = query.Where(q => q.Category == category);
        if (filter.Difficulty is { } difficulty)
            query = query.Where(q => q.Difficulty == difficulty);
        if (!string.IsNullOrWhiteSpace(filter.Topic))
            query = query.Where(q => EF.Functions.Like(q.Topic, $"%{filter.Topic.Trim()}%"));

        var questions = await query
            .OrderBy(q => q.Category).ThenBy(q => q.Topic).ThenBy(q => q.Id)
            .ToListAsync(ct);
        return questions.Select(QuestionDto.From).ToList();
    }

    public async Task<QuestionDto?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var question = await db.Questions.AsNoTracking().FirstOrDefaultAsync(q => q.Id == id, ct);
        return question is null ? null : QuestionDto.From(question);
    }

    public async Task<QuestionDto> CreateAsync(NewQuestionRequest request, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var question = ToEntity(request);
        db.Questions.Add(question);
        await db.SaveChangesAsync(ct);
        return QuestionDto.From(question);
    }

    /// <summary>Adds the questions whose prompt is not already in the bank, so re-running an import is safe.</summary>
    public async Task<ImportResult> ImportAsync(IReadOnlyList<NewQuestionRequest> requests, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = (await db.Questions.Select(q => q.Prompt).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var imported = 0;
        foreach (var request in requests)
        {
            if (!existing.Add(request.Prompt.Trim()))
                continue;
            db.Questions.Add(ToEntity(request));
            imported++;
        }

        await db.SaveChangesAsync(ct);
        return new ImportResult(imported, requests.Count - imported);
    }

    private Question ToEntity(NewQuestionRequest r) => new(
        r.Category ?? throw new ArgumentException("Category is required.", nameof(r)),
        r.Topic,
        r.Difficulty,
        r.Prompt,
        r.ModelAnswer,
        r.KeyPoints,
        r.Source,
        time.GetUtcNow().UtcDateTime);
}
