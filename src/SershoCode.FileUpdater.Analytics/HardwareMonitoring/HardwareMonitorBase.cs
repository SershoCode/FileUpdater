using System.Diagnostics;

namespace SershoCode.FileUpdater.Analytics.HardwareMonitoring;

public abstract class HardwareMonitorBase
{
    protected const int TrackIntervalMs = 100;
    protected readonly Process CurrentProcess;

    private readonly CancellationTokenSource _cancellationTokenSource;
    protected readonly CancellationToken CancellationToken;

    private readonly Queue<double> _queue;
    private const int QueueLimit = 10000;

    protected HardwareMonitorBase()
    {
        _queue = new Queue<double>();
        CurrentProcess = Process.GetCurrentProcess();

        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken = _cancellationTokenSource.Token;
    }

    protected abstract Task WorkAsync();

    public void Start()
    {
       Task.Factory.StartNew(WorkAsync, CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
    }

    protected void EnqueueValue(double value)
    {
        if (value == 0 || double.IsInfinity(value))
            return;

        if (_queue.Count > QueueLimit)
            _queue.Dequeue();

        _queue.Enqueue(value);
    }

    public double GetAverageValue()
    {
        var average = _queue.Any() ? _queue.Average() : 0;

        return GetValue(average);
    }

    public double GetMaxValue()
    {
        var max = _queue.Any() ? _queue.Max() : 0;

        return GetValue(max);
    }

    // WorkAround for some Windows PC's.
    private static double GetValue(double value) => value >= 0.01 ? value : 0.01d;
}