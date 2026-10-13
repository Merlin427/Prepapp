using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Prepapp.Domain.Questions;
using Prepapp.Domain.Review;
using Prepapp.Web.Features.Questions;
using Prepapp.Web.Features.Review;
using Prepapp.Web.Features.Stats;

namespace Prepapp.IntegrationTests;

public sealed class ApiTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly PrepAppFactory factory = new();
    private HttpClient client = null!;

    public Task InitializeAsync()
    {
        client = factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task Created_question_can_be_fetched_and_filtered()
    {
        var created = await CreateAsync(Category.AspNetCore, "Middleware");
        await CreateAsync(Category.Testing, "Mocks");

        var fetched = await client.GetFromJsonAsync<QuestionDto>($"/api/questions/{created.Id}", Json);
        fetched.Should().BeEquivalentTo(created);

        var filtered = await client.GetFromJsonAsync<List<QuestionDto>>("/api/questions?category=AspNetCore&topic=middle", Json);
        filtered.Should().ContainSingle().Which.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Invalid_question_is_rejected_with_validation_problem()
    {
        var response = await client.PostAsJsonAsync("/api/questions", new { category = "AspNetCore", topic = "", difficulty = 9, prompt = "" }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Topic").And.Contain("Difficulty").And.Contain("Prompt");
    }

    [Fact]
    public async Task Import_skips_prompts_already_in_the_bank()
    {
        await CreateAsync(Category.CSharpLanguage, "Records", prompt: "What are records?");

        var response = await client.PostAsJsonAsync("/api/questions/import", new[]
        {
            NewQuestion(Category.CSharpLanguage, "Records", "what are records?"),
            NewQuestion(Category.CSharpLanguage, "Generics", "Explain variance."),
        }, Json);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ImportResult>(Json);
        result.Should().Be(new ImportResult(Imported: 1, Skipped: 1));
    }

    [Fact]
    public async Task Rated_card_leaves_the_due_list_until_its_interval_passes()
    {
        await CreateAsync(Category.EfCoreData, "N+1");

        var due = await client.GetFromJsonAsync<List<DueCardDto>>("/api/review/due", Json);
        var card = due.Should().ContainSingle().Subject;

        var rate = await client.PostAsJsonAsync($"/api/review/{card.CardId}/rate", new RateRequest(Rating.Good), Json);
        rate.EnsureSuccessStatusCode();
        (await rate.Content.ReadFromJsonAsync<RateResult>(Json))!.IntervalDays.Should().Be(1);

        (await client.GetFromJsonAsync<List<DueCardDto>>("/api/review/due", Json)).Should().BeEmpty();

        factory.Clock.Advance(TimeSpan.FromDays(1));
        (await client.GetFromJsonAsync<List<DueCardDto>>("/api/review/due", Json)).Should().ContainSingle();
    }

    [Fact]
    public async Task Rating_an_unknown_card_returns_404()
    {
        var response = await client.PostAsJsonAsync("/api/review/999/rate", new RateRequest(Rating.Good), Json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Overview_reports_accuracy_and_weakest_topics_first()
    {
        await CreateAsync(Category.AspNetCore, "DI lifetimes");
        await CreateAsync(Category.AspNetCore, "Middleware");
        await CreateAsync(Category.Behavioral, "Conflict");

        var due = await client.GetFromJsonAsync<List<DueCardDto>>("/api/review/due", Json);
        await RateAsync(due!.Single(c => c.Topic == "DI lifetimes").CardId, Rating.Again);
        await RateAsync(due!.Single(c => c.Topic == "Middleware").CardId, Rating.Good);

        var overview = await client.GetFromJsonAsync<StatsOverviewDto>("/api/stats/overview", Json);

        overview!.TotalQuestions.Should().Be(3);
        overview.Reviewed.Should().Be(2);
        overview.Due.Should().Be(1);
        overview.Accuracy.Should().Be(0.5);
        overview.WeakestTopics.Select(t => t.Topic).Should().Equal("DI lifetimes", "Middleware");
        overview.Categories.Single(c => c.Category == Category.AspNetCore).Accuracy.Should().Be(0.5);
        overview.Categories.Single(c => c.Category == Category.Behavioral).Accuracy.Should().BeNull();
    }

    private async Task<QuestionDto> CreateAsync(Category category, string topic, string? prompt = null)
    {
        var response = await client.PostAsJsonAsync("/api/questions", NewQuestion(category, topic, prompt ?? $"Explain {topic}."), Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<QuestionDto>(Json))!;
    }

    private async Task RateAsync(int cardId, Rating rating) =>
        (await client.PostAsJsonAsync($"/api/review/{cardId}/rate", new RateRequest(rating), Json)).EnsureSuccessStatusCode();

    private static NewQuestionRequest NewQuestion(Category category, string topic, string prompt) => new()
    {
        Category = category,
        Topic = topic,
        Difficulty = 2,
        Prompt = prompt,
        ModelAnswer = "Model answer.",
        KeyPoints = ["Point one", "Point two"],
    };
}
