using jsreport.AspNetCore;
using jsreport.Binary;
using jsreport.Local;
using JsSampleProject.Interface;
using JsSampleProject.ServiceHandler;
using JsSampleReport;
using JsSampleReport.Inteface;
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

// Register application services
builder.Services.AddScoped<IMemberDetail, MemberRegistrationDetailHandler>();
builder.Services.AddScoped<IJsReportService, JsReportService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ Also add this — tells the app to serve views from /Views folder
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
