using jsreport.AspNetCore;
using jsreport.Binary;
using jsreport.Local;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NexgenCosysAPI.Repository.MemberAccount;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using NexgenCosysReport.Repository.Account;
using NexgenCosysReport.Repository.AccountOperation;
using NexgenCosysReport.Repository.Common;
using NexgenCosysReport.Repository.Member;
using NexgenCosysReport.Repository.MemberAccount;
using NexgenCosysReport.Services.CommonService;
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
builder.Services.AddScoped<IMemberAllDetails, MemberAllDetailsRepository>();
builder.Services.AddScoped<IMemberDetailsSummary, MemberDetailsSummaryRepository>();
builder.Services.AddScoped<IMemberBloodGroup, MemberBloodGroupReportRepository>();
builder.Services.AddScoped<ISoleMemberGroup, SoleMemberGroupRepository>();
builder.Services.AddScoped<IMemberBasicDetail, MemberBasicDetailsRepository>();
builder.Services.AddScoped<IBalanceSheet, BalanceSheetRepository>();
builder.Services.AddScoped<IPLAccount, PLAccountRepository>();
builder.Services.AddScoped<ISummaryTrailBalance, SummaryTrialBalanceRepository>();
builder.Services.AddScoped<ICashFlowDetail, CashFlowDetailsRepository>();
builder.Services.AddScoped<ICostofFund, CostOfFundRepository>();
builder.Services.AddScoped<ICashFlow, CashFlowRepository>();
builder.Services.AddScoped<IDetailTrailBalance, DetailTrialBalanceRepository>();
builder.Services.AddScoped<IMonthlyReport, MonthlyReportRepository>();
builder.Services.AddScoped<IRatioAnalysis, RatioAnalysisRepository>();
builder.Services.AddScoped<IOfficeProgress, OfficeProgressRepository>();
builder.Services.AddScoped<IThresholdTransaction, ThresholdTransactionRepository>();
//builder.Services.AddScoped<IThresholdTransactionDetail, ThresholdTransactionDetailRepository>();
builder.Services.AddScoped<ISavingTypeWiseBalance, SavingTypeWiseBalanceRepository>();
builder.Services.AddScoped<ISavingTypeWiseIndividualBalance, SavingTypeWiseIndividualBalanceRepository>();
builder.Services.AddScoped<ISMSCategory, SMSCategoryRepository>();
builder.Services.AddScoped<IDepositUnverified, DepositUnverifiedRepository>();
builder.Services.AddScoped<IMemberAccDeactive, MemberAccountDeactiveRepository>();
builder.Services.AddScoped<IMemberAccDetailList, MemberAccountDetailNoRepository>();
builder.Services.AddScoped<IDepositWithdrawMaxAmountRange, DepositWithdrawMaxAmountRangeRepository>();
builder.Services.AddScoped<IMemberSummary, MemberSummaryRepository>();
builder.Services.AddScoped<IMemberPenaltyDepositWithdraw, MemberPenaltyDepositWithdrawRepository>();
builder.Services.AddScoped<IMemberAccountDetail, MemberAccountDetailRepository>();
builder.Services.AddScoped<ILmtLoanMasterList, LmtLoanMasterListRepository>();
builder.Services.AddScoped<IShareType, ShareTypeRepository>();
builder.Services.AddScoped<IAccountLookUp, AccountLookUpRepository>();
builder.Services.AddScoped<IDepositeStatement, DepositStatementRepository>();
builder.Services.AddScoped<IDepositStatementVerification, DepositStatementVerifyRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuth, AuthRepository>();










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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();