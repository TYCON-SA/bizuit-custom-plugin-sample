#!/usr/bin/env node

/**
 * Create a new feature (vertical slice) in the plugin.
 *
 * Interactive mode: npm run new-feature
 * Direct mode:      npm run new-feature Products
 *
 * Creates:
 *   src/MyPlugin/Features/FeatureName/
 *   ├── Models/
 *   │   └── FeatureName.cs
 *   ├── FeatureNameRepository.cs
 *   ├── FeatureNameService.cs
 *   └── FeatureNameEndpoints.cs
 */

import { mkdirSync, writeFileSync, existsSync } from 'fs';
import { join } from 'path';
import { createInterface } from 'readline';

const ROOT_DIR = process.cwd();
const FEATURES_DIR = join(ROOT_DIR, 'src/MyPlugin/Features');

// ============================================
// Interactive CLI helpers
// ============================================

const rl = createInterface({
    input: process.stdin,
    output: process.stdout
});

function ask(question) {
    return new Promise(resolve => {
        rl.question(question, answer => resolve(answer.trim()));
    });
}

function askChoice(question, options) {
    return new Promise(resolve => {
        console.log(`\n${question}`);
        options.forEach((opt, i) => console.log(`  ${i + 1}. ${opt.label}`));
        rl.question('Select [1-' + options.length + ']: ', answer => {
            const index = parseInt(answer) - 1;
            if (index >= 0 && index < options.length) {
                resolve(options[index].value);
            } else {
                resolve(options[0].value); // Default to first option
            }
        });
    });
}

// ============================================
// Main
// ============================================

async function main() {
    console.log('\n🚀 BIZUIT Plugin - Create New Feature\n');
    console.log('═'.repeat(45));

    let featureName = process.argv[2];
    let authType = 'public';
    let useTransaction = true;

    // Interactive mode if no feature name provided
    if (!featureName) {
        // Question 1: Feature name
        featureName = await ask('\n📝 Feature name (PascalCase, e.g., Products): ');

        // Question 2: Authentication type
        authType = await askChoice('🔐 Authentication type:', [
            { label: 'Public (no authentication required)', value: 'public' },
            { label: 'Protected (requires login)', value: 'protected' },
            { label: 'Admin only (requires admin role)', value: 'admin' }
        ]);

        // Question 3: Transaction handling
        useTransaction = await askChoice('💾 Transaction handling:', [
            { label: 'Automatic (POST/PUT/DELETE use transactions)', value: true },
            { label: 'No transaction (fire-and-forget, better performance)', value: false }
        ]);
    }

    // Validate name (PascalCase)
    if (!featureName || !/^[A-Z][a-zA-Z0-9]*$/.test(featureName)) {
        console.error('\n❌ Error: Feature name must be PascalCase (e.g., Products, Orders)');
        rl.close();
        process.exit(1);
    }

    const featureDir = join(FEATURES_DIR, featureName);
    const modelsDir = join(featureDir, 'Models');

    if (existsSync(featureDir)) {
        console.error(`\n❌ Error: Feature ${featureName} already exists!`);
        rl.close();
        process.exit(1);
    }

    console.log('\n' + '─'.repeat(45));
    console.log(`📁 Creating feature: ${featureName}`);
    console.log(`   Auth: ${authType}`);
    console.log(`   Transaction: ${useTransaction ? 'automatic' : 'disabled'}`);
    console.log('─'.repeat(45) + '\n');

    // Create directories
    mkdirSync(modelsDir, { recursive: true });

    // Create model
    writeFileSync(join(modelsDir, `${featureName}.cs`), generateModel(featureName));
    console.log(`   ✅ Models/${featureName}.cs`);

    // Create repository
    writeFileSync(join(featureDir, `${featureName}Repository.cs`), generateRepository(featureName));
    console.log(`   ✅ ${featureName}Repository.cs`);

    // Create service (only if using transactions, otherwise skip)
    if (useTransaction) {
        writeFileSync(join(featureDir, `${featureName}Service.cs`), generateService(featureName));
        console.log(`   ✅ ${featureName}Service.cs`);
    }

    // Create endpoints
    writeFileSync(join(featureDir, `${featureName}Endpoints.cs`),
        generateEndpoints(featureName, authType, useTransaction));
    console.log(`   ✅ ${featureName}Endpoints.cs`);

    // Summary
    console.log('\n' + '═'.repeat(45));
    console.log(`✅ Feature ${featureName} created successfully!`);
    console.log('═'.repeat(45));

    // Next steps
    console.log('\n📝 Next steps:\n');
    console.log(`   1. Edit Models/${featureName}.cs to define your entity`);
    console.log('');
    console.log('   2. Register services in MyPlugin.cs:');
    if (useTransaction) {
        console.log(`      services.AddScoped<${featureName}Service>();`);
    }
    console.log(`      services.AddScoped<${featureName}Repository>();`);
    console.log('');
    console.log('   3. Register endpoints in MyPlugin.cs:');
    console.log(`      ${featureName}Endpoints.Map(endpoints);`);
    console.log('');
    console.log('   4. Create database table (see database/ folder for examples)');
    console.log('');
    console.log('   5. Build and test:');
    console.log('      dotnet build');
    console.log('      npm run package');
    console.log('');

    rl.close();
}

