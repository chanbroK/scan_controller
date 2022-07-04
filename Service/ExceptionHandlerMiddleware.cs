using System.Net;
using System.Text.Json;
using scan_controller.Exception;

namespace scan_controller.Service;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (System.Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            // error 분기 
            switch (exception)
            {
                case BusinessException e:
                    response.StatusCode = e.StatusCode;
                    await response.WriteAsync(JsonSerializer.Serialize(new {message = e.Message}));
                    break;
                default:
                    response.StatusCode = (int) HttpStatusCode.InternalServerError;
                    Console.WriteLine(exception.Message);
                    Console.WriteLine(exception.StackTrace);
                    await response.WriteAsync(JsonSerializer.Serialize(new {errorMessage = "미처리 예외 발생"}));
                    break;
            }
        }
    }
}