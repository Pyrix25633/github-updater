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
    /// <summary>
    /// Function to download a release
    /// (<paramref name="repository"/>, <paramref name="index"/>)
    /// </summary>
    /// <param name="repository">Repository object containing necessary informations</param>
    /// <param name="index">The repository index</param>
    public static void DownloadRelease(ref Repository repository, Index index) {
        if(repository.repository == null || repository.user == null)
            throw(new Exception("Null repository exception"));
        //Checking installation path
        if(repository.path == null) repository.path = GetFullPathFromExecutable("programs/");
        else {
            if(!Directory.Exists(repository.path)) {
                try {
                    Directory.CreateDirectory(repository.path);
                }
                catch(Exception e) {throw(new Exception("Error while attempting directory creation, exception: " + e));}
            }
            //Getting full path
            repository.path = new FileInfo(repository.path).FullName;
        }
        //Temp folder
        string temp = GetFullPathFromExecutable("github-updater.temp/");
        if(!Directory.Exists(temp)) {
            try {
                Directory.CreateDirectory(temp);
            }
            catch(Exception e) {throw(new Exception("Error while attempting directory creation, exception: " + e));}
        }
        //Choosing version
        if(index.releases == null || index.releases.Length == 0)
            throw(new Exception("This repository has 0 releases"));
        Logger.WriteLine(index.releases.Length + " Release" + ((index.releases.Length == 1) ? "" : "s")
            + " found:", ConsoleColor.Green);
        Logger.Write("  ");
        foreach(Release item in index.releases) {
            if(item.tag == null) continue;
            Logger.Write("[" + item.tag + "] ", ConsoleColor.Cyan);
        }
        Logger.WriteLine();
        Logger.Write("Insert the version you want to download (\"l\" will default to the latest version): ", ConsoleColor.Blue);
        do {
            repository.version = Logger.ReadString();
            if(!IsValidTag(index, repository.version) && repository.version != "l")
                Logger.Write("  Not a valid version. New input: ", ConsoleColor.Red);
        } while(!IsValidTag(index, repository.version) && repository.version != "l");
        //Get latest version tag
        if(repository.version == "l") {
            if(index.latest == null) throw(new Exception("Latest release is null"));
            repository.version = index.latest;
            if(!IsValidTag(index, index.latest)) throw(new Exception("Latest release is not valid"));
        }
        //Release files
        Release release = GetReleaseWithTag(index, repository.version);
        int filesCount = 0;
        if(release.linux == null && release.win == null && release.cross == null)
            throw(new Exception("This version has 0 release files"));
        char[] validOptions = new char[0];
        if(release.linux != null) {
            filesCount++; validOptions = validOptions.Append('L').ToArray();
        }
        if(release.win != null) {
            filesCount++; validOptions = validOptions.Append('W').ToArray();
        }
        if(release.cross != null) {
            filesCount++; validOptions = validOptions.Append('C').ToArray();
        }
        Logger.WriteLine(filesCount + " Release file" + ((filesCount == 1) ? "" : "s") + " found:", ConsoleColor.Green);
        Logger.Write("  ");
        if(release.linux != null) Logger.Write("[L]: " + release.linux, ConsoleColor.Cyan);
        if(release.win != null) Logger.Write("[W]: " + release.win, ConsoleColor.Cyan);
        if(release.cross != null) Logger.Write("[C]: " + release.cross, ConsoleColor.Cyan);
        Logger.WriteLine();
        Logger.Write("Chose a release file: ", ConsoleColor.Blue);
        char r;
        do {
            r = Logger.ReadChar();
            if(!validOptions.Contains(r))
                Logger.Write("  Not a valid option. New input: ", ConsoleColor.Red);
        } while(!validOptions.Contains(r));
        //TODO
    }
    /// <summary>
    /// Function to see if a tag is valid
    /// (<paramref name="index"/>, <paramref name="tag"/>)
    /// </summary>
    /// <param name="index">The index</param>
    /// <param name="tag">The tag</param>
    /// <returns>True if the tag is contained in the index</returns>
    public static bool IsValidTag(Index index, string tag) {
        if(index.releases == null) return false;
        foreach(Release item in index.releases) {
            if(item.tag == tag) return true;
        }
        return false;
    }
    /// <summary>
    /// Function to get the chosen release item from the index
    /// (<paramref name="index"/>, <paramref name="tag"/>)
    /// </summary>
    /// <param name="index">The index</param>
    /// <param name="tag">The tag</param>
    /// <returns>The chosen release item</returns>
    public static Release GetReleaseWithTag(Index index, string tag) {
        if(index.releases == null) return new Release();
        foreach(Release item in index.releases) {
            if(item.tag == tag) return item;
        }
        return new Release();
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