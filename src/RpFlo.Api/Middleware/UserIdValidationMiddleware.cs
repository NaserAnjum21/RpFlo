namespace RpFlo.Api.Middleware;

public sealed class UserIdValidationMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SafeMethods = ["GET", "HEAD", "OPTIONS"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!SafeMethods.Contains(context.Request.Method)
            && context.Request.Path.StartsWithSegments("/api"))
        {
            var header = context.Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(header) || !Guid.TryParse(header, out var userId) || userId == Guid.Empty)
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    """{"code":"Unauthorized.MissingUserId","message":"X-User-Id header is required for this operation."}""");
                return;
            }
        }

        await next(context);
    }
}
