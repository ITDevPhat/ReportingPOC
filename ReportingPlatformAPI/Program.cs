using System.Text.Json.Serialization;
using ReportingPlatform.Application.DependencyInjection;
using ReportingPlatform.Api.Middleware;
using ReportingPlatform.Api.Services;
using ReportingPlatform.Infrastructure.DependencyInjection;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console();
});

// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Application DI
builder.Services.AddApplication();

// Infrastructure DI
builder.Services.AddInfrastructure(builder.Configuration);

if (bool.TryParse(builder.Configuration["ReportingEngine:EnableBackgroundWorker"], out var enableBackgroundWorker)
    && enableBackgroundWorker)
{
    builder.Services.AddHostedService<ReportExecutionWorker>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthorization();

app.UseMiddleware<SimpleRateLimitingMiddleware>();
app.UseMiddleware<AuditMiddleware>();

app.MapControllers();

app.MapGet("/report-builder", (IWebHostEnvironment environment) =>
{
    var webRootPath = environment.WebRootPath
        ?? Path.Combine(environment.ContentRootPath, "wwwroot");
    var filePath = Path.Combine(webRootPath, "report-builder.html");

    return Results.File(filePath, "text/html");
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "OK",
    service = "ReportingPlatform.Api",
    timestamp = DateTime.UtcNow
}));

app.Run();
