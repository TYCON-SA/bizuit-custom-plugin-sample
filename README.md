# Bizuit Backend Plugin Sample

**[Español](#español) | [English](#english)**

---

# Español

Template completo para crear plugins de backend que corren en Bizuit Backend Host.

## Quick Start (5 minutos)

```bash
# 1. Clonar este template
git clone https://github.com/TYCON-SA/bizuit-custom-plugin-sample mi-plugin
cd mi-plugin

# 2. Instalar dependencias de scripts
npm install

# 3. Configurar plugin manifest
#    Para desarrollo LOCAL con valores reales:
cp plugin.local.json.example plugin.local.json
#    Editar plugin.local.json con tus valores reales (gitignored - safe)

#    Para CI/CD (deployment):
#    El pipeline usa plugin.json.example (sin credenciales)

# 4. Renombrar el plugin
#    - Editar plugin.local.json (name, version, description, defaultSettings)
#    - Renombrar carpeta src/MyPlugin → src/MiPlugin
#    - Actualizar namespace en todos los archivos .cs

# 5. Crear una nueva feature (interactivo)
npm run new-feature

# 6. Compilar
dotnet build

# 7. Empaquetar para deploy
npm run package
# Usa plugin.local.json (local) o plugin.json (CI/CD)
# Genera: dist/myplugin.1.0.8-abc1234.zip
```

### 🔒 Configuración: 3 Escenarios SUPER CLAROS

#### 1️⃣ **Debug Local con DevHost (F5 en Visual Studio/Rider)**

**📁 Archivo:** `src/DevHost/appsettings.Development.json` (gitignored ✅)

**Qué hace:** DevHost lee este archivo y pasa valores al plugin via `IConfiguration`

**Valores:** ✅ **REALES** (safe - archivo gitignored)

```json
{
  "ConnectionStrings": { "Default": "..." },
  "System": { "DashboardApiUrl": "..." },

  // ✅ Poner credenciales REALES aquí
  "AzureStorageUrl": "https://mi-storage-real.blob.core.windows.net",
  "AzureStorageKey": "mi-key-real-abc123",
  "ApiKey": "Bearer sk-xyz-real"
}
```

**Acceso en plugin:**
```csharp
var url = _config["AzureStorageUrl"];  // ← Lee de appsettings.Development.json
```

---

#### 2️⃣ **Package para Testing Local (npm run package)**

**📁 Archivo:** `plugin.local.json` (gitignored ✅ por `*.local`)

**Qué hace:** `package.mjs` lo detecta y lo usa para crear el ZIP

**Valores:** ✅ **REALES** (safe - archivo gitignored)

```json
{
  "name": "myplugin",
  "version": "1.0.8",
  "defaultSettings": {
    // ✅ Poner credenciales REALES aquí
    "AzureStorageUrl": "https://mi-storage-real.blob.core.windows.net",
    "AzureStorageKey": "mi-key-real-abc123",
    "MaxRetries": "5"
  }
}
```

**Crear archivo:**
```bash
cp plugin.local.json.example plugin.local.json
# Editar con valores reales
npm run package  # ← Usa plugin.local.json automáticamente
```

**Qué pasa:**
- ZIP incluye `defaultSettings` con valores REALES
- Al subir a Backend Host → Settings se auto-crean con valores REALES
- Listo para probar sin configurar nada en UI

---

#### 3️⃣ **CI/CD Pipeline (Deployment a Producción)**

**📁 Archivo:** `plugin.json` (commiteado ❌ - SIN credenciales)

**Qué hace:** Pipeline usa este archivo (NO existe plugin.local.json en CI/CD)

**Valores:** ❌ **VACÍOS o defaults seguros SOLAMENTE**

```json
{
  "name": "myplugin",
  "version": "1.0.8",
  "defaultSettings": {
    // ❌ NUNCA poner credenciales aquí (se commitea al repo)
    "AzureStorageUrl": "",           // Vacío
    "AzureStorageKey": "",            // Vacío
    "MaxRetries": "3"                 // Safe default (OK)
  }
}
```

**Qué pasa:**
- Pipeline: `npm run package` → NO encuentra `plugin.local.json` → Usa `plugin.json`
- ZIP incluye `defaultSettings` con strings vacíos
- Al subir a Backend Host → Settings se auto-crean VACÍOS
- **Admin configura** valores reales vía UI (`/admin/settings/plugins`)

---

#### 📋 Tabla Comparativa de Archivos

| Archivo | Gitignored | Credenciales | Escenario |
|---------|------------|--------------|-----------|
| `appsettings.Development.json` | ✅ Sí | ✅ REALES | DevHost debug (F5) |
| `plugin.local.json` | ✅ Sí (`*.local`) | ✅ REALES | `npm run package` local |
| `plugin.json` | ❌ **Commiteado** | ❌ **NUNCA** | CI/CD pipeline |
| `plugin.json.example` | ❌ Commiteado | ❌ No | Template/documentación |
| `plugin.local.json.example` | ❌ Commiteado | ❌ No | Template para copiar |

---

#### ⚠️ REGLA DE ORO

**NUNCA** pongas credenciales en archivos commiteados:
- ❌ `plugin.json` → Se commitea → **NO credentials**
- ❌ `plugin.json.example` → Se commitea → **NO credentials**
- ✅ `plugin.local.json` → Gitignored → **OK credentials**
- ✅ `appsettings.Development.json` → Gitignored → **OK credentials**

```

## Estructura del Proyecto

```
bizuit-custom-plugin-sample/
├── src/
│   ├── MyPlugin/                      # Proyecto del plugin
│   │   ├── MyPlugin.cs                # Punto de entrada (IBackendPlugin)
│   │   ├── MyPlugin.csproj            # Proyecto .NET
│   │   └── Features/
│   │       ├── Items/                 # Feature ejemplo (CRUD público)
│   │       ├── Products/              # Feature con autenticación
│   │       ├── AuditLogs/             # Feature sin transacción
│   │       └── Me/                    # Endpoint debug (info usuario)
│   └── DevHost/                       # Servidor de desarrollo local
│       ├── DevHost.csproj
│       ├── Program.cs
│       └── appsettings.json           # Connection string aquí
├── tests/MyPlugin.Tests/              # Tests unitarios (xUnit)
├── database/                          # Scripts SQL
│   ├── 001_CreateItemsTable.sql
│   ├── 002_CreateProductsTable.sql
│   ├── 003_CreateAuditLogsTable.sql
│   └── setup-database.sql             # Script consolidado
├── scripts/
│   ├── new-feature.mjs                # Crear nueva feature (interactivo)
│   └── package.mjs                    # Empaquetar para deploy
├── plugin.json                        # Metadata del plugin
├── MyPlugin.sln                       # Solución (src + tests + devhost)
└── package.json
```

## Archivo plugin.json

El archivo `plugin.json` define la metadata del plugin:

```json
{
  "name": "myplugin",
  "version": "1.0.0",
  "description": "My custom backend plugin",
  "author": "Your Name",
  "entryPoint": "MyPlugin.dll",
  "pluginClass": "MyPlugin.MyPluginPlugin",
  "requiresDatabase": true
}
```

| Campo | Requerido | Descripción |
|-------|-----------|-------------|
| `name` | ✅ | Nombre único del plugin (lowercase, sin espacios) |
| `version` | ✅ | Versión semántica (ej: 1.0.0, 1.2.3) |
| `description` | ❌ | Descripción del plugin |
| `author` | ❌ | Autor o empresa |
| `entryPoint` | ✅ | Nombre del DLL principal |
| `pluginClass` | ✅ | Clase que implementa `IBackendPlugin` (namespace completo) |
| `requiresDatabase` | ❌ | Si el plugin requiere connection string (default: `true`) |

### Campo `requiresDatabase`

- **`true` (default):** El plugin requiere una base de datos configurada. No se puede activar sin connection string.
- **`false`:** El plugin puede funcionar sin base de datos (ej: plugins de integración con APIs externas, validadores, etc.)

**Nota:** Si no incluyes este campo, el valor por defecto es `true` para compatibilidad con plugins existentes.

## Features de Ejemplo

Este template incluye 4 features que demuestran diferentes patrones:

| Feature    | Endpoints       | Autenticación      | Transacciones    |
|------------|-----------------|--------------------| -----------------|
| Items      | CRUD completo   | Público            | Automáticas      |
| Products   | CRUD completo   | Protegido + Roles  | Automáticas      |
| AuditLogs  | POST solamente  | Público            | Sin transacción  |
| Me         | GET solamente   | Protegido + Roles  | N/A (solo lectura) |

## Desarrollo Local (DevHost)

El proyecto incluye un DevHost para desarrollo y debug local sin necesidad del Backend Host completo.

### Configuración

1. Copiar `src/DevHost/appsettings.json` y configurar tus connection strings:

```json
{
  "ConnectionStrings": {
    "Default": "Server=TU_SERVIDOR;Database=TU_DATABASE;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True"
  },
  "DevHost": {
    "EnableSqlLogging": false
  }
}
```

**Connection Strings:**
- `Default`: Base de datos donde están las tablas de tu plugin (Items, Products, etc.)

**Configuración:**
- `EnableSqlLogging`: Si es `true`, loguea todas las queries SQL con parámetros y tiempos (solo Development)

2. Crear las tablas en la base de datos (ver [Setup de Base de Datos](#setup-de-base-de-datos))

### Ejecutar

```bash
# Iniciar servidor de desarrollo
npm run dev

# O con hot reload
npm run watch
```

El servidor arranca en **http://localhost:5001** con Swagger UI en la raíz.

### Características del DevHost

- ✅ Swagger UI para probar endpoints
- ✅ Usa la misma lógica del plugin que en producción
- ✅ Hot reload con `npm run watch`
- ✅ Connection string configurable
- ✅ **Autenticación con JWT tokens reales** del Dashboard
- ✅ Transacciones automáticas (POST/PUT/PATCH/DELETE con rollback automático en errores)
- ✅ **[NUEVO]** Debug con VS Code (F5) con breakpoints funcionales
- ✅ **[NUEVO]** Exception middleware con stack traces detallados en JSON
- ✅ **[NUEVO]** SQL query logging (queries, parámetros, tiempos de ejecución)
- ✅ **[NUEVO]** 4 endpoints de debug (`/api/_debug`, `/health`, `/user`, `/endpoints`)

### Debug y Troubleshooting

#### Debug Endpoints (solo Development)

| Endpoint | Descripción |
|----------|-------------|
| `GET /api/_debug` | Info completa: ambiente, DB, auth, plugin |
| `GET /api/_debug/health` | Health check rápido |
| `GET /api/_debug/user` | Info del usuario autenticado (requiere auth) |
| `GET /api/_debug/endpoints` | Lista todos los endpoints registrados |

#### SQL Query Logging

Habilitá SQL logging en `appsettings.json`:

```json
{
  "DevHost": {
    "EnableSqlLogging": true
  }
}
```

Ejemplo de log:
```
[SQL] 45ms | RowsAffected=1
INSERT INTO Products (Name, Price) VALUES (@p0, @p1)
  @p0 = Coca Cola (String)
  @p1 = 1.5 (Decimal)
```

#### Debugging con VS Code

Para debuggear con VS Code, necesitás crear el archivo `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "DevHost",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/DevHost/bin/Debug/net9.0/DevHost.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/DevHost",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

Luego:
1. Abrir panel "Run and Debug" (Ctrl+Shift+D)
2. Seleccionar perfil "DevHost"
3. Presionar F5 para iniciar con debugger
4. Colocar breakpoints en archivos .cs
5. Hacer request y el debugger se detendrá

#### Troubleshooting

| Problema | Solución |
|----------|----------|
| No veo SQL logs | Verificar `EnableSqlLogging: true` en appsettings.json |
| Debug endpoints 404 | Verificar `ASPNETCORE_ENVIRONMENT=Development` |
| Breakpoints no funcionan | Compilar en modo Debug, reiniciar VS Code |
| Exception sin stack trace | Solo en Development se muestran stack traces |

### Autenticación del DevHost

El DevHost usa **autenticación JWT real** igual que en producción. Necesitás obtener un token válido del Dashboard de BIZUIT.

#### Cómo Obtener un Token JWT

1. **Login al Dashboard**: Ingresá a tu Dashboard de BIZUIT (ej: `https://test.bizuit.com/arielschbizuitdashboard`)
2. **Abrir Developer Tools**: Presioná F12 en tu navegador
3. **Ir a Storage**:
   - Chrome/Edge: Application → Local Storage
   - Firefox: Storage → Local Storage
4. **Copiar el Token**: Buscá la clave `adminSessionToken` y copiá su valor completo
5. **Usar en DevHost**: Pegá este token en Swagger UI o en tus requests de curl

#### Uso en Swagger UI

1. Abrir http://localhost:5001 (Swagger UI)
2. Click en botón **"Authorize"** (candado arriba a la derecha)
3. Pegar tu token JWT completo del Dashboard
4. Click **"Authorize"**
5. Los endpoints protegidos ahora funcionarán con tus roles reales

#### Uso con curl

```bash
# Obtener tu token del Dashboard primero
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VybmFtZSI6ImFkbWluIiwiYml6dWl0X3Rva2VuIjoiWk1kdWZX..."

# Usar el token en requests
curl http://localhost:5001/api/products \
  -H "Authorization: Bearer $TOKEN"

# El token contiene tus roles y roleSettings reales
curl http://localhost:5001/api/me \
  -H "Authorization: Bearer $TOKEN"
```

#### Ventajas de Usar Tokens Reales

✅ **Datos Reales**: Probás con tus roles y roleSettings exactos de producción
✅ **Realista**: El comportamiento es idéntico al Backend Host en producción
✅ **Sin Mocks**: No hay datos hardcodeados, todo viene de la DB
✅ **Debugging Preciso**: Ves exactamente cómo se comportará tu plugin con usuarios reales

⚠️ **Importante**: Los tokens tienen expiración. Si tu token expira, necesitás obtener uno nuevo del Dashboard.

### Configurar Dashboard API URL (Local Development)

Si tu plugin necesita llamar al Dashboard API, configurá la URL en `src/DevHost/appsettings.json`:

```json
{
  "System": {
    "DashboardApiUrl": "https://YOUR_DASHBOARD_URL/YOUR_TENANT_API/api",
    "TenantId": "default"
  }
}
```

O mejor aún, creá `src/DevHost/appsettings.Development.json` (no commitearlo):

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=MyPluginDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True",
    "Dashboard": "Server=localhost;Database=DashboardDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
  },
  "System": {
    "DashboardApiUrl": "https://YOUR_DASHBOARD_URL/YOUR_TENANT_API/api",
    "TenantId": "default"
  }
}
```

**Nota:** El archivo `appsettings.Development.json` está en `.gitignore` para evitar commitear credenciales.

**Ejemplo de uso en plugin:**

```csharp
public class MyService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public async Task<object> GetUserFromDashboard(BizuitUserContext user)
    {
        var dashboardApiUrl = _config["System:DashboardApiUrl"];
        var client = _httpClientFactory.CreateClient("DashboardClient");

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {user.RawToken}");

        var response = await client.GetAsync($"{dashboardApiUrl}/users/{user.Username}");
        return await response.Content.ReadFromJsonAsync<object>();
    }
}
```

## Setup de Base de Datos

Antes de activar el plugin, crear las tablas en SQL Server:

```bash
# Opción 1: Script consolidado (recomendado)
sqlcmd -S <servidor> -d <database> -U <usuario> -P <password> \
  -i database/setup-database.sql

