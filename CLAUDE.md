# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 🚨 REGLA CRÍTICA: NO COMMIT/PUSH SIN CONSENTIMIENTO 🚨

**POR NINGÚN MOTIVO hagas `git commit` o `git push` sin mi consentimiento explícito.**

- ❌ NUNCA ejecutar `git commit` automáticamente
- ❌ NUNCA ejecutar `git push` sin que yo lo pida
- ✅ Siempre preguntar antes de hacer commit
- ✅ Mostrar los cambios pendientes y esperar aprobación

## Project Overview

This is a **Bizuit Backend Plugin** template - a .NET 9.0 plugin system for extending Bizuit Backend Host. Plugins are dynamically loaded DLLs that provide REST API endpoints with built-in authentication, automatic transactions, and SQL injection protection via `SafeQueryBuilder`.

### Tech Stack
- **.NET 9.0** with C# (nullable enabled, implicit usings)
- **Bizuit.Backend.Core** (v1.0.1) - Core plugin framework with SafeQueryBuilder
- **SQL Server** - Database layer with parameterized queries
- **Minimal APIs** - Endpoint routing with ASP.NET Core
- **xUnit** - Unit testing framework

## Architecture

### Plugin System Architecture

```
IBackendPlugin (interface)
  └── MyPluginPlugin (entry point)
      ├── ConfigureServices() - DI registration
      ├── ConfigureEndpoints() - Route mapping
      └── OnUnloading() - Cleanup hook
```

All plugins implement `IBackendPlugin` and are loaded by the Backend Host at runtime. The plugin lifecycle:
1. **Load**: Host discovers plugin from ZIP package
2. **Configure Services**: DI registration with connection string injection
3. **Configure Endpoints**: Route registration with auth policies
4. **Runtime**: Automatic transaction management per HTTP method
5. **Unload**: Cleanup via `OnUnloading()` hook

### Feature-Based Organization

Each feature follows a consistent pattern in `src/MyPlugin/Features/`:

```
Features/
├── Items/          # Public CRUD (no auth)
├── Products/       # Protected CRUD (auth + roles)
└── AuditLogs/      # Fire-and-forget (no transaction)

Feature Structure:
FeatureName/
├── Models/FeatureName.cs        # Entity + DTOs
├── FeatureNameRepository.cs     # Data access (SafeQueryBuilder)
├── FeatureNameService.cs        # Business logic (if transactions)
└── FeatureNameEndpoints.cs      # HTTP routes (Minimal APIs)
```

**Key Pattern**: Features with automatic transactions use a Service layer. Features marked `[NoTransaction]` call repositories directly from endpoints.

### SafeQueryBuilder - Critical Security Pattern

**ALL database operations MUST use SafeQueryBuilder** - it's impossible to have SQL injection when used correctly:

```csharp
// Repository pattern inheriting SafeRepository<T>
public class ItemsRepository : SafeRepository<Item>
{
    protected override string TableName => "Items";

    // Query with filters - all values are parameterized
    public async Task<IEnumerable<Item>> SearchAsync(string? name)
    {
        var query = Query();
        if (!string.IsNullOrEmpty(name))
        {
            query.WhereLike("Name", name);  // Parameterized: WHERE Name LIKE @p0
        }
        return await ExecuteAsync(query);
    }

    // Insert with IDENTITY return
    public async Task<int> CreateAsync(CreateItemRequest request)
    {
        var insert = Insert()
            .Set("Name", request.Name)
            .Set("Price", request.Price);
        return await ExecuteWithIdentityAsync(insert);
    }
}
```

**Never** construct raw SQL strings with concatenation. Always use `Query()`, `Insert()`, `Update()`, `Delete()` builders.

### Authentication & Authorization System

**IMPORTANT: BizuitUserContext Enhancement Required**

The default `Bizuit.Backend.Core` NuGet package (v1.0.1) does **not** include the `Roles` property on `BizuitUserContext`. This project uses a **project reference** to the enhanced version from the `backend-host/src/Bizuit.Backend.Core/` directory which adds:

```csharp
// Enhanced BizuitUserContext with role support
public List<string> Roles { get; set; } = new();

public bool HasRole(string role)
public bool HasAnyRole(params string[] roles)
public bool HasAllRoles(params string[] roles)
```

**MyPlugin.csproj references the enhanced version via ProjectReference**:
```xml
<ProjectReference Include="../../../BIZUITCustomForms/custom-forms/backend-host/src/Bizuit.Backend.Core/Bizuit.Backend.Core.csproj" />
```

#### Admin-Configurable Role Authorization

