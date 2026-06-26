using MemSignal.Abstractions;
using MemSignal.Application;
using MemSignal.Core;
using MemSignal.Infrastructure.Windows;

namespace MemSignal.Bootstrapper.Windows;

public sealed class WindowsMemoryPressureRuntime : IAsyncDisposable
{
    private readonly IDisposable? _metricsProvider;

    private WindowsMemoryPressureRuntime(
        MemoryPressureMonitorService monitorService,
        MemoryPressureOptions options,
        IDisposable? metricsProvider)
    {
        MonitorService = monitorService;
        Options = options;
        _metricsProvider = metricsProvider;
    }

    public MemoryPressureMonitorService MonitorService { get; }

    public MemoryPressureOptions Options { get; }

    public static WindowsMemoryPressureRuntime Create()
    {
        var options = MemoryPressureOptions.Default;
        IMemoryMetricsProvider metricsProvider;
        IDisposable? disposableProvider = null;

        try
        {
            var windowsProvider = new WindowsMemoryMetricsProvider();
            metricsProvider = windowsProvider;
            disposableProvider = windowsProvider;
        }
        catch (Exception ex)
        {
            metricsProvider = new UnavailableMemoryMetricsProvider(ex.Message);
        }

        var monitorService = new MemoryPressureMonitorService(
            metricsProvider,
            new MemoryPressureCalculator(options),
            options);

        return new WindowsMemoryPressureRuntime(monitorService, options, disposableProvider);
    }

    public async ValueTask DisposeAsync()
    {
        await MonitorService.DisposeAsync().ConfigureAwait(false);
        _metricsProvider?.Dispose();
    }

    private sealed class UnavailableMemoryMetricsProvider : IMemoryMetricsProvider
    {
        private readonly string _message;

        public UnavailableMemoryMetricsProvider(string message)
        {
            _message = message;
        }

        public ValueTask<MemoryMetricSnapshotResult> SampleAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(MemoryMetricSnapshotResult.Failure(_message));
        }
    }
}
