using Domain.Cocktails;
using Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddMvc();
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(cfg => cfg.JsonSerializerOptions.Converters.Add(new UniteVolumeConverter()));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
//app.UsePathBase("/api");
app.MapGet("/ping", () => Results.Ok("pong"));
app.MapControllers(); // Expose controllers

//app.UseHttpsRedirection();
app.Run();