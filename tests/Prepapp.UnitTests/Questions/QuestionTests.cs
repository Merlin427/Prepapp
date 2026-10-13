using FluentAssertions;
using Prepapp.Domain.Questions;

namespace Prepapp.UnitTests.Questions;

public class QuestionTests
{
    private static readonly DateTime Now = new(2026, 10, 13, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_question_gets_a_review_card_due_now_and_trims_input()
    {
        var question = new Question(Category.AspNetCore, " Middleware ", 1, " Why does order matter? ", "Because…", [" a ", "", "b"], " ", Now);

        question.Topic.Should().Be("Middleware");
        question.Prompt.Should().Be("Why does order matter?");
        question.KeyPoints.Should().Equal("a", "b");
        question.Source.Should().BeNull();
        question.ReviewCards.Should().ContainSingle().Which.DueAt.Should().Be(Now);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Difficulty_outside_1_to_3_is_rejected(int difficulty)
    {
        var act = () => new Question(Category.Testing, "Mocks", difficulty, "Prompt", "", [], null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