# Opción 2: Scripts individuales
sqlcmd -S <servidor> -d <database> -U <usuario> -P <password> \
  -i database/001_CreateItemsTable.sql
sqlcmd -S <servidor> -d <database> -U <usuario> -P <password> \
  -i database/002_CreateProductsTable.sql
sqlcmd -S <servidor> -d <database> -U <usuario> -P <password> \
  -i database/003_CreateAuditLogsTable.sql
```

Los scripts son idempotentes (`IF NOT EXISTS`), pueden ejecutarse múltiples veces sin errores.

## Crear Nueva Feature

El script interactivo crea toda la estructura necesaria:

```bash
npm run new-feature
```

### Preguntas del Script

1. **Nombre de la feature** (PascalCase, ej: Products, Orders, Customers)
2. **Tipo de autenticación:**
   - Pública (sin auth)
   - Protegida (requiere login)
   - Solo Admin (requiere rol admin)
3. **Manejo de transacciones:**
   - Automáticas (POST/PUT/DELETE con transacción)
   - Sin transacción (fire-and-forget, mejor performance)

### Archivos Generados

```
Features/MiFeature/
├── Models/MiFeature.cs           # Modelo + DTOs
├── MiFeatureRepository.cs        # Queries con SafeQueryBuilder
├── MiFeatureService.cs           # Lógica de negocio (si usa transacciones)
└── MiFeatureEndpoints.cs         # Endpoints HTTP
```

### Registrar la Feature

Después de crear, registrar en `MyPlugin.cs`:

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // ... código existente ...

    // Nueva feature
    services.AddScoped<MiFeatureService>();      // Solo si usa transacciones
    services.AddScoped<MiFeatureRepository>();
}

public void ConfigureEndpoints(IPluginEndpointBuilder endpoints)
{
    // ... código existente ...

    MiFeatureEndpoints.Map(endpoints);
}
```

