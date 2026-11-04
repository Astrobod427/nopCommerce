using System.Collections.Concurrent;
using System.Reflection;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Authentication.MultiFactor;
using Nop.Services.Common;

namespace Nop.Plugin.Misc.RestApi.Services;

/// <summary>
/// Second factor of storefront sign-ins. nopCommerce has no generic code check for its multi-factor plugins
/// (each one verifies in its own web page), so the supported plugins are called directly: Google Authenticator,
/// the only one shipped with nopCommerce. Its service is reached by reflection to avoid a hard dependency.
/// </summary>
public class MfaService
{
    #region Fields

    protected const string GOOGLE_SERVICE_TYPE = "Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Services.GoogleAuthenticatorService";

    //wrong codes per pending session (pending sessions live 5 minutes)
    protected static readonly ConcurrentDictionary<int, int> _failedAttempts = new();

    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly IMultiFactorAuthenticationPluginManager _multiFactorAuthenticationPluginManager;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly IStoreContext _storeContext;

    #endregion

    #region Ctor

    public MfaService(IGenericAttributeService genericAttributeService,
        IMultiFactorAuthenticationPluginManager multiFactorAuthenticationPluginManager,
        IServiceProvider serviceProvider,
        IStoreContext storeContext)
    {
        _genericAttributeService = genericAttributeService;
        _multiFactorAuthenticationPluginManager = multiFactorAuthenticationPluginManager;
        _serviceProvider = serviceProvider;
        _storeContext = storeContext;
    }

    #endregion

    #region Utilities

    protected static Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName, false))
            .FirstOrDefault(type => type != null);
    }

    protected virtual bool VerifyGoogleAuthenticatorCode(Customer customer, string code)
    {
        var serviceType = FindType(GOOGLE_SERVICE_TYPE);
        var service = serviceType == null ? null : _serviceProvider.GetService(serviceType);
        if (service == null)
            return false;

        var record = serviceType.GetMethod("GetConfigurationByCustomerEmail", BindingFlags.Public | BindingFlags.Instance)?.Invoke(service, [customer.Email]);
        if (record?.GetType().GetProperty("SecretKey")?.GetValue(record) is not string secretKey || string.IsNullOrEmpty(secretKey))
            return false;

        return serviceType.GetMethod("ValidateTwoFactorToken", BindingFlags.Public | BindingFlags.Instance)
            ?.Invoke(service, [secretKey, code]) is true;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the active second-factor method of a customer, or null when none
    /// </summary>
    public virtual async Task<string> GetActiveProviderAsync(Customer customer)
    {
        var provider = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.SelectedMultiFactorAuthenticationProviderAttribute);
        if (string.IsNullOrEmpty(provider))
            return null;

        var store = await _storeContext.GetCurrentStoreAsync();

        return await _multiFactorAuthenticationPluginManager.IsPluginActiveAsync(provider, customer, store.Id) ? provider : null;
    }

    /// <summary>
    /// Checks whether the API can verify codes of a second-factor method
    /// </summary>
    public virtual bool IsSupported(string provider)
    {
        return string.Equals(provider, RestApiDefaults.GoogleAuthenticatorSystemName, StringComparison.OrdinalIgnoreCase)
            && FindType(GOOGLE_SERVICE_TYPE) != null;
    }

    /// <summary>
    /// Verifies a second-factor code of a pending session
    /// </summary>
    /// <param name="pendingTokenId">Identifier of the pending session</param>
    /// <param name="customer">Customer</param>
    /// <param name="provider">Second-factor method</param>
    /// <param name="code">Code typed by the customer</param>
    /// <returns>Whether the code is right, and the number of attempts left when it is wrong</returns>
    public virtual (bool valid, int attemptsLeft) Verify(int pendingTokenId, Customer customer, string provider, string code)
    {
        var valid = !string.IsNullOrWhiteSpace(code)
            && string.Equals(provider, RestApiDefaults.GoogleAuthenticatorSystemName, StringComparison.OrdinalIgnoreCase)
            && VerifyGoogleAuthenticatorCode(customer, code.Trim().Replace(" ", string.Empty));

        if (valid)
        {
            _failedAttempts.TryRemove(pendingTokenId, out _);
            return (true, 0);
        }

        var failed = _failedAttempts.AddOrUpdate(pendingTokenId, 1, (_, count) => count + 1);
        var left = Math.Max(0, RestApiDefaults.MfaMaxAttempts - failed);
        if (left == 0)
            _failedAttempts.TryRemove(pendingTokenId, out _);

        return (false, left);
    }

    #endregion
}
