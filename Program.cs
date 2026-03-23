using jsreport.AspNetCore;
using jsreport.Binary;
using jsreport.Local;
using JsSampleProject.ServiceHandler;
using JsSampleReport;
using JsSampleReport.Dtos.ReportDtos;
using JsSampleReport.Inteface.ReportInterface;
using JsSampleReport.Inteface.ServiceInterface;
using JsSampleReport.Repository;
using JsSampleReport.Services.ReportService;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure jsreport with Local Reporting
builder.Services.AddJsReport(new LocalReporting()
    .UseBinary(JsReportBinary.GetBinary())
    .KillRunningJsReportProcesses()
    .AsUtility()
    .Create());


//// Production configuration
//builder.Services.AddJsReport(new LocalReporting()
//    .UseBinary(JsReportBinary.GetBinary())
//    .Configure(cfg => cfg.HttpPort = 5489)  // ✅ Use different port
//    .AsUtility()
//    .Create());

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DatabaseConnectionString")));

// ✅ Register ReportSettings from appsettings.json
builder.Services.Configure<ReportSettings>(
    builder.Configuration.GetSection(ReportSettings.SectionName));

// Register application services
builder.Services.AddMemoryCache();
// ✅ 2. Register JsReportService as SINGLETON (not Scoped/Transient)
//    Scoped = new instance per request = cache not shared between requests
builder.Services.AddSingleton<IJsReportService, JsReportService>();
// ✅ 3. Your other services
builder.Services.AddScoped<IMemberDetail, MemberRegistrationDetailHandler>();
builder.Services.AddScoped<IMemberIdCard, MemberIdCardRepository>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ Also add this — tells the app to serve views from /Views folder
//app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthorization();
// ✅ To view Reports in browser, we need to map controller routes that can return views, not just API endpoints. So we add this:
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Run();
