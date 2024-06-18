using ByteSizeLib;
using FluentFTP;
using FluentFTP.Exceptions;
using Polly;
using Polly.Retry;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SershoCode.FileUpdater.Core;

public class Updater
{
    private readonly SUpdaterOptions _options;
    private readonly List<string> _selfIgnore;
    private readonly AsyncFtpClient _ftpClient;
    private readonly AsyncRetryPolicy _disconnectWhileDownloadingPolicy;
    private readonly AsyncRetryPolicy _disconnectWhileConnectionPolicy;

    private readonly string _currentDirectory;
    private readonly int _currentDirectoryLength;
    private readonly int _externalDirectoryLength;

    private readonly Context _disconnectPolicyContext;

    public Updater(SUpdaterOptions options)
    {
        var currentProcessName = Process.GetCurrentProcess().ProcessName;

        _options = options;
        _ftpClient = new AsyncFtpClient(_options.FtpAddress, _options.User, _options.Password);
        _selfIgnore = [currentProcessName, $"{currentProcessName}.exe", $"{currentProcessName}.bak", $"{currentProcessName}.exe.bak", $"{nameof(SUpdaterOptions)}.json"];

        _currentDirectory = Directory.GetCurrentDirectory();
        _currentDirectoryLength = _currentDirectory.Length + 1;
        _externalDirectoryLength = _options.SyncFolder.Length + 2;

        _disconnectPolicyContext = new Context("Disconnect Context");
        _disconnectWhileDownloadingPolicy = GetDisconnectWhileDownloadingPolicy();
        _disconnectWhileConnectionPolicy = GetDisconnectWhileConnectingPolicy();
    }

    public async Task UpdateAsync()
    {
        var listedExternalFiles = new HashSet<string>();
        var listedExternalFolders = new HashSet<string>();
        var downloadIgnoreList = new HashSet<string>();
        var totalDownloadMegabytes = 0D;

        await ConnectAsync();

        var cpuMonitor = new CpuHardwareMonitor();
        var ramMonitor = new RamHardwareMonitor();

        var stopWatch = new Stopwatch();

        stopWatch.Start();

        if (_options.IsSendAnonymousStatistics)
        {
            cpuMonitor.Start();
            ramMonitor.Start();
        }

        await foreach (var file in _ftpClient.GetListingEnumerable(string.Empty, FtpListOption.Recursive))
        {
            var fileName = file.FullName.Remove(0, _externalDirectoryLength);

            switch (file.Type)
            {
                case FtpObjectType.File:
                    {
                        var anyIgnorePatternIsMatched = _options.DownloadIgnore.Any(pattern => Regex.IsMatch(fileName, pattern));

                        var isDownloadOnlyIfNotExists = _options.DownloadOnlyIfNotExists.Any(fileName.Contains);

                        if (anyIgnorePatternIsMatched)
                        {
                            await Logger.WriteLineAsync($"Игнорируем {CutPathIfLong(fileName)}...", ConsoleColor.Gray, isInCurrentLine: true, isAnimated: false);

                            downloadIgnoreList.Add(fileName);

                            break;
                        }

                        listedExternalFiles.Add(fileName);

                        if (File.Exists(fileName) && (isDownloadOnlyIfNotExists || await _ftpClient.CompareFile(fileName, fileName, FtpCompareOption.Size) == FtpCompareResult.Equal))
                        {
                            await Logger.WriteLineGreenAsync($"Файл {CutPathIfLong(fileName)} соответствует...", isCurrentLine: true, isAnimated: false);
                        }
                        else
                        {
                            var fileSize = ByteSize.FromBytes(file.Size).MegaBytes;

                            await Logger.WriteLineAsync($"Качаем: {CutPathIfLong(fileName)} ({ByteSize.FromMegaBytes(fileSize)})...", ConsoleColor.Yellow, isInCurrentLine: true, isAnimated: false);

                            await DownloadFileAsync(fileName, fileName);

                            totalDownloadMegabytes += fileSize;

                            await Logger.WriteLineGreenAsync($"Всего скачано: {ByteSize.FromMegaBytes(totalDownloadMegabytes)}...", isBottomLine: true, isAnimated: false);
                        }

                        break;
                    }
                case FtpObjectType.Directory:
                    {
                        listedExternalFolders.Add(fileName);
                        break;
                    }
                case FtpObjectType.Link:
                    {
                        break;
                    }
                default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
            }
        }

        Logger.ClearBottomLine();

        await Logger.WriteLineGreenAsync($"Загрузка завершена. Скачано: {ByteSize.FromMegaBytes(totalDownloadMegabytes)}. Обработано {listedExternalFiles.Count} файлов\n", isCurrentLine: true, isAnimated: true);

        await Logger.WriteLineAsync($"Отфильтровано согласно правилам: {downloadIgnoreList.Count}.", ConsoleColor.Yellow);

        var filesToDelete = await GetFilesToDeleteAsync(listedExternalFiles);
        var directoriesToDelete = await GetDirectoriesToDeleteAsync(listedExternalFolders);

#if !DEBUG
        
        if (filesToDelete.Any())
            await DeleteLocalFilesAsync(filesToDelete);

        if (directoriesToDelete.Any())
            await DeleteLocalDirectoriesAsync(directoriesToDelete);
#endif

        await DisconnectAsync();

        stopWatch.Stop();

        if (_options.IsSendAnonymousStatistics)
        {
            cpuMonitor.Stop();
            ramMonitor.Stop();

            var clickHouseStatsDto = new ClickHouseStatsDto()
            {
                OperationSystem = OsVersionManager.GetUserFriendlyName(),
                FileHandledCount = listedExternalFiles.Count,
                DownloadedMegabytes = Math.Round(totalDownloadMegabytes, 2),
                MaxCpuLoadPercentage = cpuMonitor.GetMaxValue(),
                AverageCpuLoadPercentage = cpuMonitor.GetAverageValue(),
                MaxRamLoadMegabytes = ramMonitor.GetMaxValue(),
                AverageRamLoadMegabytes = ramMonitor.GetAverageValue(),
                UpdateTimeMinutes = Math.Round(stopWatch.Elapsed.TotalMinutes, 2),
                UpdateDate = DateTime.UtcNow.ToString("yyyy-dd-MM HH:mm:ss"),
                AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown"
            };

            try
            {
                var clickHouseClient = new ClickHouseServiceClient();

                await clickHouseClient.SendClickHouseStatsAsync(clickHouseStatsDto);
            }
            catch (Exception ex)
            {
                await Logger.WriteLineAsync($"Не удалось отправить анонимную статистику, причина: {ex.Message}. Если хотите, можете отключить ее в настройках, параметр: {nameof(_options.IsSendAnonymousStatistics)}", ConsoleColor.DarkGray, isAnimated: true);
            }
        }

        await Logger.WriteLineAsync($"Все файлы успешно обновлены! Обновление заняло: {stopWatch.Elapsed:hh\\:mm\\:ss}.", ConsoleColor.DarkMagenta);
    }

