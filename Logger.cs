public class Logger {
    /// <summary>
    /// Function to output a message
    /// (<paramref name="message"/>, <paramref name="color"/>)
    /// </summary>
    /// <param name="message">The message to output</param>
    /// <param name="color">The console foreground color, default is ConsoleColor.White</param>
    public static void Write(string? message, ConsoleColor color = ConsoleColor.White) {
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
    public static void WriteLine(string? message = "", ConsoleColor color = ConsoleColor.White) {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    /// <summary>
    /// Function to input a string
    /// </summary>
    /// <returns>The string, not null</returns>
    public static string ReadString() {
        string? s;
        do {
            s = Console.ReadLine();
            if(s == null || s.Length == 0)
                Write("  The input cannot be null. New input: ", ConsoleColor.Red);
        } while(s == null || s.Length == 0);
        return s;
    }
    /// <summary>
    /// Function to input a char
    /// </summary>
    /// <returns>The char, not null</returns>
    public static char ReadChar() {
        string? s;
        do {
            s = Console.ReadLine();
            if(s == null || s.Length == 0)
                Write("  The input cannot be null. New input: ", ConsoleColor.Red);
        } while(s == null || s.Length == 0);
        return s[0];
    }
    /// <summary>
    /// Function to input a boolean, with yes/no
    /// </summary>
    /// <returns>True if the input is Y, y or Yes</returns>
    public static bool ReadYesNo() {
        string? s;
        char c = ' ';
        do {
            s = Console.ReadLine();
            if(s == null || s.Length == 0)
                Write("  The input cannot be null. New input: ", ConsoleColor.Red);
            else {
                c = s[0];
                c = Char.ToLower(c);
                if(c != 'y' && c != 'n')
                    Write("  Not a valid choice. New input: ", ConsoleColor.Red);
            }
        } while((s == null || s.Length == 0) || (c != 'y' && c!= 'n'));
        return c == 'y';
    }
}