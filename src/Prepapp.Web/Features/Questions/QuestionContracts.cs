using System.ComponentModel.DataAnnotations;
using Prepapp.Domain.Questions;

namespace Prepapp.Web.Features.Questions;

public record QuestionDto(
    int Id,
    Category Category,
    string Topic,
    int Difficulty,
    string Prompt,
    string ModelAnswer,
    IReadOnlyList<string> KeyPoints,
    string? Source)
{
    public static QuestionDto From(Question q) =>
        new(q.Id, q.Category, q.Topic, q.Difficulty, q.Prompt, q.ModelAnswer, q.KeyPoints, q.Source);
}

public class NewQuestionRequest
{
    [Required]
    public Category? Category { get; set; }

    [Required, MaxLength(100)]
    public string Topic { get; set; } = "";

    [Range(Question.MinDifficulty, Question.MaxDifficulty)]
    public int Difficulty { get; set; } = 2;

    [Required]
    public string Prompt { get; set; } = "";

    public string ModelAnswer { get; set; } = "";

    public List<string> KeyPoints { get; set; } = [];

    [MaxLength(200)]
    public string? Source { get; set; }
}

public record QuestionFilter(Category? Category = null, int? Difficulty = null, string? Topic = null);

public record ImportResult(int Imported, int Skipped);
