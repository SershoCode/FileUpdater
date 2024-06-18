using Newtonsoft.Json;

const int speedAnimationMs = 5;
const int delayToStartUpdateSeconds = 10;

await PrepareConsoleAsync();

#if !DEBUG
const string optionsFile = $"{nameof(SUpdaterOptions)}.json";
#else
const string optionsFile = $"{nameof(SUpdaterOptions)}.Development.json";
#endif

if (!File.Exists(optionsFile))
{
    await Logger.WriteLineRedAsync($"Ошибка! :( Не найден файл настроек: {optionsFile}", delayBetweenSymbolsMs: speedAnimationMs);

    await Task.Delay(TimeSpan.FromSeconds(5));

    return;
}

var options = JsonConvert.DeserializeObject<SUpdaterOptions>(await File.ReadAllTextAsync(optionsFile));

if (options is null)
{
    await Logger.WriteLineRedAsync($"Ошибка! :( Некорректный файл настроек: {optionsFile}", delayBetweenSymbolsMs: speedAnimationMs);

    await Task.Delay(TimeSpan.FromSeconds(5));

    return;
}

if (options.IsCheckAppUpdates && !string.IsNullOrEmpty(options.AppUpdateUrl))
    await UpdateItselfAsync();

if (!options.IsSilentMode)
{
    await PrintWelcomeMessageAsync();

    await GetStartAnswersFromUserAsync();
}
else
{
    await Logger.WriteLineAsync("В настройках включен тихий режим. Не будем задавать никаких вопросов, просто работаем.", isAnimated: true);
}

await Logger.WriteLineAsync($"Будем синхронизироваться с {options.FtpAddress}/{options.SyncFolder} (пользователь: {options.User})", ConsoleColor.Cyan, delayBetweenSymbolsMs: speedAnimationMs);

await PrintDelayToConsole();

await Logger.ClearConsoleAsync();

await PrintUpdateHeaderAsync();

var updater = new Updater(options);

await updater.UpdateAsync();

await Logger.WriteLineAsync("Приложение можно закрывать.", ConsoleColor.DarkMagenta);

await Task.Delay(TimeSpan.FromMinutes(5));

#region Methods

static async Task<T> GetValueFromUserOrDefaultAsync<T>(string userValue, T optionsParameter)
{
    if (!string.IsNullOrEmpty(userValue))
    {
        if (optionsParameter is Enum)
        {
            optionsParameter = (T)Enum.Parse(typeof(T), userValue);
        }
        else
        {
            optionsParameter = (T)Convert.ChangeType(userValue, typeof(T));
        }
    }

    Logger.UpLine();

    await Logger.WriteLineAsync($"{optionsParameter}");

    return optionsParameter;
}

static async Task PrepareConsoleAsync()
{
    ConsoleModeManager.EnableVirtualTerminalProcessing();

    Logger.HideCursor();

    await Logger.ClearConsoleAsync();
}

async Task PrepareSettingsAsync()
{
    await Logger.WriteLineAsync("Введите данные доступа (чтобы подтвердить значение по умолчанию - нажмите Enter)", delayBetweenSymbolsMs: speedAnimationMs);

    await Logger.WriteLineAsync($"[Настройки] Введите адрес синхронизации (по умолчанию: {options.FtpAddress}):", ConsoleColor.DarkYellow, isAnimated: false);
    options.FtpAddress = await GetValueFromUserOrDefaultAsync(Console.ReadLine(), options.FtpAddress);

    await Logger.WriteLineAsync($"[Настройки] Введите пользователя (по умолчанию: {options.User}):", ConsoleColor.DarkYellow, isAnimated: false);
    options.User = await GetValueFromUserOrDefaultAsync(Console.ReadLine(), options.User);

    await Logger.WriteLineAsync($"[Настройки] Введите пользователя (по умолчанию: {options.Password}):", ConsoleColor.DarkYellow, isAnimated: false);
    options.Password = await GetValueFromUserOrDefaultAsync(Console.ReadLine(), options.Password);

    await Logger.WriteLineAsync($"[Настройки] Введите путь до папки, с которой будем синхронизироваться (по умолчанию: {options.SyncFolder}):", ConsoleColor.DarkYellow, isAnimated: false);
    options.SyncFolder = await GetValueFromUserOrDefaultAsync(Console.ReadLine(), options.SyncFolder);
}

