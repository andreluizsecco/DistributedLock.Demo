using FluentResults;

namespace DistributedLock.Demo;

public interface IDistributedLockService
{
    Task<Result> ExecuteWithLock(string key, TimeSpan timeout, Func<Task<Result>> function, CancellationToken cancellationToken = default);
}
