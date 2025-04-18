using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models; // Add this for Swagger
using Microsoft.EntityFrameworkCore;

using OnlineVotingSystem.Data;

var builder = WebApplication.CreateBuilder(args);

// ✅ Register the database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Enable Debug Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();  // Make sure logs are printed
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Add services to the container.
builder.Services.AddControllers(); // Add API controllers
builder.Services.AddEndpointsApiExplorer(); // Enable API exploration
builder.Services.AddSwaggerGen(); // Add Swagger for testing
builder.Services.AddRazorPages(); // Keep Razor Pages if needed

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Enable Swagger UI for API testing
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.UseStaticFiles(); // Enables serving static files
app.UseFileServer(new FileServerOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/pages")
    ),
    RequestPath = "/pages",
    EnableDirectoryBrowsing = true
});

app.UseCors(options =>
{
    options.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
});

// Enable API endpoints
app.MapControllers(); // This enables your API controllers

app.Run();
