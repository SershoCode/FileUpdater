namespace SershoCode.FileUpdater.Analytics.ClickHouse;

public class ClickHouseStatsDto
{
    public required string OperationSystem { get; set; }
    public required long FileHandledCount { get; set; }
    public required double DownloadedMegabytes { get; set; }
    public required double MaxCpuLoadPercentage { get; set; }
    public required double AverageCpuLoadPercentage { get; set; }
    public required double MaxRamLoadMegabytes { get; set; }
    public required double AverageRamLoadMegabytes { get; set; }
    public required double UpdateTimeMinutes { get; set; }
    public required string UpdateDate { get; set; }
    public required string AppVersion { get; set; }
}