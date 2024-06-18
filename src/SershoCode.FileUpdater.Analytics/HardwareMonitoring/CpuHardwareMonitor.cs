using System.Diagnostics;

namespace SershoCode.FileUpdater.Analytics.HardwareMonitoring;

public class CpuHardwareMonitor : HardwareMonitorBase
{
    protected override async Task WorkAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            var totalCount = new PerformanceCounter("Process", "% Processor Time", "_Total");
            var processCount = new PerformanceCounter("Process", "% Processor Time", CurrentProcess.ProcessName);
            var numOfCores = Environment.ProcessorCount;

            var suppressFirstRunZeroValue = processCount.NextValue() / (numOfCores * totalCount.NextValue()) * 100;

            while (!CancellationToken.IsCancellationRequested)
            {
                var currentCount = processCount.NextValue() / (numOfCores * totalCount.NextValue()) * 100;

                EnqueueValue(currentCount);

                await Task.Delay(TimeSpan.FromMilliseconds(TrackIntervalMs), CancellationToken);
            }
        }

        if (OperatingSystem.IsLinux())
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                CurrentProcess.Refresh();

                var startCpuUsage = CurrentProcess.TotalProcessorTime;

                await Task.Delay(TimeSpan.FromMilliseconds(TrackIntervalMs), CancellationToken);

                var endCpuUsage = CurrentProcess.TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;

                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * TrackIntervalMs);

                var cpuUsagePercentage = cpuUsageTotal * 100;

                EnqueueValue(cpuUsagePercentage);
            }
        }
    }
}