// ============================================
// Code generators
// ============================================

function generateModel(name) {
    const singular = name.endsWith('s') ? name.slice(0, -1) : name;
    return `namespace MyPlugin.Features.${name}.Models;

/// <summary>
/// ${singular} entity model.
/// </summary>
public class ${singular}
{
    public int ${singular}Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request to create a ${singular.toLowerCase()}.
/// </summary>
public class Create${singular}Request
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Request to update a ${singular.toLowerCase()}.
/// </summary>
public class Update${singular}Request
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
`;
}

function generateRepository(name) {
    const singular = name.endsWith('s') ? name.slice(0, -1) : name;
    return `using Bizuit.Backend.Core.Database;
using MyPlugin.Features.${name}.Models;

namespace MyPlugin.Features.${name};

/// <summary>
/// Repository for ${name} using SafeQueryBuilder.
/// SQL Injection is IMPOSSIBLE.
/// </summary>
public class ${name}Repository : SafeRepository<Models.${singular}>
{
    protected override string TableName => "${name}";

    public ${name}Repository(IConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    /// <summary>
    /// Search ${name.toLowerCase()} with optional filters.
    /// </summary>
    public async Task<IEnumerable<Models.${singular}>> SearchAsync(string? name, bool? isActive)
    {
        var query = Query();

        if (!string.IsNullOrEmpty(name))
        {
            query.WhereLike("Name", name);
        }

        if (isActive.HasValue)
        {
            query.WhereEquals("IsActive", isActive.Value);
        }

        query.OrderByDescending("CreatedAt");

        return await ExecuteAsync(query);
    }

    /// <summary>
    /// Create a new ${singular.toLowerCase()}.
    /// </summary>
    public async Task<int> CreateAsync(Create${singular}Request request)
    {
        var insert = Insert()
            .Set("Name", request.Name)
            .Set("Description", request.Description)
            .Set("IsActive", true)
            .Set("CreatedAt", DateTime.UtcNow);

        return await ExecuteWithIdentityAsync(insert);
    }

    /// <summary>
    /// Update a ${singular.toLowerCase()}.
    /// </summary>
    public async Task<bool> UpdateAsync(int id, Update${singular}Request request)
    {
        var update = Update()
            .Set("Name", request.Name)
            .Set("Description", request.Description)
            .Set("IsActive", request.IsActive)
            .Set("UpdatedAt", DateTime.UtcNow)
            .WhereEquals("${singular}Id", id);

        var rows = await ExecuteAsync(update);
        return rows > 0;
    }

    /// <summary>
    /// Get ${singular.toLowerCase()} by ID.
    /// </summary>
    public async Task<Models.${singular}?> GetByIdAsync(int id)
    {
        return await ExecuteSingleAsync(
            Query().WhereEquals("${singular}Id", id));
    }

    /// <summary>
    /// Delete ${singular.toLowerCase()} by ID.
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var rows = await ExecuteAsync(
            Delete().WhereEquals("${singular}Id", id));
        return rows > 0;
    }
}
`;
}

function generateService(name) {
    const singular = name.endsWith('s') ? name.slice(0, -1) : name;
    return `using MyPlugin.Features.${name}.Models;

namespace MyPlugin.Features.${name};

/// <summary>
/// Business logic service for ${name}.
/// </summary>
public class ${name}Service
{
    private readonly ${name}Repository _repository;

    public ${name}Service(${name}Repository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Models.${singular}>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<Models.${singular}>> SearchAsync(string? name, bool? isActive)
    {
        return await _repository.SearchAsync(name, isActive);
    }

    public async Task<Models.${singular}?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<int> CreateAsync(Create${singular}Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required");
        }

        return await _repository.CreateAsync(request);
    }

    public async Task<bool> UpdateAsync(int id, Update${singular}Request request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required");
        }

        return await _repository.UpdateAsync(id, request);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }
}
`;
}

