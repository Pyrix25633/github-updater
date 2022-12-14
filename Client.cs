using System.Reflection;
using System.IO.Compression;
using System.Text;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;
using Mono.Unix.Native;

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
        string file = GetFullPathFromExecutable("index" + Path.DirectorySeparatorChar + "repositories" + Path.DirectorySeparatorChar +
            "github-updater." + repository + ".json");
        string tempFile = file + ".temp";
        if(!Directory.Exists("index" + Path.DirectorySeparatorChar + "repositories")) {
            try {Directory.CreateDirectory(GetFullPathFromExecutable("index" + Path.DirectorySeparatorChar + "repositories"));}
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
            Move(tempFile, file, true);
            Logger.WriteLine("  Succesfully downloaded index from " + user + Path.DirectorySeparatorChar + repository, ConsoleColor.Green);
            return true;
        }
        catch(Exception e) {
            Logger.WriteLine("Error dowloading index from " + user + Path.DirectorySeparatorChar + repository + ", exception: " + e, ConsoleColor.Red);
            try {File.Delete(tempFile);}
            catch(Exception) {}
            return false;
        }
    }
    /// <summary>
    /// Function to download a release
    /// (<paramref name="repository"/>, <paramref name="index"/>, <paramref name="latest"/>)
    /// </summary>
    /// <param name="repository">Repository object containing necessary informations</param>
    /// <param name="index">The repository index</param>
    /// <param name="latest">True to download the latest version without asking</param>
    public static void DownloadRelease(ref Repository repository, Index index, bool? latest) {
        if(repository.repository == null || repository.user == null)
            throw(new NullReferenceException("Null repository exception"));
        bool freshInstall = false;
        //Checking installation path
        if(repository.path == null) repository.path = GetFullPathFromExecutable("programs" + Path.DirectorySeparatorChar + repository.repository);
        if(!Directory.Exists(repository.path)) {
            try {
                Logger.WriteLine("  Warning, directory " + repository.path + " does not exist, attempting creation...", ConsoleColor.Yellow);
                Directory.CreateDirectory(repository.path);
                freshInstall = true;
                Logger.WriteLine("  Succesfully created " + repository.path, ConsoleColor.Green);
            }
            catch(Exception e) {throw(new Exception("Error while attempting directory creation, exception: " + e));}
        }
        //Getting full path
        repository.path = new FileInfo(repository.path).FullName;
        //Choosing version
        if(index.releases == null || index.releases.Length == 0)
            throw(new Exception("This repository has 0 releases"));
        if(latest == null || latest == false) {
            Logger.WriteLine(index.releases.Length + " Release" + ((index.releases.Length == 1) ? "" : "s")
                + " found:", ConsoleColor.Green);
            Logger.Write("  ");
            foreach(Release item in index.releases) {
                if(item.tag == null) continue;
                Logger.Write("[" + item.tag + "] ", ConsoleColor.Cyan);
            }
            Logger.WriteLine();
            Logger.Write("Insert the version you want to download (\"l\" will default to the latest version): ", ConsoleColor.DarkBlue);
            do {
                repository.version = Logger.ReadString();
                if(!IsValidTag(index, repository.version) && repository.version != "l")
                    Logger.Write("  Not a valid version. New input: ", ConsoleColor.Red);
            } while(!IsValidTag(index, repository.version) && repository.version != "l");
        }
        else {
            Logger.Write("Version:    ", ConsoleColor.DarkBlue);
            Logger.WriteLine(index.latest, ConsoleColor.White);
            repository.version = "l";
        }
        //Get latest version tag
        if(repository.version == "l") {
            if(index.latest == null) throw(new NullReferenceException("Null latest release exception"));
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
        char r;
        if(filesCount == 1) r = validOptions[0];
        else {
            Logger.Write("Chose a release file: ", ConsoleColor.DarkBlue);
            do {
                r = Logger.ReadChar();
                if(!validOptions.Contains(r))
                    Logger.Write("  Not a valid option. New input: ", ConsoleColor.Red);
            } while(!validOptions.Contains(r));
        }
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
        if(releaseFile == null) throw(new NullReferenceException("Null release file exception"));
        EmptyTemporaryDirectory();
        string url = "https://github.com/" + repository.user + "/" + repository.repository + "/releases/download/"
            + repository.version + "/" + releaseFile;
        string tempDir = GetFullPathFromExecutable("github-updater.temp");
        string file = tempDir + Path.DirectorySeparatorChar + releaseFile;
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
            Logger.WriteLine("  Downloading release file, operation may take some time...", ConsoleColor.Yellow);
            using (var client = new HttpClient()) {
                using (var s = client.GetStreamAsync(url)) {
                    using (var fs = new FileStream(tempFile, FileMode.Create)) {
                        s.Result.CopyTo(fs);
                    }
                }
            }
            Move(tempFile, file, true);
            Logger.WriteLine("  Release file succesfully downloaded", ConsoleColor.Green);
        }
        catch(Exception e) {
            File.Delete(tempFile);
            throw(new HttpRequestException("Error while downloading release file, exception: " + e));
        }
        if(releaseFile.EndsWith(".zip")) {
            //Deleting content except entries listed in the keep array
            if(!freshInstall) {
                Logger.WriteLine("  Deleting unnecessary files...", ConsoleColor.Yellow);
                DeleteExceptKeep(repository.path, index.keep);
                Logger.WriteLine("  Unnecessary files deleted", ConsoleColor.Green);
            }
            //Decompress zip
            Logger.WriteLine("  Extracting release file with zip, operation may take some time...", ConsoleColor.Yellow);
            string tempExtractionDir = tempDir + Path.DirectorySeparatorChar + releaseFile.Substring(0, releaseFile.Length - 4);
            try {
                Directory.CreateDirectory(tempExtractionDir);
            }
            catch(Exception) {
                Directory.Delete(tempExtractionDir, true);
                Directory.CreateDirectory(tempExtractionDir);
            }
            try {ZipFile.ExtractToDirectory(file, tempExtractionDir);}
            catch(Exception e) {throw(new Exception("Error while extracting the release file with zip, exception: " + e));}
            Logger.WriteLine("  Release extracted", ConsoleColor.Green);
            if(freshInstall) Logger.WriteLine("  Installing...", ConsoleColor.Yellow);
            else Logger.WriteLine("  Upgrading...", ConsoleColor.Yellow);
            CopyExceptKeep(tempExtractionDir, repository.path, index.keep, freshInstall);
            if(Program.exitingBecauseUpgrading) {
                Logger.WriteLine("  Launching upgrader...", ConsoleColor.Yellow);
                return;
            }
            if(freshInstall) Logger.WriteLine("  Succesfully installed " + repository.repository + " " + repository.version, ConsoleColor.Green);
            else Logger.WriteLine("  Succesfully upgraded " + repository.repository + " to " + repository.version, ConsoleColor.Green);
        }
        else if(releaseFile.EndsWith(".tar.gz")) {
            //Deleting content except entries listed in the keep array
            if(!freshInstall) {
                Logger.WriteLine("  Deleting unnecessary files...", ConsoleColor.Yellow);
                DeleteExceptKeep(repository.path, index.keep);
                Logger.WriteLine("  Unnecessary files deleted", ConsoleColor.Green);
            }
            //Decompress tar.gz
            Logger.WriteLine("  Extacting release file with tar and gzip, operation may take some time...", ConsoleColor.Yellow);
            string tempExtractionDir = tempDir + Path.DirectorySeparatorChar + releaseFile.Substring(0, releaseFile.Length - 7);
            try {
                Directory.CreateDirectory(tempExtractionDir);
            }
            catch(Exception) {
                Directory.Delete(tempExtractionDir, true);
                Directory.CreateDirectory(tempExtractionDir);
            }
            try {ExtractTarGz(file, tempExtractionDir);}
            catch(Exception e) {throw(new Exception("Error while extracting the release file with tar and gzip, exception: " + e));}
            Logger.WriteLine("  Release extracted", ConsoleColor.Green);
            if(freshInstall) Logger.WriteLine("  Installing...", ConsoleColor.Yellow);
            else Logger.WriteLine("  Upgrading...", ConsoleColor.Yellow);
            CopyExceptKeep(tempExtractionDir, repository.path, index.keep, freshInstall);
            if(Program.exitingBecauseUpgrading) {
                Logger.WriteLine("  Launching upgrader...", ConsoleColor.Yellow);
                return;
            }
            if(freshInstall) Logger.WriteLine("  Succesfully installed " + repository.repository + " " + repository.version, ConsoleColor.Green);
            else Logger.WriteLine("  Succesfully upgraded " + repository.repository + " to " + repository.version, ConsoleColor.Green);
        }
        else {
            try {
                if(repository.path != null) {
                    if(freshInstall) Logger.WriteLine("  Installing...", ConsoleColor.Yellow);
                    else Logger.WriteLine("  Upgrading...", ConsoleColor.Yellow);
                    Move(file, repository.path + Path.DirectorySeparatorChar + releaseFile);
                    if(freshInstall) Logger.WriteLine("  Succesfully installed " + repository.repository + " " + repository.version, ConsoleColor.Green);
                    else Logger.WriteLine("  Succesfully upgraded " + repository.repository + " to " + repository.version, ConsoleColor.Green);
                }
                else throw(new NullReferenceException("Null repository path"));
            }
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
        string tarFile = inputFile.Substring(0, inputFile.Length - 4);
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
        byte[] dataBuffer = new byte[4096];
        using(System.IO.Stream fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read)) {
            using(GZipInputStream gzipStream = new GZipInputStream(fs)) {
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
        if(path == null) throw(new NullReferenceException("Null path exception"));
        EnumerationOptions enumOptions = new EnumerationOptions();
        enumOptions.RecurseSubdirectories = true; enumOptions.AttributesToSkip = default;
        string[] filesToDelete = new string[0];
        if(keep != null && keep.Length > 0) {
            if(path == GetFullPathFromExecutable()) {
                keep = keep.Append("ICSharpCode.SharpZipLib.dll").ToArray();
                keep = keep.Append("Mono.Posix.NETStandard.dll").ToArray();
                keep = keep.Append("MonoPosixHelper.dll").ToArray();
                keep = keep.Append("libMonoPosixHelper.dll").ToArray();
                keep = keep.Append("libMonoPosixHelper.so").ToArray();
                keep = keep.Append("System.Diagnostics.*").ToArray();
            }
            string[] filesToKeep = GetFilesToKeep(path, keep, enumOptions);
            filesToDelete = Directory.GetFileSystemEntries(path, "*", enumOptions).Except(filesToKeep).Reverse().ToArray();
        }
        else
            filesToDelete = Directory.GetFileSystemEntries(path, "*", enumOptions).Reverse().ToArray();
        if(Environment.OSVersion.Platform != PlatformID.Unix && path == GetFullPathFromExecutable()) {
            string self = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) + ".exe";
            StreamWriter batFile = new StreamWriter(File.Create("upgrade.bat"));
            batFile.WriteLine("ECHO \"Waiting " + self + "...\"");
            batFile.WriteLine("TIMEOUT /t 1 /nobreak > NUL");
            batFile.WriteLine("ECHO \"Killing " + self + "...\"");
            batFile.WriteLine("TASKKILL /F /IM \"{0}\" > NUL", self);
            batFile.WriteLine("ECHO \"Removing unnecessary files...\"");
            foreach(string item in filesToDelete) {
                FileInfo info = new FileInfo(item);
                if((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                    batFile.WriteLine("RMDIR /S /Q " + item);
                }
                else {
                    batFile.WriteLine("DEL /Q " + item);
                }
            }
            batFile.WriteLine("ECHO \"Unnecessary files removed\"");
            batFile.Close();
            return;
        }
        foreach(string item in filesToDelete) {
            FileInfo info = new FileInfo(item);
            if((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                try {Directory.Delete(item);}
                catch(Exception e) {Logger.WriteLine("Could not remove directory " + item + ", exception: " + e, ConsoleColor.Red);}
            }
            else {
                try {File.Delete(item);}
                catch(Exception e) {Logger.WriteLine("Could not remove file " + item + ", exception: " + e, ConsoleColor.Red);}
            }
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
            string[] filesToKeep = GetFilesToKeep(source, keep, enumOptions);
            filesToCopy = Directory.GetFileSystemEntries(source, "*", enumOptions).Except(filesToKeep).ToArray();
        }
        else
            filesToCopy = Directory.GetFileSystemEntries(source, "*", enumOptions).ToArray();
        if(Environment.OSVersion.Platform != PlatformID.Unix && destination == GetFullPathFromExecutable()) {
            string self = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) + ".exe";
            StreamWriter batFile = new StreamWriter(File.Open("upgrade.bat", FileMode.Append));
            batFile.WriteLine("ECHO \"Upgrading...\"");
            foreach(string item in filesToCopy) {
                FileInfo info = new FileInfo(item);
                string destinationPath = destination + Path.DirectorySeparatorChar + item.Substring(source.Length + 1);
                if((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                    if(!Directory.Exists(destinationPath))
                        batFile.WriteLine("MKDIR " + destinationPath);
                }
                else {
                    batFile.WriteLine("MOVE /Y " + item + " " + destinationPath);
                }
            }
            batFile.WriteLine("ECHO \"Succesfully upgraded\"");
            batFile.WriteLine("DEL /Q \"%~f0\" > NUL & ECHO \"Warning: Succesfully updgraded github-updater, run 'upgrade' command again to upgrade the other installations!\"");
            batFile.Close();
            ProcessStartInfo startInfo = new ProcessStartInfo("upgrade.bat");
            startInfo.WorkingDirectory = GetFullPathFromExecutable();
            Process.Start(startInfo);
            Program.exitingBecauseUpgrading = true;
            return;
        }
        foreach(string item in filesToCopy) {
            FileInfo info = new FileInfo(item);
            string destinationPath = destination + Path.DirectorySeparatorChar + item.Substring(source.Length + 1);
            if((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                try {
                    if(!Directory.Exists(destinationPath))
                        Directory.CreateDirectory(destinationPath);
                }
                catch(Exception e) {Logger.WriteLine("Could not create directory " + item + ", exception: " + e, ConsoleColor.Red);}
            }
            else {
                try {Move(item, destinationPath, true);}
                catch(Exception e) {Logger.WriteLine("Could not copy file " + item + ", exception: " + e, ConsoleColor.Red);}
            }
        }
    }
    /// <summary>
    /// Function to get the list of files to keep
    /// (<paramref name="path"/>, <paramref name="keep"/>, <paramref name="enumOptions"/>)
    /// </summary>
    /// <param name="path">The path</param>
    /// <param name="keep">The keep array from the repository index</param>
    /// <param name="enumOptions">The enumeration options</param>
    /// <returns>The array of files to keep</returns>
    public static string[] GetFilesToKeep(string path, string[] keep, EnumerationOptions enumOptions) {
        string[] filesToKeep = new string[0];
        foreach(string item in keep) {
            FileInfo? info = null;
            if(item.Contains('*') || item.Contains('?')) { //With wildcard
                try {
                    string[] toAppend = Directory.GetFileSystemEntries(path, item, enumOptions);
                    foreach(string appendItem in toAppend) {
                        string[] toAppendFull = GetFullDirectoryListToEntry(appendItem, path);
                        foreach(string appendItemFull in toAppendFull)
                            filesToKeep = filesToKeep.Append(appendItemFull).ToArray();
                    }    
                }
                catch(Exception) {}
            }
            else
                info = new FileInfo(path + Path.DirectorySeparatorChar + item);
            if(info != null) { //Normal folder or file
                if(Directory.Exists(info.FullName) && (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                    try {
                        string[] toAppend = Directory.GetFileSystemEntries(info.FullName, "*", enumOptions);
                        foreach(string appendItem in toAppend)
                            filesToKeep = filesToKeep.Append(appendItem).ToArray();
                        string[] toAppendFull = GetFullDirectoryListToEntry(info.FullName, path);
                        foreach(string appendItemFull in toAppendFull)
                            filesToKeep = filesToKeep.Append(appendItemFull).ToArray();
                    }
                    catch(Exception) {}
                }
                else {
                    string[] toAppendFull = GetFullDirectoryListToEntry(info.FullName, path);
                    foreach(string appendItemFull in toAppendFull)
                        filesToKeep = filesToKeep.Append(appendItemFull).ToArray();
                }
            }
        }
        return filesToKeep.Distinct().ToArray();
    }
    /// <summary>
    /// Function to get all the directories to an entry included
    /// (<paramref name="entry"/>, <paramref name="path"/>)
    /// </summary>
    /// <param name="entry">The entry, either file or folder</param>
    /// <param name="path">The starting path</param>
    /// <returns>The list of all the directories and the entry</returns>
    public static string[] GetFullDirectoryListToEntry(string entry, string path) {
        if(path.EndsWith('/') || path.EndsWith('\\')) path = path.Substring(0, path.Length - 1);
        if(entry.EndsWith('/') || entry.EndsWith('\\')) entry = entry.Substring(0, entry.Length - 1);
        string[] directoryList = {entry};
        path = new FileInfo(path).FullName;
        try {
            do {
                DirectoryInfo? temp = Directory.GetParent(entry);
                if(temp != null) {
                    entry = temp.FullName;
                    if(entry != path)
                        directoryList = directoryList.Append(entry).ToArray();
                }
                else
                    return directoryList;
            } while(entry != path);
        }
        catch(Exception) {}
        return directoryList;
    }
    /// <summary>
    /// Function to empty github-updater.temp/
    /// </summary>
    public static void EmptyTemporaryDirectory() {
        string tempDir = GetFullPathFromExecutable("github-updater.temp");
        if(!Directory.Exists(tempDir)) return;
        EnumerationOptions enumOptions = new EnumerationOptions();
        enumOptions.RecurseSubdirectories = true; enumOptions.AttributesToSkip = default;
        string[] filesToDelete = Directory.GetFileSystemEntries(tempDir, "*", enumOptions).Reverse().ToArray();
        foreach(string item in filesToDelete) {
            FileInfo info = new FileInfo(item);
            if((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                try {Directory.Delete(item);}
                catch(Exception e) {Logger.WriteLine("Could not remove directory " + item + ", exception: " + e);}
            }
            else {
                try {File.Delete(item);}
                catch(Exception e) {Logger.WriteLine("Could not remove file " + item + ", exception: " + e);}
            }
        }
    }
    /// <summary>
    /// Function to move a file
    /// (<paramref name="source"/>, <paramref name="destination"/>, <paramref name="overwrite"/>)
    /// </summary>
    /// <param name="source">The source</param>
    /// <param name="destination">The destination</param>
    /// <param name="overwrite">If true the destination file is overwritten if it exists</param>
    public static void Move(string source, string destination, bool overwrite = false) {
        if(File.Exists(destination)) {
            if(!overwrite) return;
            File.Delete(destination);
        }
        Syscall.rename(source, destination);
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
    public static string GetFullPathFromExecutable(string path = "") {
        if(path == "") return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "";
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + path;
    }
    /// <summary>
    /// Function to remove an element from an array
    /// (<paramref name="source"/>, <paramref name="index"/>)
    /// </summary>
    /// <param name="source">The source array</param>
    /// <param name="index">The position of the element to remove</param>
    /// <returns>The array without that element</returns>
    public static T[] RemoveAt<T>(T[] source, int index) {
        int lenght = source.Length;
        T[] dest = new T[lenght - 1];
        if( index > 0 )
            Array.Copy(source, 0, dest, 0, index);
        if( index < lenght - 1 )
            Array.Copy(source, index + 1, dest, index, lenght - index - 1);
        return dest;
    }
}