## SafeQueryBuilder - Guía Completa

**SQL Injection es IMPOSIBLE** usando SafeQueryBuilder. Todos los valores son parametrizados automáticamente.

### Query Básico

```csharp
public class ProductsRepository : SafeRepository<Product>
{
    protected override string TableName => "Products";

    // Obtener todos
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await ExecuteAsync(Query());
    }

    // Obtener por ID
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await ExecuteSingleAsync(
            Query().WhereEquals("ProductId", id));
    }
}
```

### Filtros Disponibles

```csharp
var query = Query();

// Igualdad
query.WhereEquals("Status", "Active");

// LIKE (buscar parcial)
query.WhereLike("Name", searchTerm);  // WHERE Name LIKE @p0

// Comparaciones
query.WhereGreaterThan("Price", 100);
query.WhereGreaterOrEqual("Stock", 10);
query.WhereLessThan("Discount", 50);
query.WhereLessOrEqual("Quantity", 5);

// IN (múltiples valores)
query.WhereIn("Category", new[] { "Electronics", "Furniture" });

// Ordenamiento
query.OrderBy("Name");
query.OrderByDescending("CreatedAt");

// Paginación
query.Skip(20).Take(10);  // Página 3 con 10 items por página
```

### Insert / Update / Delete

```csharp
// INSERT
var insert = Insert()
    .Set("Name", request.Name)
    .Set("Price", request.Price)
    .Set("CreatedAt", DateTime.UtcNow);

var newId = await ExecuteWithIdentityAsync(insert);

// UPDATE
var update = Update()
    .Set("Name", request.Name)
    .Set("Price", request.Price)
    .Set("UpdatedAt", DateTime.UtcNow)
    .WhereEquals("ProductId", id);

var rowsAffected = await ExecuteAsync(update);

// DELETE
var delete = Delete()
    .WhereEquals("ProductId", id);

var rowsAffected = await ExecuteAsync(delete);
```

## Autenticación y Autorización

### Endpoint Público (sin auth)

```csharp
public static void Map(IPluginEndpointBuilder endpoints)
{
    endpoints.MapGet("items", GetAll);           // Cualquiera puede acceder
    endpoints.MapGet("items/{id:int}", GetById);
}
```

### Query Parameters para Swagger

Para que los query parameters aparezcan correctamente en Swagger/OpenAPI, usa el atributo `[FromQuery]`:

```csharp
using Microsoft.AspNetCore.Mvc;

private static async Task<IResult> Search(
    ItemsService service,
    [FromQuery] string? name = null,
    [FromQuery] decimal? minPrice = null,
    [FromQuery] DateTime? fechaDesde = null,
    [FromQuery] DateTime? fechaHasta = null)
{
    var items = await service.SearchAsync(name, minPrice, fechaDesde, fechaHasta);
    return Results.Ok(items);
}
```

**Importante:** El Backend Host detecta automáticamente los parámetros con `[FromQuery]` y los incluye en la documentación OpenAPI del plugin.

### Endpoint Protegido (requiere login)

```csharp
public static void Map(IPluginEndpointBuilder endpoints)
{
    // Lectura pública
    endpoints.MapGet("products", GetAll);

    // Escritura protegida
    endpoints.MapPost("products", Create)
        .RequireAuthorization();

    endpoints.MapPut("products/{id:int}", Update)
        .RequireAuthorization();
}
```

### Endpoint con Rol Específico

```csharp
public static void Map(IPluginEndpointBuilder endpoints)
{
    // Solo administradores pueden eliminar
    endpoints.MapDelete("products/{id:int}", Delete)
        .RequireAuthorization("admin");
}
```

### Acceder al Usuario Autenticado

```csharp
private static async Task<IResult> Create(
    CreateProductRequest request,
    ProductsService service,
    BizuitUserContext user)  // Inyectado automáticamente
{
    // Información del usuario
    var username = user.Username;
    var tenantId = user.TenantId;
    var roles = user.Roles;  // Lista de roles
    var isAdmin = user.Roles.Contains("admin");

    // Guardar quién creó el registro
    var id = await service.CreateAsync(request, username);
    return Results.Created($"/products/{id}", new { id });
}
```

### Acceder a Propiedades de Rol (RoleSettings)

Los **RoleSettings** son propiedades de negocio definidas por rol en la tabla `UserRoleSettings` del Dashboard.
Permiten filtrar datos o aplicar lógica según configuraciones específicas del usuario.

**Ejemplo:** Un vendedor tiene rol "Vendor" con setting `Producto=COCACOLA`, lo que restringe los datos que puede ver.

```csharp
private static async Task<IResult> GetMyData(
    BizuitUserContext user,
    MyService service)
{
    // ===== PROPIEDADES BÁSICAS =====
    var username = user.Username;           // Nombre de usuario
    var tenantId = user.TenantId;           // Tenant (multi-tenant)
    var roles = user.Roles;                 // Lista de roles: ["Admin", "Vendor"]
    var expiresAt = user.ExpiresAt;         // Expiración del token

    // ===== ROLE SETTINGS =====
    // Cada setting tiene: Role (nombre del rol), Name (nombre del setting), Value (valor)
    // Ejemplo: { Role: "Vendor", Name: "Producto", Value: "COCACOLA" }

    // Obtener TODOS los valores de un setting específico (de todos los roles)
    var productos = user.GetSettingValues("Producto");
    // Si el usuario tiene roles "Vendor A" (Producto=COCACOLA) y "Vendor B" (Producto=PEPSI)
    // Resultado: ["COCACOLA", "PEPSI"]

    // Verificar si tiene un valor específico en ALGÚN rol
    if (user.HasSettingValue("Producto", "COCACOLA"))
    {
        // Usuario tiene acceso a productos COCACOLA
    }

    // Obtener valor de un setting para un ROL específico
    var productoAdmin = user.GetSettingValue("Administrators", "Producto");
    // Resultado: "COCACOLA" o null si no existe

    // Obtener TODOS los settings de un rol específico
    var settingsAdmin = user.GetRoleSettings("Administrators");
    // Resultado: IEnumerable<RoleSetting> con todos los settings del rol

    // ===== HELPERS DE ROLES =====
    var isAdmin = user.HasRole("Administrators");
    var isGestorOrSuper = user.HasAnyRole("Gestores", "Supervisores");
    var isFullAdmin = user.HasAllRoles("Administrators", "BizuitAdmins");

    // ===== USO PRÁCTICO: Filtrar datos por settings =====
    var allowedProducts = user.GetSettingValues("Producto").ToList();
    var data = await service.GetDataFilteredByProducts(allowedProducts);

    return Results.Ok(data);
}
```

**Ejemplo de respuesta del endpoint `/items/my-info`:**

```json
{
  "username": "admin",
  "tenantId": "default",
  "isAuthenticated": true,
  "expiresAt": "2025-01-15T18:00:00Z",
  "roles": ["Administrators", "Gestores"],
  "roleSettings": [
    { "role": "Administrators", "name": "Producto", "value": "COCACOLA" },
    { "role": "Gestores", "name": "Producto", "value": "PEPSI" },
    { "role": "Gestores", "name": "Region", "value": "NORTE" }
  ],
  "hasAdminRole": true,
  "hasAnyGestorRole": true,
  "allProductos": ["COCACOLA", "PEPSI"],
  "hasCocacola": true
}
```

### Patrón Helper: Obtener Valor Requerido de RoleSettings

Cuando un endpoint **requiere** un valor específico de RoleSettings (ej: un VendorId o GestorId que DEBE existir), usá este patrón con tuples para extraer el valor con manejo de errores:

