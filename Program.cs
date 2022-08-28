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
        Repositories repositories;
        Index index;
        switch(arguments.command) {
            case Command.Help:
                //Printing help message
                Logger.WriteLine("Usage: github-updater [COMMAND]", ConsoleColor.Blue);
                Logger.WriteLine("Commands:", ConsoleColor.Green);
                Logger.WriteLine("  h, help                           Prints help message     ", ConsoleColor.Yellow);
                Logger.WriteLine("  l, list                           Lists all repositories  ", ConsoleColor.Yellow);
                Logger.WriteLine("  a, add                            Adds a repository       ", ConsoleColor.Yellow);
                break;
            case Command.List:
                //Listing local repositories
                //Get repositories index (github-updater.repositories.json)
                try {repositories = JsonManager.readRepositoriesIndex();}
                catch(Exception) {return;}
                if(repositories.repositories == null) return;
                //1 or more repositories, listing versions
                Logger.Write(repositories.repositories.Length.ToString() + " External repositor"
                    + ((repositories.repositories.Length == 1) ? "y" : "ies") + " found: ", ConsoleColor.Green);
                Logger.WriteLine("(The index is local and might be outdated)", ConsoleColor.Yellow);
                Logger.WriteLine();
                if(repositories.updater == null || repositories.updater.repository == null) return;
                index = JsonManager.readRepositoryIndex(repositories.updater.repository);
                Logger.WriteLine("  Repository:     " + repositories.updater.repository, ConsoleColor.Cyan);
                Logger.WriteLine("  User:           " + repositories.updater.user, ConsoleColor.Cyan);
                Logger.WriteLine("  Path:           " + repositories.updater.path, ConsoleColor.Cyan);
                Logger.WriteLine("  Latest version: " + index.latest, ConsoleColor.Cyan);
                Logger.Write("  Local version:  " + repositories.updater.version + " ", ConsoleColor.Cyan);
                if(Logger.IsOutdated(index.latest, repositories.updater.version))
                    Logger.WriteLine("✗ Outdated", ConsoleColor.Red);
                else
                    Logger.WriteLine("✓ Up-to-date", ConsoleColor.Green);
                Logger.WriteLine();
                foreach(Repository item in repositories.repositories) {
                    if(item.repository != null) {
                        index = JsonManager.readRepositoryIndex(item.repository);
                        Logger.WriteLine("  Repository:     " + item.repository, ConsoleColor.Blue);
                        Logger.WriteLine("  User:           " + item.user, ConsoleColor.Blue);
                        Logger.WriteLine("  Path:           " + item.path, ConsoleColor.Blue);
                        Logger.WriteLine("  Latest version: " + index.latest, ConsoleColor.Blue);
                        Logger.Write("  Local version:  " + item.version + " ", ConsoleColor.Blue);
                        if(Logger.IsOutdated(index.latest, item.version))
                            Logger.WriteLine("✗ Outdated", ConsoleColor.Red);
                        else
                            Logger.WriteLine("✓ Up-to-date", ConsoleColor.Green);
                        Logger.WriteLine();
                    }
                }
                break;
            case Command.Update:
                //Updating indexes
                //Get repositories index (github-updater.repositories.json)
                try {repositories = JsonManager.readRepositoriesIndex();}
                catch(Exception) {return;}
                if(repositories.repositories == null) return;
                //1 or more repositories, listing versions
                Logger.WriteLine(repositories.repositories.Length.ToString() + " External repositor"
                    + ((repositories.repositories.Length == 1) ? "y" : "ies") + " found: ", ConsoleColor.Green);
                Logger.WriteLine();
                if(repositories.updater == null || repositories.updater.repository == null || repositories.updater.user == null) return;
                Logger.WriteLine("  Repository:     " + repositories.updater.repository, ConsoleColor.Cyan);
                Logger.WriteLine("  User:           " + repositories.updater.user, ConsoleColor.Cyan);
                Client.DownloadIndex(repositories.updater.user, repositories.updater.repository);
                Logger.WriteLine();
                foreach(Repository item in repositories.repositories) {
                    if(item.repository != null && item.user != null) {
                        Logger.WriteLine("  Repository:     " + item.repository, ConsoleColor.Blue);
                        Logger.WriteLine("  User:           " + item.user, ConsoleColor.Blue);
                        Client.DownloadIndex(item.user, item.repository);
                        Logger.WriteLine();
                    }
                }
                break;
            case Command.Add:

                break;
        }
    }
}