**This plugin uses Backend Host's database-driven role configuration system**. Developers specify default roles in code, Backend Host auto-populates the database, and administrators can modify them without touching code.

**Developer Workflow** (what you do in code):

```csharp
// Developer specifies default roles that should be enforced
const string readAndCreateRoles = "Administrators,BizuitAdmins,Gestores";
const string modifyRoles = "Administrators,BizuitAdmins";

endpoints.MapGet("products", GetAll)
    .RequireAuthorization(readAndCreateRoles);  // Default roles specified

endpoints.MapDelete("products/{id:int}", Delete)
    .RequireAuthorization(modifyRoles);  // Different roles for sensitive operations
```

**Backend Host Workflow** (automatic when plugin is installed):

1. **Plugin Upload**: Admin uploads plugin ZIP to Backend Host
2. **Auto-Discovery**: Backend Host loads plugin and calls `ConfigureEndpoints()`
3. **Role Extraction**: System reads `.RequireAuthorization(roles)` from each endpoint
4. **Auto-Population**: **Backend Host should automatically INSERT into `BackendPluginEndpointRoles` table**
   - Uses roles specified by developer as defaults
   - Creates one row per endpoint with its configured roles
5. **Runtime Enforcement**: Backend Host checks table and enforces roles

**Admin Modification Workflow** (optional, after installation):

Administrators can modify roles via admin panel or SQL without touching plugin code:

```sql
-- Example: Add "CustomRole" to GET endpoints
UPDATE BackendPluginEndpointRoles
SET AllowedRoles = AllowedRoles + ',CustomRole',
    UpdatedAt = GETUTCDATE(),
    UpdatedBy = 'admin-user'
WHERE PluginId = @PluginId AND HttpMethod = 'GET';
```

**How It Works at Runtime**:
   - Every request → Backend Host checks `BackendPluginEndpointRoles` table
   - If admin modified roles → uses database-configured roles
   - If not modified → uses developer's default roles (auto-populated on install)
   - Changes apply **immediately without plugin reload**

**Role Auto-Population** (✅ implemented in Backend Host):

The Backend Host **automatically populates** the `BackendPluginEndpointRoles` table when a plugin is installed:

1. Plugin uploaded and loaded by Backend Host
2. `PluginManager.LoadPluginAsync()` calls `plugin.ConfigureEndpoints()`
3. Roles specified via `.RequireAuthorization(roles)` are captured
4. After `FinalizeEndpoints()`, system auto-inserts into `BackendPluginEndpointRoles`
5. **Only inserts if NOT exists** - preserves admin modifications on plugin reload

**Fallback** (manual configuration if needed):

The `database/005_ConfigureDefaultRoles.sql` script is provided as a fallback for manual configuration:

| Endpoints | Allowed Roles | Purpose |
|-----------|---------------|---------|
| GET (all read endpoints) | Administrators, BizuitAdmins, Gestores | View payment promises |
| POST /products | Administrators, BizuitAdmins, Gestores | Create new promises |
| PUT /products/{id} | Administrators, BizuitAdmins | Update promises |
| PUT /products/{id}/caida | Administrators, BizuitAdmins | Mark as fallen |
| DELETE /products/{id} | Administrators, BizuitAdmins | Delete promises |

**Modifying Roles (Admin Panel)**:

Administrators can modify roles without touching code:

```sql
-- Example: Add "CustomRole" to GET endpoints
UPDATE BackendPluginEndpointRoles
SET AllowedRoles = AllowedRoles + ',CustomRole',
    UpdatedAt = GETUTCDATE(),
    UpdatedBy = 'admin-user'
WHERE PluginId = @PluginId AND HttpMethod = 'GET';

-- Example: Remove "Gestores" from POST endpoint
UPDATE BackendPluginEndpointRoles
SET AllowedRoles = 'Administrators,BizuitAdmins',
    UpdatedAt = GETUTCDATE(),
    UpdatedBy = 'admin-user'
WHERE PluginId = @PluginId AND EndpointPath = '/products' AND HttpMethod = 'POST';
```

**Plugin Code Pattern** (simplified, no hardcoded roles):

```csharp
// Plugin handler - just business logic, no role checking
private static async Task<IResult> GetAll(ProductsService service, BizuitUserContext user)
{
    // Backend Host already validated authentication and roles
    var promesas = await service.GetAllAsync();
    return Results.Ok(promesas);
}
```

**Why This Architecture?**:
- ✅ Administrators can adjust permissions without developer involvement
- ✅ Role changes apply immediately (no plugin reload or code deployment)
- ✅ Centralized role management across all plugins
- ✅ Audit trail of role changes (UpdatedAt, UpdatedBy columns)
- ✅ Environment-specific roles (dev, staging, prod can have different configs)