    private async Task ConnectAsync()
    {
        var token = new CancellationToken();

        await _disconnectWhileConnectionPolicy.ExecuteAsync(async _ => await _ftpClient.AutoConnect(token), _disconnectPolicyContext);

        if (_ftpClient.IsConnected)
        {
            await _ftpClient.SetWorkingDirectory(_options.SyncFolder, token);
        }
        else
        {
            await Logger.WriteLineRedAsync("Не смогли подключиться к удаленному серверу. Проверьте адрес сервера или настройки интернета.");

            Environment.Exit(0);
        }
    }

    private async Task DisconnectAsync()
    {
        await _ftpClient.Disconnect();
    }

    private async Task ReconnectAsync()
    {
        await DisconnectAsync();
        await ConnectAsync();
    }

    private async Task DownloadFileAsync(string localFilePath, string remoteFilePath)
    {
        await _disconnectWhileDownloadingPolicy.ExecuteAsync(async _ => await DownloadAsync(localFilePath, remoteFilePath), _disconnectPolicyContext);
    }

    private async Task DownloadAsync(string localFilePath, string remoteFilePath)
    {
        await _ftpClient.DownloadFile(localFilePath, remoteFilePath, FtpLocalExists.Overwrite);
    }

    private async Task<IEnumerable<string>> GetFilesToDeleteAsync(IEnumerable<string> externalFiles)
    {
        var localFiles = ConvertLocalPathsToFtpFormat(Directory.GetFiles(_currentDirectory, "*.*", SearchOption.AllDirectories));

        var filesToDelete = localFiles.Except(externalFiles).Where(fileToDelete => !_selfIgnore.Contains(fileToDelete));

        var filesToDeleteWithoutIgnore = filesToDelete.Where(fileToDelete => !IsPathIgnoredBySettings(fileToDelete));

        await Logger.WriteLineAsync($"Файлов к удалению: {filesToDeleteWithoutIgnore.Count()} (отфильтровано согласно правилам: {CalculateCountDifferenceBetweenCollections(filesToDelete, filesToDeleteWithoutIgnore)})", ConsoleColor.Yellow);

        return filesToDeleteWithoutIgnore;
    }

