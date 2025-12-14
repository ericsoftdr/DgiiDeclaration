# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DgiiDeclaration is a full-stack automation system for Dominican tax declarations (DGII - Dirección General de Impuestos Internos). It uses browser automation via PuppeteerSharp to log into the DGII portal and automatically submit tax declarations (Forms 606, 607, IT-1, IT-1A, IR-3) with zero values for multiple companies.

## Architecture

### Backend (.NET 8)
- **Technology**: ASP.NET Core 8.0 Web API
- **Database**: SQL Server via Entity Framework Core
- **Browser Automation**: PuppeteerSharp with headless Chrome
- **Logging**: Serilog with file and console sinks
- **API Documentation**: Swagger/OpenAPI

### Frontend (Angular 17)
- **Technology**: Angular 17 with PrimeNG UI components
- **Template Base**: Sakai-ng template
- **State Management**: RxJS observables

### Key Design Patterns

**Browser Automation Workflow** (`DgiiService.cs`):
1. Initializes headless browser with custom Chrome executable path
2. Handles DGII login with optional two-factor authentication (token cards)
3. Navigates through tax declaration forms
4. Saves PDF evidence of declarations organized by accounting manager and company
5. Sends email notifications with results

**Token Management**: Companies can require token-based two-factor authentication. Token values are stored per company and validated during the process. The system tracks which tokens have been validated to avoid reuse.

**Declaration Processing**: The system determines which periods need declaration by:
- Fetching the last declared period from DGII portal
- Calculating next period based on company start date
- Declaring all missing periods up to current month
- Handling both monthly (606, 607, IT-1) and different forms

**Database Structure**:
- `AccountingManager`: Groups companies by accounting manager
- `CompanyCredential`: Stores company RNC, password, and DGII credentials
- `CompanyCredentialToken`: Stores two-factor authentication tokens
- Relationships: AccountingManager → CompanyCredentials (one-to-many)

## Common Development Commands

### Backend

**Run the API**:
```bash
cd backend
dotnet run
```

**Build**:
```bash
cd backend
dotnet build
```

**Build for Release**:
```bash
cd backend
dotnet build -c Release
```

**Publish**:
```bash
cd backend
dotnet publish -c Release
```

**Entity Framework Migrations**:
```bash
cd backend
# Create new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Rollback to specific migration
dotnet ef database update MigrationName

# Remove last migration
dotnet ef migrations remove
```

**Run tests** (if test project exists):
```bash
cd backend
dotnet test
```

### Frontend

**Install dependencies**:
```bash
cd frontend
npm install
```

**Run dev server** (http://localhost:4200):
```bash
cd frontend
ng serve
# or
npm start
```

**Build**:
```bash
cd frontend
ng build
```

**Build for production**:
```bash
cd frontend
ng build --configuration production
```

**Run tests**:
```bash
cd frontend
ng test
```

**Run linter**:
```bash
cd frontend
ng lint
```

**Generate component/service**:
```bash
cd frontend
ng generate component component-name
ng generate service service-name
```

## Configuration

### Backend Configuration (`appsettings.json`)

**Connection String**: Update `ConnectionStrings:AccountingDb` with SQL Server connection details.

**Critical Settings** (`Config` section):
- `DgiiUrlLogin`: DGII portal login URL (should not change)
- `ChromePath`: Path to Chrome executable for Puppeteer (must be installed separately)
- `PathPdfOfEvidence`: Base directory for saving PDF evidence files
- `SmtpServer`, `SmtpPort`, `SmtpUser`, `SmtpPass`: Email configuration for notifications
- `ToEmail`: Recipient email for automated notifications

**File Organization**: PDFs are saved as:
```
{PathPdfOfEvidence}\{AccountingManagerName}\{RNC-CompanyName}\{YYYY-MM-FormType - CompanyName}.pdf
```

### Frontend Configuration

API endpoint is configured in `src/environments/environment.ts` (not committed to repo).

## Important Implementation Notes

### PuppeteerSharp Automation

**Custom Extension**: `PuppeteerExtensions.cs` provides `WaitForSelectorWithTimeoutAsync` with a 5-minute default timeout for slow-loading DGII pages.

**Login Flow**:
1. Waits for username/password fields
2. Submits credentials
3. Handles three outcomes via `Task.WhenAny`:
   - Error message appears
   - Token/2FA required
   - Successful navigation to home

**Token Handling**: When 2FA is required, the system:
- Extracts the token ID from the `data-original-title` attribute
- Looks up the token value from `CompanyCredentialTokens`
- Submits and validates the token
- Marks the token as validated to prevent reuse

**Frame/IFrame Navigation**: DGII uses iframes for messages and confirmation dialogs. The code waits for specific frames (e.g., `/OFV/AvisoMensajes.aspx`, `/OFV/plantilla/Mensaje`) before interacting.

**New Window Handling**: Interactive declarations open in new browser windows. The code uses `Browser.TargetCreated` event to detect and interact with popup windows.

### Declaration Types

- **606/607**: Zero declaration forms (submitted via simple form)
- **IT-1**: Monthly ITBIS declaration (interactive form with token)
- **IT-1A**: IT-1 Annex (required for periods >= 201801)
- **IR-3**: Income tax declaration (conditional based on company obligations)

### Error Handling

- Companies with unread messages in DGII portal cannot be processed
- Wrong credentials or tokens throw exceptions with detailed messages
- Process sends email summary even on partial failures
- Each company processing is isolated; one failure doesn't stop others

### Frontend Architecture

**Feature Modules**:
- `features/dgii`: Company credential management (list, create, edit)
- `features/managers`: Accounting manager management
- Shared services in `core/` and `shared/`

**API Integration**: `ApiService` provides centralized HTTP methods with error handling.

**Loading Indicator**: `LoadingInterceptor` shows/hides loading spinner for all HTTP requests.

## Database Connection String Format

The SQL Server connection string uses:
```
Data Source={server},{port}; Initial Catalog={database}; User Id={user};Password={password}; TrustServerCertificate=True
```

`TrustServerCertificate=True` is set to avoid SSL certificate validation issues.

## Chrome/Puppeteer Setup

PuppeteerSharp requires a Chromium/Chrome executable. The path is configured in `appsettings.json` under `Config:ChromePath`.

The application does NOT auto-download Chrome. You must:
1. Download Chrome or use an existing installation
2. Update the `ChromePath` setting to point to `chrome.exe`

The browser runs in non-headless mode (`Headless = false`) to allow visual debugging of DGII portal interactions.
