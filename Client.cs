using System.Reflection;
using System.IO.Compression;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;

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
        bool freshInstall = false;
        //Checking installation path
        if(repository.path == null) repository.path = GetFullPathFromExecutable("programs/" + repository.repository);
        if(!Directory.Exists(repository.path)) {
            try {
                Directory.CreateDirectory(repository.path);
                freshInstall = true;
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
        DownloadRelease(repository, index, releaseFile, freshInstall);
    }
    /// <summary>
    /// Function to download a release
    /// (<paramref name="repository"/>, <paramref name="index"/>, <paramref name="releaseFile"/>, <paramref name="freshInstall"/>)
    /// </summary>
    /// <param name="repository">Repository object containing necessary informations</param>
    /// <param name="index">The repository index</param>
    /// <param name="releaseFile">The release file name</param>
    /// <param name="freshInstall">If it is a fresh install or an upgrade</param>
    public static void DownloadRelease(Repository repository, Index index, string? releaseFile, bool freshInstall) {
        if(releaseFile == null) throw(new Exception("Null release file exception"));
        EmptyTemporaryDirectory();
        string url = "https://github.com/" + repository.user + "/" + repository.repository + "/releases/download/"
            + repository.version + "/" + releaseFile;
        string tempDir = GetFullPathFromExecutable("github-updater.temp");
        string file = tempDir + "/" + releaseFile;
        string tempFile = file + ".temp";
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
            File.Move(tempFile, file);
            Logger.WriteLine("Release file succesfully downloaded", ConsoleColor.Green);
        }
        catch(Exception e) {
            File.Delete(tempFile);
            throw(new Exception("Error while downloading release file, exception: " + e));
        }
        if(releaseFile.EndsWith(".zip")) {
            //Deleting content except entries listed in the keep array
            Logger.WriteLine("Deleting unnecessary files...", ConsoleColor.Yellow);
            if(!freshInstall) DeleteExceptKeep(repository.path, index.keep);
            Logger.WriteLine("Unnecessary files deleted", ConsoleColor.Green);
            //Decompress zip
            Logger.WriteLine("Extracting release file with zip, operation may take some time...", ConsoleColor.Yellow);
            string tempExtractionDir = tempDir + "/" + releaseFile.Substring(0, releaseFile.Length - 4);
            try {
                Directory.CreateDirectory(tempExtractionDir);
            }
            catch(Exception) {
                Directory.Delete(tempExtractionDir, true);
                Directory.CreateDirectory(tempExtractionDir);
            }
            try {ZipFile.ExtractToDirectory(file, tempExtractionDir);}
            catch(Exception e) {throw(new Exception("Error while extracting the release file with zip, exception: " + e));}
            Logger.WriteLine("Release extracted", ConsoleColor.Green);
            Logger.WriteLine("Installing...", ConsoleColor.Yellow);
            CopyExceptKeep(tempExtractionDir, repository.path, index.keep, freshInstall);
            Logger.WriteLine("Succesfully installed " + repository.repository + " " + repository.version, ConsoleColor.Green);
        }
        else if(releaseFile.EndsWith(".tar.gz")) {
            //Deleting content except entries listed in the keep array
            Logger.WriteLine("Deleting unnecessary files...", ConsoleColor.Yellow);
            if(!freshInstall) DeleteExceptKeep(repository.path, index.keep);
            Logger.WriteLine("Unnecessary files deleted", ConsoleColor.Green);
            //Decompress tar.gz
            Logger.WriteLine("Decompressing release file with tar and gzip, operation may take some time...", ConsoleColor.Yellow);
            string tempExtractionDir = tempDir + "/" + releaseFile.Substring(0, releaseFile.Length - 7);
            try {
                Directory.CreateDirectory(tempExtractionDir);
            }
            catch(Exception) {
                Directory.Delete(tempExtractionDir, true);
                Directory.CreateDirectory(tempExtractionDir);
            }
            try {ExtractTarGz(file, tempExtractionDir);}
            catch(Exception e) {throw(new Exception("Error while extracting the release file with tar and gzip, exception: " + e));}
            Logger.WriteLine("Release extracted", ConsoleColor.Green);
            Logger.WriteLine("Installing...", ConsoleColor.Yellow);
            CopyExceptKeep(tempExtractionDir, repository.path, index.keep, freshInstall);
            Logger.WriteLine("Succesfully installed " + repository.repository + " " + repository.version, ConsoleColor.Green);
        }
        else {
            try {if(repository.path != null) File.Move(file, repository.path + "/" + releaseFile);}
            catch(Exception e) {Logger.WriteLine("Could not copy file " + file + ", exception: " + e, ConsoleColor.Red);}
        }
        EmptyTemporaryDirectory();
    }
    /// <summary>
    /// Function to extract a .tar.gz file
    /// (<paramref name="inputFile"/>, <paramref name="outputDir"/>)
    /// </summary>
    /// <param name="inputFile">The .tar.gz file</param>
    /// <param name="outputDir">The output directory</param>
    public static void ExtractTarGz(string inputFile, string outputDir) {
        string tarFile = inputFile.Substring(inputFile.Length - 4);
        ExtractGz(inputFile, tarFile);
        ExtractTar(tarFile, outputDir);
    }
    /// <summary>
    /// Function to extract a .gz file
    /// (<paramref name="inputFile"/>, <paramref name="outputFile"/>)
    /// </summary>
    /// <param name="inputFile">The .gz file</param>
    /// <param name="outputFile">The output file</param>
    public static void ExtractGz(string inputFile, string outputFile) {
        byte[ ] dataBuffer = new byte[4096];
        using(System.IO.Stream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read)) {
            using(GZipInputStream gzipStream = new GZipInputStream(fs)) {
                // Change this to your needs
                using(FileStream fsOut = File.Create(outputFile)) {
                    StreamUtils.Copy(gzipStream, fsOut, dataBuffer);
                }
            }
        }
    }
    /// <summary>
    /// Function to extract a .tar file
    /// (<paramref name="inputFile"/>, <paramref name="outputDir"/>)
    /// </summary>
    /// <param name="inputFile">The .tar file</param>
    /// <param name="outputDir">The output directory</param>
    public static void ExtractTar(String inputFile, String outputDir) {
        Stream inStream = File.OpenRead(inputFile);
        TarArchive tarArchive = TarArchive.CreateInputTarArchive(inStream, Encoding.UTF8);
        tarArchive.ExtractContents(outputDir);
        tarArchive.Close();
        inStream.Close();
    }
    /// <summary>
    /// Function to delete all entries except those on a keep array
    /// (<paramref name="path"/>, <paramref name="keep"/>)
    /// </summary>
    /// <param name="path">The path</param>
    /// <param name="keep">The keep array</param>
    public static void DeleteExceptKeep(string? path, string[]? keep) {
        if(path == null) return;
        EnumerationOptions enumOptions = new EnumerationOptions();
        enumOptions.RecurseSubdirectories = true; enumOptions.AttributesToSkip = default;
        string[] filesToDelete = new string[0];
        if(keep != null && keep.Length > 0) {
            string[] filesToKeep = new string[0];
            foreach(string item in keep) {
                FileInfo info = new FileInfo(path + "/" + item);
                if(Directory.Exists(info.FullName) && (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                    try {
                        string[] toAppend = Directory.GetFileSystemEntries(info.FullName, "*", enumOptions);
                        foreach(string appendItem in toAppend)
                            filesToKeep = filesToKeep.Append(appendItem).ToArray();
                    }
                    catch(Exception) {}
                }
                else
                    filesToKeep = filesToKeep.Append(info.FullName).ToArray();
            }
            filesToDelete = Directory.GetFileSystemEntries(path, "*", enumOptions).Except(filesToKeep).Reverse().ToArray();
        }
        else
            filesToDelete = Directory.GetFileSystemEntries(path, "*", enumOptions).Reverse().ToArray();
        foreach(string item in filesToDelete) {
            FileInfo info = new FileInfo(item);
            if((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                try {Directory.Delete(item);}
                catch(Exception e) {Logger.WriteLine("Could not remove directory " + item + ", exception: " + e, ConsoleColor.Red);}
            else
                try {File.Delete(item);}
                catch(Exception e) {Logger.WriteLine("Could not remove file " + item + ", exception: " + e, ConsoleColor.Red);}
        }
    }
    /// <summary>
    /// Function to copy all entries except those on a keep array
    /// (<paramref name="source"/>, <paramref name="destination"/>, <paramref name="keep"/>, <paramref name="freshInstall"/>)
    /// </summary>
    /// <param name="source">The source folder</param>
    /// <param name="destination">The destination folder</param>
    /// <param name="keep">The keep array</param>
    /// <param name="freshInstall">If it is a fresh install or an upgrade</param>
    public static void CopyExceptKeep(string? source, string? destination, string[]? keep, bool freshInstall) {
        if(source == null || destination == null) return;
        EnumerationOptions enumOptions = new EnumerationOptions();
        enumOptions.RecurseSubdirectories = true; enumOptions.AttributesToSkip = default;
        string[] filesToCopy = new string[0];
        if(keep != null && keep.Length > 0 && !freshInstall) {
            string[] filesToKeep = new string[0];
            foreach(string item in keep) {
                FileInfo info = new FileInfo(source + "/" + item);
                if((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                    try {
                        string[] toAppend = Directory.GetFileSystemEntries(info.FullName, "*", enumOptions);
                        foreach(string appendItem in toAppend)
                            filesToKeep = filesToKeep.Append(appendItem).ToArray();
                    }
                    catch(Exception) {}
                }
                else
                    filesToKeep = filesToKeep.Append(info.FullName).ToArray();
            }
            filesToCopy = Directory.GetFileSystemEntries(source, "*", enumOptions).Except(filesToKeep).ToArray();
        }
        else
            filesToCopy = Directory.GetFileSystemEntries(source, "*", enumOptions).ToArray();
        foreach(string item in filesToCopy) {
            FileInfo info = new FileInfo(item);
            string destinationPath = destination + "/" + item.Substring(source.Length + 1);
            if((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                try {
                    if(!Directory.Exists(destinationPath))
                        Directory.CreateDirectory(destinationPath);
                }
                catch(Exception e) {Logger.WriteLine("Could not create directory " + item + ", exception: " + e, ConsoleColor.Red);}
            }
            else {
                try {File.Move(item, destinationPath);}
                catch(Exception e) {Logger.WriteLine("Could not copy file " + item + ", exception: " + e, ConsoleColor.Red);}
            }
        }
    }
    public static void EmptyTemporaryDirectory() {
        string tempDir = GetFullPathFromExecutable("github-updater.temp");
        EnumerationOptions enumOptions = new EnumerationOptions();
        enumOptions.RecurseSubdirectories = true; enumOptions.AttributesToSkip = default;
        string[] filesToDelete = Directory.GetFileSystemEntries(tempDir, "*", enumOptions).Reverse().ToArray();
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
    /// Function to see if a repository is already in github-updater.repositories.json
    /// (<paramref name="repositories"/>, <paramref name="repository"/>, <paramref name="pos"/>)
    /// </summary>
    /// <param name="repositories">The repositories index</param>
    /// <param name="repository">The repository</param>
    /// <param name="pos">Output of the position, -1 if not found</param>
    /// <returns>True the repository is in the index</returns>
    public static bool IsInRepositoriesIndex(Repositories repositories, Repository repository, out int pos) {
        pos = -1;
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