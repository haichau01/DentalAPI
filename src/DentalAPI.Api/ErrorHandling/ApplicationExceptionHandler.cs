using DentalAPI.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DentalAPI.Api.ErrorHandling;

/// <summary>
/// Turns expected application exceptions into RFC 9457 problem details.
/// Anything else is logged as an error and falls through to the default handler, which returns a 500.
/// </summary>
/// <remarks>
/// This handler is the only place exceptions are logged (see Program.cs). Expected exceptions are logged
/// by type only, because their messages can contain request data such as patient details.
/// </remarks>
public sealed class ApplicationExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApplicationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = ToProblemDetails(exception);
        if (problemDetails is null)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            return false;
        }

        logger.LogInformation(
            "{ExceptionType} mapped to HTTP {StatusCode} for {Method} {Path}",
            exception.GetType().Name,
            problemDetails.Status,
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });
    }

    private static ProblemDetails? ToProblemDetails(Exception exception) => exception switch
    {
        ValidationException validationException => ToValidationProblem(validationException),
        NotFoundException notFound => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Resource not found",
            Detail = notFound.Message,
        },
        ConflictException conflict => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = conflict.Message,
        },
        AuthenticationFailedException => Problem(StatusCodes.Status401Unauthorized, "Authentication failed", exception),
        ForbiddenException => Problem(StatusCodes.Status403Forbidden, "Forbidden", exception),
        InvalidVerificationCodeException => Problem(StatusCodes.Status400BadRequest, "Invalid verification code", exception),
        VerificationCodeCooldownException => Problem(StatusCodes.Status429TooManyRequests, "Too many requests", exception),
        _ => null,
    };

    private static ProblemDetails Problem(int status, string title, Exception exception) => new()
    {
        Status = status,
        Title = title,
        Detail = exception.Message,
    };

    private static ValidationProblemDetails ToValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        };
    }
}
