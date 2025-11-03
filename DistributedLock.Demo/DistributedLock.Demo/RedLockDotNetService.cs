using FluentResults;
using RedLockNet;

namespace DistributedLock.Demo;

public class RedLockDotNetService : IDistributedLockService
{
    private readonly IDistributedLockFactory _distributedLockFactory;

    public RedLockDotNetService(IDistributedLockFactory distributedLockFactory) =>
        _distributedLockFactory = distributedLockFactory;

    public async Task<Result> ExecuteWithLock(string key, TimeSpan timeout, Func<Task<Result>> function, CancellationToken cancellationToken = default)
    {
        await using var redlock = await _distributedLockFactory.CreateLockAsync(key, timeout, timeout, TimeSpan.FromSeconds(1), cancellationToken);

        if (redlock.IsAcquired == false)
            return Result.Fail($"Could not acquire lock for key {key}");

        return await function();
    }
}
