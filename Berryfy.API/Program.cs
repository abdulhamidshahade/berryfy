using Berryfy.API.OpenApi.Transformers;
using Berryfy.Application.Authorization.Handlers;
using Berryfy.Application.Config;
using Berryfy.Application.DI;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Infrastructure;
using Berryfy.Infrastructure.Data;
using Berryfy.Infrastructure.DI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<VersionInfoTransformer>();
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddAutoMapper(cfg => { }, typeof(Berryfy.Application.Mapping.AssemblyMarker));

builder.Services.AddApplicationServices(builder.Host, builder.Configuration);
builder.Services.AddInfrastructureServices();

builder.Services.AddHealthChecks()
    .AddCheck<Berryfy.Infrastructure.Data.DatabaseHealthCheck>("database");

builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<ResendSettings>(builder.Configuration.GetSection(ResendSettings.Name));
builder.Services.AddHttpClient<Resend.ResendClient>();
builder.Services.AddOptions<Resend.ResendClientOptions>()
    .Configure<IOptions<ResendSettings>>((options, resend) =>
    {
        var key = resend.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            key = Environment.GetEnvironmentVariable("ResendSettings__ApiKey")
                ?? Environment.GetEnvironmentVariable("RESEND_APITOKEN")
                ?? string.Empty;
        }

        options.ApiToken = key;
    });
builder.Services.AddTransient<Resend.IResend, Resend.ResendClient>();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("ApiSettings:JwtOptions"));

JwtOptions jwtOptions = new JwtOptions();
builder.Configuration.GetSection("ApiSettings:JwtOptions").Bind(jwtOptions);

var corsSection = builder.Configuration.GetSection("Cors:AllowedOrigins");
var corsOrigins = corsSection.Get<string[]>() ?? Array.Empty<string>();
var corsOriginsOverride = builder.Configuration["Cors:Origins"];
if (!string.IsNullOrWhiteSpace(corsOriginsOverride))
{
    corsOrigins = corsOriginsOverride
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

if (corsOrigins.Length == 0 && builder.Environment.IsDevelopment())
{
    corsOrigins =
    [
        "http://localhost:3000",
        "http://localhost:30305",
        "https://localhost:7105",
        "http://host.docker.internal:3000",
        "http://frontend:30305"
    ];
}
else if (corsOrigins.Length == 0 && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "Configure Cors:AllowedOrigins (array) or Cors:Origins (semicolon-separated) for this environment.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var useForwardedHeaders = builder.Configuration.GetValue("Security:UseForwardedHeaders", true);
if (useForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddProblemDetails();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyConstants.SuperAdminOnly, policy =>
        policy.RequireRole(RoleConstants.SuperAdmin));

    options.AddPolicy(PolicyConstants.AdminAndAbove, policy =>
        policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin));

    options.AddPolicy(PolicyConstants.UserAndAbove, policy =>
        policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin, RoleConstants.User));

    options.AddPolicy(PolicyConstants.AllRoles, policy =>
        policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin, RoleConstants.User));

    options.AddPolicy(PolicyConstants.CanManageUsers, policy =>
    {
        policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin);
        policy.RequireClaim("Permission", "ManageUsers");
    });

    options.AddPolicy(PolicyConstants.CanManageProducts, policy =>
        policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin));

    options.AddPolicy(PolicyConstants.CanManageOrders, policy =>
        policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin));

    options.AddPolicy(PolicyConstants.CanManageCoupons, policy =>
        policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin));

    options.AddPolicy(PolicyConstants.CanViewReports, policy =>
        policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin));

    options.AddPolicy(PolicyConstants.CanManageShops, policy =>
        policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin));

    options.AddPolicy(PolicyConstants.CanManageInventory, policy =>
        policy.RequireRole(RoleConstants.SuperAdmin, RoleConstants.Admin));
});

builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = jwtOptions.RequireHttps;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
    };
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = builder.Configuration.GetValue("RateLimiting:GlobalPermitLimit", 400),
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = builder.Configuration.GetValue("RateLimiting:GlobalQueueLimit", 20),
        });
    });

    options.AddFixedWindowLimiter("DefaultPolicy", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = builder.Configuration.GetValue("RateLimiting:DefaultPermitLimit", 100);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = builder.Configuration.GetValue("RateLimiting:DefaultQueueLimit", 10);
    });
});

builder.Configuration.AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets(Assembly.GetExecutingAssembly())
    .AddEnvironmentVariables()
    .Build();

var migrationConnectionString = builder.Configuration.GetConnectionString("PostgreConnectionString")
    ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
    ?? throw new InvalidOperationException("PostgreSQL connection string is not configured.");
DatabaseMigrator.Run(migrationConnectionString);

var app = builder.Build();

if (useForwardedHeaders)
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler("/error");

var requireHttpsRedirect = builder.Configuration.GetValue("Security:RequireHttpsRedirection", false);
if (requireHttpsRedirect && !app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

var useHsts = builder.Configuration.GetValue("Security:UseHsts", false);
if (useHsts && app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseRateLimiter();

Log.Information(
    "Startup configuration → RunSeedOnStartup={RunSeed}, Environment={Env}",
    builder.Configuration.GetValue("Database:RunSeedOnStartup", false),
    app.Environment.EnvironmentName);

try
{
    using var seedScope = app.Services.CreateScope();
    var dataSeeder = seedScope.ServiceProvider.GetRequiredService<DataSeeder>();
    await dataSeeder.SeedDataAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Database seeding failed; continuing startup (API will run without full seed data).");
    if (app.Environment.IsDevelopment())
    {
        throw;
    }
}

var enableApiDocs = app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue("OpenApi:EnableInProduction", false);

if (enableApiDocs)
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Berryfy API")
            .WithTheme(Scalar.AspNetCore.ScalarTheme.Purple)
            .WithDefaultHttpClient(Scalar.AspNetCore.ScalarTarget.CSharp, Scalar.AspNetCore.ScalarClient.HttpClient);
    });
}

app.UseCors("AllowSpecificOrigin");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.StatusCode = report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            })
        };
        await context.Response.WriteAsJsonAsync(result);
    }
}).DisableRateLimiting();

// Liveness only (no DB): use for Docker HEALTHCHECK so the API container can become healthy
// while /health remains a full readiness check (database + migrations).
app.MapGet("/health/live", () => Results.Text("OK", "text/plain"))
    .DisableRateLimiting()
    .WithTags("Health");

app.UseSerilogRequestLogging();

app.MapControllers();

app.Run();