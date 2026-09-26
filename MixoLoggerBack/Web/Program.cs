using Domain.Cocktails;
using Infrastructure.Persistance;
using Web;
using Web.Deploiement;
using Web.Securite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddMvc();
// Document OpenAPI généré par ASP.NET Core lui-même : sa version de Microsoft.OpenApi suit toujours
// celle du framework. Swashbuckle.SwaggerGen, compilé contre une autre version, levait une
// MissingMethodException en .NET 11 ; seule l'interface Swagger UI (fichiers statiques) est gardée.
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "MixoLogger API";
    document.Info.Description = "API de MixoLogger : recettes, Mon bar, notes, comptes.";
    return Task.CompletedTask;
}));
builder.Services.AddControllers()
    .AddJsonOptions(cfg => cfg.JsonSerializerOptions.Converters.Add(new UniteVolumeConverter()));

// Authentification par cookie, tout protégé par défaut, CORS restreint au front (lève B4 et B7).
builder.Services.AddSecurite(builder.Configuration, builder.Environment);

// Derrière un reverse proxy : en-têtes transférés, clés des cookies conservées (docs/DEPLOIEMENT.md).
builder.Services.AddDeploiement(builder.Configuration, builder.Environment);

// Configuration structure application
Application.DependencyInjection.AddApplication(builder.Services);

// Base SQLite (F10) : ConnectionStrings:MixoLogger, chemin relatif au dossier de l'API.
builder.Services.AddPersistance(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddHostedService<Web.Sauvegardes.SauvegardesPeriodiques>();

var app = builder.Build();

await app.Services.MettreAJourBaseAsync();
await app.SynchroniserComptesAsync();

app.UseDeploiement();
app.UseExceptionHandler();

// La documentation de l'API reste en développement : en production, rien ne la rend utile au public.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "MixoLogger API"));
}
app.UseCors(SecuriteExtensions.PolitiqueCors);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
//app.UsePathBase("/api");
app.MapGet("/ping", () => Results.Ok("pong")).AllowAnonymous();
app.MapControllers(); // Expose controllers

// Document OpenAPI (/openapi/v1.json), en développement seulement, comme l'interface qui le lit.
if (app.Environment.IsDevelopment())
    app.MapOpenApi().AllowAnonymous();

//app.UseHttpsRedirection();
app.Run();

/// <summary>Rendu visible pour les tests d'intégration (WebApplicationFactory).</summary>
public partial class Program;
