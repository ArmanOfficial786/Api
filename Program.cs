using jsreport.AspNetCore;
using jsreport.Binary;
using jsreport.Local;
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using NexgenCosysReport.Repository.Account;
using NexgenCosysReport.Repository.Common;
using NexgenCosysReport.Repository.Member;
using NexgenCosysReport.Repository.MemberAccount;
using NexgenCosysReport.Services.CommonService;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;

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
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();


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
builder.Services.AddScoped<IReportFileResponse, ReportFileResponse>();
builder.Services.AddSingleton<IRazorRenderService, RazorRenderService>();
builder.Services.AddScoped<IPdfChunkService, PdfChunkService>();
builder.Services.AddSingleton<IJsReportService, JsReportService>();
builder.Services.AddSingleton<IProgressivePdfService, ProgressivePdfService>();
builder.Services.AddHostedService<ProgressiveTempCleanupService>();
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
builder.Services.AddScoped<ISavingAcWiseBalance, SavingAcWiseBalanceRepository>();
builder.Services.AddScoped<IComCalendar, CalendarRepository>();
builder.Services.AddScoped<IDepositeType, DepositeTypeRepository>();

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
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();