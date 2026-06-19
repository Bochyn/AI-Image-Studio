using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using RhinoImageStudio.Backend.Options;

namespace RhinoImageStudio.Backend.Services;

/// <summary>
/// Interface for secure secret storage
/// </summary>
public interface ISecretStorage
{
    Task<string?> GetSecretAsync(string key);
    Task SetSecretAsync(string key, string value);
    Task DeleteSecretAsync(string key);
    Task<bool> HasSecretAsync(string key);
}

/// <summary>
/// Secure secret storage using ASP.NET Core Data Protection with DPAPI legacy migration.
/// </summary>
public class DataProtectionSecretStorage : ISecretStorage
{
    private readonly string _storageDirectory;
    private readonly IDataProtector _protector;
    private readonly ILogger<DataProtectionSecretStorage> _logger;

    public DataProtectionSecretStorage(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<DataProtectionSecretStorage> logger,
        IOptions<SecretStorageOptions> options)
    {
        _protector = dataProtectionProvider.CreateProtector("RhinoImageStudio.Secrets.v1");
        _logger = logger;
        _storageDirectory = options.Value.StoragePath;

        Directory.CreateDirectory(_storageDirectory);
    }

    private string GetFilePath(string key)
    {
        var safeKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", "");
        return Path.Combine(_storageDirectory, $"{safeKey}.enc");
    }

    public async Task<string?> GetSecretAsync(string key)
    {
        var filePath = GetFilePath(key);

        if (!File.Exists(filePath))
        {
            _logger.LogDebug("Secret not found: {Key}", key);
            return null;
        }

        try
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
                return null;

            // Legacy Windows DPAPI: binary blob
            if (IsLikelyDpapiBlob(filePath))
            {
                var migrated = await TryMigrateDpapiSecretAsync(key, filePath);
                if (migrated != null)
                    return migrated;
                return null;
            }

            var encryptedData = await File.ReadAllTextAsync(filePath);
            var secret = _protector.Unprotect(encryptedData);
            _logger.LogDebug("Secret retrieved: {Key}", key);
            return secret;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to decrypt secret: {Key}", key);
            return null;
        }
    }

    public async Task SetSecretAsync(string key, string value)
    {
        var filePath = GetFilePath(key);

        try
        {
            var encryptedData = _protector.Protect(value);
            await File.WriteAllTextAsync(filePath, encryptedData, Encoding.UTF8);
            _logger.LogInformation("Secret stored: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store secret: {Key}", key);
            throw;
        }
    }

    public Task DeleteSecretAsync(string key)
    {
        var filePath = GetFilePath(key);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Secret deleted: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public async Task<bool> HasSecretAsync(string key)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
            return false;

        return await GetSecretAsync(key) != null;
    }

    private static bool IsLikelyDpapiBlob(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        if (bytes.Length < 4)
            return false;

        // DPAPI blobs are binary; Data Protection text payloads are base64-ish ASCII
        return bytes.Take(Math.Min(64, bytes.Length)).Any(b => b < 32 || b > 126);
    }

    private async Task<string?> TryMigrateDpapiSecretAsync(string key, string filePath)
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Legacy DPAPI secret for {Key} cannot be migrated on this platform.", key);
            return null;
        }

        try
        {
            var encryptedData = await File.ReadAllBytesAsync(filePath);
            var decryptedData = ProtectedData.Unprotect(
                encryptedData,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser);
            var secret = Encoding.UTF8.GetString(decryptedData);

            await SetSecretAsync(key, secret);
            _logger.LogInformation("Migrated legacy DPAPI secret to Data Protection: {Key}", key);
            return secret;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to migrate legacy DPAPI secret: {Key}", key);
            return null;
        }
    }
}