### Transaction Management

**Automatic transactions** are provided by Backend Host based on HTTP method:

| Method | Transaction | Use Case |
|--------|-------------|----------|
| GET/HEAD/OPTIONS | No | Read-only operations |
| POST/PUT/PATCH/DELETE | Yes | Data mutations |

**Opt-out for special cases** using `.NoTransaction()`:

```csharp
// AuditLogs - fire-and-forget logging
endpoints.MapPost("audit-logs", Create)
    .NoTransaction();  // Must persist even if parent operation fails
```

Use `NoTransaction` for: logging/auditing, high-frequency writes, operations that shouldn't be rolled back.

## Development Commands

### Local Development (DevHost)

The DevHost provides a standalone server for testing with **mock authentication** that simulates Backend Host behavior:

```bash
# Start development server (http://localhost:5000 + Swagger UI)
npm run dev

# Hot reload during development
npm run watch
```

#### Mock Authentication System

DevHost uses mock Bearer token authentication to simulate production behavior:

**Available Mock Tokens**:
- `admin` → Roles: Administrators, BizuitAdmins (full access)
- `gestor` → Role: Gestores (read/create only for Products)
- `user` → No roles (authenticated but unauthorized)

**Testing with curl**:
```bash
# No auth - Should fail with 403
curl -X GET 'http://localhost:5000/api/products'

# Admin token - Should succeed
curl -X GET 'http://localhost:5000/api/products' \
  -H 'Authorization: Bearer admin'

# Gestor token - Can read/create, cannot delete
curl -X GET 'http://localhost:5000/api/products' \
  -H 'Authorization: Bearer gestor'

curl -X DELETE 'http://localhost:5000/api/products/1' \
  -H 'Authorization: Bearer gestor'  # 403 Forbidden

curl -X DELETE 'http://localhost:5000/api/products/1' \
  -H 'Authorization: Bearer admin'   # 200 OK or 404
```

**Swagger UI Authentication**:
1. Click "Authorize" button in Swagger UI
2. Enter token value: `admin`, `gestor`, or `user`
3. All subsequent requests will include the Bearer token

**Important Differences from Production**:
- DevHost still has **no automatic transactions** - test transaction behavior in Backend Host
- Mock tokens are simple strings, not real JWTs
- All mock users belong to "dev-tenant"
- Production uses real JWT authentication with Azure AD or other identity providers

Configure connection string in `src/DevHost/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Default": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=USER;Password=PASS;TrustServerCertificate=True"
  }
}
```

### Building and Testing

```bash
# Build entire solution (plugin + tests + devhost)
dotnet build

# Run all unit tests (xUnit)
dotnet test

# Run tests with details
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~ProductsServiceTests"
```

### Database Setup

Before running, create tables using scripts in `database/`:

```bash
# Recommended: Use consolidated script
sqlcmd -S <server> -d <database> -U <user> -P <password> \
  -i database/setup-database.sql

# Or: Run individual migration scripts in order
sqlcmd -S <server> -d <database> -U <user> -P <password> \
  -i database/001_CreateItemsTable.sql
```

All scripts are idempotent (`IF NOT EXISTS`) - safe to run multiple times.

### Database Schema Discovery

To inspect existing table structures when creating features for existing tables, use the connection string from `src/DevHost/appsettings.json`:

```bash
# Get table structure (columns, types, nullable)
sqlcmd -S <server> -d <database> -U <user> -P <password> -C \
  -Q "SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
      FROM INFORMATION_SCHEMA.COLUMNS
      WHERE TABLE_NAME = 'YourTableName'
      ORDER BY ORDINAL_POSITION" \
  -W -s "," -h -1

# Get primary key
sqlcmd -S <server> -d <database> -U <user> -P <password> -C \
  -Q "SELECT COLUMN_NAME
      FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
      WHERE TABLE_NAME = 'YourTableName' AND CONSTRAINT_NAME LIKE 'PK%'" \
  -W -h -1

# Get foreign keys
sqlcmd -S <server> -d <database> -U <user> -P <password> -C \
  -Q "SELECT
        fk.name AS FK_Name,
        OBJECT_NAME(fk.parent_object_id) AS Table_Name,
        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS Column_Name,
        OBJECT_NAME(fk.referenced_object_id) AS Referenced_Table,
        COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS Referenced_Column
      FROM sys.foreign_keys fk
      INNER JOIN sys.foreign_key_columns fc ON fk.object_id = fc.constraint_object_id
      WHERE OBJECT_NAME(fk.parent_object_id) = 'YourTableName'" \
  -W -h -1
```

