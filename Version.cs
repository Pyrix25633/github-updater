public class Version {
    public int major, minor, patch;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <returns>Version object template</returns>
    public Version() {
        major = 0; minor = 0; patch = 0;
    }
    /// <summary>
    /// Constructor
    /// (<paramref name="version"/>)
    /// </summary>
    /// <param name="latest">The string version</param>
    /// <returns>Version object</returns>
    public Version(string? version) {
        if(version == null) throw(new Exception("Null version exception"));
        string[] splitted = version.Split('.');
        if(splitted.Length != 3) throw(new Exception("Wrong version format"));
        try {
            major = int.Parse(splitted[0]);
            minor = int.Parse(splitted[1]);
            patch = int.Parse(splitted[2]);
        }
        catch(Exception e) {
            throw(new Exception("Wrong version format, exception: " + e));
        }
    }
    /// <summary>
    /// Function know if the local version is outdated
    /// (<paramref name="latest"/>, <paramref name="local"/>)
    /// </summary>
    /// <param name="latest">The latest version</param>
    /// <param name="local">The local version</param>
    /// <returns>True if it is outdated: e.g. local 1.3.7, latest 1.5.3</returns>
    public static bool IsOutdated(Version latest, Version local) {
        if(latest.major > local.major) return true;
        if(latest.major == local.major) {
            if(latest.minor > local.minor) return true;
            if(latest.minor == local.minor && latest.patch > local.patch) return true;
        }
        return false;
    }
}