using Api.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Tests.UnitTests.Api.Middlewares
{
    public class CsrfProtectionMiddlewareTests
    {
        private static CsrfProtectionMiddleware CreateMiddleware(
            Action? onNext = null,
            params string[] allowedOrigins)
        {
            var inMemory = new Dictionary<string, string?>();
            for (var i = 0; i < allowedOrigins.Length; i++)
            {
                inMemory[$"AllowedOrigins:{i}"] = allowedOrigins[i];
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemory)
                .Build();

            return new CsrfProtectionMiddleware(_ =>
            {
                onNext?.Invoke();
                return Task.CompletedTask;
            }, configuration);
        }

        [Fact]
        public async Task InvokeAsync_HangfirePath_BypassesCsrfChecksEvenWithCookies()
        {
            var nextCalled = false;
            var middleware = CreateMiddleware(() => nextCalled = true, "http://localhost:4200");

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.Path = "/hangfire/jobs";
            context.Request.Headers.Cookie = "accessToken=jwt-cookie";

            await middleware.InvokeAsync(context);

            nextCalled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task InvokeAsync_SafeGetMethodWithCookies_BypassesCsrfChecks()
        {
            var nextCalled = false;
            var middleware = CreateMiddleware(() => nextCalled = true, "http://localhost:4200");

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/api/users";
            context.Request.Headers.Cookie = "accessToken=jwt-cookie";

            await middleware.InvokeAsync(context);

            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_PostWithCookieAndCaseInsensitiveXRequestedWith_AndTrailingSlashAllowedOrigin_AllowsRequest()
        {
            var nextCalled = false;
            var middleware = CreateMiddleware(() => nextCalled = true, "http://localhost:4200/");

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.Path = "/api/Auth/refresh";
            context.Request.Headers.Cookie = "refreshToken=rt-cookie";
            context.Request.Headers["Origin"] = "http://localhost:4200";
            context.Request.Headers["X-Requested-With"] = "xmlhttprequest";

            await middleware.InvokeAsync(context);

            nextCalled.Should().BeTrue();
        }
    }
}
