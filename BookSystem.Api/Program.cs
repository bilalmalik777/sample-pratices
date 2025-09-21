using BookSystem.Repoistory;
using BookSystem.Services;
using BookSystem.Services.Interface;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()  // Allows requests from any origin
              .AllowAnyHeader()  // Allows any header
              .AllowAnyMethod(); // Allows any HTTP method (GET, POST, PUT, DELETE, etc.)
    });
});

builder.Services.AddDbContext<BookContext>(options => options.UseInMemoryDatabase("BookDb"));

// Add services to the container.

builder.Services.AddTransient<IBookService, BookService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseRewriter(new RewriteOptions().AddRedirect("^$", "swagger"));
app.UseCors("AllowAll");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); 
app.UseRouting();
app.UseEndpoints(routes =>
{
    routes.MapControllers();
});

app.Run();