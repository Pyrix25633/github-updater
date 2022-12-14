public class Arguments {
    /// <summary>
    /// Initializer
    /// </summary>
    public Arguments() {
        errors = 0;
        installArguments = new InstallArguments();
        upgradeArguments = new UpgradeArguments();
        removeArguments = new RemoveArguments();
    }

    public Int16 errors;
    public Command command;
    public InstallArguments installArguments;
    public UpgradeArguments upgradeArguments;
    public RemoveArguments removeArguments;

    /// <summary>
    /// Function to parse the arguments
    /// (<paramref name="args"/>)
    /// </summary>
    /// <param name="args">The array of string arguments</param>
    public void Parse(string[] args) {
        Int16 length = (Int16)args.Length;
        if(length == 0) {
            errors = 255;
            return;
        }
        for(Int16 i = 0; i < length; i++) {
            switch(args[i]) {
                case "h":
                case "help":
                    command = Command.Help;
                    break;
                case "l":
                case "list":
                    command = Command.List;
                    break;
                case "i":
                case "install":
                    command = Command.Install;
                    if(length - i >= 4) {
                        installArguments.repository = args[i + 1];
                        installArguments.user = args[i + 2];
                        installArguments.path = args[i + 3];
                        if(length - i >= 5)
                            installArguments.latest = (args[i + 4] == "l") || (args[i + 4] == "latest");
                        else
                            installArguments.latest = false;
                        i = length;
                    }
                    break;
                case "u":
                case "update":
                    command = Command.Update;
                    break;
                case "p":
                case "upgrade":
                    command = Command.Upgrade;
                    if(length - i >= 2) {
                        upgradeArguments.everything = (args[i + 1] == "e") || (args[i + 1] == "everything");
                        i = length;
                    }
                    break;
                case "r":
                case "remove":
                    command = Command.Remove;
                    if(length - i >= 2) {
                        removeArguments.repository = args[i + 1];
                        i = length;
                    }
                    break;
                case "--":
                    if(length == 1) {
                        errors = 255;
                        return;
                    }
                    break;
                default:
                    errors += 1;
                    break;
            }
        }
    }
}

public class InstallArguments {
    public InstallArguments() {
        repository = null;
        user = null;
        path = null;
        latest = null;
    }
    public string? repository;
    public string? user;
    public string? path;
    public bool? latest;
}

public class UpgradeArguments {
    public UpgradeArguments() {
        everything = null;
    }
    public bool? everything;
}

public class RemoveArguments {
    public RemoveArguments() {
        repository = null;
    }
    public string? repository;
}

public enum Command {
    Help = 0,
    List = 1,
    Install = 2,
    Update = 3,
    Upgrade = 4,
    Remove = 5
}