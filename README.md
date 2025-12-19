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

# 3. Renombrar el plugin
#    - Editar plugin.json (name, displayName, description)
#    - Renombrar carpeta src/MyPlugin → src/MiPlugin
#    - Actualizar namespace en todos los archivos .cs

# 4. Crear una nueva feature (interactivo)
npm run new-feature

# 5. Compilar
dotnet build

# 6. Empaquetar para deploy
npm run package
# Genera: dist/myplugin.1.0.0.zip
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
    "Default": "Server=TU_SERVIDOR;Database=TU_DATABASE;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True",
    "Dashboard": "Server=TU_SERVIDOR;Database=TU_DASHBOARD_DATABASE;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True"
  },
  "DevHost": {
    "UseMockRoleSettings": false
  }
}
```

**Connection Strings:**
- `Default`: Base de datos donde están las tablas de tu plugin (Items, Products, etc.)
- `Dashboard` (opcional): Base de datos del Dashboard de BIZUIT para obtener roles y roleSettings REALES

**Configuración:**
- `UseMockRoleSettings`: Si es `true`, usa datos mock aunque tengas Dashboard connection (útil para testing offline)
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
- ✅ **Autenticación con datos REALES** de Dashboard (roles y roleSettings)
- ✅ Fallback a mock si no hay Dashboard connection
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

#### Dos Modos de Autenticación

**1. Modo REAL (con Dashboard connection):**
- Configurá `ConnectionStrings:Dashboard` apuntando a tu Dashboard DB
- En Swagger, usá cualquier **username válido** como Bearer token (ej: `admin`, `jperez`)
- DevHost consulta la DB y obtiene los roles y roleSettings REALES de ese usuario
- Ideal para probar con datos de producción/staging

**2. Modo MOCK (sin Dashboard connection):**
- Si no configurás `Dashboard` connection string
- Usá tokens predefinidos: `admin`, `gestor`, `user`
- Roles y roleSettings son hardcodeados (ver tabla abajo)
- Ideal para desarrollo rápido sin DB

#### ¿Qué es la Autenticación del DevHost?

**Autenticación del DevHost** = Autenticación **simplificada** para desarrollo local

**El Problema en Producción:**

En producción, el Backend Host usa autenticación JWT real y compleja:
1. Usuario hace login → recibe token JWT complejo (200+ caracteres)
2. Token tiene firma criptográfica, expiración, claims cifrados
3. Servidor valida firma, verifica expiración, extrae roles
4. **Requiere infraestructura**: Azure AD, base de usuarios, certificados, etc.

Ejemplo de token JWT real:
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

**La Solución para DevHost:**

Para desarrollo LOCAL, necesitamos algo **simple** pero **realista**:
- Tokens simples de texto plano: `"admin"`, `"gestor"`, `"user"`
- Mapeo directo a roles sin criptografía
- Sin necesidad de Azure AD o infraestructura externa
- Se comporta **igual** que autenticación real desde el punto de vista del plugin

#### Cómo Funciona

El `MockBearerAuthenticationHandler` simula el proceso completo:

```csharp
// 1. Recibe el header Authorization
Authorization: Bearer admin

// 2. Extrae el token simple
token = "admin"

// 3. Mapea a roles según tabla hardcodeada
"admin"  → Roles: ["Administrators", "BizuitAdmins"]
"gestor" → Roles: ["Gestores"]
"user"   → Roles: [] (sin roles, solo autenticado)

// 4. Crea Claims (como lo haría JWT real)
Claims:
  - NameIdentifier: "admin-001"
  - Name: "admin-user"
  - Role: "Administrators"
  - Role: "BizuitAdmins"

// 5. BizuitUserContext se puebla desde estos Claims
user.Username = "admin-user"
user.Roles = ["Administrators", "BizuitAdmins"]
user.IsAuthenticated = true
```

#### Tokens Disponibles

| Token    | Usuario       | Roles                      | Descripción |
|----------|---------------|----------------------------|-------------|
| `admin`  | `admin-user`  | Administrators, BizuitAdmins | Acceso completo a todas las operaciones |
| `gestor` | `gestor-user` | Gestores                   | Operaciones de gestión, sin eliminación |
| `user`   | `basic-user`  | *(ninguno)*                | Solo autenticado, sin permisos especiales |

**Personalización de Roles (Modo MOCK):**

En modo MOCK (sin Dashboard connection), los roles están hardcodeados en `DevHost/Program.cs` dentro del `MockBearerAuthenticationHandler`. Si necesitás modificar los roles mock, buscá el método `GetRoles` en ese archivo.

#### Uso en Swagger UI

1. Abrir http://localhost:5001 (Swagger UI)
2. Click en botón **"Authorize"** (candado arriba a la derecha)
3. Ingresar uno de los tokens: `admin`, `gestor`, o `user`
4. Click **"Authorize"**
5. Los endpoints protegidos ahora funcionarán según tus roles

#### Uso con curl

```bash
# Sin autenticación → 401 Unauthorized
curl http://localhost:5001/api/products

# Con token "admin" → 200 OK (tiene rol "Administrators")
curl http://localhost:5001/api/products \
  -H "Authorization: Bearer admin"

# Con token "user" → 403 Forbidden (no tiene roles necesarios)
curl http://localhost:5001/api/products \
  -H "Authorization: Bearer user"

# Con token "gestor" → 200 OK si el endpoint permite "Gestores"
curl http://localhost:5001/api/products \
  -H "Authorization: Bearer gestor"
