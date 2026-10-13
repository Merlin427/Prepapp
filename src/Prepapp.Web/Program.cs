using System.Text.Json.Serialization;
using Prepapp.Infrastructure;
using Prepapp.Web.Components;
using Prepapp.Web.Features.Questions;
using Prepapp.Web.Features.Review;
using Prepapp.Web.Features.Stats;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<QuestionService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<StatsService>();

builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// API errors get ProblemDetails; only UI routes re-execute to the Blazor not-found page.
app.UseWhen(ctx => IsApi(ctx), api => api.UseStatusCodePages());
app.UseWhen(ctx => !IsApi(ctx), ui => ui.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapQuestionEndpoints();
app.MapReviewEndpoints();
app.MapStatsEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.InitialiseDatabaseAsync();

app.Run();

static bool IsApi(HttpContext ctx) => ctx.Request.Path.StartsWithSegments("/api");

public partial class Program;
