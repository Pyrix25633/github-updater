using Newtonsoft.Json;

public class JsonManager {
    public static Repositories readRepositoriesIndex() {
        Repositories repositories = new Repositories();
        Repositories? temp;
        string fileContent = File.ReadAllText("index/github-updater.repositories.json");
        temp = JsonConvert.DeserializeObject<Repositories>(fileContent);
        if(temp != null) {
            repositories = temp;
        }
        else {
            throw(new Exception("Error while parsing repositories index"));
        }
        return repositories;
    }
    public static Index readRepositoryIndex(string repository) {
        Index index = new Index();
        Index? temp;
        string fileContent = File.ReadAllText("index/repositories/github-updater." + repository + ".json");
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
    public string? version {get; set;}
}

public class Index {
    public string? latest {get; set;}
    public Release[]? releases {get; set;}
}

public class Release {
    public string? tag {get; set;}
    public string? linux {get; set;}
    public string? win {get; set;}
    public string? cross {get; set;}
}