# NexGen CoSys Reports API - Project Documentation

**Project Name:** NexgenCosysReport  
**Framework:** ASP.NET Core 10.0  
**Type:** REST API with Report Generation  
**Repository:** https://github.com/ArmanOfficial786/Api (Branch: VisualAccountStatement)  
**Last Updated:** June 2026

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Technology Stack](#technology-stack)
4. [Project Structure](#project-structure)
5. [Core Components](#core-components)
6. [Database Context](#database-context)
7. [Controllers](#controllers)
8. [Services & Business Logic](#services--business-logic)
9. [Data Transfer Objects (DTOs)](#data-transfer-objects-dtos)
10. [Interfaces](#interfaces)
11. [Models](#models)
12. [Views & Report Templates](#views--report-templates)
13. [Utilities & Helpers](#utilities--helpers)
14. [Configuration](#configuration)
15. [Getting Started](#getting-started)
16. [API Endpoints](#api-endpoints)
17. [Features](#features)
18. [Development Guidelines](#development-guidelines)

---

## Project Overview

**NexgenCosysReport** is a comprehensive REST API designed to generate various financial and membership reports for the NexGen Cooperative System (CoSys). The application specializes in:

- **Report Generation:** Creating PDF, Excel, and HTML reports from Razor templates
- **Member Management:** Handling member registration, ID cards, and account statements
- **Account Reports:** Generating savings account-wise balance reports
- **Data Aggregation:** Collecting and formatting data from multiple database tables
- **Progressive PDF Streaming:** Supporting large PDF generation with chunked delivery
- **Multi-format Output:** Supporting PDF, Excel, PNG, and HTML exports

The system integrates with **jsreport** for advanced template rendering and **Entity Framework Core** for database access.

---

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Client (React)                        │
│                   (localhost:3000)                       │
└──────────────────────────────┬──────────────────────────┘
							   │
					┌──────────▼─────────┐
					│  CORS Policy       │
					│ (AllowReactApp)    │
					└──────────┬─────────┘
							   │
┌──────────────────────────────▼──────────────────────────┐
│           ASP.NET Core 10 API (localhost:5106)          │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │         Controllers (API Endpoints)             │    │
│  │  • Account Reports                             │    │
│  │  • Member Management                           │    │
│  │  • Common Data (Branch, Collectors, etc.)     │    │
│  │  • Preview/Home                                │    │
│  └────────────────────────────────────────────────┘    │
│                       │                                  │
│  ┌────────────────────▼────────────────────────────┐   │
│  │      Report Services Layer                      │   │
│  │  • JsReportService (Template Rendering)        │   │
│  │  • RazorRenderService (Razor to HTML)          │   │
│  │  • PdfChunkService (PDF Segmentation)          │   │
│  │  • ProgressivePdfService (Streaming PDFs)      │   │
│  │  • ProgressiveTempCleanupService               │   │
│  └────────────────────────────────────────────────┘    │
│                       │                                  │
│  ┌────────────────────▼────────────────────────────┐   │
│  │    Repository/Data Access Layer                │   │
│  │  • Account Repositories                        │   │
│  │  • Member Repositories                         │   │
│  │  • Common Data Repositories                    │   │
│  │  • Database Context (EF Core)                  │   │
│  └────────────────────────────────────────────────┘    │
│                       │                                  │
└───────────────────────┼──────────────────────────────────┘
						│
		┌───────────────┼────────────────┐
		│               │                │
   ┌────▼──────┐  ┌────▼──────┐  ┌────▼──────────┐
   │   jsreport│  │   SQL     │  │  Temp Storage │
   │  Service  │  │  Server   │  │  (PDF Files)  │
   │ (Template)│  │           │  │               │
   └───────────┘  └───────────┘  └───────────────┘
```

### Design Pattern: Clean Architecture with Repository Pattern

- **Controllers:** Handle HTTP requests/responses
- **Services:** Implement business logic and orchestration
- **Repositories:** Manage data access and database queries
- **Interfaces:** Define contracts for dependency injection
- **DTOs:** Transfer data between layers

---

## Technology Stack

### Core Framework
- **ASP.NET Core 10.0** - Web framework
- **.NET 10.0** - Runtime

### Database & ORM
- **Entity Framework Core 10.0.2** - Object-Relational Mapping
- **SQL Server** - Database engine (NexGenCoSysDBDev)
- **Dapper 2.1.66** - Lightweight ORM for custom queries

### Report & Document Generation
- **jsreport 3.8.x** - Template-based report engine
- **jsreport.AspNetCore 3.8.1** - ASP.NET Core integration
- **jsreport.Local 3.8.2** - Local server mode
- **jsreport.Binary 4.4.0** - Embedded binary
- **itext 9.6.0** - PDF generation and manipulation
- **itext7.bouncy-castle-adapter 9.6.0** - Cryptography support
- **PdfSharpCore 1.3.67** - PDF processing
- **UglyToad.PdfPig 1.7.0** - PDF reading/manipulation
- **DocumentFormat.OpenXml 3.5.1** - Office document generation
- **HtmlToOpenXml.dll 3.3.1** - HTML to Word/Excel conversion

### Image & Media Processing
- **SixLabors.ImageSharp 3.1.12** - Image processing

### API & Documentation
- **Swagger/Swashbuckle 10.1.1** - OpenAPI documentation and UI
- **Microsoft.AspNetCore.OpenApi 10.0.1** - OpenAPI support

### Utilities
- **DateConverter.Core 2.1.0** - Date/calendar conversion (Gregorian ↔ Bikram Sambat)
- **Microsoft.VisualStudio.Web.CodeGeneration.Design 10.0.2** - Scaffolding
- **Microsoft.AspNetCore.Http.Extensions** - HTTP utilities
- **IHostApplicationLifetime** - Application lifecycle management

### Development Tools
- **Visual Studio Community 2026 (18.6.2)**
- **PowerShell** - Terminal
- **Git** - Version control

---

## Project Structure

```
D:\Projects\NexgenCosysReports\Api\
│
├── Controllers/                          # API Endpoints (89 C# files total)
│   ├── Account/
│   │   └── SavingACWiseBalanceReportController.cs
│   ├── Common/
│   │   ├── BranchController.cs
│   │   ├── CollectionCenterController.cs
│   │   ├── CollectorController.cs
│   │   ├── ComCalendarController.cs
│   │   ├── DepositeTypeController.cs
│   │   ├── MemberGroupController.cs
│   │   ├── MemberLookUpController.cs
│   │   └── OrderByController.cs
│   ├── Member/
│   │   ├── MemberIdCardController.cs
│   │   └── MemberRegistrationController.cs
│   ├── MembeAccount/
│   │   └── AccountStatementController.cs
│   └── Preview/
│       └── HomeController.cs
│
├── Models/                              # Entity Models (8 files)
│   ├── ComCalendar.cs
│   ├── HurCollector.cs
│   ├── MemMemberRegistration.cs
│   ├── SycCollectionCenter.cs
│   ├── SycDepositType.cs
│   ├── SycMemberGroup.cs
│   ├── UsmOffice.cs
│   └── UsmRelationUserToOffice.cs
│
├── DbContext/
│   └── AppDbContext.cs                  # Entity Framework Core context
│
├── Repository/                          # Data Access Layer
│   ├── Account/
│   │   └── SavingAcWiseBalanceRepo.cs
│   ├── Common/
│   │   ├── BranchRepository.cs
│   │   ├── CollectionCenterRepository.cs
│   │   ├── CalendarRepository.cs (ComCanalenderRepository.cs)
│   │   ├── CommonHeaderRepository.cs
│   │   ├── DepositeTypeRepository.cs
│   │   ├── MemberGroupRepository.cs
│   │   └── MemberLookUpRepository.cs
│   ├── Member/
│   │   ├── MemberIdCardRepository.cs
│   │   └── MemberRegistrationDetailHandler.cs
│   └── MemberAccount/
│       └── AccountStatementRepository.cs
│
├── Services/                            # Business Logic Layer
│   ├── ReportService/
│   │   ├── JsReportService.cs           # Main report generation
│   │   ├── JsReportTemplateHelper.cs    # Template utilities
│   │   ├── RazorRenderService.cs        # Razor template rendering
│   │   ├── PdfChunkService.cs           # PDF segmentation
│   │   ├── ProgressivepsfService.cs     # Progressive PDF delivery
│   │   ├── ProgressiveTempCleanupService.cs # Cleanup service
│   │   └── ReportFileResponse.cs        # File response handling
│   └── CommonService/
│       ├── DateConverterService.cs      # Date conversion
│       └── OrderByService.cs            # Sorting/filtering logic
│
├── Inteface/                            # Service Interfaces (Note: typo in folder name)
│   ├── ReportInterface/
│   │   ├── IJsReportService.cs
│   │   ├── IRazorRenderService.cs
│   │   ├── IPdfChunkService.cs
│   │   ├── IProgressivePdfService.cs
│   │   └── IReportFileResponse.cs
│   └── ServiceInterface/
│       ├── Account/
│       │   └── ISavingAcWiseBalance.cs
│       ├── Common/
│       │   ├── IBranch.cs
│       │   ├── ICollectionCenter.cs
│       │   ├── IComCalendar.cs
│       │   ├── ICommonHeaderRepository.cs
│       │   ├── IDateConverterService.cs
│       │   ├── IDepositeType.cs
│       │   ├── IMemberGroup.cs
│       │   ├── IMemberLookUp.cs
│       │   └── IOrderBy.cs
│       ├── Member/
│       │   ├── IMemberDetail.cs
│       │   └── IMemberIdCard.cs
│       └── MemberAccount/
│           └── IAccountStatement.cs
│
├── Dtos/                                # Data Transfer Objects
│   ├── ReportDtos/
│   │   ├── ReportSettings.cs            # Report configuration
│   │   ├── ProgressivePdfJob.cs         # PDF job info
│   │   └── PageFormat.cs                # Page size settings
│   └── RequestDtos/
│       ├── Account/
│       │   └── SavingAcWiseBalanceReqResponse.cs
│       ├── Common/
│       │   ├── BranchResponse.cs
│       │   ├── CollectionCenterReqResponseDtos.cs
│       │   ├── CollectorReqResponse.cs
│       │   ├── ComCalenderReqResponse.cs
│       │   ├── DepositTypeResponse.cs
│       │   ├── GeneralResponse.cs
│       │   ├── MemberGroupReqResponse.cs
│       │   ├── MemberLookUpDtos.cs
│       │   ├── MemberLookUpRequest.cs
│       │   └── OrderByResponse.cs
│       ├── Member/
│       │   ├── MemberDetailRequest.cs
│       │   └── MemberIdCardRequest.cs
│       └── MemberAccount/
│           └── AccountStatementRequest.cs
│
├── Views/                               # Razor Templates (7 files)
│   ├── Report/
│   │   ├── SavingAcWiseBalance.cshtml
│   │   ├── AccountStatementReport.cshtml
│   │   ├── MemberIdCard.cshtml
│   │   └── MemberRegistrationReport.cshtml
│   ├── VisualReport/
│   │   ├── VAccountStatementReport.cshtml
│   │   └── VMemberRegistrationReport.cshtml
│   └── Shared/
│       └── _CommonHeader.cshtml         # Shared header template
│
├── Utils/                               # Utility Classes
│   ├── Enum/
│   │   ├── Enums.cs                    # Business enumerations
│   │   ├── EnumExtensions.cs           # Enum helper methods
│   │   └── PageFormat.cs               # PDF page formats
│   └── Report/
│       ├── CustomHeaderReponse.cs      # Header utilities
│       ├── ImageUtils.cs               # Image processing
│       ├── ReportExportHelper.cs       # Export formatting
│       └── ReportUtils.cs              # General report utilities
│
├── Properties/
│   └── launchSettings.json              # Debug/launch configuration
│
├── jsreport/                            # jsreport server & templates
│   └── node_modules/                    # npm dependencies
│
├── bin/ & obj/                          # Build output (excluded from docs)
│
├── Program.cs                           # Application startup & configuration
├── NexgenCosysReport.csproj             # Project file
├── appsettings.json                     # Application configuration
├── appsettings.Development.json         # Development-specific config
└── package.json                         # npm dependencies for jsreport
```

---

## Core Components

### 1. Program.cs - Application Startup

The `Program.cs` file is the entry point and configures:

#### jsreport Server Setup
- Initializes local jsreport server with binary execution
- Configures jsreport with `DoTrustUserCode()` for template safety
- Manages jsreport process lifecycle (start on app startup, kill on shutdown)

#### Dependency Injection Container
```csharp
// Report Services
builder.Services.AddSingleton<IJsReportService, JsReportService>();
builder.Services.AddSingleton<IRazorRenderService, RazorRenderService>();
builder.Services.AddScoped<IPdfChunkService, PdfChunkService>();
builder.Services.AddSingleton<IProgressivePdfService, ProgressivePdfService>();
builder.Services.AddHostedService<ProgressiveTempCleanupService>();

// Data Access
builder.Services.AddScoped<IBranch, BranchRepository>();
builder.Services.AddScoped<IOrderBy, OrderByService>();
builder.Services.AddScoped<ISavingAcWiseBalance, SavingAcWiseBalanceRepository>();
// ... more repositories
```

#### CORS Configuration
- Allows requests from React frontend (localhost:3000, localhost:5106)
- Enables credential-based requests
- Exposes custom headers for progressive PDF delivery

#### Database Context
- Configures Entity Framework Core with SQL Server
- Connection string from `appsettings.json`

---

## Database Context

### AppDbContext

Located in `DbContext/AppDbContext.cs`

**Mapped Entities:**
- `ComCalendar` - Calendar/holiday management
- `HurCollector` - Collector information
- `MemMemberRegistration` - Member details
- `SycCollectionCenter` - Collection center data
- `SycDepositType` - Deposit type definitions
- `SycMemberGroup` - Member grouping
- `UsmOffice` - Office/branch information
- `UsmRelationUserToOffice` - User-to-office mapping

**Key Features:**
- Nullable reference type support (`Nullable: enable`)
- Table naming conventions matching source database
- Relationship configurations in `OnModelCreating`
- Supports .NET 10 EF Core features

---

## Controllers

### Account Reports

#### SavingACWiseBalanceReportController
**Route:** `/api/SavingACWiseBalanceReport`

- Generates savings account-wise balance reports
- Supports PDF, Excel, and HTML output formats
- Features progressive PDF streaming for large reports
- Integrates custom headers with report data
- Status: Partially commented out (under development)

**Key Methods:**
- Report generation with filtering and sorting
- Batch export capabilities
- Progressive PDF delivery with chunk tracking

### Member Management

#### MemberRegistrationController
**Route:** `/api/MemberRegistration`

- Retrieves member registration details
- Returns member information with account links
- Supports filtering and pagination

#### MemberIdCardController
**Route:** `/api/MemberIdCard`

- Generates member ID card reports
- Supports PDF export
- Includes member photo/image handling

### Account & Statements

#### AccountStatementController
**Route:** `/api/AccountStatement`

- Generates member account statements
- Supports date range filtering
- Returns transaction history and balances
- Supports both PDF and visual HTML reports

### Common Data Endpoints

#### BranchController
**Route:** `/api/Branch`
- Returns all branches/offices
- Branch filtering and lookup

#### CollectionCenterController
**Route:** `/api/CollectionCenter`
- Collection center data
- Location and manager information

#### CollectorController
**Route:** `/api/Collector`
- Collector/officer information
- Collector assignments and details

#### MemberLookUpController
**Route:** `/api/MemberLookUp`
- Member search/autocomplete
- Member ID validation
- Quick member lookup

#### ComCalendarController
**Route:** `/api/ComCalendar`
- Calendar/holiday definitions
- Date range queries

#### DepositeTypeController
**Route:** `/api/DepositeType`
- Deposit type definitions
- Interest rate information

#### MemberGroupController
**Route:** `/api/MemberGroup`
- Member group classifications
- Group hierarchy

#### OrderByController
**Route:** `/api/OrderBy`
- Sorting/filtering options
- Standard field ordering

### Preview/Home

#### HomeController
- Health check endpoint
- API information endpoint
- Environment verification

---

## Services & Business Logic

### Report Services

#### JsReportService
**File:** `Services/ReportService/JsReportService.cs`

Primary service for report generation using jsreport templates.

**Key Features:**
- Template-based report rendering
- Multiple output formats (PDF, Excel, PNG, HTML)
- HTML caching for performance
- Memory cache support
- Timeout handling (600 seconds default)

**Methods:**
```csharp
Task<T> GenerateReportAsync<T>(string reportPath, object data, OutputFormat format);
Task<byte[]> GenerateFromHtmlAsync(string html, string reportName, string format);
bool TryGetCachedHtml(string reportKey);
string? GetCachedHtml(string reportKey);
```

#### RazorRenderService
**File:** `Services/ReportService/RazorRenderService.cs`

Renders Razor templates to HTML strings.

**Purpose:** Convert .cshtml templates to HTML before PDF generation

#### PdfChunkService
**File:** `Services/ReportService/PdfChunkService.cs`

Segments large PDFs into chunks for progressive delivery.

**Use Case:** Large reports that exceed memory/response size limits

**Process:**
1. Split PDF into configurable chunk size
2. Return chunks sequentially
3. Track completion progress

#### ProgressivePdfService
**File:** `Services/ReportService/ProgressivepsfService.cs`

Manages progressive/streaming PDF delivery to clients.

**Features:**
- Tracks PDF generation job status
- Returns completion percentage
- Memory-efficient chunking
- Temporary file management

**Headers Provided:**
- `X-Pages-Ready` - Number of ready pages
- `X-Is-Complete` - Generation completion status
- `X-Total-Chunks` - Total PDF chunks
- `X-Completed-Chunks` - Completed chunks
- `X-Size-Bytes` - Total file size

#### ProgressiveTempCleanupService
**File:** `Services/ReportService/ProgressiveTempCleanupService.cs`

Hosted background service that periodically cleans temporary PDF files.

### Common Services

#### DateConverterService
**File:** `Services/CommonService/DateConverterService.cs`

Handles date conversion between Gregorian and Bikram Sambat calendars.

**Integration:** Uses `DateConverter.Core` library
**Use Case:** Reports for Nepali organizations/users

#### OrderByService
**File:** `Services/CommonService/OrderByService.cs`

Provides standard field sorting and filtering options.

---

## Data Transfer Objects (DTOs)

### ReportDtos

#### ReportSettings
Configuration for report generation:
```csharp
public class ReportSettings
{
	public string WebRootPath { get; set; }  // Path to report assets
}
```

#### PageSizeSetting
Page configuration for PDF generation:
```csharp
public class PageSizeSetting
{
	public PageFormat Format { get; set; }      // A4, Letter, Custom, etc.
	public bool Landscape { get; set; }
	public double? CustomWidth { get; set; }
	public double? CustomHeight { get; set; }
	public PageUnit Unit { get; set; }          // mm, inch, cm, pt
	public string MarginTop { get; set; }
	public string MarginBottom { get; set; }
	public string MarginLeft { get; set; }
	public string MarginRight { get; set; }
}
```

#### ProgressivePdfJob
Tracks progressive PDF generation:
```csharp
public class ProgressivePdfJob
{
	public string JobId { get; set; }
	public int TotalChunks { get; set; }
	public int CompletedChunks { get; set; }
	public int PagesReady { get; set; }
	public bool IsComplete { get; set; }
	public long SizeBytes { get; set; }
}
```

### Request/Response DTOs

#### SavingAcWiseBalanceReqResponse
Account balance report request/response

#### AccountStatementRequest
Member account statement query parameters

#### MemberIdCardRequest
Member ID card report request

#### MemberDetailRequest
Member information request

**Common Fields:**
- Filtering parameters
- Date ranges
- Pagination info
- Sorting preferences

### Common DTOs

#### GeneralResponse
Generic API response wrapper:
```csharp
public class GeneralResponse
{
	public bool IsValid { get; set; }
	public string? Message { get; set; }
	public object? Data { get; set; }
	public int? StatusCode { get; set; }
}
```

---

## Interfaces

### Report Interfaces

#### IJsReportService
Main report generation contract

#### IRazorRenderService
Razor template rendering contract

#### IPdfChunkService
PDF segmentation contract

#### IProgressivePdfService
Progressive PDF delivery contract

#### IReportFileResponse
File response handling contract

### Service Interfaces

#### Account Services
- `ISavingAcWiseBalance` - Savings account balance reports

#### Common Services
- `IBranch` - Branch data access
- `ICollectionCenter` - Collection center data
- `IComCalendar` - Calendar/holiday management
- `ICollector` - Collector information (implied)
- `ICommonHeaderRepository` - Shared header data
- `IDateConverterService` - Date conversion
- `IDepositeType` - Deposit type definitions
- `IMemberGroup` - Member group data
- `IMemberLookUp` - Member search/lookup
- `IOrderBy` - Sorting/filtering

#### Member Services
- `IMemberDetail` - Member registration details
- `IMemberIdCard` - Member ID card data

#### Account Services
- `IAccountStatement` - Account statement generation

---

## Models

Entity models mapped from database tables:

### MemMemberRegistration
Member registration details:
```csharp
- MemberId (string, unique)
- FirstName, MiddleName, LastName
- BirthDate (Date + Nepali Date string)
- Address (Permanent + Temporary)
- Phone, Email
- Member Type, Salutation
- Office assignment
- Account status
```

### UsmOffice
Office/branch information:
```csharp
- OfficeId (long)
- OfficeName, Code
- Location details
- Contact information
- Office type
```

### SycCollectionCenter
Collection center data:
```csharp
- CenterId
- CenterName, Code
- Location, Officer assignment
- Status information
```

### SycMemberGroup
Member grouping:
```csharp
- GroupId
- GroupName, Description
- Category
```

### HurCollector
Collector/officer information:
```csharp
- CollectorId
- Name, Code
- Office assignment
- Contact details
```

### ComCalendar
Calendar and holiday definitions:
```csharp
- CalendarId
- DateRange (English + Nepali)
- Holiday/Event name
- Type classification
```

### SycDepositType
Deposit type definitions:
```csharp
- DepositTypeId
- TypeName, Code
- Interest rate
- Term details
```

### UsmRelationUserToOffice
User-to-office mapping:
```csharp
- Relationship tracking
- Multi-office support
- Permissions mapping
```

---

## Views & Report Templates

Razor templates located in `Views/Report` and `Views/VisualReport`:

### Report Templates

#### SavingAcWiseBalance.cshtml
Savings account-wise balance report template

#### AccountStatementReport.cshtml
Member account statement template
- Transaction history
- Balance summary
- Date range display
- Pagination support

#### MemberIdCard.cshtml
Member ID card template
- Member photo
- Personal details
- ID card formatting
- QR code (optional)

#### MemberRegistrationReport.cshtml
Member registration report
- Complete member information
- Registration date
- Office/center assignment
- Member status

### Visual Report Templates

#### VAccountStatementReport.cshtml
HTML-based account statement (visual variant)
- Dashboard-style layout
- Interactive elements
- Chart support

#### VMemberRegistrationReport.cshtml
Visual member registration report

### Shared Components

#### _CommonHeader.cshtml
Reusable header component for all reports
- Company/organization logo
- Date/time information
- Report title
- User/preparator info
- Print styling

---

## Utilities & Helpers

### Enum Utilities

#### Enums.cs
Business enumeration definitions:
```csharp
- OutputFormat (PDF, EXCEL, HTML, PNG)
- PageUnit (mm, inch, cm, pt)
- ReportType
- Member status enumerations
```

#### EnumExtensions.cs
Extension methods for enums:
```csharp
ToDisplayName()
ToValue()
FromValue()
```

#### PageFormat.cs
PDF page format enumeration:
```csharp
- A4, A3, Letter
- Legal, Tabloid
- Custom dimensions
```

### Report Utilities

#### CustomHeaderReponse.cs
Custom header value extraction and formatting

#### ImageUtils.cs
Image processing and embedding:
- Image resizing
- Format conversion
- Base64 encoding
- File path resolution

#### ReportExportHelper.cs
Export formatting utilities:
- Excel formatting
- CSV generation
- Data table conversion

#### ReportUtils.cs
General report utilities:
- Data formatting
- Number/currency formatting
- Date formatting (with Nepali support)
- Table pagination

---

## Configuration

### appsettings.json

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft.AspNetCore": "Warning"
	}
  },
  "ConnectionStrings": {
	"DatabaseConnectionString": "Server=DESKTOP-G41KGSS\\SQLEXPRESS;Database=NexGenCoSysDBDev;User ID=SA;Password=cosys123;TrustServerCertificate=True;Encrypt=False"
  },
  "ReportSettings": {
	"WebRootPath": "C:\\inetpub\\wwwroot\\Images"
  },
  "jsreport": {
	"url": "http://localhost:5488"
  },
  "AllowedHosts": "*"
}
```

### appsettings.Development.json

Development-specific overrides:
```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Debug"
	}
  }
}
```

### Properties/launchSettings.json

Debug launch profiles for Visual Studio

### NexgenCosysReport.csproj

Project configuration:
```xml
<TargetFramework>net10.0</TargetFramework>
<Nullable>enable</Nullable>              <!-- Nullable reference types -->
<ImplicitUsings>enable</ImplicitUsings>  <!-- Global using statements -->
<PlatformTarget>x64</PlatformTarget>     <!-- 64-bit platform -->
<ServerGarbageCollection>true</ServerGarbageCollection>
<ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
```

---

## Getting Started

### Prerequisites
- Visual Studio Community 2026 (18.6.2) or higher
- .NET 10 SDK
- SQL Server with NexGenCoSysDBDev database
- Node.js (for jsreport)

### Installation Steps

1. **Clone Repository**
   ```powershell
   git clone https://github.com/ArmanOfficial786/Api.git
   cd Api
   git checkout VisualAccountStatement
   ```

2. **Install Dependencies**
   ```powershell
   dotnet restore
   npm install  # For jsreport dependencies
   ```

3. **Configure Database**
   - Update connection string in `appsettings.json`
   - Ensure SQL Server is running
   - Database should already exist with required tables

4. **Configure Report Settings**
   - Update `WebRootPath` in `appsettings.json` if needed
   - Ensure jsreport server URL is accessible

5. **Build Solution**
   ```powershell
   dotnet build
   ```

6. **Run Application**
   ```powershell
   dotnet run
   ```
   - API will be available at `https://localhost:5106`
   - Swagger UI available at `/swagger`

### Debugging in Visual Studio

1. Open `NexgenCosysReport.sln` in Visual Studio
2. Set breakpoints as needed
3. Press `F5` to start debugging
4. API will start at configured URL (check launchSettings.json)

---

## API Endpoints

### Account Reports
- `GET /api/SavingACWiseBalanceReport/list` - List account balance reports
- `POST /api/SavingACWiseBalanceReport/generate` - Generate new report
- `GET /api/SavingACWiseBalanceReport/{jobId}/chunks` - Get PDF chunks (progressive)
- `GET /api/SavingACWiseBalanceReport/{jobId}/status` - Check generation status

### Member Management
- `GET /api/MemberRegistration/{memberId}` - Get member details
- `GET /api/MemberIdCard/{memberId}` - Generate member ID card
- `POST /api/MemberIdCard/export` - Export member ID cards

### Account Statements
- `POST /api/AccountStatement/generate` - Generate account statement
- `GET /api/AccountStatement/{memberId}` - Get statement data
- `POST /api/AccountStatement/{memberId}/export` - Export statement

### Common Data
- `GET /api/Branch` - List all branches
- `GET /api/CollectionCenter` - List collection centers
- `GET /api/Collector` - List collectors
- `GET /api/MemberLookUp/{searchText}` - Search members
- `GET /api/ComCalendar/holidays` - Get holidays
- `GET /api/DepositeType` - List deposit types
- `GET /api/MemberGroup` - List member groups

### Other
- `GET /api/Home/health` - Health check
- `GET /api/Home/info` - API information

---

## Features

### Report Generation
✅ PDF generation from Razor templates  
✅ Excel export support  
✅ HTML output for web display  
✅ PNG/image export for previews  
✅ Custom page sizing and margins  
✅ Template caching for performance  
✅ Timeout protection (600 seconds)

### Progressive PDF Delivery
✅ Large PDF streaming in chunks  
✅ Progress tracking headers  
✅ Memory-efficient processing  
✅ Automatic cleanup of temporary files  
✅ Concurrent PDF job management

### Data Access
✅ Entity Framework Core ORM  
✅ SQL Server integration  
✅ Dapper support for complex queries  
✅ Connection pooling  
✅ Query optimization

### API Features
✅ REST architecture  
✅ CORS support for React frontend  
✅ Swagger/OpenAPI documentation  
✅ Request validation  
✅ Error handling and logging  
✅ Health check endpoints

### Date Handling
✅ Gregorian date support  
✅ Bikram Sambat (Nepali) calendar conversion  
✅ Date range filtering  
✅ Holiday management

### Image Processing
✅ Image resizing  
✅ Format conversion  
✅ Base64 encoding  
✅ Photo embedding in reports

---

## Development Guidelines

### Code Organization
- **Controllers:** Handle routing and HTTP concerns only
- **Services:** Implement business logic and orchestration
- **Repositories:** Abstract data access patterns
- **Interfaces:** Define contracts for all services
- **DTOs:** Transfer data between layers
- **Models:** Entity definitions matching database

### Naming Conventions
- **Classes:** PascalCase (e.g., `BranchRepository`)
- **Methods:** PascalCase (e.g., `GetBranchAsync`)
- **Properties:** PascalCase (e.g., `BranchName`)
- **Fields:** `_camelCase` for private fields
- **Constants:** `UPPER_CASE`
- **Interfaces:** Prefix with `I` (e.g., `IBranch`)

### Async/Await Patterns
- Use `async/await` for all I/O operations
- Methods accessing data should return `Task<T>`
- Use `.ConfigureAwait(false)` in libraries
- Proper cancellation token support

### Dependency Injection
- Register services in `Program.cs`
- Use constructor injection in controllers/services
- Prefer interfaces over concrete types
- Use appropriate lifetimes: `Scoped`, `Singleton`, `Transient`

### Error Handling
- Use try-catch for specific exceptions
- Log errors with context
- Return meaningful error messages to clients
- Use custom exception types where appropriate

### Database Access
- Use Entity Framework Core for standard queries
- Use Dapper for complex/performance-critical queries
- Always use parameterized queries
- Implement proper transaction handling
- Use lazy loading or explicit includes for relationships

### Report Development
- Store templates in `Views/Report` or `Views/VisualReport`
- Use `.cshtml` format for Razor templates
- Use `_CommonHeader.cshtml` for consistent formatting
- Test templates with sample data before deployment
- Consider mobile/print CSS

### Testing
- Unit tests for business logic
- Integration tests for repository methods
- Mock external dependencies (jsreport, database)
- Test error scenarios
- Test date conversions for both calendars

### Performance Considerations
- Cache frequently accessed data
- Use pagination for large result sets
- Optimize PDF generation for memory usage
- Consider async operations for long-running tasks
- Monitor jsreport server performance

### Security
- Validate all input parameters
- Use parameterized queries for SQL
- Implement proper authentication (if required)
- Sanitize data before rendering in templates
- Protect sensitive configuration in environment variables

### Git Workflow
- Current branch: `VisualAccountStatement`
- Create feature branches from main branch
- Use descriptive commit messages
- Keep commits atomic and logical
- Submit pull requests for review

---

## Project Statistics

- **Total C# Source Files:** 89
- **Controllers:** 11 (covering 6 domains)
- **Services:** 7 major services
- **Repositories:** 10 data access implementations
- **Interfaces:** 18+ service contracts
- **Models:** 8 database entities
- **DTOs:** 20+ data transfer objects
- **Razor Templates:** 7 report templates
- **Target Framework:** .NET 10.0
- **NuGet Packages:** 25+ dependencies

---

## Additional Resources

- **jsreport Documentation:** https://jsreport.net/
- **Entity Framework Core:** https://docs.microsoft.com/en-us/ef/core/
- **ASP.NET Core:** https://docs.microsoft.com/en-us/aspnet/core/
- **Swagger/OpenAPI:** https://swagger.io/
- **SQL Server:** https://www.microsoft.com/en-us/sql-server/

---

## Notes

### Known Issues/TODOs
1. `SavingACWiseBalanceReportController` is partially commented out (under development)
2. Folder naming: "Inteface" should be "Interface" (typo in codebase)
3. Test coverage needed for report generation
4. Performance optimization for large PDF exports

### Future Enhancements
- Add authentication/authorization layer
- Implement report caching strategy
- Add email delivery for reports
- Multi-language support
- Advanced filtering and search
- Dashboard analytics
- Real-time notifications

---

## Last Updated
**June 10, 2026**

For questions or contributions, refer to the GitHub repository or contact the development team.