```csharp
/// <summary>
/// Obtiene un VendorId requerido desde los RoleSettings del usuario.
/// Retorna un error si el usuario no tiene VendorId configurado.
/// </summary>
private static (int VendorId, IResult? Error) GetVendorIdFromUser(BizuitUserContext user)
{
    // Primero intentar obtener de un rol específico
    var vendorIdValue = user.GetSettingValue("Vendors", "VendorId");

    // Si no está, buscar en cualquier rol que tenga VendorId
    if (string.IsNullOrEmpty(vendorIdValue))
    {
        vendorIdValue = user.GetSettingValues("VendorId").FirstOrDefault();
    }

    if (string.IsNullOrEmpty(vendorIdValue))
    {
        return (0, Results.BadRequest(new
        {
            error = "User does not have VendorId configured in RoleSettings",
            detail = "The user must have the 'VendorId' setting configured",
            username = user.Username,
            roles = user.Roles,
            roleSettings = user.RoleSettings.Select(s => new { s.Role, s.Name, s.Value })
        }));
    }

    if (!int.TryParse(vendorIdValue, out var vendorId))
    {
        return (0, Results.BadRequest(new
        {
            error = "VendorId value is not a valid integer",
            detail = $"The VendorId setting value '{vendorIdValue}' cannot be converted to an integer"
        }));
    }

    return (vendorId, null);
}

// Uso en endpoint:
private static async Task<IResult> Create(
    CreateOrderRequest request,
    OrdersService service,
    BizuitUserContext user)
{
    // Extraer VendorId requerido de RoleSettings
    var vendorIdResult = GetVendorIdFromUser(user);
    if (vendorIdResult.Error != null)
    {
        return vendorIdResult.Error;
    }

    // Usar el valor extraído
    var id = await service.CreateAsync(request, vendorIdResult.VendorId);
    return Results.Created($"/orders/{id}", new { id });
}
```

**Beneficios de este patrón:**
- ✅ Error claro cuando falta la configuración
- ✅ Valida el tipo de dato (int en este caso)
- ✅ Debug fácil: incluye username, roles y roleSettings en el error
- ✅ Reutilizable: podés crear helpers similares para otros settings (GestorId, ProductoId, etc.)

## Transacciones

### Comportamiento Automático

Por defecto, el Backend Host maneja transacciones automáticamente:

| Método HTTP | Transacción |
|-------------|-------------|
| GET         | No          |
| HEAD        | No          |
| OPTIONS     | No          |
| POST        | Sí          |
| PUT         | Sí          |
| PATCH       | Sí          |
| DELETE      | Sí          |

**No necesitas hacer nada** - las transacciones se crean y commitean automáticamente. Si hay un error, se hace rollback.

### Opt-Out: Sin Transacción

Para casos especiales donde NO querés transacción (mejor performance):

```csharp
public static void Map(IPluginEndpointBuilder endpoints)
{
    // Logging: fire-and-forget, debe persistir aunque falle otra cosa
    endpoints.MapPost("audit-logs", Create)
        .NoTransaction();
}
```

**Casos de uso para `.NoTransaction()`:**
- Logging/auditoría (debe persistir aunque falle la operación principal)
- Operaciones de alta frecuencia (mejor performance)
- Operaciones que no modifican datos críticos

## Tests Unitarios

El proyecto incluye tests con xUnit:

```bash
# Ejecutar todos los tests
dotnet test

# Con más detalle
dotnet test --verbosity normal

# Solo tests de un archivo
dotnet test --filter "FullyQualifiedName~ProductsServiceTests"
```

### Estructura de Tests

```csharp
public class ProductsServiceTests
{
    [Fact]
    public void CreateRequest_WithEmptyName_ShouldFail()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "",  // Inválido
            SKU = "PROD-001",
            Price = 100
        };

        // Act & Assert
        Assert.True(string.IsNullOrWhiteSpace(request.Name));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CreateRequest_WithNegativePrice_ShouldFail(decimal price)
    {
        var request = new CreateProductRequest
        {
            Name = "Test",
            SKU = "TEST",
            Price = price
        };

        Assert.True(request.Price < 0);
    }
}
```

## Deploy

### 1. Empaquetar

```bash
npm run package
# Genera: dist/myplugin.1.0.0.zip
```

### 2. Subir al Backend Host

**Opción A: Admin UI**
1. Ir a Admin Panel → Settings → Plugins
2. Click "Upload Plugin"
3. Seleccionar el ZIP
4. Configurar connection string
5. Activar

**Opción B: API**

```bash
# Subir
curl -X POST https://host/api/admin/plugins/upload \
     -H "Authorization: Bearer $TOKEN" \
     -F "name=myplugin" \
     -F "version=1.0.0" \
     -F "file=@dist/myplugin.1.0.0.zip"

# Configurar connection string
curl -X PUT https://host/api/admin/plugins/{pluginId}/connection-string \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"connectionString": "Server=...;Database=...;..."}'

# Activar
curl -X POST https://host/api/admin/plugins/{pluginId}/activate \
     -H "Authorization: Bearer $TOKEN"
```

### 3. Probar Endpoints

Una vez activo:

```bash
# Items (público)
curl https://host/api/plugins/myplugin/items
curl https://host/api/plugins/myplugin/items/1

# Products (requiere auth)
curl https://host/api/plugins/myplugin/products \
     -H "Authorization: Bearer $TOKEN"

# AuditLogs (POST sin transacción)
curl -X POST https://host/api/plugins/myplugin/audit-logs \
     -H "Content-Type: application/json" \
     -d '{"action": "TEST", "entityType": "Test"}'
```

### Swagger del Plugin

Cada plugin activo tiene su propia documentación Swagger:

```
https://host/api/plugins/myplugin/swagger
```

## Configuración del Plugin

El plugin recibe configuración del Backend Host:

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Connection string (configurado en admin panel)
    var connectionString = configuration.GetConnectionString("Default");

    // Configuración custom (key-value desde admin)
    var apiKey = configuration["MyApiKey"];
    var maxItems = configuration.GetValue<int>("MaxItemsPerPage", 100);
}
```

### ⚠️ CRÍTICO: IConfiguration NO está disponible via DI en Backend Host

**El Backend Host NO registra `IConfiguration` en el contenedor de DI.** Si creás un servicio que depende de `IConfiguration`, fallará en runtime:

```
System.InvalidOperationException: Unable to resolve service for type
'Microsoft.Extensions.Configuration.IConfiguration' while attempting to activate 'MyService'.
```

**❌ NO hagas esto** - fallará en Backend Host:
```csharp
// Esta clase depende de IConfiguration via constructor injection
public class MyService
{
    public MyService(IConfiguration configuration) // ❌ Fallará!
    {
        _connectionString = configuration.GetConnectionString("Default");
    }
}

// En ConfigureServices:
services.AddSingleton<MyService>();  // ❌ DI no puede resolver IConfiguration
```

**✅ Hacé esto en su lugar** - usá factory delegate:
```csharp
// Misma clase, mismo constructor
public class MyService
{
    public MyService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default");
    }
}

// En ConfigureServices - usá factory para pasar configuration directamente:
services.AddSingleton<MyService>(sp =>
{
    return new MyService(configuration);  // ✅ Pasá configuration del scope de ConfigureServices
});
```

**Por qué funciona**: El parámetro `configuration` está disponible en el scope del método `ConfigureServices`. Usando un factory delegate, lo pasás directamente en lugar de depender de la resolución de DI.

## Troubleshooting

### Error: "Table does not exist"

Ejecutar los scripts SQL de `database/` antes de activar el plugin.

### Error: "Connection string not configured"

Configurar el connection string en Admin Panel → Plugins → [tu plugin] → Connection String.

### Error: "Plugin failed to load"

1. Verificar que el ZIP contiene `plugin.json` en la raíz
2. Verificar que el DLL está en la carpeta correcta dentro del ZIP
3. Revisar logs del Backend Host

### Error: "Unauthorized" en endpoints protegidos

Verificar que el token JWT incluye los roles necesarios.

---

# English

Complete template for creating backend plugins that run on Bizuit Backend Host.

## Quick Start (5 minutes)

```bash
# 1. Clone this template
git clone https://github.com/TYCON-SA/bizuit-custom-plugin-sample my-plugin
cd my-plugin

# 2. Install script dependencies
npm install

# 3. Rename the plugin
#    - Edit plugin.json (name, displayName, description)
#    - Rename folder src/MyPlugin → src/MyPlugin
#    - Update namespace in all .cs files

# 4. Create a new feature (interactive)
npm run new-feature

# 5. Build
dotnet build

