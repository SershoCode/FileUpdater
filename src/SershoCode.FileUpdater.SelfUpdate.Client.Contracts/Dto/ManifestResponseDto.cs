namespace SershoCode.FileUpdater.SelfUpdate.Client.Contracts.Dto;

public record ManifestResponseDto
{
    public required string Md5Hash { get; set; }
}