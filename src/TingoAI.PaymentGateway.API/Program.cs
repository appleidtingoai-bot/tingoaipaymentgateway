using Microsoft.EntityFrameworkCore;
using System.Linq;
using Serilog;
using TingoAI.PaymentGateway.API.Middleware;
using TingoAI.PaymentGateway.Application.Interfaces;
using TingoAI.PaymentGateway.Application.Services;
using TingoAI.PaymentGateway.Domain.Repositories;
using TingoAI.PaymentGateway.Infrastructure.Caching;
using TingoAI.PaymentGateway.Infrastructure.ExternalServices;
using TingoAI.PaymentGateway.Infrastructure.Persistence;
using TingoAI.PaymentGateway.Infrastructure.Repositories;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/tingoai-payment-gateway-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "TingoAI Payment Gateway API", Version = "v1" });
        // Include XML comments if available for better documentation in Swagger UI
        try
        {
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (System.IO.File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        }
        catch
        {
            // ignore if xml doc can't be loaded
        }
    });

    // Configure PostgreSQL
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Register Redis Cache
    builder.Services.AddSingleton<RedisCache>();

    // Register HTTP Client for GlobalPay
    builder.Services.AddHttpClient<IGlobalPayClient, GlobalPayClient>();

    // Register repositories
    builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

    // Register application services
    builder.Services.AddScoped<IPaymentService, PaymentService>();
    builder.Services.AddScoped<ITransactionService, TransactionService>();

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Add response compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<RateLimitingMiddleware>();

    // Allow enabling Swagger in Production via configuration when needed for debugging.
    // Default is false; set `EnableSwaggerInProduction=true` in appsettings or environment to enable.
    var enableSwaggerInProd = builder.Configuration.GetValue<bool>("EnableSwaggerInProduction", false);
    if (app.Environment.IsDevelopment() || enableSwaggerInProd)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            // Explicitly set endpoint so Swagger UI can find the generated JSON
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "TingoAI Payment Gateway API v1");
            // serve the UI at /swagger (default) — change RoutePrefix if you want it at root
            c.RoutePrefix = "swagger";
        });

        // Redirect root requests to the Swagger UI so GET / doesn't return 404 when enabled
        app.MapGet("/", () => Results.Redirect("/swagger"));
    }

    app.UseResponseCompression();
    app.UseCors("AllowAll");
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    // Auto-migrate or ensure database creation on startup (development convenience)
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // If there are pending migrations, apply them. If no migrations exist
            // (e.g. developer didn't scaffold migrations), fall back to EnsureCreated
            // so the schema is created for development/testing.
            var pending = db.Database.GetPendingMigrations();
            if (pending != null && pending.Any())
            {
                db.Database.Migrate();
            }
            else
            {
                db.Database.EnsureCreated();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database migration/creation failed on startup. Continuing without migration.");
        }
    }

    Log.Information("TingoAI Payment Gateway API starting...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
