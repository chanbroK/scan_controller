using scan_controller.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

// app.UseHttpsRedirection();

// app.UseAuthorization();

app.MapControllers();

// middleware 사용
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.Run();