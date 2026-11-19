using System.Text.Json;
using Api.Responses;

namespace Api.Middlewares
{
    /// <summary>
    /// Middleware anti-CSRF para endpoints mutantes autenticados mediante cookies HttpOnly.
    /// Exige origen válido (cuando Origin o Referer están presentes) y un encabezado personalizado anti-CSRF
    /// salvo en el dashboard de Hangfire o cuando la petición usa autenticación Bearer explícita.
    /// </summary>
    public class CsrfProtectionMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly RequestDelegate _next;
        private readonly HashSet<string> _allowedOrigins;

        public CsrfProtectionMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;

            var configuredOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            _allowedOrigins = new HashSet<string>(
                configuredOrigins
                    .Where(origin => !string.IsNullOrWhiteSpace(origin))
                    .Select(origin => origin.Trim().TrimEnd('/')),
                StringComparer.OrdinalIgnoreCase);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/hangfire"))
            {
                await _next(context);
                return;
            }

            var method = context.Request.Method;
            if (!HttpMethods.IsPost(method) &&
                !HttpMethods.IsPut(method) &&
                !HttpMethods.IsPatch(method) &&
                !HttpMethods.IsDelete(method))
            {
                await _next(context);
                return;
            }

            var hasAccessCookie = context.Request.Cookies.TryGetValue("accessToken", out var accessToken)
                && !string.IsNullOrWhiteSpace(accessToken);
            var hasRefreshCookie = context.Request.Cookies.TryGetValue("refreshToken", out var refreshToken)
                && !string.IsNullOrWhiteSpace(refreshToken);

            if (!hasAccessCookie && !hasRefreshCookie)
            {
                await _next(context);
                return;
            }

            if (context.Request.Headers.Authorization.Any(header =>
                    !string.IsNullOrEmpty(header) &&
                    header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            var rawOriginOrReferer = GetHeaderValue(context.Request.Headers, "Origin")
                ?? GetHeaderValue(context.Request.Headers, "Referer");

            if (rawOriginOrReferer != null)
            {
                var normalizedOrigin = Uri.TryCreate(rawOriginOrReferer, UriKind.Absolute, out var uri)
                    ? $"{uri.Scheme}://{uri.Authority}"
                    : rawOriginOrReferer.TrimEnd('/');

                var sameOrigin = context.Request.Host.HasValue
                    ? $"{context.Request.Scheme}://{context.Request.Host}"
                    : null;

                var isAllowedOrigin = _allowedOrigins.Contains(normalizedOrigin)
                    || (sameOrigin != null && string.Equals(normalizedOrigin, sameOrigin, StringComparison.OrdinalIgnoreCase));

                if (!isAllowedOrigin)
                {
                    await WriteForbiddenAsync(context, "Origen de petición no permitido (CSRF).");
                    return;
                }
            }

            var hasXRequestedWith = context.Request.Headers.TryGetValue("X-Requested-With", out var xRequestedWith)
                && xRequestedWith.Any(value => string.Equals(value?.Trim(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase));

            var hasCsrfProtection = context.Request.Headers.TryGetValue("X-CSRF-Protection", out var csrfProtection)
                && csrfProtection.Any(value => string.Equals(value?.Trim(), "1", StringComparison.Ordinal));

            if (!hasXRequestedWith && !hasCsrfProtection)
            {
                await WriteForbiddenAsync(context, "Encabezado anti-CSRF requerido para sesiones basadas en cookies.");
                return;
            }

            await _next(context);
        }

        private static string? GetHeaderValue(IHeaderDictionary headers, string headerName)
        {
            if (!headers.TryGetValue(headerName, out var values))
                return null;

            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return null;
        }

        private static Task WriteForbiddenAsync(HttpContext context, string errorMessage)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>
            {
                Success = false,
                Errors = new List<string> { errorMessage }
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }
}