**Important**: Use the `-C` flag to trust server certificate (equivalent to `TrustServerCertificate=True` in connection string).

**Example using actual connection from appsettings.json**:
```bash
sqlcmd -S <server> -d <database> -U <user> -P '<password>' -C \
  -Q "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Products'"
```

### Creating New Features

Use the interactive script generator:

```bash
npm run new-feature
```

This wizard prompts for:
1. **Feature name** (PascalCase: Products, Orders, Customers)
2. **Auth type** (public, protected, admin-only)
3. **Transaction mode** (automatic or none)

Generated files must be manually registered in `MyPlugin.cs`:

```csharp
// In ConfigureServices()
services.AddScoped<MyFeatureRepository>();
services.AddScoped<MyFeatureService>();  // Only if using transactions

// In ConfigureEndpoints()
MyFeatureEndpoints.Map(endpoints);
```

### Packaging for Deployment

```bash
# Create deployment ZIP with plugin metadata
npm run package
# Output: dist/myplugin.1.0.0.zip
```

The ZIP structure includes:
- `plugin.json` (metadata at root)
- `MyPlugin.dll` (compiled plugin)
- Dependencies (if any)

Upload via Backend Host Admin UI or API.

## Key Constraints and Patterns

### Must Follow
- **All database operations** use SafeQueryBuilder - never raw SQL strings
- **Feature registration** in `MyPlugin.cs` is manual - script doesn't auto-register
- **Connection strings** are injected via `IConfiguration.GetConnectionString("Default")`
- **Endpoint routes** are auto-prefixed with `/api/plugins/{name}/{version}/`
- **Service layer** is only needed for features using automatic transactions

### Naming Conventions
- **Features**: PascalCase folder names (Products, AuditLogs)
- **Endpoints**: Lowercase with hyphens (`products`, `audit-logs`)
- **Database tables**: Match entity names (Products table → Product entity)
- **DTOs**: Suffix with Request/Response (CreateProductRequest, ProductResponse)

### Testing Strategy
- Unit tests in `tests/MyPlugin.Tests/` using xUnit
- Focus on business logic validation (request validation, edge cases)
- No integration tests in template (DevHost used for manual endpoint testing)
- Test naming: `MethodName_Scenario_ExpectedResult` pattern

### Database Patterns
- **Primary keys**: Always `{EntityName}Id` (ProductId, ItemId)
- **Timestamps**: `CreatedAt` (required), `UpdatedAt` (optional), `DeletedAt` (soft delete)
- **Audit fields**: `CreatedBy`, `UpdatedBy` stored as usernames (from `BizuitUserContext`)

## Common Tasks

### Add a new authenticated endpoint
1. Create feature with `npm run new-feature` (select "protected")
2. Register repository + service in `MyPlugin.cs → ConfigureServices()`
3. Add endpoint mapping in `MyPlugin.cs → ConfigureEndpoints()`
4. Use `BizuitUserContext` parameter in endpoint handler
5. Add `.RequireAuthorization()` or `.RequireAuthorization("role")`

### Add a search/filter endpoint
1. Add method to repository using `Query()` builder
2. Chain `.WhereEquals()`, `.WhereLike()`, `.WhereGreaterThan()` as needed
3. Return via `ExecuteAsync(query)`
4. Add query parameters to endpoint handler signature
5. Wire through service layer if using transactions

### Add a no-transaction endpoint
1. Create feature with `npm run new-feature` (select "no transaction")
2. Call repository directly from endpoint (no service layer)
3. Add `.NoTransaction()` to endpoint registration
4. Use for logging, high-frequency writes, or fire-and-forget operations

### Debug in production
- Each plugin has Swagger UI at: `https://host/api/plugins/{name}/swagger`
- Check Backend Host logs for plugin load errors
- Verify connection string is configured in Admin Panel
- Ensure database tables exist before activating plugin

## Configuration System

Plugins receive configuration from Backend Host admin panel:

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Connection string (configured per plugin in admin)
    var connString = configuration.GetConnectionString("Default");

    // Custom key-value settings
    var apiKey = configuration["MyApiKey"];
    var maxItems = configuration.GetValue<int>("MaxItemsPerPage", 100);
}
```

Configuration is injected at plugin load time and stored per-plugin instance.

## Solution Structure

The `.sln` includes three projects:
- **MyPlugin** (`src/MyPlugin/`) - Main plugin DLL
- **MyPlugin.Tests** (`tests/MyPlugin.Tests/`) - xUnit test project
- **DevHost** (`src/DevHost/`) - Local development server (not deployed)

Build outputs go to standard `bin/` and `obj/` directories. Only `MyPlugin.dll` and its dependencies are packaged for deployment.
