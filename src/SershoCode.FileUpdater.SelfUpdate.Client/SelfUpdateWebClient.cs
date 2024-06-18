using Newtonsoft.Json;
using SershoCode.FileUpdater.SelfUpdate.Client.Contracts;
using SershoCode.FileUpdater.SelfUpdate.Client.Contracts.Dto;

namespace SershoCode.FileUpdater.SelfUpdate.Client;
public class SelfUpdateWebClient : ISelfUpdateClient
{
    private readonly HttpClient _httpClient;

    public SelfUpdateWebClient()
    {
        _httpClient = new HttpClient();
    }

    public async Task DownloadUpdateAsync(string updateUrl, string fileName)
    {
        await using var responseStream = await _httpClient.GetStreamAsync(updateUrl);

        await using var fileStream = new FileStream(fileName, FileMode.Create);

        await responseStream.CopyToAsync(fileStream);
    }

    public async Task<bool> IsUpdateAvailableAsync(string manifestUrl, string currentAppHash)
    {
        var response = await _httpClient.GetStringAsync(manifestUrl);

        var manifestDto = JsonConvert.DeserializeObject<ManifestResponseDto>(response);

        return currentAppHash != manifestDto!.Md5Hash;
    }
}
