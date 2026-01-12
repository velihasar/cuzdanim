using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.IoC;
using Core.Utilities.Messages;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Core.Extensions
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;


        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception e)
            {
                await HandleExceptionAsync(httpContext, e);
            }
        }


        private async Task HandleExceptionAsync(HttpContext httpContext, Exception e)
        {
            // Log exception
            try
            {
                var logger = ServiceTool.ServiceProvider?.GetService<FileLogger>();
                if (logger != null)
                {
                    var requestPath = httpContext.Request.Path;
                    var requestMethod = httpContext.Request.Method;
                    var requestBody = await GetRequestBodyAsync(httpContext);
                    
                    var logMessage = $"[ExceptionMiddleware] Exception caught - " +
                                    $"Path: {requestPath}, " +
                                    $"Method: {requestMethod}, " +
                                    $"Exception Type: {e.GetType().Name}, " +
                                    $"Message: {e.Message}, " +
                                    $"StackTrace: {e.StackTrace}, " +
                                    $"InnerException: {(e.InnerException != null ? e.InnerException.Message : "None")}, " +
                                    $"Request Body: {requestBody}";
                    
                    logger.Error(logMessage);
                }
            }
            catch
            {
                // Ignore logging errors
            }

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            _ = e.Message;
            string message;
            if (e.GetType() == typeof(ValidationException))
            {
                var validationException = e as ValidationException;
                var errors = validationException?.Errors?.Select(x => new
                {
                    PropertyName = x.PropertyName,
                    ErrorMessage = x.ErrorMessage,
                    ErrorCode = x.ErrorCode
                }).ToList();
                
                message = JsonConvert.SerializeObject(new
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors
                });
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            else if (e.GetType() == typeof(ApplicationException))
            {
                message = JsonConvert.SerializeObject(new
                {
                    Success = false,
                    Message = e.Message
                });
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            else if (e.GetType() == typeof(UnauthorizedAccessException))
            {
                message = JsonConvert.SerializeObject(new
                {
                    Success = false,
                    Message = e.Message
                });
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
            else if (e.GetType() == typeof(SecurityException))
            {
                message = JsonConvert.SerializeObject(new
                {
                    Success = false,
                    Message = e.Message
                });
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
            else if (e.GetType() == typeof(NotSupportedException))
            {
                message = JsonConvert.SerializeObject(new
                {
                    Success = false,
                    Message = e.Message
                });
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            else
            {
                // Her zaman JSON formatında döndür
                message = JsonConvert.SerializeObject(new
                {
                    Success = false,
                    Message = ExceptionMessage.InternalServerError
                });
            }
            await httpContext.Response.WriteAsync(message);
        }

        private async Task<string> GetRequestBodyAsync(HttpContext httpContext)
        {
            try
            {
                if (httpContext.Request.Body == null)
                    return "No body";

                httpContext.Request.EnableBuffering();
                httpContext.Request.Body.Position = 0;

                using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                httpContext.Request.Body.Position = 0;

                return string.IsNullOrEmpty(body) ? "Empty body" : body;
            }
            catch
            {
                return "Could not read body";
            }
        }
    }
}