# 6. Package for deployment
npm run package
# Generates: dist/myplugin.1.0.0.zip
```

## Project Structure

```
bizuit-custom-plugin-sample/
├── src/
│   ├── MyPlugin/                      # Plugin project
│   │   ├── MyPlugin.cs                # Entry point (IBackendPlugin)
│   │   ├── MyPlugin.csproj            # .NET project
│   │   └── Features/
│   │       ├── Items/                 # Example feature (public CRUD)
│   │       ├── Products/              # Feature with authentication
│   │       ├── AuditLogs/             # Feature without transaction
│   │       └── Me/                    # Debug endpoint (user info)
│   └── DevHost/                       # Local development server
│       ├── DevHost.csproj
│       ├── Program.cs
│       └── appsettings.json           # Connection string here
├── tests/MyPlugin.Tests/              # Unit tests (xUnit)
├── database/                          # SQL scripts
│   ├── 001_CreateItemsTable.sql
│   ├── 002_CreateProductsTable.sql
│   ├── 003_CreateAuditLogsTable.sql
│   └── setup-database.sql             # Consolidated script
├── scripts/
│   ├── new-feature.mjs                # Create new feature (interactive)
│   └── package.mjs                    # Package for deployment
├── plugin.json                        # Plugin metadata
├── MyPlugin.sln                       # Solution (src + tests + devhost)
└── package.json
```

## plugin.json File

The `plugin.json` file defines the plugin metadata:

```json
{
  "name": "myplugin",
  "version": "1.0.0",
  "description": "My custom backend plugin",
  "author": "Your Name",
  "entryPoint": "MyPlugin.dll",
  "pluginClass": "MyPlugin.MyPluginPlugin",
  "requiresDatabase": true
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `name` | ✅ | Unique plugin name (lowercase, no spaces) |
| `version` | ✅ | Semantic version (e.g., 1.0.0, 1.2.3) |
| `description` | ❌ | Plugin description |
| `author` | ❌ | Author or company |
| `entryPoint` | ✅ | Main DLL name |
| `pluginClass` | ✅ | Class implementing `IBackendPlugin` (full namespace) |
| `requiresDatabase` | ❌ | Whether plugin requires connection string (default: `true`) |

### `requiresDatabase` Field

- **`true` (default):** Plugin requires a configured database. Cannot be activated without a connection string.
- **`false`:** Plugin can work without a database (e.g., external API integration plugins, validators, etc.)

**Note:** If you don't include this field, the default value is `true` for backwards compatibility with existing plugins.

## Example Features

This template includes 4 features demonstrating different patterns:

| Feature    | Endpoints     | Authentication    | Transactions    |
|------------|---------------|-------------------| ----------------|
| Items      | Full CRUD     | Public            | Automatic       |
| Products   | Full CRUD     | Protected + Roles | Automatic       |
| AuditLogs  | POST only     | Public            | No transaction  |
| Me         | GET only      | Protected + Roles | N/A (read only) |

## Local Development (DevHost)

The project includes a DevHost for local development and debugging without needing the full Backend Host.

### Configuration

1. Copy `src/DevHost/appsettings.json` and configure your connection strings:

```json
{
  "ConnectionStrings": {
    "Default": "Server=YOUR_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True"
  },
  "DevHost": {
    "EnableSqlLogging": false
  }
}
```

**Connection Strings:**
- `Default`: Database where your plugin tables live (Items, Products, etc.)

**Configuration:**
- `EnableSqlLogging`: If `true`, logs all SQL queries with parameters and execution times (Development only)

2. Create the tables in the database (see [Database Setup](#database-setup))

### Run

```bash
# Start development server
npm run dev

# Or with hot reload
npm run watch
```

The server starts at **http://localhost:5001** with Swagger UI at the root.

### DevHost Features

- ✅ Swagger UI for testing endpoints
- ✅ Uses the same plugin logic as in production
- ✅ Hot reload with `npm run watch`
- ✅ Configurable connection string
- ✅ **Real JWT token authentication** from Dashboard
- ✅ Automatic transactions (POST/PUT/PATCH/DELETE with automatic rollback on errors)
- ✅ **[NEW]** Debug with VS Code (F5) with working breakpoints
- ✅ **[NEW]** Exception middleware with detailed stack traces in JSON
- ✅ **[NEW]** SQL query logging (queries, parameters, execution times)
- ✅ **[NEW]** 4 debug endpoints (`/api/_debug`, `/health`, `/user`, `/endpoints`)

### Debug and Troubleshooting

#### Debug Endpoints (Development only)

| Endpoint | Description |
|----------|-------------|
| `GET /api/_debug` | Full info: environment, DB, auth, plugin |
| `GET /api/_debug/health` | Quick health check |
| `GET /api/_debug/user` | Authenticated user info (requires auth) |
| `GET /api/_debug/endpoints` | Lists all registered endpoints |

#### SQL Query Logging

Enable SQL logging in `appsettings.json`:

```json
{
  "DevHost": {
    "EnableSqlLogging": true
  }
}
```

Example log:
```
[SQL] 45ms | RowsAffected=1
INSERT INTO Products (Name, Price) VALUES (@p0, @p1)
  @p0 = Coca Cola (String)
  @p1 = 1.5 (Decimal)
```

#### Debugging with VS Code

To debug with VS Code, you need to create the `.vscode/launch.json` file:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "DevHost",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/DevHost/bin/Debug/net9.0/DevHost.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/DevHost",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

Then:
1. Open "Run and Debug" panel (Ctrl+Shift+D)
2. Select "DevHost" profile
3. Press F5 to start with debugger
4. Place breakpoints in .cs files
5. Make a request and debugger will stop

#### Troubleshooting

| Problem | Solution |
|---------|----------|
| No SQL logs | Verify `EnableSqlLogging: true` in appsettings.json |
| Debug endpoints 404 | Verify `ASPNETCORE_ENVIRONMENT=Development` |
| Breakpoints not working | Build in Debug mode, restart VS Code |
| Exception without stack trace | Stack traces only shown in Development |

### DevHost Authentication

DevHost uses **real JWT token authentication** just like in production. You need to obtain a valid token from the BIZUIT Dashboard.

#### How to Get a JWT Token

1. **Login to Dashboard**: Access your BIZUIT Dashboard (e.g., `https://test.bizuit.com/arielschbizuitdashboard`)
2. **Open Developer Tools**: Press F12 in your browser
3. **Go to Storage**:
   - Chrome/Edge: Application → Local Storage
   - Firefox: Storage → Local Storage
4. **Copy the Token**: Find the `adminSessionToken` key and copy its complete value
5. **Use in DevHost**: Paste this token in Swagger UI or your curl commands

#### Using in Swagger UI

1. Open http://localhost:5001 (Swagger UI)
2. Click **"Authorize"** button (padlock icon top-right)
3. Paste your complete JWT token from Dashboard
4. Click **"Authorize"**
5. Protected endpoints will now work with your real roles

#### Using with curl

```bash
# Get your token from Dashboard first
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VybmFtZSI6ImFkbWluIiwiYml6dWl0X3Rva2VuIjoiWk1kdWZX..."

# Use the token in requests
curl http://localhost:5001/api/products \
  -H "Authorization: Bearer $TOKEN"

# The token contains your actual roles and roleSettings
curl http://localhost:5001/api/me \
  -H "Authorization: Bearer $TOKEN"
```

#### Benefits of Using Real Tokens

✅ **Real Data**: Test with your exact roles and roleSettings from production
✅ **Realistic**: Behavior is identical to Backend Host in production
✅ **No Mocks**: No hardcoded data, everything comes from the DB
✅ **Accurate Debugging**: See exactly how your plugin will behave with real users

⚠️ **Important**: Tokens have expiration. If your token expires, you need to get a new one from Dashboard.

## Database Setup

Before activating the plugin, create the tables in SQL Server:

```bash
# Option 1: Consolidated script (recommended)
sqlcmd -S <server> -d <database> -U <user> -P <password> \
  -i database/setup-database.sql

# Option 2: Individual scripts
sqlcmd -S <server> -d <database> -U <user> -P <password> \
  -i database/001_CreateItemsTable.sql
sqlcmd -S <server> -d <database> -U <user> -P <password> \
  -i database/002_CreateProductsTable.sql
sqlcmd -S <server> -d <database> -U <user> -P <password> \
  -i database/003_CreateAuditLogsTable.sql
```

Scripts are idempotent (`IF NOT EXISTS`), they can be executed multiple times without errors.

## Create New Feature

The interactive script creates the complete structure:

```bash
npm run new-feature
```

### Script Questions

1. **Feature name** (PascalCase, e.g., Products, Orders, Customers)
2. **Authentication type:**
   - Public (no auth)
   - Protected (requires login)
   - Admin only (requires admin role)
3. **Transaction handling:**
   - Automatic (POST/PUT/DELETE with transaction)
   - No transaction (fire-and-forget, better performance)

### Generated Files

```
Features/MyFeature/
├── Models/MyFeature.cs           # Model + DTOs
├── MyFeatureRepository.cs        # Queries with SafeQueryBuilder
├── MyFeatureService.cs           # Business logic (if using transactions)
└── MyFeatureEndpoints.cs         # HTTP endpoints
```

### Register the Feature

After creating, register in `MyPlugin.cs`:

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // ... existing code ...

    // New feature
    services.AddScoped<MyFeatureService>();      // Only if using transactions
    services.AddScoped<MyFeatureRepository>();
}

public void ConfigureEndpoints(IPluginEndpointBuilder endpoints)
{
    // ... existing code ...

    MyFeatureEndpoints.Map(endpoints);
}
```

## SafeQueryBuilder - Complete Guide

**SQL Injection is IMPOSSIBLE** using SafeQueryBuilder. All values are automatically parameterized.

### Basic Query

```csharp
public class ProductsRepository : SafeRepository<Product>
{
    protected override string TableName => "Products";

    // Get all
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await ExecuteAsync(Query());
    }

    // Get by ID
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await ExecuteSingleAsync(
            Query().WhereEquals("ProductId", id));
    }
}
```

### Available Filters

```csharp
var query = Query();

