namespace DropFlow.Api.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            UnauthorizedAccessException => new ErrorResponse
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "Accès non autorisé",
                Detail = environment.IsDevelopment() ? exception.Message : null
            },
            ArgumentException => new ErrorResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Requête invalide",
                Detail = exception.Message
            },
            KeyNotFoundException => new ErrorResponse
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = "Ressource introuvable",
                Detail = environment.IsDevelopment() ? exception.Message : null
            },
            _ => new ErrorResponse
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "Une erreur interne s'est produite",
                Detail = environment.IsDevelopment() ? exception.Message : null
            }
        };

        context.Response.StatusCode = response.StatusCode;
        await context.Response.WriteAsJsonAsync(response);
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}