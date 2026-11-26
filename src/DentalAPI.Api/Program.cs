using DentalAPI.Api.ErrorHandling;
using DentalAPI.Application;
using DentalAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();

// The built-in middleware logs every exception with a stack trace, including expected 400/404s.
// ApplicationExceptionHandler does the logging instead, so real errors stand out.
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await app.Services.InitializeDevelopmentDatabaseAsync();
}

// HTTPS is terminated by the reverse proxy in deployed environments, so the app serves plain HTTP.
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
