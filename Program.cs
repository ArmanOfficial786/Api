using jsreport.AspNetCore;
using jsreport.Binary;
using jsreport.Local;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Extensions;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Initialize jsreport server with error handling
ILocalWebServerReportingService? jsreportServer = null;
try
{
    jsreportServer = new LocalReporting()
        .UseBinary(JsReportBinary.GetBinary())
        .KillRunningJsReportProcesses()
        .Configure(cfg =>
        {
            cfg.DoTrustUserCode();
            return cfg;
        })
        .AsWebServer()
        .Create();

    jsreportServer.StartAsync().GetAwaiter().GetResult();
}
catch (Exception ex)
{
    Console.WriteLine($"[Warning] jsreport initialization failed: {ex.Message}");
    jsreportServer = null;
}

if (jsreportServer != null)
{
    builder.Services.AddJsReport(jsreportServer);
}

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Duplicate-name detection: only used to decide when a schema/operation ID
// needs a parent-namespace prefix. Non-duplicated names are left untouched.

var duplicateTypeNames = AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a =>
    {
        try { return a.GetTypes(); }
        catch { return Array.Empty<Type>(); }
    })
    .Where(t => t.Namespace != null && t.Namespace.StartsWith("NexgenCosysReport"))
    .GroupBy(t => t.Name)
    .Where(g => g.Count() > 1)
    .Select(g => g.Key)
    .ToHashSet();

var duplicateControllerNames = typeof(Program).Assembly
    .GetTypes()
    .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
    .Select(t => t.Name.EndsWith("Controller") ? t.Name[..^"Controller".Length] : t.Name)
    .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
    .Where(g => g.Count() > 1)
    .Select(g => g.Key)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "NexgenCosysReport API", Version = "v1" });

    // DTO schema names: unchanged unless duplicated elsewhere.
    options.CustomSchemaIds(type => GetSchemaId(type, duplicateTypeNames));

    // Tags: same grouping you already have ("Account/AccountingReports" etc).
    options.TagActionsBy(apiDesc =>
    {
        var cad = apiDesc.ActionDescriptor as ControllerActionDescriptor;
        if (cad == null) return new[] { "Other" };

        var ns = cad.ControllerTypeInfo.Namespace ?? "";
        const string prefix = "NexgenCosysReport.Controllers.";
        var relative = ns.StartsWith(prefix) ? ns[prefix.Length..] : ns;

        var tag = string.IsNullOrEmpty(relative) ? "Root" : relative.Replace('.', '/');
        return new[] { tag };
    });

    // Operation IDs (drive generated TS method names): left null (default
    // naming) unless the controller's bare name is duplicated elsewhere.
    options.CustomOperationIds(apiDesc =>
    {
        var cad = apiDesc.ActionDescriptor as ControllerActionDescriptor;
        if (cad == null) return null;

        var controllerName = cad.ControllerName;
        var actionName = cad.ActionName;

        if (!duplicateControllerNames.Contains(controllerName))
        {
            return null;
        }

        var ns = cad.ControllerTypeInfo.Namespace ?? "";
        const string prefix = "NexgenCosysReport.Controllers.";
        var relative = ns.StartsWith(prefix) ? ns[prefix.Length..] : ns;
        var group = (string.IsNullOrEmpty(relative) ? "Root" : relative).Replace('.', '_');

        return $"{group}_{controllerName}_{actionName}";
    });

    options.DocInclusionPredicate((docName, apiDesc) => true);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Bearer prefix is added automatically."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5106")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders(
                "X-Pagination",
                "X-Message",
                "X-IsValid",
                "X-StatusCode",
                "Content-Disposition",
                "X-Pages-Ready",
                "X-Is-Complete",
                "X-Total-Chunks",
                "X-Completed-Chunks",
                "X-Size-Bytes"
                );
    });
});

// Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DatabaseConnectionString")));

// ReportSettings
builder.Services.Configure<ReportSettings>(
    builder.Configuration.GetSection(ReportSettings.SectionName));

var settingsCheck = builder.Configuration
    .GetSection(ReportSettings.SectionName)
    .Get<ReportSettings>();
Console.WriteLine($"[Startup] WebRootPath = '{settingsCheck?.WebRootPath}'");

// Services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CustomHeaderResponse>();
builder.Services.AddHostedService<ProgressiveTempCleanupService>();

builder.Services.AddRepositoriesAndServices(
    typeof(Program).Assembly
);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

if (jsreportServer != null)
{
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopping.Register(() =>
    {
        try
        {
            Console.WriteLine("[Shutdown] Stopping jsreport server...");
            jsreportServer.KillAsync().GetAwaiter().GetResult();
            Console.WriteLine("[Shutdown] jsreport server stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Shutdown] Error stopping jsreport: {ex.Message}");
        }
    });
}

app.UseSwagger();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}
else
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NexgenCosysReport API v1");
        c.RoutePrefix = "api-docs";
    });
}

app.UseCors("AllowReactApp");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

DapperTypeMaps.Register();

app.MapControllers();
app.Run();

// Generates a stable, collision-safe schema ID for Swagger/OpenAPI.
// Non-duplicated types keep their plain name. Duplicated types (same
// class name in two different namespaces) get prefixed with their
// namespace path so Swashbuckle doesn't throw a schemaId collision.
static string GetSchemaId(Type type, HashSet<string> duplicateTypeNames)
{
    if (type.IsGenericType)
    {
        var genericTypeName = type.Name.Split('`')[0];
        var argNames = type.GetGenericArguments()
            .Select(arg => GetSchemaId(arg, duplicateTypeNames));
        return $"{genericTypeName}Of{string.Join("And", argNames)}";
    }

    if (type.IsGenericParameter)
    {
        return type.Name;
    }

    if (duplicateTypeNames.Contains(type.Name))
    {
        var ns = type.Namespace?.Replace("NexgenCosysReport.Dtos.RequestDtos.", "")
                                 .Replace("NexgenCosysReport.Dtos.", "")
                                 .Replace("NexgenCosysReport.", "")
                                 .Replace(".", "_");
        return $"{ns}_{type.Name}";
    }

    return type.Name;
}