using System.Net.Http.Json;
using Bizuit.Backend.Core.Auth;
using Microsoft.Extensions.Configuration;
using MyPlugin.Features.Items.Models;

namespace MyPlugin.Features.Items;

/// <summary>
/// Business logic service for Items.
/// </summary>
public class ItemsService
{
    private readonly ItemsRepository _repository;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory? _httpClientFactory;

    public ItemsService(
        ItemsRepository repository,
        IConfiguration config,
        IHttpClientFactory? httpClientFactory = null)
    {
        _repository = repository;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IEnumerable<Item>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<Item>> SearchAsync(string? name, decimal? minPrice, bool? isActive)
    {
        return await _repository.SearchAsync(name, minPrice, isActive);
    }

    public async Task<Item?> GetByIdAsync(int itemId)
    {
        return await _repository.GetByIdAsync(itemId);
    }

    public async Task<int> CreateAsync(CreateItemRequest request)
    {
        // Add business validation here
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required");
        }

        if (request.Price < 0)
        {
            throw new ArgumentException("Price cannot be negative");
        }

        return await _repository.CreateAsync(request);
    }

    public async Task<bool> UpdateAsync(int itemId, UpdateItemRequest request)
    {
        // Verify item exists
        var existing = await _repository.GetByIdAsync(itemId);
        if (existing == null)
        {
            return false;
        }

        // Add business validation here
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required");
        }

        return await _repository.UpdateAsync(itemId, request);
    }

    public async Task<bool> DeleteAsync(int itemId)
    {
        return await _repository.DeleteAsync(itemId);
    }

    /// <summary>
    /// EXAMPLE: Call Dashboard API from plugin.
    /// This demonstrates how to access the Dashboard API URL from system configuration
    /// and make authenticated requests using the user's token.
    /// </summary>
    /// <param name="user">User context with authentication token</param>
    /// <param name="endpoint">Dashboard API endpoint (relative to base URL)</param>
    /// <returns>Response from Dashboard API</returns>
    public async Task<object?> CallDashboardApiAsync(BizuitUserContext user, string endpoint)
    {
        var dashboardApiUrl = _config["System:DashboardApiUrl"];
        if (string.IsNullOrEmpty(dashboardApiUrl))
        {
            throw new InvalidOperationException("Dashboard API URL not configured in system settings");
        }

        if (_httpClientFactory == null)
        {
            throw new InvalidOperationException("HttpClientFactory not registered. Ensure DashboardClient is configured in ConfigureServices.");
        }

        var client = _httpClientFactory.CreateClient("DashboardClient");

        // Use user's RawToken for authentication
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {user.RawToken}");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", user.TenantId);

        try
        {
            var response = await client.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<object>();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to call Dashboard API endpoint '{endpoint}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// EXAMPLE: Using custom plugin settings for external service configuration.
    /// This demonstrates how to access settings configured by admin in the UI.
    /// Settings are stored in BackendPluginConfig table and loaded into IConfiguration.
    /// </summary>
    /// <param name="data">File data to upload</param>
    /// <param name="fileName">Name of the file</param>
    /// <returns>URL of uploaded file</returns>
    public async Task<string> UploadToExternalStorageAsync(byte[] data, string fileName)
    {
        // Access custom settings configured by admin
        var storageUrl = _config["AzureStorageUrl"];
        var storageKey = _config["AzureStorageKey"];
        var containerName = _config.GetValue("ContainerName", "files");

        // Validate required settings
        if (string.IsNullOrEmpty(storageUrl))
        {
            throw new InvalidOperationException(
                "AzureStorageUrl setting not configured. " +
                "Please configure it in Admin UI: /admin/settings/plugins → Configuration section");
        }

        if (string.IsNullOrEmpty(storageKey))
        {
            throw new InvalidOperationException(
                "AzureStorageKey setting not configured and marked as encrypted.");
        }

        // Use the configured settings
        // In real implementation, you would use Azure.Storage.Blobs package
        var uploadUrl = $"{storageUrl}/{containerName}/{fileName}";

        // Simulated upload logic
        // var blobClient = new BlobServiceClient(storageUrl, new StorageSharedKeyCredential(accountName, storageKey));
        // await blobClient.UploadBlobAsync(containerName, fileName, new BinaryData(data));

        return uploadUrl;
    }
}
