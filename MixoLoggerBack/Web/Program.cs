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
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(cfg => cfg.JsonSerializerOptions.Converters.Add(new UniteVolumeConverter()));

// Authentification par cookie, tout protégé par défaut, CORS restreint au front (lève B4 et B7).
builder.Services.AddSecurite(builder.Configuration, builder.Environment);

// Derrière un reverse proxy : en-têtes transférés, clés des cookies conservées (docs/DEPLOIEMENT.md).
builder.Services.AddDeploiement(builder.Configuration, builder.Environment);

builder.Services.AddSwaggerGen(builder =>
{
    builder.SupportNonNullableReferenceTypes();
    // Microsoft.OpenApi 3.x a supprimé le sous-espace de noms `Models` :
    // OpenApiInfo vit désormais directement sous `Microsoft.OpenApi`.
    builder.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "MixoLogger API",
        Version = "v1",
        Description = "API for MixoLogger application"
    });
});

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
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(SecuriteExtensions.PolitiqueCors);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
//app.UsePathBase("/api");
app.MapGet("/ping", () => Results.Ok("pong")).AllowAnonymous();
app.MapControllers(); // Expose controllers

//app.UseHttpsRedirection();
app.Run();

/// <summary>Rendu visible pour les tests d'intégration (WebApplicationFactory).</summary>
public partial class Program;
