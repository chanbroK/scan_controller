var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

// app.UseHttpsRedirection();

// app.UseAuthorization();

app.MapControllers();


try
{
    app.Run();
}catch(Exception ex)
{
    Console.WriteLine(ex.ToString());
    Console.ReadLine();
}