```

#### Por Qué es Útil

✅ **Fácil de usar**: Solo escribir "admin" en lugar de token JWT de 200 caracteres
✅ **Realista**: Se comporta exactamente igual que JWT real
✅ **Sin infraestructura**: No necesita Azure AD, base de datos de usuarios, certificados
✅ **Múltiples roles**: Probar diferentes escenarios (admin, gestor, user)
✅ **Desarrollo rápido**: Cambiar de usuario solo cambiando el token
✅ **Debugging fácil**: Ver exactamente qué roles tiene cada usuario

⚠️ **Solo para desarrollo**: En producción, el Backend Host usa JWT real con validación criptográfica

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
    "Default": "Server=YOUR_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True",
    "Dashboard": "Server=YOUR_SERVER;Database=YOUR_DASHBOARD_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True"
  },
  "DevHost": {
    "UseMockRoleSettings": false
  }
}
```

**Connection Strings:**
- `Default`: Database where your plugin tables live (Items, Products, etc.)
- `Dashboard` (optional): BIZUIT Dashboard database for REAL roles and roleSettings

**Configuration:**
- `UseMockRoleSettings`: If `true`, uses mock data even with Dashboard connection (useful for offline testing)
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
- ✅ **REAL authentication data** from Dashboard (roles and roleSettings)
- ✅ Fallback to mock if no Dashboard connection
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

#### Two Authentication Modes

**1. REAL Mode (with Dashboard connection):**
- Configure `ConnectionStrings:Dashboard` pointing to your Dashboard DB
- In Swagger, use any **valid username** as Bearer token (e.g., `admin`, `jperez`)
- DevHost queries the DB and gets the REAL roles and roleSettings for that user
- Ideal for testing with production/staging data

**2. MOCK Mode (without Dashboard connection):**
- If you don't configure `Dashboard` connection string
- Use predefined tokens: `admin`, `gestor`, `user`
- Roles and roleSettings are hardcoded (see table below)
- Ideal for fast development without DB

#### What is DevHost Authentication?

**Mock Authentication** = **Fake/simulated** authentication for development only

**The Problem in Production:**

In production, Backend Host uses real, complex JWT authentication:
1. User logs in → receives complex JWT token (200+ characters)
2. Token has cryptographic signature, expiration, encrypted claims
3. Server validates signature, verifies expiration, extracts roles
4. **Requires infrastructure**: Azure AD, user database, certificates, etc.

Example of real JWT token:
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

**The Solution for DevHost:**

For LOCAL development, we need something **simple** but **realistic**:
- Simple plain-text tokens: `"admin"`, `"gestor"`, `"user"`
- Direct role mapping without cryptography
- No need for Azure AD or external infrastructure
- Behaves **exactly** like real auth from the plugin's perspective

#### How It Works

The `MockBearerAuthenticationHandler` simulates the complete process:

```csharp
// 1. Receives Authorization header
Authorization: Bearer admin

// 2. Extracts simple token
token = "admin"

// 3. Maps to roles via hardcoded table
"admin"  → Roles: ["Administrators", "BizuitAdmins"]
"gestor" → Roles: ["Gestores"]
"user"   → Roles: [] (no roles, just authenticated)

// 4. Creates Claims (just like real JWT would)
Claims:
  - NameIdentifier: "admin-001"
  - Name: "admin-user"
  - Role: "Administrators"
  - Role: "BizuitAdmins"

// 5. BizuitUserContext is populated from these Claims
user.Username = "admin-user"
user.Roles = ["Administrators", "BizuitAdmins"]
user.IsAuthenticated = true
```

#### Available Tokens

| Token    | Username      | Roles                      | Description |
|----------|---------------|----------------------------|-------------|
| `admin`  | `admin-user`  | Administrators, BizuitAdmins | Full access to all operations |
| `gestor` | `gestor-user` | Gestores                   | Management operations, no delete |
| `user`   | `basic-user`  | *(none)*                   | Just authenticated, no special permissions |

**Role Customization (MOCK Mode):**

In MOCK mode (without Dashboard connection), roles are hardcoded in `DevHost/Program.cs` inside the `MockBearerAuthenticationHandler`. If you need to modify mock roles, look for the `GetRoles` method in that file.

#### Using in Swagger UI

1. Open http://localhost:5001 (Swagger UI)
2. Click **"Authorize"** button (padlock icon top-right)
3. Enter one of the tokens: `admin`, `gestor`, or `user`
4. Click **"Authorize"**
5. Protected endpoints will now work according to your roles

#### Using with curl

```bash
# No authentication → 401 Unauthorized
curl http://localhost:5001/api/products

# With "admin" token → 200 OK (has "Administrators" role)
curl http://localhost:5001/api/products \
  -H "Authorization: Bearer admin"

# With "user" token → 403 Forbidden (no required roles)
curl http://localhost:5001/api/products \
  -H "Authorization: Bearer user"

# With "gestor" token → 200 OK if endpoint allows "Gestores"
curl http://localhost:5001/api/products \
  -H "Authorization: Bearer gestor"
```

#### Why It's Useful

✅ **Easy to use**: Just type "admin" instead of 200-character JWT token
✅ **Realistic**: Behaves exactly like real JWT
✅ **No infrastructure**: No Azure AD, user database, or certificates needed
✅ **Multiple roles**: Test different scenarios (admin, gestor, user)
✅ **Fast development**: Switch users by just changing token
✅ **Easy debugging**: See exactly what roles each user has

⚠️ **Development only**: In production, Backend Host uses real JWT with cryptographic validation

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
