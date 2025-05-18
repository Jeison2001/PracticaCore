using System.Net;
using System.Text.Json;
using Api.Responses;

namespace Api.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = new ApiResponse<object>
            {
                Success = false,
                Errors = new List<string>()
            };
        
            if (exception is FluentValidation.ValidationException validationException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Errors.AddRange(validationException.Errors.Select(e => e.ErrorMessage));
            }
            else if (exception is KeyNotFoundException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Errors.Add(exception.Message);
            }
            else if (exception is UnauthorizedAccessException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Errors.Add("No autorizado.");
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Errors.Add(exception.Message);
            }
        
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
