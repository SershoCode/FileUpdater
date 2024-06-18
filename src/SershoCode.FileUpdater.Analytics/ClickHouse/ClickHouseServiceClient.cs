using Grpc.Net.Client;
using SershoCode.DbProxy.SUpdater;

namespace SershoCode.FileUpdater.Analytics.ClickHouse;

public class ClickHouseServiceClient
{
    private const string address = "http://gamefarming.ru:9898";
    public async Task SendClickHouseStatsAsync(ClickHouseStatsDto dto)
    {
        using var channel = GrpcChannel.ForAddress(address);

        var client = new SUpdaterProtoService.SUpdaterProtoServiceClient(channel);

        var reply = await client.WriteStatsToClickHouseAsync(new WriteStatsToClickHouseRequest()
        {
            OperationSystem = dto.OperationSystem,
            FileHandledCount = dto.FileHandledCount,
            DownloadedMegabytes = dto.DownloadedMegabytes,
            MaxCpuLoadPercentage = dto.MaxCpuLoadPercentage,
            AverageCpuLoadPercentage = dto.AverageCpuLoadPercentage,
            MaxRamLoadMegabytes = dto.MaxRamLoadMegabytes,
            AverageRamLoadMegabytes = dto.AverageRamLoadMegabytes,
            UpdateTimeMinutes = dto.UpdateTimeMinutes,
            UpdateDate = dto.UpdateDate,
            AppVersion = dto.AppVersion
        });
    }
}