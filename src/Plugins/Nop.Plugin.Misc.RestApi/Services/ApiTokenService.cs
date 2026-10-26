using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Nop.Core;
using Nop.Services.Helpers;
using Nop.Data;
using Nop.Plugin.Misc.RestApi.Domain;

namespace Nop.Plugin.Misc.RestApi.Services;

/// <summary>
/// Issues, validates and revokes API access tokens and manages API clients
/// </summary>
public class ApiTokenService
{
    #region Fields

    protected readonly IRepository<ApiClient> _apiClientRepository;
    protected readonly IRepository<ApiToken> _apiTokenRepository;
    protected readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public ApiTokenService(IRepository<ApiClient> apiClientRepository,
        IRepository<ApiToken> apiTokenRepository,
        IWebHelper webHelper)
    {
        _apiClientRepository = apiClientRepository;
        _apiTokenRepository = apiTokenRepository;
        _webHelper = webHelper;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Generates a URL-safe random string
    /// </summary>
    /// <param name="bytes">Number of random bytes</param>
    public static string GenerateRandomString(int bytes = 32)
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(bytes));
    }

    /// <summary>
    /// Gets the SHA-256 hash (hex) of a secret value
    /// </summary>
    /// <param name="value">Secret value</param>
    public static string Hash(string value)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)));
    }

    #endregion

    #region Tokens

    /// <summary>
    /// Issues a new token; the clear token is returned once and only its hash is stored
    /// </summary>
    /// <returns>Clear token and stored entity</returns>
    public virtual async Task<(string token, ApiToken entity)> IssueTokenAsync(ApiTokenKind kind, int customerId, int storeId, TimeSpan lifetime, int? apiClientId = null)
    {
        var token = GenerateRandomString();
        var now = DateTime.UtcNow;
        var entity = new ApiToken
        {
            TokenHash = Hash(token),
            Kind = kind,
            CustomerId = customerId,
            ApiClientId = apiClientId,
            StoreId = storeId,
            IpAddress = _webHelper.GetCurrentIpAddress(),
            CreatedOnUtc = now,
            ExpiresOnUtc = now.Add(lifetime)
        };
        await _apiTokenRepository.InsertAsync(entity, false);

        return (token, entity);
    }

    /// <summary>
    /// Gets a valid (not expired) token by its clear value
    /// </summary>
    public virtual async Task<ApiToken> GetValidTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var hash = Hash(token);
        var now = DateTime.UtcNow;
        var entity = _apiTokenRepository.Table.FirstOrDefault(t => t.TokenHash == hash);
        if (entity == null)
            return null;

        if (entity.ExpiresOnUtc <= now)
        {
            await _apiTokenRepository.DeleteAsync(entity, false);
            return null;
        }

        //avoid a database write on every request
        if (!entity.LastUsedOnUtc.HasValue || entity.LastUsedOnUtc.Value.AddMinutes(1) < now)
        {
            entity.LastUsedOnUtc = now;
            await _apiTokenRepository.UpdateAsync(entity, false);
        }

        return entity;
    }

    /// <summary>
    /// Gets a token by identifier
    /// </summary>
    public virtual async Task<ApiToken> GetTokenByIdAsync(int id)
    {
        return await _apiTokenRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Searches active tokens
    /// </summary>
    public virtual async Task<IPagedList<ApiToken>> SearchTokensAsync(ApiTokenKind? kind = null, int? apiClientId = null, int? customerId = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var now = DateTime.UtcNow;

        return await _apiTokenRepository.GetAllPagedAsync(query =>
        {
            query = query.Where(t => t.ExpiresOnUtc > now);
            if (kind.HasValue)
                query = query.Where(t => t.KindId == (int)kind.Value);
            if (apiClientId.HasValue)
                query = query.Where(t => t.ApiClientId == apiClientId.Value);
            if (customerId.HasValue)
                query = query.Where(t => t.CustomerId == customerId.Value);

            return query.OrderByDescending(t => t.LastUsedOnUtc ?? t.CreatedOnUtc);
        }, pageIndex, pageSize);
    }

    /// <summary>
    /// Revokes (deletes) a token
    /// </summary>
    public virtual async Task RevokeTokenAsync(ApiToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        await _apiTokenRepository.DeleteAsync(token, false);
    }

    /// <summary>
    /// Revokes every token of a customer or of an API client
    /// </summary>
    /// <returns>Number of revoked tokens</returns>
    public virtual async Task<int> RevokeTokensAsync(int? customerId = null, int? apiClientId = null)
    {
        if (apiClientId.HasValue)
            return await _apiTokenRepository.DeleteAsync(t => t.ApiClientId == apiClientId.Value);

        if (customerId.HasValue)
            return await _apiTokenRepository.DeleteAsync(t => t.CustomerId == customerId.Value);

        return 0;
    }

    /// <summary>
    /// Deletes expired tokens
    /// </summary>
    /// <returns>Number of deleted tokens</returns>
    public virtual async Task<int> DeleteExpiredTokensAsync()
    {
        var now = DateTime.UtcNow;

        return await _apiTokenRepository.DeleteAsync(t => t.ExpiresOnUtc <= now);
    }

    #endregion

    #region API clients

    /// <summary>
    /// Gets all API clients
    /// </summary>
    public virtual async Task<IPagedList<ApiClient>> GetAllClientsAsync(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return await _apiClientRepository.GetAllPagedAsync(query => query.OrderBy(c => c.Name), pageIndex, pageSize);
    }

    /// <summary>
    /// Gets an API client by identifier
    /// </summary>
    public virtual async Task<ApiClient> GetClientByIdAsync(int id)
    {
        return await _apiClientRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets an API client by its public client identifier
    /// </summary>
    public virtual Task<ApiClient> GetClientByClientIdAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return Task.FromResult<ApiClient>(null);

        return Task.FromResult(_apiClientRepository.Table.FirstOrDefault(c => c.ClientId == clientId));
    }

    /// <summary>
    /// Validates client credentials
    /// </summary>
    /// <returns>The client when the credentials are valid and the client is active; otherwise null</returns>
    public virtual async Task<ApiClient> ValidateClientAsync(string clientId, string clientSecret)
    {
        var client = await GetClientByClientIdAsync(clientId);
        if (client == null || !client.Active || string.IsNullOrEmpty(clientSecret))
            return null;

        var expected = Convert.FromHexString(client.SecretHash);
        var actual = Convert.FromHexString(Hash(clientSecret));
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            return null;

        client.LastUsedOnUtc = DateTime.UtcNow;
        await _apiClientRepository.UpdateAsync(client, false);

        return client;
    }

    /// <summary>
    /// Creates an API client
    /// </summary>
    /// <returns>Created client and its clear secret (shown once)</returns>
    public virtual async Task<(ApiClient client, string secret)> CreateClientAsync(string name, int customerId, bool active)
    {
        var secret = GenerateRandomString();
        var client = new ApiClient
        {
            Name = name,
            ClientId = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16)),
            SecretHash = Hash(secret),
            CustomerId = customerId,
            Active = active,
            CreatedOnUtc = DateTime.UtcNow
        };
        await _apiClientRepository.InsertAsync(client);

        return (client, secret);
    }

    /// <summary>
    /// Updates an API client
    /// </summary>
    public virtual async Task UpdateClientAsync(ApiClient client)
    {
        await _apiClientRepository.UpdateAsync(client);
    }

    /// <summary>
    /// Generates a new secret for an API client and revokes its sessions
    /// </summary>
    /// <returns>New clear secret (shown once)</returns>
    public virtual async Task<string> RegenerateSecretAsync(ApiClient client)
    {
        var secret = GenerateRandomString();
        client.SecretHash = Hash(secret);
        await _apiClientRepository.UpdateAsync(client);
        await RevokeTokensAsync(apiClientId: client.Id);

        return secret;
    }

    /// <summary>
    /// Deletes an API client and its sessions
    /// </summary>
    public virtual async Task DeleteClientAsync(ApiClient client)
    {
        await RevokeTokensAsync(apiClientId: client.Id);
        await _apiClientRepository.DeleteAsync(client);
    }

    #endregion
}