async Task UpdateItselfAsync()
{
    await Logger.WriteLineAsync("Проверяем версию приложения...", ConsoleColor.Gray, isAnimated: true, delayBetweenSymbolsMs: speedAnimationMs);

    try
    {
        var selfUpdater = new SelfUpdater(options);

        var isUpdateAvailable = await selfUpdater.IsUpdateAvailable();

        if (isUpdateAvailable)
        {
            await Logger.WriteLineAsync("Доступна новая версия, обновляемся...", ConsoleColor.DarkMagenta, isAnimated: true, delayBetweenSymbolsMs: speedAnimationMs);

            await selfUpdater.UpdateAsync();
        }
        else
        {
            await Logger.WriteLineGreenAsync("Вы используете последнюю версию.", isAnimated: true, delayBetweenSymbolsMs: speedAnimationMs);

            await Task.Delay(TimeSpan.FromSeconds(2));

            await Logger.ClearConsoleAsync();
        }
    }
    catch (Exception ex)
    {
        await Logger.WriteLineRedAsync("Не удалось обработать обновление.", isAnimated: true);
        await Logger.WriteLineRedAsync($"Причина: {ex.Message}", isAnimated: true);
        await Logger.WriteLineRedAsync("Если хотите использовать текущую версию, нажмите Enter.", isAnimated: true);

        Console.ReadLine();
    }
}

async Task GetStartAnswersFromUserAsync()
{
    await Logger.WriteLineAsync("Хотите автоматически взять данные из файла конфигурации? ('Enter' - Да / 'n' - Настроить вручную).", delayBetweenSymbolsMs: speedAnimationMs);

    var answer = Console.ReadLine();

    if (answer?.Equals("n", StringComparison.InvariantCultureIgnoreCase) == true)
    {
        await PrepareSettingsAsync();
    }
    else
    {
        Logger.UpLine();
    }
}

static async Task PrintWelcomeMessageAsync()
{
    await Logger.WriteLineAsync("Добро пожаловать в Sersho's File Updater.");

    await Logger.WriteLineRedAsync("Внимание!", delayBetweenSymbolsMs: speedAnimationMs);
    await Logger.WriteLineRedAsync("Апдейтер удаляет лишние файлы.", delayBetweenSymbolsMs: speedAnimationMs);
    await Logger.WriteLineRedAsync("Он должен строго лежать в корне вашей игры или в пустой папке.", delayBetweenSymbolsMs: speedAnimationMs);
    await Logger.WriteLineRedAsync($"Сейчас вы находитесь в {Directory.GetCurrentDirectory()}", delayBetweenSymbolsMs: speedAnimationMs);
    await Logger.WriteLineRedAsync("Если вы уверены, что в ней можно удалять файлы, то продолжайте.", delayBetweenSymbolsMs: speedAnimationMs);
    await Logger.WriteLineAsync("Нажмите Enter, чтобы начать.");

    Console.ReadKey();
}

static async Task PrintUpdateHeaderAsync()
{
    const char delimiter = '¤';

    await Logger.WriteLineAsync(new string(delimiter, 37), ConsoleColor.DarkGreen, isAnimated: true, delayBetweenSymbolsMs: speedAnimationMs);
    await Logger.WriteLineAsync($"{new string(delimiter, 13)} ОБНОВЛЕНИЕ {new string(delimiter, 12)}", ConsoleColor.DarkGreen, isAnimated: true, delayBetweenSymbolsMs: speedAnimationMs);
    await Logger.WriteLineAsync(new string(delimiter, 37), ConsoleColor.DarkGreen, isAnimated: true, delayBetweenSymbolsMs: speedAnimationMs);
}

async Task PrintDelayToConsole()
{
    var str = $"Загрузка начнется автоматически через {delayToStartUpdateSeconds} секунд...";

    await Logger.WriteLineAsync(str, isInCurrentLine: true);

    const int timeOffset = 12;

    const string targetWord = "через";

    var cursorPosOfTimer = str.IndexOf(targetWord, StringComparison.Ordinal) + targetWord.Length + timeOffset;

    for (var i = delayToStartUpdateSeconds; i != 0; i--)
    {
        Console.CursorLeft = cursorPosOfTimer;

        Console.Write(new string(' ', 12));

        Console.CursorLeft = cursorPosOfTimer;

        Console.Write($"{i} {(i >= 5 ? "секунд" : i != 1 ? "секунды" : "секунду")}...");

        await Task.Delay(1000);
    }
}

#endregion