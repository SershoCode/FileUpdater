namespace SershoCode.FileUpdater.Logging;

public static class Logger
{
    private const ConsoleColor DefaultTextColor = ConsoleColor.White;
    private const ConsoleColor DefaultBackgroundColor = ConsoleColor.Black;
    private const int DefaultAnimationDelayMs = 12;
    private static int _lastLineWriteNumber = 0;

    public static async Task WriteLineAsync(string text,
                                 ConsoleColor textColor = DefaultTextColor,
                                 ConsoleColor backgroundColor = DefaultBackgroundColor,
                                 bool isInCurrentLine = false,
                                 bool isInBottomLine = false,
                                 bool isAnimated = true,
                                 int delayBetweenSymbolsMs = DefaultAnimationDelayMs)
    {
        var currentDate = isInBottomLine ? string.Empty : DateTime.Now.ToString("[HH:mm:ss]");

        var textWithDate = $"{currentDate} {text}";

        SetConsoleColors(textColor, backgroundColor);

        if (isInCurrentLine)
            PrepareCurrentLine();

        if (isInBottomLine)
            PrepareBottomLine();

        if (isAnimated)
            await AnimateAsync(textWithDate, delayBetweenSymbolsMs);
        else
            Console.Write(textWithDate);

        if (!isInCurrentLine && !isInBottomLine)
            Console.Write('\n');

        if (isInBottomLine)
            Console.SetCursorPosition(0, _lastLineWriteNumber);

        SetConsoleColors(DefaultTextColor, DefaultBackgroundColor);
    }

    public static void HideCursor()
    {
        Console.CursorVisible = false;
    }

    private static void PrepareCurrentLine()
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, Console.CursorTop);
    }

    private static void PrepareBottomLine()
    {
        _lastLineWriteNumber = Console.CursorTop;

        var bottomLineNumber = Console.WindowHeight - 1;

        Console.SetCursorPosition(0, bottomLineNumber);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, bottomLineNumber);
    }

    public static void ClearBottomLine()
    {
        var currentCursorPos = Console.CursorTop;

        var bottomLineNumber = Console.WindowHeight - 1;

        Console.SetCursorPosition(0, bottomLineNumber);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, currentCursorPos);
    }

    public static async Task ClearConsoleAsync()
    {
        var currentCursorPos = Console.CursorTop;

        for (var i = currentCursorPos; i != -1; i--)
        {
            Console.SetCursorPosition(0, i);

            Console.Write(new string(' ', Console.WindowWidth));

            await Task.Delay(50);
        }

        Console.SetCursorPosition(0, 0);
    }

    public static async Task WriteLineRedAsync(string text, bool isCurrentLine = false, bool isBottomLine = false, bool isAnimated = true, int delayBetweenSymbolsMs = DefaultAnimationDelayMs)
    {
        await WriteLineAsync(text, ConsoleColor.Red, DefaultBackgroundColor, isCurrentLine, isBottomLine, isAnimated, delayBetweenSymbolsMs);
    }

    public static async Task WriteLineGreenAsync(string text, bool isCurrentLine = false, bool isBottomLine = false, bool isAnimated = true, int delayBetweenSymbolsMs = DefaultAnimationDelayMs)
    {
        await WriteLineAsync(text, ConsoleColor.Green, DefaultBackgroundColor, isCurrentLine, isBottomLine, isAnimated, delayBetweenSymbolsMs);
    }

    private static async Task AnimateAsync(string text, int delayBetweenSymbolsMs = DefaultAnimationDelayMs)
    {
        foreach (var ch in text)
        {
            await Console.Out.WriteAsync(ch);

            await Task.Delay(delayBetweenSymbolsMs);
        }
    }

    public static void UpLine()
    {
        Console.CursorTop--;

        PrepareCurrentLine();
    }

    private static void SetConsoleColors(ConsoleColor textColor, ConsoleColor backgroundColor)
    {
        if (Console.ForegroundColor != textColor)
            Console.ForegroundColor = textColor;

        if (Console.BackgroundColor != backgroundColor)
            Console.BackgroundColor = backgroundColor;
    }
}