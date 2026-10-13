using FluentAssertions;
using Prepapp.Domain.Review;

namespace Prepapp.UnitTests.Review;

public class ReviewCardTests
{
    private static readonly DateTime Now = new(2026, 10, 13, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_card_is_due_immediately_with_default_ease()
    {
        var card = ReviewCard.CreateNew(Now);

        card.IsDue(Now).Should().BeTrue();
        card.EaseFactor.Should().Be(ReviewCard.InitialEaseFactor);
        card.Repetitions.Should().Be(0);
        card.LastRating.Should().BeNull();
    }

    [Fact]
    public void Successive_good_ratings_follow_sm2_intervals_1_6_then_interval_times_ease()
    {
        var card = ReviewCard.CreateNew(Now);

        card.Rate(Rating.Good, Now);
        card.IntervalDays.Should().Be(1);

        card.Rate(Rating.Good, Now);
        card.IntervalDays.Should().Be(6);

        card.Rate(Rating.Good, Now);
        card.IntervalDays.Should().Be(15); // 6 * 2.5

        card.Repetitions.Should().Be(3);
        card.EaseFactor.Should().BeApproximately(2.5, 1e-9); // Good (q=4) leaves ease unchanged
        card.DueAt.Should().Be(Now.AddDays(15));
    }

    [Fact]
    public void Again_resets_repetitions_and_schedules_for_tomorrow()
    {
        var card = ReviewCard.CreateNew(Now);
        card.Rate(Rating.Good, Now);
        card.Rate(Rating.Good, Now);

        card.Rate(Rating.Again, Now);

        card.Repetitions.Should().Be(0);
        card.IntervalDays.Should().Be(1);
        card.DueAt.Should().Be(Now.AddDays(1));
        card.EaseFactor.Should().BeApproximately(2.5 - 0.54, 1e-9);
        card.LastRating.Should().Be(Rating.Again);
    }

    [Theory]
    [InlineData(Rating.Hard, 2.36)]
    [InlineData(Rating.Good, 2.5)]
    [InlineData(Rating.Easy, 2.6)]
    public void Rating_adjusts_ease_factor_per_sm2(Rating rating, double expectedEase)
    {
        var card = ReviewCard.CreateNew(Now);

        card.Rate(rating, Now);

        card.EaseFactor.Should().BeApproximately(expectedEase, 1e-9);
    }

    [Fact]
    public void Ease_factor_never_drops_below_minimum()
    {
        var card = ReviewCard.CreateNew(Now);

        for (var i = 0; i < 10; i++)
            card.Rate(Rating.Again, Now);

        card.EaseFactor.Should().Be(ReviewCard.MinEaseFactor);
    }

    [Fact]
    public void Card_is_not_due_until_its_interval_has_passed()
    {
        var card = ReviewCard.CreateNew(Now);
        card.Rate(Rating.Good, Now);

        card.IsDue(Now.AddHours(23)).Should().BeFalse();
        card.IsDue(Now.AddDays(1)).Should().BeTrue();
    }
}
