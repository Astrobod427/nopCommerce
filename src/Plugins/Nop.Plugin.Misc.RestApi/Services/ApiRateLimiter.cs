using System.Threading.RateLimiting;

namespace Nop.Plugin.Misc.RestApi.Services;

/// <summary>
/// Per IP address request limits of the API (singleton). The limiters are rebuilt when the limits are changed
/// in the plugin settings.
/// </summary>
public class ApiRateLimiter : IDisposable
{
    #region Fields

    protected readonly Lock _lock = new();
    protected PartitionedRateLimiter<string> _general;
    protected PartitionedRateLimiter<string> _auth;
    protected (int general, int auth) _limits = (-1, -1);

    #endregion

    #region Utilities

    /// <summary>
    /// Creates a sliding window limiter (one window per minute split in 6 segments) partitioned by IP address
    /// </summary>
    protected static PartitionedRateLimiter<string> Create(int permitsPerMinute)
    {
        if (permitsPerMinute <= 0)
            return null;

        return PartitionedRateLimiter.Create<string, string>(ip => RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    }

    protected virtual void EnsureLimiters(RestApiSettings settings)
    {
        var limits = (settings.RequestsPerMinute, settings.AuthRequestsPerMinute);
        if (limits == _limits)
            return;

        lock (_lock)
        {
            if (limits == _limits)
                return;

            var (general, auth) = (_general, _auth);
            _general = Create(limits.RequestsPerMinute);
            _auth = Create(limits.AuthRequestsPerMinute);
            _limits = limits;
            general?.Dispose();
            auth?.Dispose();
        }
    }

    protected static (bool allowed, TimeSpan? retryAfter) Acquire(PartitionedRateLimiter<string> limiter, string ip)
    {
        if (limiter == null)
            return (true, null);

        using var lease = limiter.AttemptAcquire(ip);
        if (lease.IsAcquired)
            return (true, null);

        return (false, lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? retryAfter : TimeSpan.FromSeconds(10));
    }

    #endregion

    #region Methods

    /// <summary>
    /// Counts a request of an IP address against the limits
    /// </summary>
    /// <param name="settings">Plugin settings</param>
    /// <param name="ip">Client IP address</param>
    /// <param name="isAuthRequest">Whether the request is a sign-in, registration, guest session or client token request</param>
    /// <returns>Whether the request is allowed and, if not, when to retry</returns>
    public virtual (bool allowed, TimeSpan? retryAfter) TryAcquire(RestApiSettings settings, string ip, bool isAuthRequest)
    {
        EnsureLimiters(settings);
        ip = string.IsNullOrEmpty(ip) ? "unknown" : ip;

        var result = Acquire(_general, ip);
        if (result.allowed && isAuthRequest)
            result = Acquire(_auth, ip);

        return result;
    }

    public void Dispose()
    {
        _general?.Dispose();
        _auth?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}
