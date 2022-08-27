using System;

public class Program {
    static void Main(string[] args) {
        // Version
        string version = "0.1.0";
        Logger.WriteLine();
        Logger.WriteLine("GitHub updater " + version, ConsoleColor.Cyan);
        Logger.WriteLine();
        //Parsing the arguments
        Arguments arguments = new Arguments();
        arguments.Parse(args);
        if(arguments.errors > 0) {
            if(arguments.errors == 255) {
                Logger.WriteLine("Error: no command detected. Use \"help\" for usage", ConsoleColor.Red);
                Logger.WriteLine();
                return;
            }
            else {
                Logger.WriteLine("Error: unknown command. Use \"help\" for usage", ConsoleColor.Red);
                Logger.WriteLine();
                return;
            }
        }
        //Executing the command
        switch(arguments.command) {
            case Command.Help:
                Logger.WriteLine("Usage: github-updater [COMMAND]", ConsoleColor.Blue);
                Logger.WriteLine("Commands:", ConsoleColor.Green);
                Logger.WriteLine("  h, help                           Prints help message", ConsoleColor.Yellow);
                break;
        }
        Logger.WriteLine();
    }
}