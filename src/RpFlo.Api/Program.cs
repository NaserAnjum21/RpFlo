using FluentValidation;
using RpFlo.Application;
using RpFlo.Infrastructure;
using RpFlo.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=procurementflow;Username=postgres;Password=postgres";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddControllers();
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
app.MapControllers();

await SeedData.SeedAsync(app.Services);

app.Run();

public partial class Program { }
