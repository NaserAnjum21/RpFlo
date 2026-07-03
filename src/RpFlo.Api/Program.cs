using System.Text.Json.Serialization;
using FluentValidation;
using RpFlo.Application;
using RpFlo.Infrastructure;
using RpFlo.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1433;Database=procurementflow;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<RpFlo.Api.Middleware.ErrorHandlingMiddleware>();
app.UseMiddleware<RpFlo.Api.Middleware.UserIdValidationMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

await SeedData.SeedAsync(app.Services);

app.Run();

public partial class Program { }