    private async Task<IEnumerable<string>> GetDirectoriesToDeleteAsync(IEnumerable<string> externalDirectories)
    {
        var localDirectories = ConvertLocalPathsToFtpFormat(Directory.GetDirectories(_currentDirectory, "*.*", SearchOption.AllDirectories));

        var directoriesToDelete = localDirectories.Except(externalDirectories);

        var directoriesToDeleteWithoutIgnore = directoriesToDelete.Where(directoryToDelete => !IsPathIgnoredBySettings(directoryToDelete));

        await Logger.WriteLineAsync($"Директорий к удалению: {directoriesToDeleteWithoutIgnore.Count()} (отфильтровано согласно правилам: {CalculateCountDifferenceBetweenCollections(directoriesToDelete, directoriesToDeleteWithoutIgnore)}).", ConsoleColor.Yellow);

        return directoriesToDeleteWithoutIgnore;
    }

    private static async Task DeleteLocalFilesAsync(IEnumerable<string> files)
    {
        await Logger.WriteLineRedAsync("Удаляем лишние файлы...");

        foreach (var file in files)
        {
            await Logger.WriteLineRedAsync($"Удаляем лишний {file}...", isAnimated: false);

            File.SetAttributes(file, FileAttributes.Normal);

            File.Delete(file);
        }
    }

    private async Task DeleteLocalDirectoriesAsync(IEnumerable<string> directories)
    {
        await Logger.WriteLineRedAsync("Удаляем лишние директории...");

        foreach (var directory in directories.OrderByDescending(directory => directory.Length))
        {
            await Logger.WriteLineRedAsync($"Удаляем директорию {directory}...", isAnimated: false);

            if (!Directory.Exists(directory))
                continue;

            Directory.Delete(Path.Combine(_currentDirectory, directory), true);
        }
    }

    private IEnumerable<string> ConvertLocalPathsToFtpFormat(IEnumerable<string> localPaths)
    {
        return localPaths.Select(localDirectory =>
            ConvertBackSlashesToUnixFormat(RemoveBasePath(localDirectory, FileLocation.Local)));
    }

    private string RemoveBasePath(string path, FileLocation location)
    {
        return path.Remove(0, location is FileLocation.Local ? _currentDirectoryLength : _externalDirectoryLength);
    }

    private static string ConvertBackSlashesToUnixFormat(string path)
    {
        return !OperatingSystem.IsWindows() ? path : path.Replace("\\", "/");
    }

    private bool IsPathIgnoredBySettings(string path)
    {
        return _options.DeleteIgnore.Any(pattern => !string.IsNullOrEmpty(pattern) && Regex.IsMatch(path, pattern));
    }

    private static int CalculateCountDifferenceBetweenCollections<T>(IEnumerable<T> firstCollection, IEnumerable<T> secondCollection)
    {
        return firstCollection.Count() - secondCollection.Count();
    }

    public AsyncRetryPolicy GetDisconnectWhileDownloadingPolicy()
    {
        return Policy
            .Handle<FtpException>(ex => ex.InnerException?.Message.Contains("Timed out") == true)
            .WaitAndRetryAsync(
                10,
                _ => TimeSpan.FromMilliseconds(2000),
                async (_, _, _) => await ReconnectAsync());
    }

    private static string CutPathIfLong(string path)
    {
        var maxPathLength = (Console.WindowWidth / 2) - 3;

        if (path.Length < maxPathLength)
            return path;

        var splittedPath = path.Split('/');

        var halfOfDirectories = splittedPath.Length / 2;

        var beautify = string.Join('/', splittedPath.Skip(halfOfDirectories).Take(splittedPath.Length - halfOfDirectories));

        return $".../{beautify}";
    }

    public AsyncRetryPolicy GetDisconnectWhileConnectingPolicy()
    {
        return Policy
            .Handle<TimeoutException>()
            .WaitAndRetryAsync(
                10,
                _ => TimeSpan.FromMilliseconds(2000),
                async (_, _, _) => await ReconnectAsync());
    }
}