// Equality
query.WhereEquals("Status", "Active");

// LIKE (partial search)
query.WhereLike("Name", searchTerm);  // WHERE Name LIKE @p0

// Comparisons
query.WhereGreaterThan("Price", 100);
query.WhereGreaterOrEqual("Stock", 10);
query.WhereLessThan("Discount", 50);
query.WhereLessOrEqual("Quantity", 5);

// IN (multiple values)
query.WhereIn("Category", new[] { "Electronics", "Furniture" });

// Ordering
query.OrderBy("Name");
query.OrderByDescending("CreatedAt");

// Pagination
query.Skip(20).Take(10);  // Page 3 with 10 items per page
```

### Insert / Update / Delete

```csharp
// INSERT
var insert = Insert()
    .Set("Name", request.Name)
    .Set("Price", request.Price)
    .Set("CreatedAt", DateTime.UtcNow);

var newId = await ExecuteWithIdentityAsync(insert);

// UPDATE
var update = Update()
    .Set("Name", request.Name)
    .Set("Price", request.Price)
    .Set("UpdatedAt", DateTime.UtcNow)
    .WhereEquals("ProductId", id);

var rowsAffected = await ExecuteAsync(update);

// DELETE
var delete = Delete()
    .WhereEquals("ProductId", id);

var rowsAffected = await ExecuteAsync(delete);
```

## Authentication and Authorization

### Public Endpoint (no auth)

```csharp
public static void Map(IPluginEndpointBuilder endpoints)
{
    endpoints.MapGet("items", GetAll);           // Anyone can access
    endpoints.MapGet("items/{id:int}", GetById);
}
```

### Query Parameters for Swagger

To properly display query parameters in Swagger/OpenAPI, use the `[FromQuery]` attribute:

```csharp
using Microsoft.AspNetCore.Mvc;

private static async Task<IResult> Search(
    ItemsService service,
    [FromQuery] string? name = null,
    [FromQuery] decimal? minPrice = null,
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null)
{
    var items = await service.SearchAsync(name, minPrice, fromDate, toDate);
    return Results.Ok(items);
}
```

**Important:** The Backend Host automatically detects parameters with `[FromQuery]` and includes them in the plugin's OpenAPI documentation.

### Protected Endpoint (requires login)

```csharp
public static void Map(IPluginEndpointBuilder endpoints)
{
    // Public read
    endpoints.MapGet("products", GetAll);

    // Protected write
    endpoints.MapPost("products", Create)
        .RequireAuthorization();

    endpoints.MapPut("products/{id:int}", Update)
        .RequireAuthorization();
}
```

### Role-Specific Endpoint

```csharp
public static void Map(IPluginEndpointBuilder endpoints)
{
    // Only administrators can delete
    endpoints.MapDelete("products/{id:int}", Delete)
        .RequireAuthorization("admin");
}
```

### Access Authenticated User

```csharp
private static async Task<IResult> Create(
    CreateProductRequest request,
    ProductsService service,
    BizuitUserContext user)  // Automatically injected
{
    // User information
    var username = user.Username;
    var tenantId = user.TenantId;
    var roles = user.Roles;  // List of roles
    var isAdmin = user.Roles.Contains("admin");

    // Save who created the record
    var id = await service.CreateAsync(request, username);
    return Results.Created($"/products/{id}", new { id });
}
```

### Access Role Properties (RoleSettings)

**RoleSettings** are business properties defined per role in the Dashboard's `UserRoleSettings` table.
They allow filtering data or applying logic based on user-specific configurations.

**Example:** A salesperson has role "Vendor" with setting `Producto=COCACOLA`, restricting what data they can see.

```csharp
private static async Task<IResult> GetMyData(
    BizuitUserContext user,
    MyService service)
{
    // ===== BASIC PROPERTIES =====
    var username = user.Username;           // Username
    var tenantId = user.TenantId;           // Tenant (multi-tenant)
    var roles = user.Roles;                 // List of roles: ["Admin", "Vendor"]
    var expiresAt = user.ExpiresAt;         // Token expiration

    // ===== ROLE SETTINGS =====
    // Each setting has: Role (role name), Name (setting name), Value (setting value)
    // Example: { Role: "Vendor", Name: "Producto", Value: "COCACOLA" }

    // Get ALL values for a specific setting (across all roles)
    var productos = user.GetSettingValues("Producto");
    // If user has roles "Vendor A" (Producto=COCACOLA) and "Vendor B" (Producto=PEPSI)
    // Result: ["COCACOLA", "PEPSI"]

    // Check if user has a specific value in ANY role
    if (user.HasSettingValue("Producto", "COCACOLA"))
    {
        // User has access to COCACOLA products
    }

    // Get setting value for a SPECIFIC role
    var productoAdmin = user.GetSettingValue("Administrators", "Producto");
    // Result: "COCACOLA" or null if not exists

    // Get ALL settings for a specific role
    var settingsAdmin = user.GetRoleSettings("Administrators");
    // Result: IEnumerable<RoleSetting> with all settings for the role

    // ===== ROLE HELPERS =====
    var isAdmin = user.HasRole("Administrators");
    var isGestorOrSuper = user.HasAnyRole("Gestores", "Supervisores");
    var isFullAdmin = user.HasAllRoles("Administrators", "BizuitAdmins");

    // ===== PRACTICAL USE: Filter data by settings =====
    var allowedProducts = user.GetSettingValues("Producto").ToList();
    var data = await service.GetDataFilteredByProducts(allowedProducts);

    return Results.Ok(data);
}
```

**Example response from `/items/my-info` endpoint:**

```json
{
  "username": "admin",
  "tenantId": "default",
  "isAuthenticated": true,
  "expiresAt": "2025-01-15T18:00:00Z",
  "roles": ["Administrators", "Gestores"],
  "roleSettings": [
    { "role": "Administrators", "name": "Producto", "value": "COCACOLA" },
    { "role": "Gestores", "name": "Producto", "value": "PEPSI" },
    { "role": "Gestores", "name": "Region", "value": "NORTE" }
  ],
  "hasAdminRole": true,
  "hasAnyGestorRole": true,
  "allProductos": ["COCACOLA", "PEPSI"],
  "hasCocacola": true
}
```

### Helper Pattern: Get Required Value from RoleSettings

When an endpoint **requires** a specific RoleSetting value (e.g., a VendorId or GestorId that MUST exist), use this tuple pattern to extract the value with proper error handling:

```csharp
/// <summary>
/// Gets a required VendorId from the user's RoleSettings.
/// Returns an error result if the user doesn't have VendorId configured.
/// </summary>
private static (int VendorId, IResult? Error) GetVendorIdFromUser(BizuitUserContext user)
{
    // Try to get VendorId from a specific role first
    var vendorIdValue = user.GetSettingValue("Vendors", "VendorId");

    // If not found, try to get from any role that has VendorId
    if (string.IsNullOrEmpty(vendorIdValue))
    {
        vendorIdValue = user.GetSettingValues("VendorId").FirstOrDefault();
    }

    if (string.IsNullOrEmpty(vendorIdValue))
    {
        return (0, Results.BadRequest(new
        {
            error = "User does not have VendorId configured in RoleSettings",
            detail = "The user must have the 'VendorId' setting configured",
            username = user.Username,
            roles = user.Roles,
            roleSettings = user.RoleSettings.Select(s => new { s.Role, s.Name, s.Value })
        }));
    }

    if (!int.TryParse(vendorIdValue, out var vendorId))
    {
        return (0, Results.BadRequest(new
        {
            error = "VendorId value is not a valid integer",
            detail = $"The VendorId setting value '{vendorIdValue}' cannot be converted to an integer"
        }));
    }

    return (vendorId, null);
}

