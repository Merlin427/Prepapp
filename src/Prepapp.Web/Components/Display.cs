using Prepapp.Domain.Questions;

namespace Prepapp.Web.Components;

public static class Display
{
    public static string Label(this Category category) => category switch
    {
        Category.CSharpLanguage => "C# language",
        Category.AspNetCore => "ASP.NET Core",
        Category.EfCoreData => "EF Core & data",
        Category.Algorithms => "Algorithms",
        Category.SystemDesign => "System design",
        Category.Testing => "Testing & practices",
        Category.Behavioral => "Behavioral",
        _ => category.ToString(),
    };

    public static string Percent(double? value) => value is { } v ? $"{v:P0}" : "–";

    public static string DifficultyLabel(int difficulty) => difficulty switch
    {
        1 => "Easy",
        2 => "Medium",
        3 => "Hard",
        _ => difficulty.ToString(),
    };
}
