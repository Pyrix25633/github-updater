using Newtonsoft.Json;

public class JsonManager {
    /// <summary>
    /// Function to read github-updater.repositories.json
    /// </summary>
    /// <returns>The chosen release item</returns>
    public static Repositories ReadRepositoriesIndex() {
        Repositories repositories = new Repositories();
        string index = Client.GetFullPathFromExecutable("index" + Path.DirectorySeparatorChar + "github-updater.repositories.json");
        if(File.Exists(index)) {
            try {
                Repositories? temp;
                string fileContent = File.ReadAllText(index);
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
                throw(new Exception("Error while reading repositories index, exception: " + e));
            }
        }
        else { //Index file not there
            repositories.updater = new Repository();
            repositories.updater.user = "Pyrix25633";
            repositories.updater.repository = "github-updater";
            repositories.updater.path = Client.GetFullPathFromExecutable();
            repositories.updater.version = Program.version;
            string indexDirectory = Client.GetFullPathFromExecutable("index");
            string repositoriesDirectory = indexDirectory + Path.DirectorySeparatorChar + "repositories";
            string githubUpdaterIndex = repositoriesDirectory + Path.DirectorySeparatorChar + "github-updater.github-updater.json";
            if(!Directory.Exists(indexDirectory))
                Directory.CreateDirectory(indexDirectory);
            if(!Directory.Exists(repositoriesDirectory))
                Directory.CreateDirectory(repositoriesDirectory);
            if(!File.Exists(githubUpdaterIndex))
                Client.DownloadIndex(repositories.updater.user, repositories.updater.repository);
            repositories.repositories = null;
            WriteRepositoriesIndex(repositories);
        }
        return repositories;
    }
    /// <summary>
    /// Function to write to github-updater.repositories.json
    /// (<paramref name="repositories"/>)
    /// </summary>
    /// <param name="repositories">The repositories object</param>
    public static void WriteRepositoriesIndex(Repositories repositories) {
        string tempFile = "index" + Path.DirectorySeparatorChar + "github-updater.repositories.temp.json";
        string file = "index" + Path.DirectorySeparatorChar + "github-updater.repositories.json";
        try {
            File.WriteAllText(Client.GetFullPathFromExecutable(tempFile),
                JsonConvert.SerializeObject(repositories, Formatting.Indented));
            File.Delete(file);
            File.Move(tempFile, file);
        }
        catch (Exception e) {
            File.Delete(tempFile);
            throw(new Exception("Error while writing to repositories index, exception: " + e));
        }
    }
    /// <summary>
    /// Function to read github-updater.<repository>.json
    /// (<paramref name="repository"/>)
    /// </summary>
    /// <param name="repository">The repository name</param>
    /// <returns>The chosen release item</returns>
    public static Index ReadRepositoryIndex(string repository) {
        Index index = new Index();
        Index? temp;
        string fileContent = File.ReadAllText(
            Client.GetFullPathFromExecutable("index" + Path.DirectorySeparatorChar + "repositories" + Path.DirectorySeparatorChar + "github-updater." + repository + ".json"));
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