public class Arguments {
    /// <summary>
    /// Initializer
    /// </summary>
    public Arguments() {
        errors = 0;
    }

    public Int16 errors;
    public Command command;

    /// <summary>
    /// Function to parse the arguments
    /// </summary>
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
                case "a":
                case "add":
                    command = Command.Add;
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

public enum Command {
    Help = 0,
    List = 1,
    Add = 2
}