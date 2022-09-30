using System;

public class Program {
    public static string version = "0.0.1";
    public static bool upgradeEverything = false, exitingBecauseUpgrading = false;
    public static string outdatedSymbol = "", upToDateSymbol = "";
    static void Main(string[] args) {
        //Version
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
        //Setting chars
        if(Environment.OSVersion.Platform == PlatformID.Unix) {
            outdatedSymbol = "✗"; upToDateSymbol = "✓";
        }
        else {
            outdatedSymbol = "x"; upToDateSymbol = "v";
        }
        //Executing the command
        switch(arguments.command) {
            case Command.Help:
                //Printing help message
                Help();
                break;
            case Command.List:
                //Listing all repositories
                List();
                break;
            case Command.Install:
                //Installing a release
                Install(arguments.installArguments);
                break;
            case Command.Update:
                //Downloading updated indexes
                Update();
                break;
            case Command.Upgrade:
                //Upgrading installations
                Upgrade(arguments.upgradeArguments);
                break;
            case Command.Remove:
                //Removing an installation
                Remove(arguments.removeArguments);
                break;
        }
    }
    public static void Help() {
        //Print help command
        Logger.WriteLine("Usage: github-updater [COMMAND]", ConsoleColor.DarkBlue);
        Logger.WriteLine("Commands:", ConsoleColor.Green);
        Logger.WriteLine("  h, help                                                                            Print help message            ", ConsoleColor.Yellow);
        Logger.WriteLine("  l, list                                                                            List all repositories         ", ConsoleColor.Yellow);
        Logger.WriteLine("  i, install <(repository) (user) (path)> <\"l\"/\"latest\">                             Install a release             ", ConsoleColor.Yellow);
        Logger.WriteLine("     Optional arguments:                                                                                           ", ConsoleColor.Yellow);
        Logger.WriteLine("      (repository) = Repository name, (user) = User name, (path) = Installation path                               ", ConsoleColor.Yellow);
        Logger.WriteLine("      \"l\"/\"latest\" = Select latest version without asking                                                          ", ConsoleColor.Yellow);
        Logger.WriteLine("  u, update                                                                          Download updated indexes      ", ConsoleColor.Yellow);
        Logger.WriteLine("  p, upgrade <\"e\"/\"everything\">                                                      Upgrade installations         ", ConsoleColor.Yellow);
        Logger.WriteLine("     Optional arguments:                                                                                           ", ConsoleColor.Yellow);
        Logger.WriteLine("      \"e\"/\"everything\" = Upgrade everything without asking for confirmation                                        ", ConsoleColor.Yellow);
        Logger.WriteLine("  r, remove <(repository)>                                                           Removes an installation       ", ConsoleColor.Yellow);
        Logger.WriteLine("     Optional arguments:                                                                                           ", ConsoleColor.Yellow);
        Logger.WriteLine("      (repository) = Repository name                                                                               ", ConsoleColor.Yellow);
        Logger.WriteLine("<> = Optional argument                                                                                             ", ConsoleColor.Cyan);
        Logger.WriteLine("\"\" = Literal string without quotation marks                                                                        ", ConsoleColor.Cyan);
        Logger.WriteLine();
    }
    public static void List() {
        Repositories repositories;
        Index index;
        //Get repositories index (github-updater.repositories.json)
        try {repositories = JsonManager.ReadRepositoriesIndex();}
        catch(Exception e) {
            Logger.WriteLine("Error while parsing repositories index, exception: " + e, ConsoleColor.Red);
            return;
        }
        Logger.WriteLine("(The indexes are local and might be outdated)", ConsoleColor.Yellow);
        Logger.WriteLine();
        //github-updater repository
        if(repositories.updater == null || repositories.updater.repository == null) return;
        index = JsonManager.ReadRepositoryIndex(repositories.updater.repository);
        Logger.Write("  Repository:     ", ConsoleColor.DarkBlue); Logger.WriteLine(repositories.updater.repository, ConsoleColor.Cyan);
        Logger.Write("  User:           ", ConsoleColor.DarkBlue); Logger.WriteLine(repositories.updater.user, ConsoleColor.Cyan);
        Logger.Write("  Path:           ", ConsoleColor.DarkBlue); Logger.WriteLine(repositories.updater.path, ConsoleColor.Cyan);
        Logger.Write("  Latest version: ", ConsoleColor.DarkBlue); Logger.WriteLine(index.latest, ConsoleColor.Cyan);
        Logger.Write("  Local version:  ", ConsoleColor.DarkBlue); Logger.Write(repositories.updater.version + " ", ConsoleColor.Cyan);
        Version latest = new Version(), local = new Version();
        try {
            latest = new Version(index.latest); local = new Version(repositories.updater.version);
            if(Version.IsOutdated(latest, local))
                Logger.WriteLine(outdatedSymbol + " Outdated", ConsoleColor.Red);
            else
                Logger.WriteLine(upToDateSymbol + " Up-to-date", ConsoleColor.Green);
        }
        catch(Exception e) {
            Logger.WriteLine("Error while parsing version, exception: " + e, ConsoleColor.Red);
        }
        Logger.WriteLine();
        //External repositories
        if(repositories.repositories == null || repositories.repositories.Length == 0) {
            Logger.WriteLine("0 External repositories found", ConsoleColor.Yellow);
            Logger.WriteLine();
            return;
        }
        Logger.WriteLine(repositories.repositories.Length.ToString() + " External repositor"
            + ((repositories.repositories.Length == 1) ? "y" : "ies") + " found: ", ConsoleColor.Green);
        Logger.WriteLine();
        foreach(Repository item in repositories.repositories) {
            if(item.repository != null) {
                index = JsonManager.ReadRepositoryIndex(item.repository);
                Logger.Write("  Repository:     ", ConsoleColor.DarkBlue); Logger.WriteLine(item.repository, ConsoleColor.Cyan);
                Logger.Write("  User:           ", ConsoleColor.DarkBlue); Logger.WriteLine(item.user, ConsoleColor.Cyan);
                Logger.Write("  Path:           ", ConsoleColor.DarkBlue); Logger.WriteLine(item.path, ConsoleColor.Cyan);
                Logger.Write("  Latest version: ", ConsoleColor.DarkBlue); Logger.WriteLine(index.latest, ConsoleColor.Cyan);
                Logger.Write("  Local version:  ", ConsoleColor.DarkBlue); Logger.Write(item.version + " ", ConsoleColor.Cyan);
                try {
                    latest = new Version(index.latest); local = new Version(item.version);
                    if(Version.IsOutdated(latest, local))
                        Logger.WriteLine(outdatedSymbol + " Outdated", ConsoleColor.Red);
                    else
                        Logger.WriteLine(upToDateSymbol + " Up-to-date", ConsoleColor.Green);
                }
                catch(Exception e) {
                    Logger.WriteLine("Error while parsing version, exception: " + e, ConsoleColor.Red);
                }
                Logger.WriteLine();
            }
        }
    }
    public static void Install(InstallArguments args) {
        Repository repository = new Repository();
        //Asking for repository, user and path
        if(args.repository == null || args.user == null || args.path == null) {
            Logger.Write("Insert the repository name: ", ConsoleColor.DarkBlue);
            repository.repository = Logger.ReadString();
            Logger.Write("Insert the user name: ", ConsoleColor.DarkBlue);
            repository.user = Logger.ReadString();
            Logger.Write("Insert the installation path: ", ConsoleColor.DarkBlue);
            do {
                repository.path = Logger.ReadString();
                if(!Directory.Exists(repository.path))
                    Logger.Write("  Not a valid path. New input: ", ConsoleColor.Red);
            } while(!Directory.Exists(repository.path));
        }
        else { //Repository, user and path already given by arguments
            repository.repository = args.repository;
            repository.user = args.user;
            repository.path = args.path;
            Logger.Write("Repository: ", ConsoleColor.DarkBlue); Logger.WriteLine(repository.repository, ConsoleColor.White);
            Logger.Write("User:       ", ConsoleColor.DarkBlue); Logger.WriteLine(repository.user, ConsoleColor.White);
            Logger.Write("Path:       ", ConsoleColor.DarkBlue); Logger.WriteLine(repository.path, ConsoleColor.White);
        }
        if(repository.path == "./" || repository.path == "." || repository.path == ".\\") repository.path = null;
        //Downloading the index
        if(!Client.DownloadIndex(repository.user, repository.repository)) {
            Logger.WriteLine("Error, either the repository doesn't have a github-updater." + repository.repository + ".json file, "
            + "the repository doesn't exist or another error occourred", ConsoleColor.Red);
            return;
        }
        //Reading the index
        Index index = JsonManager.ReadRepositoryIndex(repository.repository);
        try {Client.DownloadRelease(ref repository, index, args.latest);}
        catch(Exception e) {
            Logger.WriteLine("Error while downloading release, exception: " + e, ConsoleColor.Red);
            return;
        }
        //Adding the repository to github-updater.repositories.json
        Repositories repositories;
        try {repositories = JsonManager.ReadRepositoriesIndex();}
        catch(Exception e) {
            Logger.WriteLine("Error while parsing repositories index, exception: " + e, ConsoleColor.Red);
            return;
        }
        if(repositories.repositories == null) repositories.repositories = new Repository[0];
        int pos;
        if(Client.IsInRepositoriesIndex(repositories, repository, out pos))
            repositories.repositories[pos] = repository;
        else
            repositories.repositories = repositories.repositories.Append(repository).ToArray();
        try {JsonManager.WriteRepositoriesIndex(repositories);}
        catch(Exception e) {Logger.WriteLine(e.ToString(), ConsoleColor.Red);}
        Logger.WriteLine();
    }
    public static void Update() {
        Repositories repositories;
        //Get repositories index (github-updater.repositories.json)
        try {repositories = JsonManager.ReadRepositoriesIndex();}
        catch(Exception e) {
            Logger.WriteLine("Error while parsing repositories index, exception: " + e, ConsoleColor.Red);
            return;
        }
        //github-updater repository
        if(repositories.updater == null || repositories.updater.repository == null || repositories.updater.user == null) return;
        Logger.Write("  Repository: ", ConsoleColor.DarkBlue); Logger.WriteLine(repositories.updater.repository, ConsoleColor.Cyan);
        Logger.Write("  User:       ", ConsoleColor.DarkBlue); Logger.WriteLine(repositories.updater.user, ConsoleColor.Cyan);
        Client.DownloadIndex(repositories.updater.user, repositories.updater.repository);
        Logger.WriteLine();
        //External repositories
        if(repositories.repositories == null || repositories.repositories.Length == 0) {
            Logger.WriteLine("0 External repositories found", ConsoleColor.Yellow);
            Logger.WriteLine();
            return;
        }
        Logger.WriteLine(repositories.repositories.Length.ToString() + " External repositor"
            + ((repositories.repositories.Length == 1) ? "y" : "ies") + " found: ", ConsoleColor.Green);
        Logger.WriteLine();
        foreach(Repository item in repositories.repositories) {
            if(item.repository != null && item.user != null) {
                Logger.Write("  Repository: ", ConsoleColor.DarkBlue); Logger.WriteLine(item.repository, ConsoleColor.Cyan);
                Logger.Write("  User:       ", ConsoleColor.DarkBlue); Logger.WriteLine(item.user, ConsoleColor.Cyan);
                Client.DownloadIndex(item.user, item.repository);
                Logger.WriteLine();
            }
        }
    }
    public static void Upgrade(UpgradeArguments args) {
        Repositories repositories;
        Index index;
        //Get repositories index (github-updater.repositories.json)
        try {repositories = JsonManager.ReadRepositoriesIndex();}
        catch(Exception e) {
            Logger.WriteLine("Error while parsing repositories index, exception: " + e, ConsoleColor.Red);
            return;
        }
        Logger.WriteLine("(The indexes are local and might be outdated)", ConsoleColor.Yellow);
        //Asking if the user wants to update everything
        if(args.everything == null || args.everything == false) {
            Logger.Write("Do you want to update everything without confirmation? [Y/n]: ", ConsoleColor.DarkBlue);
            upgradeEverything = Logger.ReadYesNo();
        }
        else upgradeEverything = true;
        Logger.WriteLine();
        //github-updater repository
        if(repositories.updater == null || repositories.updater.repository == null) return;
        index = JsonManager.ReadRepositoryIndex(repositories.updater.repository);
        Logger.Write("  Repository:     ", ConsoleColor.DarkBlue); Logger.WriteLine(repositories.updater.repository, ConsoleColor.Cyan);
        Logger.Write("  User:           ", ConsoleColor.DarkBlue); Logger.WriteLine(repositories.updater.user, ConsoleColor.Cyan);
        Logger.Write("  Path:           ", ConsoleColor.DarkBlue); Logger.WriteLine(repositories.updater.path, ConsoleColor.Cyan);
        Logger.Write("  Local version:  ", ConsoleColor.DarkBlue); Logger.Write(repositories.updater.version + " ", ConsoleColor.Cyan);
        Version latest = new Version(), local = new Version();
        bool outdated = false, update = false;
        try {
            latest = new Version(index.latest); local = new Version(repositories.updater.version);
            if(Version.IsOutdated(latest, local)) {
                Logger.WriteLine(outdatedSymbol + " Outdated", ConsoleColor.Red);
                outdated = true;
            }
            else
                Logger.WriteLine(upToDateSymbol + " Up-to-date", ConsoleColor.Green);
        }
        catch(Exception e) {
            Logger.WriteLine("Error while parsing version, exception: " + e, ConsoleColor.Red);
        }
        //Updating
        if(outdated) {
            if(!upgradeEverything) {
                Logger.Write("  Do you want to update it to the latest version? [Y/n]: ", ConsoleColor.DarkBlue);
                update = Logger.ReadYesNo();
            }
            if(upgradeEverything || update) {
                Repository temp = repositories.updater;
                try {
                    Client.DownloadRelease(ref temp, index, true);
                    if(exitingBecauseUpgrading){
                        //Updating repositories index
                        try {JsonManager.WriteRepositoriesIndex(repositories);}
                        catch(Exception e) {Logger.WriteLine(e.ToString(), ConsoleColor.Red);}
                        return;
                    }
                }
                catch(Exception e) {Logger.WriteLine("Error while downloading release, exception: " + e, ConsoleColor.Red);}
                repositories.updater = temp;
            }
        }
        Logger.WriteLine();
        //External repositories
        if(repositories.repositories == null || repositories.repositories.Length == 0) {
            Logger.WriteLine("0 External repositories found", ConsoleColor.Yellow);
            Logger.WriteLine();
        }
        else {
            Logger.Write(repositories.repositories.Length.ToString() + " External repositor"
                + ((repositories.repositories.Length == 1) ? "y" : "ies") + " found: ", ConsoleColor.Green);
            Logger.WriteLine();
            int i = 0;
            foreach(Repository item in repositories.repositories) {
                if(item.repository != null) {
                    index = JsonManager.ReadRepositoryIndex(item.repository);
                    Logger.Write("  Repository:     ", ConsoleColor.DarkBlue); Logger.WriteLine(item.repository, ConsoleColor.Cyan);
                    Logger.Write("  User:           ", ConsoleColor.DarkBlue); Logger.WriteLine(item.user, ConsoleColor.Cyan);
                    Logger.Write("  Path:           ", ConsoleColor.DarkBlue); Logger.WriteLine(item.path, ConsoleColor.Cyan);
                    Logger.Write("  Local version:  ", ConsoleColor.DarkBlue); Logger.Write(item.version + " ", ConsoleColor.Cyan);
                    outdated = false;
                    try {
                        latest = new Version(index.latest); local = new Version(item.version);
                        if(Version.IsOutdated(latest, local)) {
                            Logger.WriteLine(outdatedSymbol + " Outdated", ConsoleColor.Red);
                            outdated = true;
                        }
                        else
                            Logger.WriteLine(upToDateSymbol + " Up-to-date", ConsoleColor.Green);
                    }
                    catch(Exception e) {
                        Logger.WriteLine("Error while parsing version, exception: " + e, ConsoleColor.Red);
                    }
                    //Updating
                    if(outdated) {
                        if(!upgradeEverything) {
                            Logger.Write("  Do you want to update it to the latest version? [Y/n]: ", ConsoleColor.DarkBlue);
                            update = Logger.ReadYesNo();
                        }
                        if(upgradeEverything || update) {
                            Repository temp = item;
                            try {Client.DownloadRelease(ref temp, index, true);}
                            catch(Exception e) {Logger.WriteLine("Error while downloading release, exception: " + e, ConsoleColor.Red);}
                            repositories.repositories[i] = temp;
                        }
                    }
                    Logger.WriteLine();
                }
                i++;
            }
        }
        //Updating repositories index
        try {JsonManager.WriteRepositoriesIndex(repositories);}
        catch(Exception e) {Logger.WriteLine(e.ToString(), ConsoleColor.Red);}
    }
    public static void Remove(RemoveArguments args) {
        Repositories repositories;
        Repository repository = new Repository();
        //Get repositories index (github-updater.repositories.json)
        try {repositories = JsonManager.ReadRepositoriesIndex();}
        catch(Exception e) {
            Logger.WriteLine("Error while parsing repositories index, exception: " + e, ConsoleColor.Red);
            return;
        }
        //Asking for repository
        if(args.repository == null) {
            Logger.Write("Insert the repository name: ", ConsoleColor.DarkBlue);
            repository.repository = Logger.ReadString();
        }
        else { //Repository already given by argument
            repository.repository = args.repository;
            Logger.Write("Repository: ", ConsoleColor.DarkBlue); Logger.WriteLine(repository.repository, ConsoleColor.White);
        }
        //Searching repository in the repositories index
        int pos;
        if(Client.IsInRepositoriesIndex(repositories, repository, out pos) && repositories.repositories != null) {
            repository = repositories.repositories[pos];
            //Removing it from the index
            repositories.repositories = Client.RemoveAt(repositories.repositories, pos);
            try {
                JsonManager.WriteRepositoriesIndex(repositories);
                Logger.WriteLine("  Succesfully removed " + repository.repository + " from repositories index", ConsoleColor.Green);
            }
            catch(Exception e) {Logger.WriteLine(e.ToString(), ConsoleColor.Red);}
        }
        else {
            Logger.WriteLine("Error, could not find repository " + repository.repository + " in the repositories index", ConsoleColor.Red);
        }
        //Removing the repository index
        string file = Client.GetFullPathFromExecutable("index/repositories/github-updater." + repository.repository + ".json");
        try {
            if(!File.Exists(file)) throw(new FileNotFoundException());
            File.Delete(file);
            Logger.WriteLine("  Succesfully removed " + repository.repository + " index file", ConsoleColor.Green);
        }
        catch(Exception e) {Logger.WriteLine("Error while attempting to remove file " + file + ", exception: " + e, ConsoleColor.Red);}
        //Removing installation
        try {
            if(repository.path == null) throw(new NullReferenceException("Null path exception"));
            Client.DeleteExceptKeep(repository.path, null);
            Directory.Delete(repository.path);
            Logger.WriteLine("  Succesfully removed installation from " + repository.path, ConsoleColor.Green);
        }
        catch(Exception e) {Logger.WriteLine("Error while attempting to remove installation, exception: " + e, ConsoleColor.Red);}
        Logger.WriteLine();
    }
}