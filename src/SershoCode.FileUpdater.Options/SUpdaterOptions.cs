namespace SershoCode.FileUpdater.Options;

public class SUpdaterOptions
{
    public required string FtpAddress { get; set; }
    public required string SyncFolder { get; set; }
    public required string User { get; set; }
    public required string Password { get; set; }
    public required List<string> DownloadIgnore { get; set; }
    public required List<string> DownloadOnlyIfNotExists { get; set; }
    public required List<string> DeleteIgnore { get; set; }
    public required string AppUpdateUrl { get; set; }
    public bool IsCheckAppUpdates { get; set; }
    public bool IsSilentMode { get; set; }
    public bool IsSendAnonymousStatistics { get; set; }

    public SUpdaterOptions()
    {
        DownloadIgnore = [];
        DownloadOnlyIfNotExists = [];
        DeleteIgnore = [];
    }
}