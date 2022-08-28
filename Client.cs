public class Client {
    public static bool downloadIndex(string user, string repository) {
        string url = "https://raw.githubusercontent.com/" + user + "/" + repository + "/main/github-updater." + repository + ".json";
        try {
            using (var client = new HttpClient()) {
                using (var s = client.GetStreamAsync(url)) {
                    using (var fs = new FileStream("index/repositories/github-updater." + repository + ".json", FileMode.OpenOrCreate)) {
                        s.Result.CopyTo(fs);
                    }
                }
            }
            Logger.WriteLine("Succesfully downloaded index from " + user + "/" + repository, ConsoleColor.Green);
            return true;
        }
        catch(Exception e) {
            Logger.WriteLine("Error dowloading index from " + user + "/" + repository + ", exception: " + e, ConsoleColor.Red);
            return false;
        }
    }
}