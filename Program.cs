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
                Logger.WriteLine("  h, help                           Prints help message     ", ConsoleColor.Yellow);
                Logger.WriteLine("  l, list                           Lists all repositories  ", ConsoleColor.Yellow);
                Logger.WriteLine("  a, add                            Adds a repository       ", ConsoleColor.Yellow);
                break;
            case Command.List:
                Repositories repositories;
                try {
                    repositories = JsonManager.readRepositoriesIndex();
                }
                catch(Exception e) {
                    Logger.WriteLine("Error while reading repositories index, exception: " + e, ConsoleColor.Red);
                    return;
                }
                Index index;
                if(repositories.repositories == null) {
                    Logger.WriteLine("0 Repositories found", ConsoleColor.Yellow);
                    return;
                }
                foreach(Repository item in repositories.repositories) {
                    if(item.repository != null) {
                        index = JsonManager.readRepositoryIndex(item.repository);
                        Logger.WriteLine("Repository: " + item.repository, ConsoleColor.Blue);
                        Logger.WriteLine("Latest version: " + index.latest, ConsoleColor.Blue);
                        Logger.Write("Local version: " + item.version + " ", ConsoleColor.Blue);
                        if(Logger.IsOutdated(index.latest, item.version))
                            Logger.WriteLine("✗ Outdated", ConsoleColor.Red);
                        else
                            Logger.WriteLine("✓ Up-to-date", ConsoleColor.Green);
                    }
                }
                break;
            case Command.Add:

                break;
        }
    }
}