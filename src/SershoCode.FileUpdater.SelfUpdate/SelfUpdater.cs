using SershoCode.FileUpdater.Logging;
using SershoCode.FileUpdater.Options;
using SershoCode.FileUpdater.SelfUpdate.Client;
using SershoCode.FileUpdater.SelfUpdate.Client.Contracts;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;

namespace SershoCode.FileUpdater.SelfUpdate;

public class SelfUpdater
{
    private readonly string _currentApplicationName;
    private readonly string _currentApplicationBackupName;
    private readonly string _downloadingApplicationName;
    private readonly string _currentDirectory;
    private readonly string _updateDirectory;

    private readonly string _manifestUrl;
    private readonly string _downloadUrl;

    private readonly string _productName;
    private readonly bool _isWindows;

    private readonly ISelfUpdateClient _selfUpdateWebClient;

    public SelfUpdater(SUpdaterOptions options)
    {
        _isWindows = OperatingSystem.IsWindows();
        _productName = Assembly.GetEntryAssembly()?.GetName().Name ?? "SUpdater";

        _currentApplicationName = Process.GetCurrentProcess().ProcessName + (_isWindows ? ".exe" : string.Empty);
        _currentApplicationBackupName = $"{_currentApplicationName}.bak";
        _downloadingApplicationName = $"{_productName.Replace(".exe", "")}.zip";

        _downloadUrl = $"{options.AppUpdateUrl}/{_productName}/{(_isWindows ? "win-x64" : "linux-x64")}/{_productName}.zip";
        _manifestUrl = _downloadUrl.Replace(".zip", ".manifest.json");

        _currentDirectory = Directory.GetCurrentDirectory();
        _updateDirectory = Path.Combine(_currentDirectory, $"{_currentApplicationName.Replace(".exe", "")}_Temp");

        _selfUpdateWebClient = new SelfUpdateWebClient();
    }

    public async Task UpdateAsync()
    {
        await Logger.WriteLineAsync("Скачиваем обновление...", isAnimated: true);

        PrepareDirectoriesToUpdate();

        await DownloadUpdateAsync(_downloadUrl, Path.Combine(_updateDirectory, $"{_productName}.zip"));

        await Logger.WriteLineAsync("Обрабатываем обновление...", isAnimated: true);

        UnZipArchive(_updateDirectory, _downloadingApplicationName);

        RenameUpdateIfUserRenamedApplication();

        MoveUpdateFromTempToUser();

        DeleteDirectoriesIfExists(_updateDirectory);

        await Logger.WriteLineGreenAsync("Отлично. Теперь начинаем работу на актуальной версии.", isAnimated: true);

        await Task.Delay(TimeSpan.FromSeconds(1));

        await Logger.ClearConsoleAsync();

        var processInfo = new ProcessStartInfo
        {
            FileName = _currentApplicationName,
            CreateNoWindow = false,
        };

        if (_isWindows)
            Process.Start(processInfo);
        else
            await Process.Start(processInfo)?.WaitForExitAsync()!;

        Environment.Exit(0);
    }

    public async Task<bool> IsUpdateAvailable()
    {
        return await _selfUpdateWebClient.IsUpdateAvailableAsync(_manifestUrl, CalculateMd5Hash(_currentApplicationName));
    }

    private static string CalculateMd5Hash(string fileName)
    {
        using var md5 = MD5.Create();

        using var stream = File.OpenRead(fileName);

        var hash = md5.ComputeHash(stream);

        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public async Task DownloadUpdateAsync(string updateUrl, string fileName)
    {
        await _selfUpdateWebClient.DownloadUpdateAsync(updateUrl, fileName);
    }

    private void PrepareDirectoriesToUpdate()
    {
        DeleteFilesIfExists(_currentApplicationBackupName);
        DeleteDirectoriesIfExists(_updateDirectory);

        Directory.CreateDirectory(_updateDirectory);
    }

    private void RenameUpdateIfUserRenamedApplication()
    {
        var correctApplicationName = Path.Combine(_updateDirectory, $"{_productName}{(_isWindows ? ".exe" : string.Empty)}");

        var userVersionOfApplicationName = Path.Combine(_updateDirectory, _currentApplicationName);

        if (correctApplicationName != userVersionOfApplicationName)
            File.Move(correctApplicationName, userVersionOfApplicationName);
    }

    private void MoveUpdateFromTempToUser()
    {
        var backupFilePath = Path.Combine(_currentDirectory, _currentApplicationBackupName);
        var newVersionFilePath = Path.Combine(_updateDirectory, _currentApplicationName);
        var oldVersionFilePath = Path.Combine(_currentDirectory, _currentApplicationName);

        File.Move(_currentApplicationName, backupFilePath);
        File.Move(newVersionFilePath, oldVersionFilePath);
    }

    private void UnZipArchive(string archivePath, string archiveName)
    {
        using var archive = ZipFile.OpenRead(Path.Combine(archivePath, archiveName));

        foreach (var entry in archive.Entries)
        {
            if (entry.Name.Contains(nameof(SUpdaterOptions)))
                continue;

            entry.ExtractToFile(Path.Combine(_updateDirectory, entry.Name));
        }
    }

    private void DeleteFilesIfExists(params string[] files)
    {
        foreach (var file in files)
        {
            var filePath = Path.Combine(_currentDirectory, file);

            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    private void DeleteDirectoriesIfExists(params string[] directories)
    {
        foreach (var directory in directories)
        {
            var directoryPath = Path.Combine(_currentDirectory, directory);

            if (Directory.Exists(directoryPath))
                Directory.Delete(directoryPath, true);
        }
    }
}