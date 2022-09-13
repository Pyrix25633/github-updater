using System.Reflection;
using System.IO.Compression;
using System.Text;

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
        string file = GetFullPathFromExecutable("index/repositories/github-updater." + repository + ".json");
        string tempFile = file + ".temp";
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
        if(repository.path == null) repository.path = GetFullPathFromExecutable("programs/" + repository.repository);
        if(!Directory.Exists(repository.path)) {
            try {
                Directory.CreateDirectory(repository.path);
            }
            catch(Exception e) {throw(new Exception("Error while attempting directory creation, exception: " + e));}
        }
        //Getting full path
        repository.path = new FileInfo(repository.path).FullName;
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
        if(release.linux != null) Logger.WriteLine("  [L]: " + release.linux, ConsoleColor.Cyan);
        if(release.win != null) Logger.WriteLine("  [W]: " + release.win, ConsoleColor.Cyan);
        if(release.cross != null) Logger.WriteLine("  [C]: " + release.cross, ConsoleColor.Cyan);
        Logger.Write("Chose a release file: ", ConsoleColor.Blue);
        char r;
        do {
            r = Logger.ReadChar();
            if(!validOptions.Contains(r))
                Logger.Write("  Not a valid option. New input: ", ConsoleColor.Red);
        } while(!validOptions.Contains(r));
        string? releaseFile = null;
        switch(r) {
            case 'L': releaseFile = release.linux; break;
            case 'W': releaseFile = release.win; break;
            case 'C': releaseFile = release.cross; break;
        }
        DownloadRelease(repository, index, releaseFile);
    }
    public static void DownloadRelease(Repository repository, Index index, string? releaseFile) {
        if(releaseFile == null) throw(new Exception("Null release file exception"));
        string url = "https://github.com/" + repository.user + "/" + repository.repository + "/releases/download/"
            + repository.version + "/" + releaseFile;
        string tempDir = GetFullPathFromExecutable("github-updater.temp");
        string tempFile = tempDir + "/" + releaseFile + ".temp";
        //Checking temporary directory
        if(!Directory.Exists(tempDir)) {
            try {
                Directory.CreateDirectory(tempDir);
            }
            catch(Exception e) {throw(new Exception("Error while attempting directory creation, exception: " + e));}
        }
        //Downloading the file
        try {
            Logger.WriteLine("Downloading release file, operation may take some time...", ConsoleColor.Yellow);
            using (var client = new HttpClient()) {
                using (var s = client.GetStreamAsync(url)) {
                    using (var fs = new FileStream(tempFile, FileMode.Create)) {
                        s.Result.CopyTo(fs);
                    }
                }
            }
            Logger.WriteLine("Release file succesfully downloaded", ConsoleColor.Green);
            if(releaseFile.EndsWith(".zip")) {
                //Deleting content except entries listed in the keep array
                Logger.WriteLine("Deleting unnecessary files...", ConsoleColor.Yellow);
                DeleteExceptKeep(repository.path, index.keep);
                //Decompress zip
                Logger.WriteLine("Extracting release file with zip, operation may take some time...", ConsoleColor.Yellow);
                string tempExtractionDir = tempDir + "/" + releaseFile;
                try {
                    Directory.CreateDirectory(tempExtractionDir);
                }
                catch(Exception) {
                    Directory.Delete(tempExtractionDir, true);
                    Directory.CreateDirectory(tempExtractionDir);
                }
                ZipFile.ExtractToDirectory(tempFile, tempExtractionDir);
                Logger.WriteLine("Release extracted", ConsoleColor.Green);
                CopyExceptKeep(tempExtractionDir, repository.path, index.keep);
            }
            else if(releaseFile.EndsWith(".tar.gz")) {
                //Deleting content except entries listed in the keep array
                Logger.WriteLine("Deleting unnecessary files...", ConsoleColor.Yellow);
                DeleteExceptKeep(repository.path, index.keep);
                //Decompress tar.gz
                Logger.WriteLine("Decompressing release file with tar.gz, operation may take some time...", ConsoleColor.Yellow);
                string tempExtractionDir = tempDir + "/" + releaseFile;
                try {
                    Directory.CreateDirectory(tempExtractionDir);
                }
                catch(Exception) {
                    Directory.Delete(tempExtractionDir, true);
                    Directory.CreateDirectory(tempExtractionDir);
                }
                ExtractTarGz(tempFile, tempExtractionDir);
                Logger.WriteLine("Release extracted", ConsoleColor.Green);
                CopyExceptKeep(tempExtractionDir, repository.path, index.keep);
            }
            else {
                try {if(repository.path != null) File.Move(tempFile, repository.path + "/" + releaseFile);}
                catch(Exception e) {Logger.WriteLine("Could not copy file " + tempFile + ", exception: " + e, ConsoleColor.Red);}
            }
        }
        catch(Exception e) {throw(new Exception("Error while downloading release file, exception: " + e));}
    }
    /// <summary>
    /// Function to extract a .tar.gz file
    /// (<paramref name="inputFile"/>, <paramref name="outputDir"/>)
    /// </summary>
    /// <param name="inputFile">The .tar.gz file</param>
    /// <param name="tag">The output directory</param>
    public static void ExtractTarGz(string inputFile, string outputDir) {
        using (var stream = File.OpenRead(inputFile)) {
            using (var gzip = new GZipStream(stream, CompressionMode.Decompress)){
				const int chunk = 4096;
				using (var memStr = new MemoryStream()){
					int read;
					var buffer = new byte[chunk];
					do{
						read = gzip.Read(buffer, 0, chunk);
						memStr.Write(buffer, 0, read);
					} while(read == chunk);
					memStr.Seek(0, SeekOrigin.Begin);
					var buffer2 = new byte[100];
			        while(true) {
				        stream.Read(buffer2, 0, 100);
                        var name = Encoding.ASCII.GetString(buffer2).Trim('\0');
                        if(String.IsNullOrWhiteSpace(name))
                            break;
                        stream.Seek(24, SeekOrigin.Current);
                        stream.Read(buffer2, 0, 12);
                        var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer2, 0, 12).Trim('\0').Trim(), 8);
                        stream.Seek(376L, SeekOrigin.Current);
                        var output = Path.Combine(outputDir, name);
                        var outputDirectoryName = Path.GetDirectoryName(output);
                        if(!Directory.Exists(outputDirectoryName))
                            if(outputDirectoryName != null)
                                Directory.CreateDirectory(outputDirectoryName);
                        if(!name.Equals("./", StringComparison.InvariantCulture)) {
                            using (var str = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write)) {
                                var buf = new byte[size];
                                stream.Read(buf, 0, buf.Length);
                                str.Write(buf, 0, buf.Length);
                            }
                        }
				        var pos = stream.Position;
                        var offset = 512 - (pos  % 512);
                        if(offset == 512)
                            offset = 0;
                        stream.Seek(offset, SeekOrigin.Current);
                    }
				}
			}
        }
    }
    /// <summary>
    /// Function to delete all entries except those on a keep array
    /// (<paramref name="path"/>, <paramref name="keep"/>)
    /// </summary>
    /// <param name="path">The path</param>
    /// <param name="keep">The keep array</param>
    public static void DeleteExceptKeep(string? path, string[]? keep) {
        if(path == null || keep == null || keep.Length == 0) return;
        EnumerationOptions enumOptions = new EnumerationOptions();
        enumOptions.RecurseSubdirectories = true; enumOptions.AttributesToSkip = default;
        string[] filesToDelete = Directory.GetFileSystemEntries(path, "*", enumOptions).Except(keep).Reverse().ToArray();
        foreach(string item in filesToDelete) {
            FileInfo info = new FileInfo(item);
            if((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                try {Directory.Delete(item);}
                catch(Exception e) {Logger.WriteLine("Could not remove directory " + item + ", exception: " + e);}
            else
                try {File.Delete(item);}
                catch(Exception e) {Logger.WriteLine("Could not remove file " + item + ", exception: " + e);}
        }
    }
    public static void CopyExceptKeep(string? source, string? destination, string[]? keep) {
        if(source == null || destination == null || keep == null || keep.Length == 0) return;
        EnumerationOptions enumOptions = new EnumerationOptions();
        enumOptions.RecurseSubdirectories = true; enumOptions.AttributesToSkip = default;
        string[] filesToCopy = Directory.GetFileSystemEntries(source, "*", enumOptions).Except(keep).Reverse().ToArray();
        foreach(string item in filesToCopy) {
            FileInfo info = new FileInfo(item);
            string destinationPath = destination + "/" + item;
            if((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                try {
                    if(!Directory.Exists(destinationPath))
                        Directory.CreateDirectory(destinationPath);
                }
                catch(Exception e) {Logger.WriteLine("Could not create directory " + item + ", exception: " + e);}
            else
                try {File.Move(source + "/" + item, destinationPath);}
                catch(Exception e) {Logger.WriteLine("Could not copy file " + item + ", exception: " + e);}
        }
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
    public static bool IsInRepositoriesIndex(Repositories repositories, Repository repository, out int pos) {
        pos = 0;
        if(repositories.repositories == null || repositories.repositories.Length == 0) return false;
        for(int i = 0; i < repositories.repositories.Length; i++) {
            if(repositories.repositories[i].repository == repository.repository) {
                pos = i;
                return true;
            }
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