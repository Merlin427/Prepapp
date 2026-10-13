using Prepapp.Domain.Questions;

namespace Prepapp.Domain.Review;

/// <summary>
/// Spaced-repetition state for one question, scheduled with SM-2.
/// </summary>
public class ReviewCard
{
    public const double InitialEaseFactor = 2.5;
    public const double MinEaseFactor = 1.3;

    private ReviewCard() { } // EF Core

    public static ReviewCard CreateNew(DateTime nowUtc) => new()
    {
        EaseFactor = InitialEaseFactor,
        IntervalDays = 0,
        Repetitions = 0,
        DueAt = nowUtc,
    };

    public int Id { get; private set; }
    public int QuestionId { get; private set; }
    public Question Question { get; private set; } = null!;
    public double EaseFactor { get; private set; }
    public int IntervalDays { get; private set; }
    public DateTime DueAt { get; private set; }
    public int Repetitions { get; private set; }
    public Rating? LastRating { get; private set; }
    public DateTime? LastReviewedAt { get; private set; }

    public bool IsDue(DateTime nowUtc) => DueAt <= nowUtc;

    /// <summary>
    /// Applies SM-2. The four buttons map to SM-2 quality 1 (Again), 3 (Hard), 4 (Good) and 5 (Easy);
    /// Again is a lapse that resets repetitions and brings the card back tomorrow.
    /// </summary>
    public void Rate(Rating rating, DateTime nowUtc)
    {
        var quality = rating switch
        {
            Rating.Again => 1,
            Rating.Hard => 3,
            Rating.Good => 4,
            Rating.Easy => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(rating), rating, null),
        };

        if (quality < 3)
        {
            Repetitions = 0;
            IntervalDays = 1;
        }
        else
        {
            IntervalDays = Repetitions switch
            {
                0 => 1,
                1 => 6,
                _ => (int)Math.Round(IntervalDays * EaseFactor),
            };
            Repetitions++;
        }

        var q = 5 - quality;
        EaseFactor = Math.Max(MinEaseFactor, EaseFactor + (0.1 - q * (0.08 + q * 0.02)));

        LastRating = rating;
        LastReviewedAt = nowUtc;
        DueAt = nowUtc.AddDays(IntervalDays);
    }
}
