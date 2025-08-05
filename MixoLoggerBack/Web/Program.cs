using Domain.Cocktails;

var builder = WebApplication.CreateBuilder(args);

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
    builder.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
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

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
//app.UsePathBase("/api");
app.MapGet("/ping", () => Results.Ok("pong"));
app.MapControllers(); // Expose controllers

//app.UseHttpsRedirection();
app.Run();