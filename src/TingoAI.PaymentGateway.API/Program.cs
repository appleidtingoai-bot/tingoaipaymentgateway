using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.IO;
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

// Load a local .env file (useful for local development). Copy `.env.example` -> `.env` and
// the loader will set process environment variables before the host is built.
void LoadDotEnv()
{
    try
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (!File.Exists(envPath)) return;

        foreach (var raw in File.ReadAllLines(envPath))
        {
            var line = raw?.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            var idx = line.IndexOf('=');
            if (idx <= 0) continue;

            var key = line.Substring(0, idx).Trim();
            var val = line.Substring(idx + 1).Trim();

            if (val.Length >= 2 && ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'"))))
            {
                val = val.Substring(1, val.Length - 2);
            }

            Environment.SetEnvironmentVariable(key, val);
        }
    }
    catch
    {
        // don't block startup on env load errors
    }
}

try
{
    // Load local .env into process environment (if present)
    LoadDotEnv();

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "TingoAI Payment Gateway API", Version = "v1" });

        // Add Basic auth security definition so Swagger UI can send credentials to protected endpoints
        c.AddSecurityDefinition("basic", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "basic",
            Description = "Basic HTTP Authentication"
        });

        // Apply operation filter to only add security requirement to operations with marker attribute
        c.OperationFilter<TingoAI.PaymentGateway.API.Swagger.BasicAuthOperationFilter>();

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

    // Register HTTP Client for GlobalPay and configure it from IConfiguration
    builder.Services.AddHttpClient<IGlobalPayClient, GlobalPayClient>((sp, client) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var baseUrl = config["GlobalPay:BaseUrl"];
        var publicKey = config["GlobalPay:PublicKey"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("GlobalPay:BaseUrl is not configured");
        }

        if (string.IsNullOrWhiteSpace(publicKey))
        {
            throw new InvalidOperationException("GlobalPay:PublicKey is not configured");
        }

        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Add("apikey", publicKey);
        client.DefaultRequestHeaders.Add("Language", "en");
    });

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

    // Register BasicAuthFilter so it can be applied via ServiceFilter
    builder.Services.AddScoped<TingoAI.PaymentGateway.API.Filters.BasicAuthFilter>();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    // Allow enabling Swagger in Production via configuration when needed for debugging.
    // Default is false; set `EnableSwaggerInProduction=true` in appsettings or environment to enable.
    var enableSwaggerInProd = builder.Configuration.GetValue<bool>("EnableSwaggerInProduction", false);
    Log.Information("Environment: {Environment}, EnableSwaggerInProduction: {EnableSwagger}", app.Environment.EnvironmentName, enableSwaggerInProd);
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

    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<RateLimitingMiddleware>();

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
