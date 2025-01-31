using System.Text;
using Carter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using EventService.Infrastructure;
using AspNetCoreRateLimit;
using EventService.WebApi.Middleware;
using EventService.Application.Interfaces.Services.Payments;
using EventService.Application.Services.Payments;
using EventService.WebApi.Swagger;
using EventService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Configure options pattern
builder.Services.Configure<AppSettings>(builder.Configuration);
var configuration = builder.Configuration.Get<AppSettings>();

// Configure Serilog with Elasticsearch sink
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(configuration.ElasticSearch.Uri))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "eventservice-logs-{0:yyyy.MM}"
    })
    .Enrich.WithProperty("Service", "Event Processing")
    .CreateLogger();

builder.Host.UseSerilog();

// ✅ Add Middleware Dependencies
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddInMemoryRateLimiting();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCarter();

builder.Services.AddHttpClient<PaymentService>();

builder.Services.AddScoped<IPaymentService>(sp =>
    new PaymentService(
        sp.GetRequiredService<HttpClient>(),
        builder.Configuration["Zarinpal:MerchantId"] ?? throw new InvalidOperationException("Zarinpal Merchant ID is missing!"),
        sp.GetRequiredService<ILogger<PaymentService>>()
    ));

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration.Jwt.Issuer,
            ValidAudience = configuration.Jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.Jwt.Key))
        };
    });

builder.Services.AddAuthorization();

// Add Redis Caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConfigurationOptions = ConfigurationOptions.Parse(configuration.Redis.ConnectionString);
});

// Configure Polly for Resilience & Retry Policies
builder.Services.AddHttpClient("EventServiceClient")
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(2);
    });

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true; // ✅ Allow case-insensitive deserialization
    options.SerializerOptions.PropertyNamingPolicy = null; // ✅ Ensure names match exactly
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles; // ✅ Prevent circular reference issues
});

// Add OpenAPI (Swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Event Service API", Version = "v1" });
    options.AddSecurityDefinition("X-Tenant-Id", new OpenApiSecurityScheme
    {
        Name = "X-Tenant-Id",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Tenant ID required for multi-tenant operations",
    });

    options.OperationFilter<SwaggerTenantHeaderFilter>();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.MigrateAndSeedAsync();
    }
    catch (Exception ex)
    {
        throw;
    }
}

// Enable Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ Register Middleware Here
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<PerformanceMonitoringMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<WebhookVerificationMiddleware>();

app.UseIpRateLimiting(); // ✅ Apply Rate Limiting

app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging();

app.MapCarter();

app.Run();

