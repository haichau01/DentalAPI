using System.Text.Json.Serialization;
using DentalAPI.Api.Authentication;
using DentalAPI.Api.BackgroundJobs;
using DentalAPI.Api.Development;
using DentalAPI.Api.ErrorHandling;
using DentalAPI.Api.Networking;
using DentalAPI.Api.OpenApi;
using DentalAPI.Api.RateLimiting;
using DentalAPI.Application;
using DentalAPI.Application.Common.Security;
using DentalAPI.Infrastructure;
using DentalAPI.Infrastructure.Notifications;
using DentalAPI.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddDentalAuthentication();
builder.Services.AddDentalRateLimiting();
builder.Services.AddTrustedForwardedHeaders(builder.Configuration);
builder.Services.AddScoped<IClientContext, HttpContextClientContext>();
builder.Services.AddHostedService<ExpiredRegistrationCleanupWorker>();

// No real SMS or email provider is integrated yet. Failing at startup outside Development keeps a deployment
// from silently accepting registrations whose verification codes can never arrive.
if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("No SMS/email provider is configured; patient registration cannot work outside Development.");
}

builder.Services.AddDevelopmentNotifications();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();

// The built-in middleware logs every exception with a stack trace, including expected 400/404s.
// ApplicationExceptionHandler does the logging instead, so real errors stand out.
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();

// After authentication, because some limits are counted per signed-in account.
app.UseRateLimiter();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapDevelopmentOutbox();
    await app.Services.InitializeDevelopmentDatabaseAsync();
}

await app.Services.EnsureBootstrapAdminAsync();

// HTTPS is terminated by the reverse proxy in deployed environments, so the app serves plain HTTP.
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
