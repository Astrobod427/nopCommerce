using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Misc.RestApi.Services;

/// <summary>
/// Scheduled task deleting expired API sessions (System > Schedule tasks)
/// </summary>
public class PurgeExpiredTokensTask : IScheduleTask
{
    protected readonly ApiTokenService _apiTokenService;

    public PurgeExpiredTokensTask(ApiTokenService apiTokenService)
    {
        _apiTokenService = apiTokenService;
    }

    /// <summary>
    /// Executes a task
    /// </summary>
    public async Task ExecuteAsync()
    {
        await _apiTokenService.DeleteExpiredTokensAsync();
    }
}
