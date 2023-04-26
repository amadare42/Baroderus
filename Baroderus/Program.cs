using Baroderus;

void Run()
{
    var programArgs = RunArguments.ParseArgs(args);
    var cycle = programArgs.RunMode == RunMode.Unset;

    do
    {
        switch (programArgs.RunMode)
        {
            case RunMode.Backup:
                {
                    EnsureValidRootDir(programArgs);
                    var replacer = new Replacer(programArgs.RootDir);
                    var replacements = replacer.GetAllReplacements();
                    replacer.PerformBackup(replacements.Keys.ToList());

                    cycle = false;
                    break;
                }
            case RunMode.Restore:
                {
                    EnsureValidRootDir(programArgs);
                    var replacer = new Replacer(programArgs.RootDir);
                    replacer.RestoreBackup(programArgs.TargetPath ?? programArgs.RootDir);

                    cycle = false;
                    break;
                }
            case RunMode.Patch:
                {
                    EnsureValidRootDir(programArgs);
                    var replacer = new Replacer(programArgs.RootDir);
                    if (Directory.Exists(replacer.GetBackupDir()))
                    {
                        Console.WriteLine("Backup dir detected. Should we restore the backup first? (y/n)");
                        Console.WriteLine("Current backup will be overriden. If not sure, answer 'y'.");
                        if (ConsoleUtil.YesNo())
                        {
                            replacer.RestoreBackup(programArgs.TargetPath ?? programArgs.RootDir);
                        }
                    }
                    
                    var replacements = replacer.GetAllReplacements();
                    replacer.PerformBackup(replacements.Keys.ToList());
                    replacer.ProcessReplacements(programArgs.TargetPath ?? programArgs.RootDir, replacements);

                    cycle = false;
                    break;
                }

            case RunMode.Help:
                {
                    RunArguments.PrintHelp();
                    break;
                }

            default:
                {
                    var list = new List<string>
                    {
                        "Patch",
                        "Backup",
                        "Restore",
                        "CLI Help",
                        "Exit"
                    };

                    ConsoleUtil.SelectOption("What to do?", list, (i, s) =>
                    {
                        switch (s)
                        {
                            case "Patch":
                                programArgs.RunMode = RunMode.Patch;
                                break;
                            case "Backup":
                                programArgs.RunMode = RunMode.Backup;
                                break;
                            case "Restore":
                                programArgs.RunMode = RunMode.Restore;
                                break;
                            case "CLI Help":
                                programArgs.RunMode = RunMode.Help;
                                break;
                            case "Exit":
                                cycle = false;
                                break;
                        }
                    });
                    break;
                }
        }
    }
    while (cycle);


    Console.WriteLine("Done. Press any key to exit...");
    Console.ReadKey();
    Application.Exit();
}

void EnsureValidRootDir(RunArguments runArguments)
{
    if (string.IsNullOrEmpty(runArguments.RootDir))
    {
        // default
        runArguments.RootDir = @"C:\Program Files (x86)\Steam\steamapps\common\Barotrauma";
        if (File.Exists("rootDir.txt"))
        {
            runArguments.RootDir = File.ReadAllText("rootDir.txt");
            if (Directory.Exists(runArguments.RootDir))
            {
                Console.WriteLine("Assuming Barotrauma is installed at " + runArguments.RootDir);
                return;
            }
        }
        
        Console.WriteLine("Assuming Barotrauma is installed at " + runArguments.RootDir);
    }

    while (!Directory.Exists(runArguments.RootDir))
    {
        runArguments.RootDir = ConsoleUtil.SelectFolder("Barotrauma folder");
    }

    try
    {
        File.WriteAllText("rootDir.txt", runArguments.RootDir);
        Console.WriteLine("Saved root dir to rootDir.txt");
    }
    catch (Exception ex)
    {
        Console.WriteLine("[this is fine] Failed to save root dir to rootDir.txt: " + ex.Message);
    }
}

var thread = new Thread(() =>
{
    Application.SetHighDpiMode(HighDpiMode.SystemAware);
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Run();
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();