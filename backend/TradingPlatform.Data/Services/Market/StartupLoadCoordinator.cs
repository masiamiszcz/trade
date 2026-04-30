using System.Threading;
using System.Threading.Tasks;

namespace TradingPlatform.Data.Services.Market;

public interface IStartupLoadCoordinator
{
    Task WaitForReadyAsync(CancellationToken cancellationToken = default);
    void MarkReady();
    void MarkFailed(Exception exception);
}

public sealed class StartupLoadCoordinator : IStartupLoadCoordinator
{
    private readonly TaskCompletionSource<bool> _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void MarkReady()
    {
        _readyTcs.TrySetResult(true);
    }

    public void MarkFailed(Exception exception)
    {
        _readyTcs.TrySetException(exception);
    }

    public Task WaitForReadyAsync(CancellationToken cancellationToken = default)
    {
        return _readyTcs.Task.WaitAsync(cancellationToken);
    }
}
