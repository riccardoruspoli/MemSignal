using MemSignal.Abstractions;
using MemSignal.Core;

namespace MemSignal.Application;

public sealed class MemoryPressureMonitorService : IAsyncDisposable
{
    private readonly IMemoryMetricsProvider _metricsProvider;
    private readonly MemoryPressureCalculator _calculator;
    private readonly MemoryPressureOptions _options;
    private readonly object _sync = new();
    private CancellationTokenSource? _stopSignal;
    private Task? _loopTask;

    public MemoryPressureMonitorService(
        IMemoryMetricsProvider metricsProvider,
        MemoryPressureCalculator calculator,
        MemoryPressureOptions? options = null)
    {
        _metricsProvider = metricsProvider;
        _calculator = calculator;
        _options = options ?? MemoryPressureOptions.Default;
    }

    public event EventHandler<MemoryPressureUpdate>? UpdateProduced;

    public bool IsRunning
    {
        get
        {
            lock (_sync)
            {
                return _loopTask is { IsCompleted: false };
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_loopTask is { IsCompleted: false })
            {
                return Task.CompletedTask;
            }

            _stopSignal = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loopTask = RunAsync(_stopSignal.Token);
            return Task.CompletedTask;
        }
    }

    public async Task StopAsync()
    {
        Task? loopTask;
        CancellationTokenSource? stopSignal;

        lock (_sync)
        {
            loopTask = _loopTask;
            stopSignal = _stopSignal;
            _loopTask = null;
            _stopSignal = null;
        }

        if (stopSignal is null)
        {
            return;
        }

        await stopSignal.CancelAsync().ConfigureAwait(false);

        if (loopTask is not null)
        {
            try
            {
                await loopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        stopSignal.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await SampleOnceAsync(cancellationToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(_options.SamplingInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            await SampleOnceAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SampleOnceAsync(CancellationToken cancellationToken)
    {
        MemoryPressureUpdate update;

        try
        {
            var snapshotResult = await _metricsProvider.SampleAsync(cancellationToken).ConfigureAwait(false);

            if (!snapshotResult.IsSuccess || snapshotResult.Snapshot is null)
            {
                update = MemoryPressureUpdate.Unknown(snapshotResult.ErrorMessage ?? "Required memory metrics are unavailable.");
            }
            else
            {
                update = MemoryPressureUpdate.Known(_calculator.Calculate(snapshotResult.Snapshot));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            update = MemoryPressureUpdate.Unknown(ex.Message);
        }

        UpdateProduced?.Invoke(this, update);
    }
}
