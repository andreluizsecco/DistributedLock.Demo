using FluentResults;
using Medallion.Threading;

namespace DistributedLock.Demo;

public class DistributedLockService : IDistributedLockService
{
    private readonly IDistributedLockProvider _distributedLockProvider;

    public DistributedLockService(IDistributedLockProvider distributedLockProvider) =>
        _distributedLockProvider = distributedLockProvider;

    public async Task<Result> ExecuteWithLock(string key, TimeSpan timeout, Func<Task<Result>> function, CancellationToken cancellationToken = default)
    {
        await using var redlock = await _distributedLockProvider.TryAcquireLockAsync(key, timeout, cancellationToken);

        if (redlock is null)
            return Result.Fail($"Could not acquire lock for key {key}");

        return await function();
    }
}
