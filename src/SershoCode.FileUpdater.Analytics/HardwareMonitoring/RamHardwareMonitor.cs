namespace SershoCode.FileUpdater.Analytics.HardwareMonitoring;

public class RamHardwareMonitor : HardwareMonitorBase
{
    protected override async Task WorkAsync()
    {
        while (!CancellationToken.IsCancellationRequested)
        {
            CurrentProcess.Refresh();

            EnqueueValue(CurrentProcess.WorkingSet64 / 1024d / 1024d);

            await Task.Delay(TimeSpan.FromMilliseconds(TrackIntervalMs));
        }
    }
}