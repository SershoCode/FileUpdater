namespace SershoCode.FileUpdater.SelfUpdate.Client.Contracts;

// To request dto.
public interface ISelfUpdateClient
{
    Task<bool> IsUpdateAvailableAsync(string manifestUrl, string currentAppHash);
    Task DownloadUpdateAsync(string updateUrl, string fileName);
}
