
using Baroderus;

class RunArguments
{
    public string RootDir { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\Barotrauma";
    public string? TargetPath { get; set; }
    public RunMode RunMode { get; set; }
    
    public static RunArguments ParseArgs(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h") || args.Contains("/?"))
        {
            return new RunArguments { RunMode = RunMode.Help };
        }
        
        var result = new RunArguments();
        var rootDirIndex = args.IndexOfAny("--root", "-r", "/r");
        if (rootDirIndex != -1)
        {
            result.RootDir = args[rootDirIndex + 1];
        }
        
        var restorePathIndex = args.IndexOfAny("--target", "-t", "/t");
        if (restorePathIndex != -1)
        {
            result.TargetPath = args[restorePathIndex + 1];
        }

        if (args.Contains("--backup") || args.Contains("-b"))
        {
            result.RunMode = RunMode.Backup;
        }
        else if (args.Contains("--restore") || args.Contains("-rs"))
        {
            result.RunMode = RunMode.Restore;
        }
        else if (args.Contains("--patch") || args.Contains("-p"))
        {
            result.RunMode = RunMode.Patch;
        }
        else
        {
            result.RunMode = RunMode.Unset;
        }
        
        return result;
    }

    public static void PrintHelp()
    {
        Console.WriteLine("Baroderus - Barotrauma file patcher");
        Console.WriteLine("Usage: Baroderus.exe [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  --help, -h, /?  Show this help message and exit");
        Console.WriteLine("  --root, -r, /r  Specify the root directory of the game");
        Console.WriteLine("  --backup, -b    Backup the game files");
        Console.WriteLine("  --restore, -rs   Restore the game files from the backup");
        Console.WriteLine("  --target, -t, /t  Specify the path to target");
        Console.WriteLine("  --patch, -p    Patch the game files");
    }
}

public static class ArrayUtils
{
    public static int IndexOfAny<T>(this T[] array, params T[] values)
    {
        for (var i = 0; i < array.Length; i++)
        {
            if (values.Contains(array[i]))
            {
                return i;
            }
        }
        return -1;
    }
}

enum RunMode
{
    Help,
    Backup,
    Restore,
    Patch,
    Unset
}

