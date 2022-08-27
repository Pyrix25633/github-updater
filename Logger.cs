using System.IO.Compression;

public class Logger {
    private static string barFull = "█", barEmpty = " ";
    /// <summary>
    /// Function to output a message
    /// (<paramref name="message"/>, <paramref name="color"/>)
    /// </summary>
    /// <param name="message">The message to output</param>
    /// <param name="color">The console foreground color, default is ConsoleColor.White</param>
    public static void Write(string message, ConsoleColor color = ConsoleColor.White) {
        Console.ForegroundColor = color;
        Console.Write(message);
        Console.ResetColor();
    }
    /// <summary>
    /// Function to output a message and go to a new line
    /// (<paramref name="message"/>, <paramref name="color"/>)
    /// </summary>
    /// <param name="message">The message to output, default is empty string</param>
    /// <param name="color">The console foreground color, default is ConsoleColor.White</param>
    public static void WriteLine(string message = "", ConsoleColor color = ConsoleColor.White) {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    /// <summary>
    /// Function to clear the last console line
    ///(<paramref name="line"/>)
    /// </summary>
    /// <param name="line">The line to remove, default 1</param>
    public static void RemoveLine(Int16 line = 1) {
        Int32 currentLineCursor = Console.CursorTop;
        Console.SetCursorPosition(0, currentLineCursor - line);
        for (Int32 i = 0; i < Console.WindowWidth; i++)
            Console.Write(" ");
        Console.SetCursorPosition(0, currentLineCursor - line);
    }
    /// <summary>
    /// Function to get the string time
    /// </summary>
    /// <returns>The string time, hh:mm:ss.msmsms</returns>
    public static string TimeString() {
        int hour = DateTime.Now.Hour, minute = DateTime.Now.Minute, 
            second = DateTime.Now.Second, millisecond = DateTime.Now.Millisecond;
        return "[" + (hour < 10 ? "0" : "") + hour.ToString() + ":" + (minute < 10 ? "0" : "") + minute.ToString() + ":" +
               (second < 10 ? "0" : "") + second.ToString() + "." +
               (millisecond < 100 ? (millisecond < 10 ? "00" : "0") : "") + millisecond.ToString() + "] ";
    }
    /// <summary>
    /// Function to get the long string time
    /// </summary>
    /// <returns>The long string time, YYYY-MM-DD_hh.mm.ss.msmsms</returns>
    public static string LongTimeString() {
        int year = DateTime.Now.Year, month = DateTime.Now.Month, day = DateTime.Now.Day,
            hour = DateTime.Now.Hour, minute = DateTime.Now.Minute, 
            second = DateTime.Now.Second, millisecond = DateTime.Now.Millisecond;
        return year.ToString() +  "-" + (month < 10 ? "0" : "") + month.ToString() + "-" +
               (day < 10 ? "0" : "") + day.ToString() + "_" + (hour < 10 ? "0" : "") + hour.ToString() + "." +
               (minute < 10 ? "0" : "") + minute.ToString() + "." + (second < 10 ? "0" : "") + second.ToString() + "." +
               (millisecond < 100 ? (millisecond < 10 ? "00" : "0") : "") + millisecond.ToString();
    }
    /// <summary>
    /// Function to print a progress bar string
    /// (<paramref name="current"/>, <paramref name="total"/>)
    /// </summary>
    /// <param name="current">The current stage</param>
    /// <param name="total">The total</param>
    public static void ProgressBar(UInt64 current, UInt64 total) {
        string bar = "[";
        Int16 percent = (Int16)((float)current / total * 100);
        for(Int16 i = 1; i <= percent; i++) {
            bar += barFull;
        }
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(bar);
        bar = "";
        for(Int16 i = (Int16)(percent + 1); i <= 100; i++) {
            bar += barEmpty;
        }
        Console.BackgroundColor = ConsoleColor.DarkGray;
        Console.Write(bar);
        bar = "] " + percent.ToString() + "% (" + HumanReadableSize(current) + "/" + HumanReadableSize(total) + ")";
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine(bar);
        Console.ResetColor();
    }
    /// <summary>
    /// Function to print a progress bar string
    /// (<paramref name="size"/>)
    /// </summary>
    /// <param name="size">The size in bytes</param>
    /// <returns>A string with size and unit</returns>
    public static string HumanReadableSize(UInt64 size) {
        UInt16 unit = 1024;
        // Bytes
        if(size < unit) return size.ToString() + "B";
        // KiBytes
        UInt64 KiBytes = (UInt64)Math.Floor((float)size / unit);
        UInt16 Bytes = (UInt16)(size % unit);
        if(KiBytes < unit) return KiBytes.ToString() + "KiB&" + Bytes.ToString() + "B";
        // MiBytes
        UInt32 MiBytes = (UInt32)Math.Floor((float)KiBytes / unit);
        KiBytes %= unit;
        if(MiBytes < unit) return MiBytes.ToString() + "MiB&" + KiBytes.ToString() + "KiB";
        UInt16 GiBytes = (UInt16)Math.Floor((float)MiBytes / unit);
        MiBytes %= unit;
        return GiBytes.ToString() + "GiB&" + MiBytes.ToString() + "MiB";
    }
}