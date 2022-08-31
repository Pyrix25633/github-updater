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
                //Printing help message
                Help();
                break;
            case Command.List:
                //Listing local repositories
                List();
                break;
            case Command.Update:
                //Updating indexes
                Update();
                break;
            case Command.Install:
                //Adding a repository
                Install();
                break;
        }
    }
    public static void Help() {
        Logger.WriteLine("Usage: github-updater [COMMAND]", ConsoleColor.Blue);
        Logger.WriteLine("Commands:", ConsoleColor.Green);
        Logger.WriteLine("  h, help                           Prints help message     ", ConsoleColor.Yellow);
        Logger.WriteLine("  l, list                           Lists all repositories  ", ConsoleColor.Yellow);
        Logger.WriteLine("  i, install                        Install a release       ", ConsoleColor.Yellow);
    }
    public static void List() {
        Repositories repositories;
        Index index;
        //Get repositories index (github-updater.repositories.json)
        try {repositories = JsonManager.readRepositoriesIndex();}
        catch(Exception e) {
            Logger.WriteLine("Error while parsing repositories index, exception: " + e, ConsoleColor.Red);
            return;
        }
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
        Version latest = new Version(), local = new Version();
        try {
            latest = new Version(index.latest); local = new Version(repositories.updater.version);
            if(Version.IsOutdated(latest, local))
            Logger.WriteLine("✗ Outdated", ConsoleColor.Red);
        else
            Logger.WriteLine("✓ Up-to-date", ConsoleColor.Green);
        }
        catch(Exception e) {
            Logger.WriteLine("Error while parsing version, exception: " + e, ConsoleColor.Red);
        }
        Logger.WriteLine();
        foreach(Repository item in repositories.repositories) {
            if(item.repository != null) {
                index = JsonManager.readRepositoryIndex(item.repository);
                Logger.WriteLine("  Repository:     " + item.repository, ConsoleColor.Blue);
                Logger.WriteLine("  User:           " + item.user, ConsoleColor.Blue);
                Logger.WriteLine("  Path:           " + item.path, ConsoleColor.Blue);
                Logger.WriteLine("  Latest version: " + index.latest, ConsoleColor.Blue);
                Logger.Write("  Local version:  " + item.version + " ", ConsoleColor.Blue);
                try {
                    latest = new Version(index.latest); local = new Version(item.version);
                    if(Version.IsOutdated(latest, local))
                        Logger.WriteLine("✗ Outdated", ConsoleColor.Red);
                    else
                        Logger.WriteLine("✓ Up-to-date", ConsoleColor.Green);
                }
                catch(Exception e) {
                    Logger.WriteLine("Error while parsing version, exception: " + e, ConsoleColor.Red);
                }
                Logger.WriteLine();
            }
        }
    }
    public static void Update() {
        Repositories repositories;
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
    }
    public static void Install() {
        //Asking repository, user, path and version
        Logger.Write("Insert the repository name: ", ConsoleColor.Blue);
        string r = Logger.ReadString();
        Logger.Write("Insert the user name: ", ConsoleColor.Blue);
        string u = Logger.ReadString();
        Logger.Write("Insert the installation path: ", ConsoleColor.Blue);
        string p;
        do {
            p = Logger.ReadString();
            if(!Directory.Exists(p))
                Logger.Write("Not a valid path. New input: ", ConsoleColor.Red);
        } while(!Directory.Exists(p));
        //Downloading the index
        if(!Client.DownloadIndex(u, r)) {
            Logger.WriteLine("Error either the repository doesn't have a github-updater." + r + ".json file, "
            + "or the repository doesn't exist");
            return;
        }
        //TODO
        Logger.Write("Insert the version: ", ConsoleColor.Blue);
        string v = Logger.ReadString();
        Logger.WriteLine();
    }
}