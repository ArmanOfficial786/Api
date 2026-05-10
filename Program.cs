using jsreport.AspNetCore;
using jsreport.Binary;
using jsreport.Local;
using NexgenCosysReport;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Services.CommonService;
using NexgenCosysReport.Services.ReportService;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Repository.Common;
using NexgenCosysReport.Repository.Member;
using NexgenCosysReport.Repository.MemberAccount;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using NexgenCosysReport.Inteface.ServiceInterface.Member;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// jsreport
builder.Services.AddJsReport(new LocalReporting()
    .UseBinary(JsReportBinary.GetBinary())
    .KillRunningJsReportProcesses()
    .Configure(cfg => cfg.DoTrustUserCode())  // ? required for custom extensions
    .AsUtility()
    .Create());

// ? CORS — handles null origin + all localhost ports
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5106")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

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
builder.Services.AddSingleton<IJsReportService, JsReportService>();
builder.Services.AddScoped<IMemberDetail, MemberRegistrationDetailHandler>();
builder.Services.AddScoped<IMemberIdCard, MemberIdCardRepository>();
builder.Services.AddScoped<IAccountStatement, AccountStatementRepository>();
builder.Services.AddScoped<IBranch, BranchRepository>();
builder.Services.AddScoped<IOrderBy, OrderByService>();
builder.Services.AddScoped<IDateConverterService, DateConverterService>();
builder.Services.AddScoped<IMemberLookUp, MemberLookUpRepository>();
builder.Services.AddScoped<ICollectionCenter, CollectionCenterRepository>();
builder.Services.AddScoped<IMemberGroup, MemberGroupRepository>();
builder.Services.AddScoped<ICommonHeaderRepository, CommonHeaderRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ? CORRECT middleware order
// 1. CORS must be FIRST — handles OPTIONS preflight before any redirect
app.UseCors("AllowReactApp");

// 2. Skip HTTPS redirect in development — causes null origin
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 3. Routing and auth after CORS
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
