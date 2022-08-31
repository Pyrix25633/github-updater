using Newtonsoft.Json;

public class JsonManager {
    public static Repositories readRepositoriesIndex() {
        Repositories repositories = new Repositories();
        try {
            Repositories? temp;
            string fileContent = File.ReadAllText(Client.GetFullPathFromExecutable("index/github-updater.repositories.json"));
            temp = JsonConvert.DeserializeObject<Repositories>(fileContent);
            if(temp != null && temp.updater != null && temp.updater.repository != null
                && temp.updater.user != null && temp.updater.version != null) {
                repositories = temp;
            }
            else {
                throw(new Exception("Error while parsing repositories index"));
            }
        }
        catch(Exception e) {
            Logger.WriteLine("Error while reading repositories index, exception: " + e, ConsoleColor.Red);
            throw(new Exception());
        }
        //0 repositories found
        if(repositories.repositories == null || repositories.repositories.Length == 0) {
            Logger.WriteLine("0 External repositories found", ConsoleColor.Yellow);
        }
        return repositories;
    }
    public static Index readRepositoryIndex(string repository) {
        Index index = new Index();
        Index? temp;
        string fileContent = File.ReadAllText(
            Client.GetFullPathFromExecutable("index/repositories/github-updater." + repository + ".json"));
        temp = JsonConvert.DeserializeObject<Index>(fileContent);
        if(temp != null) {
            index = temp;
        }
        else {
            throw(new Exception("Error while parsing " + repository + " repository index"));
        }
        return index;
    }
}

public class Repositories {
    public Repository? updater {get; set;}
    public Repository[]? repositories {get; set;}
}

public class Repository {
    public string? repository {get; set;}
    public string? user {get; set;}
    public string? path {get; set;}
    public string? version {get; set;}
}

public class Index {
    public string? latest {get; set;}
    public string[]? keep {get; set;}
    public Release[]? releases {get; set;}
}

public class Release {
    public string? tag {get; set;}
    public string? linux {get; set;}
    public string? win {get; set;}
    public string? cross {get; set;}
}