// Usage in endpoint:
private static async Task<IResult> Create(
    CreateOrderRequest request,
    OrdersService service,
    BizuitUserContext user)
{
    // Extract required VendorId from RoleSettings
    var vendorIdResult = GetVendorIdFromUser(user);
    if (vendorIdResult.Error != null)
    {
        return vendorIdResult.Error;
    }

    // Use the extracted value
    var id = await service.CreateAsync(request, vendorIdResult.VendorId);
    return Results.Created($"/orders/{id}", new { id });
}
```

**Benefits of this pattern:**
- ✅ Clear error when configuration is missing
- ✅ Validates data type (int in this case)
- ✅ Easy debugging: includes username, roles and roleSettings in error
- ✅ Reusable: you can create similar helpers for other settings (GestorId, ProductoId, etc.)

## Transactions

### Automatic Behavior

By default, Backend Host handles transactions automatically:

| HTTP Method | Transaction |
|-------------|-------------|
| GET         | No          |
| HEAD        | No          |
| OPTIONS     | No          |
| POST        | Yes         |
| PUT         | Yes         |
| PATCH       | Yes         |
| DELETE      | Yes         |

**You don't need to do anything** - transactions are created and committed automatically. If there's an error, rollback happens.

### Opt-Out: No Transaction

For special cases where you DON'T want a transaction (better performance):

```csharp
public static void Map(IPluginEndpointBuilder endpoints)
{
    // Logging: fire-and-forget, should persist even if something else fails
    endpoints.MapPost("audit-logs", Create)
        .NoTransaction();
}
```

**Use cases for `.NoTransaction()`:**
- Logging/auditing (should persist even if main operation fails)
- High-frequency operations (better performance)
- Operations that don't modify critical data

## Unit Tests

The project includes tests with xUnit:

```bash
# Run all tests
dotnet test

# With more detail
dotnet test --verbosity normal

# Only tests from a specific file
dotnet test --filter "FullyQualifiedName~ProductsServiceTests"
```

### Test Structure

```csharp
public class ProductsServiceTests
{
    [Fact]
    public void CreateRequest_WithEmptyName_ShouldFail()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "",  // Invalid
            SKU = "PROD-001",
            Price = 100
        };

        // Act & Assert
        Assert.True(string.IsNullOrWhiteSpace(request.Name));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CreateRequest_WithNegativePrice_ShouldFail(decimal price)
    {
        var request = new CreateProductRequest
        {
            Name = "Test",
            SKU = "TEST",
            Price = price
        };

        Assert.True(request.Price < 0);
    }
}
```

## Deployment

### 1. Package

```bash
npm run package
# Generates: dist/myplugin.1.0.0.zip
```

### 2. Upload to Backend Host

**Option A: Admin UI**
1. Go to Admin Panel → Settings → Plugins
2. Click "Upload Plugin"
3. Select the ZIP
4. Configure connection string
5. Activate

**Option B: API**

```bash
# Upload
curl -X POST https://host/api/admin/plugins/upload \
     -H "Authorization: Bearer $TOKEN" \
     -F "name=myplugin" \
     -F "version=1.0.0" \
     -F "file=@dist/myplugin.1.0.0.zip"

# Configure connection string
curl -X PUT https://host/api/admin/plugins/{pluginId}/connection-string \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"connectionString": "Server=...;Database=...;..."}'

# Activate
curl -X POST https://host/api/admin/plugins/{pluginId}/activate \
     -H "Authorization: Bearer $TOKEN"
```

### 3. Test Endpoints

Once active:

```bash
# Items (public)
curl https://host/api/plugins/myplugin/items
curl https://host/api/plugins/myplugin/items/1

# Products (requires auth)
curl https://host/api/plugins/myplugin/products \
     -H "Authorization: Bearer $TOKEN"

# AuditLogs (POST without transaction)
curl -X POST https://host/api/plugins/myplugin/audit-logs \
     -H "Content-Type: application/json" \
     -d '{"action": "TEST", "entityType": "Test"}'
```

### Plugin Swagger

Each active plugin has its own Swagger documentation:

```
https://host/api/plugins/myplugin/swagger
```

## Plugin Configuration

The plugin receives configuration from Backend Host:

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Connection string (configured in admin panel)
    var connectionString = configuration.GetConnectionString("Default");

    // Custom configuration (key-value from admin)
    var apiKey = configuration["MyApiKey"];
    var maxItems = configuration.GetValue<int>("MaxItemsPerPage", 100);
}
```

### ⚠️ CRITICAL: IConfiguration is NOT available via DI in Backend Host

**The Backend Host does NOT register `IConfiguration` in the DI container.** If you create a service that depends on `IConfiguration`, it will fail at runtime:

```
System.InvalidOperationException: Unable to resolve service for type
'Microsoft.Extensions.Configuration.IConfiguration' while attempting to activate 'MyService'.
```

**❌ DON'T do this** - will fail in Backend Host:
```csharp
// This class depends on IConfiguration via constructor injection
public class MyService
{
    public MyService(IConfiguration configuration) // ❌ Will fail!
    {
        _connectionString = configuration.GetConnectionString("Default");
    }
}

// In ConfigureServices:
services.AddSingleton<MyService>();  // ❌ DI can't resolve IConfiguration
```

**✅ DO this instead** - use factory delegate:
```csharp
// Same class, same constructor
public class MyService
{
    public MyService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default");
    }
}

// In ConfigureServices - use factory to pass configuration directly:
services.AddSingleton<MyService>(sp =>
{
    return new MyService(configuration);  // ✅ Pass configuration from ConfigureServices scope
});
```

**Why this works**: The `configuration` parameter is available in the `ConfigureServices` method scope. By using a factory delegate, you pass it directly instead of relying on DI resolution.

## System Configuration

Plugins have access to system-level configuration automatically injected by the Backend Host. These configs are **read-only** and managed by system administrators in the SystemConfiguration table.

### Available System Configs

| Key | Description | Example Value |
|-----|-------------|---------------|
| `System:DashboardApiUrl` | Dashboard API base URL | `https://YOUR_DASHBOARD_URL/YOUR_TENANT_API/api` |
| `System:TenantId` | Current tenant identifier | `default`, `tenant1`, `tenant2` |

### Accessing System Configuration

**During Service Registration (ConfigureServices):**

```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("Default");

    // Get system configuration injected by Backend Host
    var dashboardApiUrl = configuration["System:DashboardApiUrl"];
    var tenantId = configuration["System:TenantId"];

    Console.WriteLine($"[MyPlugin] Loaded for tenant '{tenantId}' with Dashboard API: {dashboardApiUrl}");

    // Optional: Register HttpClient for Dashboard API calls
    if (!string.IsNullOrEmpty(dashboardApiUrl))
    {
        services.AddHttpClient("DashboardClient", client =>
        {
            client.BaseAddress = new Uri(dashboardApiUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
    }

    // Register your services
    services.AddScoped<ItemsService>();
    services.AddScoped<ItemsRepository>();
}
```

**During Request Handling (in services):**

```csharp
public class ItemsService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ItemsRepository _repository;

    public ItemsService(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ItemsRepository repository)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _repository = repository;
    }

    // Example: Call Dashboard API from plugin
    public async Task<object> CallDashboardAsync(BizuitUserContext user, string endpoint)
    {
        var dashboardApiUrl = _config["System:DashboardApiUrl"];
        var client = _httpClientFactory.CreateClient("DashboardClient");

        // Use user's RawToken for authentication
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {user.RawToken}");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", user.TenantId);

        var response = await client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<object>();
    }
}
```

### Multi-Tenant Considerations

- The `System:DashboardApiUrl` is loaded **once** when the plugin is loaded (for the `default` tenant)
- For true multi-tenant support where each tenant has different Dashboard URLs, this will be enhanced in a future version
- The `BizuitUserContext.TenantId` property always contains the current request's tenant ID
- The `BizuitUserContext.RawToken` can be used to authenticate with the Dashboard API

