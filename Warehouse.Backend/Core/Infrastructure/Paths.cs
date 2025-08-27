namespace Warehouse.Backend.Core.Infrastructure;

public static class Paths
{
    public static string GetDatabasePath()
    {
        string appPath = GetApplicationPath();
        Directory.CreateDirectory(appPath);
        return Path.Combine(appPath, "warehouse.db");
    }

    public static string GetDataProtectionPath() => Path.Combine(GetApplicationPath(), "keys");

    private static string GetApplicationPath()
    {
        string appDataPath;
        if (OperatingSystem.IsWindows())
        {
            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        else
        {
            appDataPath = Directory.GetCurrentDirectory();
        }

        string warehouseFolder = Path.Combine(
            appDataPath,
            OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ? ".warehouse" : "Warehouse");
        return warehouseFolder;
    }
}
