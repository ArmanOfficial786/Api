using jsreport.AspNetCore;
using jsreport.Binary;
using jsreport.Local;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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


// ? Fix 3: Configure lambda must return cfg
var jsreportServer = new LocalReporting()
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

// ? Fix 1: Only AddJsReport needed — AddJsReportMVC does not exist
builder.Services.AddJsReport(jsreportServer);
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "NexgenCosysReport API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Just paste the token — 'Bearer ' prefix is added automatically."
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
                // ← Add these progressive headers:
                "X-Pages-Ready",
                "X-Is-Complete",
                "X-Total-Chunks",
                "X-Completed-Chunks",
                "X-Size-Bytes"
                );
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"]!;
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


// Startup verification
var settingsCheck = builder.Configuration
    .GetSection(ReportSettings.SectionName)
    .Get<ReportSettings>();
Console.WriteLine($"[Startup] WebRootPath = '{settingsCheck?.WebRootPath}'");

// Services
builder.Services.AddMemoryCache();

builder.Services.AddScoped<CustomHeaderResponse>();
builder.Services.AddHostedService<ProgressiveTempCleanupService>();

// Auto-register repositories and services via reflection
builder.Services.AddRepositoriesAndServices(
    typeof(Program).Assembly
);










builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

// ? Fix 2: StopAsync requires CancellationToken
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("[Shutdown] Stopping jsreport server...");
    jsreportServer.KillAsync().GetAwaiter().GetResult(); // ? correct method
    Console.WriteLine("[Shutdown] jsreport server stopped.");
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 1. CORS FIRST
app.UseCors("AllowReactApp");

// 2. Skip HTTPS in dev
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 3. Routing and auth
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

DapperTypeMaps.Register();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();