### Use Cases for Dashboard API Integration

Common scenarios where you might call the Dashboard API from a plugin:

1. **Fetch User Details**: Get full user profile information
2. **Access BPMN Processes**: Trigger or query business processes
3. **Retrieve Companies/Roles**: Get organization structure data
4. **Send Notifications**: Trigger emails or alerts through Dashboard
5. **Validate Permissions**: Check user permissions in Dashboard context

**Example - Get User Details:**

```csharp
public async Task<UserDetails> GetUserDetailsAsync(BizuitUserContext user)
{
    var endpoint = $"/users/{user.Username}";
    return await CallDashboardApiAsync(user, endpoint);
}
```

**Example - Trigger BPMN Process:**

```csharp
public async Task TriggerProcessAsync(BizuitUserContext user, string processName, object parameters)
{
    var endpoint = $"/bpmn/processes/{processName}/start";
    var client = _httpClientFactory.CreateClient("DashboardClient");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {user.RawToken}");

    var response = await client.PostAsJsonAsync(endpoint, parameters);
    response.EnsureSuccessStatusCode();
}
```

## Custom Plugin Settings

Configure runtime settings via Admin UI without hardcoding values in your plugin code.

### What Are Plugin Settings?

Plugin Settings are **custom key-value configurations** stored in the `BackendPluginConfig` database table. They allow you to store values that shouldn't be hardcoded:

- 🌐 External service URLs (Azure Storage, S3, webhooks)
- 🔑 API keys and credentials (stored encrypted)
- 🚩 Feature flags (enable/disable features)
- ⚙️ Configurable limits (max retries, timeouts, page sizes)
- 🏢 Environment-specific values (different per deployment)

### When to Use Settings vs Hardcoded Values

**✅ Use Settings for:**
- External URLs that vary per environment
- API keys and credentials
- Business rules that may change (thresholds, limits)
- Feature toggles
- Values that differ between test/production

**❌ Keep Hardcoded in Code:**
- Business logic and algorithms
- Database table names
- Endpoint paths
- Fixed constants that never change

### Accessing Settings in Plugin Code

Settings are automatically loaded into `IConfiguration` when the plugin starts. Access them using standard .NET configuration patterns:

**Example: Azure Storage for Audio Files**

```csharp
using Microsoft.Extensions.Configuration;

public class AudioService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AudioService> _logger;

    public AudioService(IConfiguration config, ILogger<AudioService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<string> UploadAudioAsync(byte[] audioData, string fileName)
    {
        // Access custom settings
        var storageUrl = _config["AzureStorageUrl"];
        var storageKey = _config["AzureStorageKey"];
        var containerName = _config.GetValue("ContainerName", "audios");  // With default

        // Validate required settings
        if (string.IsNullOrEmpty(storageUrl))
        {
            _logger.LogError("AzureStorageUrl not configured");
            throw new InvalidOperationException(
                "AzureStorageUrl not configured. " +
                "Please configure it in Admin UI: /admin/settings/plugins");
        }

        // Use settings
        var blobUrl = $"{storageUrl}/{containerName}/{fileName}";
        // ... upload logic
        return blobUrl;
    }

    public int GetMaxRetries()
    {
        // Access with default value
        return _config.GetValue<int>("MaxRetries", 3);
    }

    public bool IsDebugEnabled()
    {
        // Access boolean setting
        return _config.GetValue<bool>("EnableDebugLogs", false);
    }
}
```

### Configuring Settings in Admin UI

**Production - Via Admin Panel:**

1. Navigate to `/admin/settings/plugins`
2. Click on your plugin
3. Scroll to "Configuration" section
4. Click "Add Config" button
5. Enter your settings:

| Key | Value | Encrypted |
|-----|-------|-----------|
| `AzureStorageUrl` | `https://YOUR_ACCOUNT.blob.core.windows.net/audios` | ☐ No |
| `AzureStorageKey` | `your-secret-access-key` | ☑️ **Yes** |
| `ContainerName` | `audios` | ☐ No |
| `MaxRetries` | `5` | ☐ No |

6. Click "Save Config"
7. Click "Reload Plugin" to apply changes

**⚠️ Important:** Settings marked as "Encrypted" are stored encrypted in the database using AES encryption. Always encrypt:
- API keys
- Passwords
- Access tokens
- Any sensitive credentials

### Local Development - DevHost Configuration

For local testing, configure settings in `src/DevHost/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=MyPluginDB;...",
    "Dashboard": "Server=localhost;Database=DashboardDB;..."
  },
  "System": {
    "DashboardApiUrl": "https://YOUR_DASHBOARD_URL/api",
    "TenantId": "default"
  },

  // Custom Plugin Settings (for local testing)
  "AzureStorageUrl": "https://YOUR_ACCOUNT.blob.core.windows.net",
  "AzureStorageKey": "YOUR_LOCAL_KEY",
  "ContainerName": "test-audios",
  "MaxRetries": "3",
  "EnableDebugLogs": "true"
}
```

**📄 Example file:** See `src/DevHost/appsettings.Development.json.example` for a complete template.

**🔒 Security:** Never commit `appsettings.Development.json` - it's already in `.gitignore`.

### Common Settings Examples

| Setting Key | Example Value | Encrypted | Use Case |
|-------------|---------------|-----------|----------|
| `AzureStorageUrl` | `https://storage.blob.core.windows.net/files` | No | External file storage URL |
| `AzureStorageKey` | `abc123...` | **Yes** | Storage account access key |
| `WebhookUrl` | `https://api.example.com/webhook` | No | Webhook callback endpoint |
| `ApiKey` | `Bearer sk-xyz...` | **Yes** | External API authentication |
| `MaxRetries` | `5` | No | Retry limit for external API calls |
| `TimeoutSeconds` | `30` | No | HTTP client timeout |
| `EnableDebugLogs` | `true` | No | Feature flag for debug logging |
| `S3BucketName` | `my-bucket` | No | AWS S3 bucket name |
| `SmtpHost` | `smtp.example.com` | No | Email server hostname |
| `SmtpPassword` | `password123` | **Yes** | SMTP authentication |

### Best Practices

**1. Always Validate Settings:**
```csharp
var url = _config["ExternalApiUrl"];
if (string.IsNullOrEmpty(url))
{
    _logger.LogError("ExternalApiUrl not configured");
    throw new InvalidOperationException("ExternalApiUrl must be configured");
}
```

**2. Use Default Values for Optional Settings:**
```csharp
var maxRetries = _config.GetValue<int>("MaxRetries", 3);
var timeout = _config.GetValue<TimeSpan>("Timeout", TimeSpan.FromSeconds(30));
var enabled = _config.GetValue<bool>("EnableFeature", false);
```

**3. Log Missing Required Settings:**
```csharp
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    var apiKey = configuration["ExternalApiKey"];
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.WriteLine("⚠️  WARNING: ExternalApiKey not configured - external integration disabled");
    }
}
```

**4. Document Required Settings:**
Always document in your plugin's README which settings are required vs optional, with examples.

**5. Use Encrypted for Sensitive Data:**
- ✅ API keys → Encrypted
- ✅ Passwords → Encrypted
- ✅ Access tokens → Encrypted
- ❌ Public URLs → NOT encrypted
- ❌ Feature flags → NOT encrypted

### Settings vs System Configuration

**System Configuration (`System:*`):**
- Injected automatically by Backend Host
- Read-only for your plugin
- Same for all plugins (DashboardApiUrl, TenantId)

**Custom Settings (your own keys):**
- Defined by you (the plugin developer)
- Configured by admin in UI
- Specific to your plugin's needs

```csharp
// System config (read-only, injected by host)
var dashboardUrl = configuration["System:DashboardApiUrl"];

// Custom settings (configurable by admin)
var storageUrl = configuration["AzureStorageUrl"];
```

## Troubleshooting

### Error: "Table does not exist"

Run the SQL scripts from `database/` before activating the plugin.

### Error: "Connection string not configured"

Configure the connection string in Admin Panel → Plugins → [your plugin] → Connection String.

### Error: "Plugin failed to load"

1. Verify the ZIP contains `plugin.json` at the root
2. Verify the DLL is in the correct folder inside the ZIP
3. Check Backend Host logs

### Error: "Unauthorized" on protected endpoints

Verify the JWT token includes the required roles.

---

## License

MIT
