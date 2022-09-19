using System;

public class Program {
    public static string version = "0.1.0";
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
                Install(arguments.installArguments);
                break;
            case Command.Remove:
                //Removing a repository
                Remove(arguments.removeArguments);
                break;
        }
    }
    public static void Help() {
        //Print help command
        Logger.WriteLine("Usage: github-updater [COMMAND]", ConsoleColor.Blue);
        Logger.WriteLine("Commands:", ConsoleColor.Green);
        Logger.WriteLine("  h, help                                                                            Prints help message           ", ConsoleColor.Yellow);
        Logger.WriteLine("  l, list                                                                            Lists all repositories        ", ConsoleColor.Yellow);
        Logger.WriteLine("  i, install <(repository) (user) (path)> <\"l\">                                      Installs a release            ", ConsoleColor.Yellow);
        Logger.WriteLine("     Optional arguments:                                                                                           ", ConsoleColor.Yellow);
        Logger.WriteLine("      (repository) = Repository name, (user) = User name, (path) = Installation path                               ", ConsoleColor.Yellow);
        Logger.WriteLine("      \"l\" = Select latest version without asking                                                                   ", ConsoleColor.Yellow);
        Logger.WriteLine("  r, remove <(repository)>                                                             Removes an installation       ", ConsoleColor.Yellow);
        Logger.WriteLine("     Optional arguments:                                                                                           ", ConsoleColor.Yellow);
        Logger.WriteLine("      (repository) = Repository name                                                                               ", ConsoleColor.Yellow);
        Logger.WriteLine("<> = Optional argument \"\" = Literal string without quotation marks                                                 ", ConsoleColor.Cyan);
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
        Logger.WriteLine("(The index is local and might be outdated)", ConsoleColor.Yellow);
        Logger.WriteLine();
        //github-updater repository
        if(repositories.updater == null || repositories.updater.repository == null) return;
        index = JsonManager.ReadRepositoryIndex(repositories.updater.repository);
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
        //External repositories
        if(repositories.repositories == null || repositories.repositories.Length == 0)
            Logger.WriteLine("0 External repositories found", ConsoleColor.Yellow);
        else {
            Logger.Write(repositories.repositories.Length.ToString() + " External repositor"
                + ((repositories.repositories.Length == 1) ? "y" : "ies") + " found: ", ConsoleColor.Green);
            Logger.WriteLine();
            foreach(Repository item in repositories.repositories) {
                if(item.repository != null) {
                    index = JsonManager.ReadRepositoryIndex(item.repository);
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
        Logger.WriteLine("  Repository: " + repositories.updater.repository, ConsoleColor.Cyan);
        Logger.WriteLine("  User:       " + repositories.updater.user, ConsoleColor.Cyan);
        Client.DownloadIndex(repositories.updater.user, repositories.updater.repository);
        Logger.WriteLine();
        //External repositories
        if(repositories.repositories == null || repositories.repositories.Length == 0)
            Logger.WriteLine("0 External repositories found", ConsoleColor.Yellow);
        else {
            Logger.WriteLine(repositories.repositories.Length.ToString() + " External repositor"
                + ((repositories.repositories.Length == 1) ? "y" : "ies") + " found: ", ConsoleColor.Green);
            Logger.WriteLine();
            foreach(Repository item in repositories.repositories) {
                if(item.repository != null && item.user != null) {
                    Logger.WriteLine("  Repository: " + item.repository, ConsoleColor.Blue);
                    Logger.WriteLine("  User:       " + item.user, ConsoleColor.Blue);
                    Client.DownloadIndex(item.user, item.repository);
                    Logger.WriteLine();
                }
            }
        }
    }
    public static void Install(InstallArguments args) {
        Repository repository = new Repository();
        //Asking for repository, user and path
        if(args.repository == null || args.user == null || args.path == null) {
            Logger.Write("Insert the repository name: ", ConsoleColor.Blue);
            repository.repository = Logger.ReadString();
            Logger.Write("Insert the user name: ", ConsoleColor.Blue);
            repository.user = Logger.ReadString();
            Logger.Write("Insert the installation path: ", ConsoleColor.Blue);
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
            Logger.Write("Repository: ", ConsoleColor.Blue); Logger.WriteLine(repository.repository, ConsoleColor.White);
            Logger.Write("User:       ", ConsoleColor.Blue); Logger.WriteLine(repository.user, ConsoleColor.White);
            Logger.Write("Path:       ", ConsoleColor.Blue); Logger.WriteLine(repository.path, ConsoleColor.White);
            if(!Directory.Exists(repository.path)) {
                Logger.WriteLine("Error, directory " + args.path + " does not exist", ConsoleColor.Red);
            }
        }
        if(repository.path == "./" || repository.path == ".") repository.path = null;
        //Downloading the index
        if(!Client.DownloadIndex(repository.user, repository.repository)) {
            Logger.WriteLine("Error, either the repository doesn't have a github-updater." + repository.repository + ".json file, "
            + "or the repository doesn't exist");
            return;
        }
        //Reading the index
        Index index = JsonManager.ReadRepositoryIndex(repository.repository);
        try {
            Client.DownloadRelease(ref repository, index, args.latest);
        }
        catch(Exception e) {
            Logger.WriteLine("Error while downloading the release, exception: " + e, ConsoleColor.Red);
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
            Logger.Write("Insert the repository name: ", ConsoleColor.Blue);
            repository.repository = Logger.ReadString();
        }
        else { //Repository already given by argument
            repository.repository = args.repository;
            Logger.Write("Repository: ", ConsoleColor.Blue); Logger.WriteLine(repository.repository, ConsoleColor.White);
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
    }
}