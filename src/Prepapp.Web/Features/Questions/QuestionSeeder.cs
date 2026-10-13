using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Prepapp.Infrastructure.Data;

namespace Prepapp.Web.Features.Questions;

/// <summary>Applies migrations and, when the bank is empty, imports the seed questions file.</summary>
public static class QuestionSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(QuestionSeeder));

        var dbFactory = services.GetRequiredService<IDbContextFactory<PrepDbContext>>();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            await db.Database.MigrateAsync();
            if (await db.Questions.AnyAsync())
                return;
        }

        var seedFile = app.Configuration["Seed:QuestionsFile"];
        if (string.IsNullOrWhiteSpace(seedFile))
            return;

        var path = Path.Combine(app.Environment.ContentRootPath, seedFile);
        if (!File.Exists(path))
        {
            logger.LogWarning("Seed file {SeedFile} not found; starting with an empty question bank", path);
            return;
        }

        await using var stream = File.OpenRead(path);
        var requests = await JsonSerializer.DeserializeAsync<List<NewQuestionRequest>>(stream, JsonOptions) ?? [];
        var result = await services.GetRequiredService<QuestionService>().ImportAsync(requests);
        logger.LogInformation("Seeded {Imported} questions from {SeedFile}", result.Imported, path);
    }
}
