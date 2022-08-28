public class Client {
    public static bool DownloadIndex(string user, string repository) {
        string url = "https://raw.githubusercontent.com/" + user + "/" + repository + "/main/github-updater." + repository + ".json";
        string tempFile = "index/repositories/github-updater." + repository + ".temp.json";
        string file = "index/repositories/github-updater." + repository + ".json";
        try {
            using (var client = new HttpClient()) {
                using (var s = client.GetStreamAsync(url)) {
                    using (var fs = new FileStream("index/repositories/github-updater." + repository + ".temp.json", FileMode.Create)) {
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
            File.Delete(tempFile);
            return false;
        }
    }
}