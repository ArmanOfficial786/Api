//using jsreport.AspNetCore;
//using jsreport.Binary;
//using jsreport.Local;
//using JsSampleProject.ServiceHandler;
//using JsSampleReport;
//using JsSampleReport.Dtos.ReportDtos;
//using JsSampleReport.Inteface.ReportInterface;
//using JsSampleReport.Inteface.ServiceInterface;
//using JsSampleReport.Repository;
//using JsSampleReport.Services.ReportService;
//using Microsoft.EntityFrameworkCore;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
////builder.Services.AddControllers();
//builder.Services.AddControllersWithViews();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//// Configure jsreport with Local Reporting
//builder.Services.AddJsReport(new LocalReporting()
//    .UseBinary(JsReportBinary.GetBinary())
//    .KillRunningJsReportProcesses()
//    .AsUtility()
//    .Create());


////// Production configuration
////builder.Services.AddJsReport(new LocalReporting()
////    .UseBinary(JsReportBinary.GetBinary())
////    .Configure(cfg => cfg.HttpPort = 5489)  // ✅ Use different port
////    .AsUtility()
////    .Create());

//// Add CORS
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowReactApp", policy =>
//    {
//        policy.WithOrigins("http://localhost:3000", "https://localhost:7212")
//              .AllowAnyHeader()
//              .AllowAnyMethod()
//              .AllowCredentials();
//    });
//});

//// Configure Database
//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DatabaseConnectionString")));

//// ✅ Register ReportSettings from appsettings.json
//builder.Services.Configure<ReportSettings>(
//    builder.Configuration.GetSection(ReportSettings.SectionName));

//// Register application services
//builder.Services.AddMemoryCache();
//// ✅ 2. Register JsReportService as SINGLETON (not Scoped/Transient)
////    Scoped = new instance per request = cache not shared between requests
//builder.Services.AddSingleton<IJsReportService, JsReportService>();
//// ✅ 3. Your other services
//builder.Services.AddScoped<IMemberDetail, MemberRegistrationDetailHandler>();
//builder.Services.AddScoped<IMemberIdCard, MemberIdCardRepository>();
//builder.Services.AddScoped<IAccountStatement, AccountStatementRepository>();



//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
//app.UseCors("AllowReactApp");
//// ✅ Also add this — tells the app to serve views from /Views folder
////app.UseStaticFiles();
//app.UseHttpsRedirection();
//app.UseRouting();
//app.UseAuthorization();

//// ✅ To view Reports in browser, we need to map controller routes that can return views, not just API endpoints. So we add this:
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");
//app.MapControllers();

//app.Run();









using jsreport.AspNetCore;
using jsreport.Binary;
using jsreport.Local;
using JsSampleProject.ServiceHandler;
using JsSampleReport;
using JsSampleReport.Dtos.ReportDtos;
using JsSampleReport.Inteface.ReportInterface;
using JsSampleReport.Inteface.ServiceInterface;
using JsSampleReport.Repository;
using JsSampleReport.Services.CommonService;
using JsSampleReport.Services.ReportService;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// jsreport
builder.Services.AddJsReport(new LocalReporting()
    .UseBinary(JsReportBinary.GetBinary())
    .KillRunningJsReportProcesses()
    .AsUtility()
    .Create());

// ✅ CORS — handles null origin + all localhost ports
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ CORRECT middleware order
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
