using System.Reflection;

public class Client {
    /// <summary>
    /// Function to download the release index of a repository
    /// (<paramref name="user"/>, <paramref name="repository"/>)
    /// </summary>
    /// <param name="user">The github username</param>
    /// <param name="repository">The github repository name</param>
    /// <returns>True if the download succedeed</returns>
    public static bool DownloadIndex(string user, string repository) {
        string url = "https://raw.githubusercontent.com/" + user + "/" + repository + "/main/github-updater." + repository + ".json";
        string tempFile = GetFullPathFromExecutable("index/repositories/github-updater." + repository + ".temp.json");
        string file = GetFullPathFromExecutable("index/repositories/github-updater." + repository + ".json");
        if(!Directory.Exists("index/repositories")) {
            try {Directory.CreateDirectory(GetFullPathFromExecutable("index/repositories"));}
            catch(Exception e) {
                Logger.WriteLine("Error creating folder for repositories indexes, exception: " + e, ConsoleColor.Red);
                return false;
            }
        }
        try {
            using (var client = new HttpClient()) {
                using (var s = client.GetStreamAsync(url)) {
                    using (var fs = new FileStream(tempFile, FileMode.Create)) {
                        s.Result.CopyTo(fs);
                    }
                }
            }
            if(File.Exists(file)) {
                File.Delete(file);
            }
            File.Move(tempFile, file);
            Logger.WriteLine("  Succesfully downloaded index from " + user + "/" + repository, ConsoleColor.Green);
            return true;
        }
        catch(Exception e) {
            Logger.WriteLine("  Error dowloading index from " + user + "/" + repository + ", exception: " + e, ConsoleColor.Red);
            try {File.Delete(tempFile);}
            catch(Exception) {}
            return false;
        }
    }
    public static void DownloadRelease(Repository repository, Index index) {
        if(repository.repository == null || repository.user == null)
            throw(new Exception("Null repository exception"));
        //Checking installation path
        if(repository.path == null) repository.path = "";
        else {
            if(!Directory.Exists(repository.path)) {
                try {
                    Directory.CreateDirectory(repository.path);
                }
                catch(Exception e) {throw(new Exception("Error while attempting directory creation, exception: " + e));}
            }
        }
        //TODO
    }
    /// <summary>
    /// Function to get the full path from the executable location
    /// (<paramref name="path"/>)
    /// </summary>
    /// <param name="path">The relative path from the executable</param>
    /// <returns>The full path</returns>
    public static string GetFullPathFromExecutable(string path) {
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + path;
    }
}