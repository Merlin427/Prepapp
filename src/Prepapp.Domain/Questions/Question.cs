using Prepapp.Domain.Review;

namespace Prepapp.Domain.Questions;

public class Question
{
    public const int MinDifficulty = 1;
    public const int MaxDifficulty = 3;

    private Question() { } // EF Core

    public Question(
        Category category,
        string topic,
        int difficulty,
        string prompt,
        string modelAnswer,
        IEnumerable<string> keyPoints,
        string? source,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentOutOfRangeException.ThrowIfLessThan(difficulty, MinDifficulty);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(difficulty, MaxDifficulty);

        Category = category;
        Topic = topic.Trim();
        Difficulty = difficulty;
        Prompt = prompt.Trim();
        ModelAnswer = modelAnswer?.Trim() ?? "";
        KeyPoints = keyPoints.Select(k => k.Trim()).Where(k => k.Length > 0).ToList();
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        ReviewCards.Add(ReviewCard.CreateNew(nowUtc));
    }

    public int Id { get; private set; }
    public Category Category { get; private set; }
    public string Topic { get; private set; } = "";
    public int Difficulty { get; private set; }
    public string Prompt { get; private set; } = "";
    public string ModelAnswer { get; private set; } = "";
    public List<string> KeyPoints { get; private set; } = [];
    public string? Source { get; private set; }

    public List<ReviewCard> ReviewCards { get; private set; } = [];
}
