using System.Net;
using System.Text.Json;

namespace scan_controller.Exception;

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
                    await response.WriteAsync(JsonSerializer.Serialize(new {message = "Not Handled Exception"}));
                    break;
            }
        }
    }
}