namespace RpFlo.Api.Middleware;

public sealed class UserIdValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresUserId(context.Request))
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

    private static bool RequiresUserId(HttpRequest request) =>
        request.Path.StartsWithSegments("/api") &&
        !request.Path.StartsWithSegments("/api/users");
}