function generateEndpoints(name, authType = 'public', useTransaction = true) {
    const singular = name.endsWith('s') ? name.slice(0, -1) : name;
    const route = name.toLowerCase();

    // Determine auth imports and method chains
    const needsAuth = authType !== 'public';
    const needsNoTransaction = !useTransaction;

    let imports = `using Bizuit.Backend.Abstractions;
using Microsoft.AspNetCore.Http;
using MyPlugin.Features.${name}.Models;`;

    if (needsAuth) {
        imports += `\nusing Bizuit.Backend.Core.Auth;`;
    }

    // Generate endpoint registrations based on auth type
    let endpointRegistrations = '';
    let methodParams = '';

    if (authType === 'public') {
        endpointRegistrations = `        // Public endpoints (no authentication required)
        endpoints.MapGet("${route}", GetAll);
        endpoints.MapGet("${route}/search", Search);
        endpoints.MapGet("${route}/{id:int}", GetById);
        endpoints.MapPost("${route}", Create)${needsNoTransaction ? '\n            .NoTransaction()' : ''};
        endpoints.MapPut("${route}/{id:int}", Update)${needsNoTransaction ? '\n            .NoTransaction()' : ''};
        endpoints.MapDelete("${route}/{id:int}", Delete)${needsNoTransaction ? '\n            .NoTransaction()' : ''};`;
    } else if (authType === 'protected') {
        endpointRegistrations = `        // Public read endpoints
        endpoints.MapGet("${route}", GetAll);
        endpoints.MapGet("${route}/search", Search);
        endpoints.MapGet("${route}/{id:int}", GetById);

        // Protected write endpoints (requires authentication)
        endpoints.MapPost("${route}", Create)
            .RequireAuthorization()${needsNoTransaction ? '\n            .NoTransaction()' : ''};
        endpoints.MapPut("${route}/{id:int}", Update)
            .RequireAuthorization()${needsNoTransaction ? '\n            .NoTransaction()' : ''};
        endpoints.MapDelete("${route}/{id:int}", Delete)
            .RequireAuthorization()${needsNoTransaction ? '\n            .NoTransaction()' : ''};`;
        methodParams = ', BizuitUserContext user';
    } else if (authType === 'admin') {
        endpointRegistrations = `        // All endpoints require admin role
        endpoints.MapGet("${route}", GetAll)
            .RequireAuthorization("admin");
        endpoints.MapGet("${route}/search", Search)
            .RequireAuthorization("admin");
        endpoints.MapGet("${route}/{id:int}", GetById)
            .RequireAuthorization("admin");
        endpoints.MapPost("${route}", Create)
            .RequireAuthorization("admin")${needsNoTransaction ? '\n            .NoTransaction()' : ''};
        endpoints.MapPut("${route}/{id:int}", Update)
            .RequireAuthorization("admin")${needsNoTransaction ? '\n            .NoTransaction()' : ''};
        endpoints.MapDelete("${route}/{id:int}", Delete)
            .RequireAuthorization("admin")${needsNoTransaction ? '\n            .NoTransaction()' : ''};`;
        methodParams = ', BizuitUserContext user';
    }

    const serviceOrRepo = useTransaction ? `${name}Service service` : `${name}Repository repository`;
    const serviceOrRepoCall = useTransaction ? 'service' : 'repository';

    return `${imports}

namespace MyPlugin.Features.${name};

/// <summary>
/// Minimal API endpoints for ${name}.
/// Auth: ${authType}
/// Transaction: ${useTransaction ? 'automatic' : 'disabled (.NoTransaction())'}
/// </summary>
public static class ${name}Endpoints
{
    public static void Map(IPluginEndpointBuilder endpoints)
    {
${endpointRegistrations}
    }

    private static async Task<IResult> GetAll(${serviceOrRepo})
    {
        var items = await ${serviceOrRepoCall}.GetAllAsync();
        return Results.Ok(items);
    }

    private static async Task<IResult> Search(
        ${serviceOrRepo},
        string? name = null,
        bool? isActive = null)
    {
        var items = await ${serviceOrRepoCall}.SearchAsync(name, isActive);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetById(int id, ${serviceOrRepo})
    {
        var item = await ${serviceOrRepoCall}.GetByIdAsync(id);
        if (item == null)
        {
            return Results.NotFound(new { error = "${singular} not found" });
        }
        return Results.Ok(item);
    }

    private static async Task<IResult> Create(
        Create${singular}Request request,
        ${serviceOrRepo}${methodParams})
    {
        try
        {
            var id = await ${serviceOrRepoCall}.CreateAsync(request);
            return Results.Created(\$"/${route}/{id}", new { id });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Update(
        int id,
        Update${singular}Request request,
        ${serviceOrRepo}${methodParams})
    {
        try
        {
            var updated = await ${serviceOrRepoCall}.UpdateAsync(id, request);
            if (!updated)
            {
                return Results.NotFound(new { error = "${singular} not found" });
            }
            return Results.Ok(new { message = "${singular} updated" });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Delete(
        int id,
        ${serviceOrRepo}${methodParams})
    {
        var deleted = await ${serviceOrRepoCall}.DeleteAsync(id);
        if (!deleted)
        {
            return Results.NotFound(new { error = "${singular} not found" });
        }
        return Results.Ok(new { message = "${singular} deleted" });
    }
}
`;
}

main();
