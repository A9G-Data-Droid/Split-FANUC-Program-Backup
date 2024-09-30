using System;
using System.IO;
using System.Reflection;
using File = System.IO.File;

namespace SplitFANUCProgramBackup;

public static class Program
{
    private static string ThisExecutableName => AppDomain.CurrentDomain.FriendlyName;
    private static Version? AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version;
    private static string VersionNumber => AssemblyVersion?.ToString() ?? string.Empty;

    private static string BuildDate
    {
        get {
            var buildDateTime = File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location);

            return buildDateTime.ToString("u");
        }
    }
    
    /// <summary>
    /// Command line entry point
    /// </summary>
    /// <param name="args">Requires only one argument: full path to the backup file.</param>
    /// <returns>Zero for success</returns>
    public static int Main(string[] args)
    {
        DisplayHeader();

        if (args.Length != 1)
        {
            DisplayHelp();
            return 1;
        }

        FileInfo backupFile;
        try
        {
            backupFile = new FileInfo(args[0]);
        } 
        catch (Exception err)
        {
            Console.WriteLine(err.Message);
            DisplayHelp();
            return err.HResult;
        }               

        if (!backupFile.Exists)
        {
            NotFoundError(backupFile.FullName);
            return 2;
        }

        // Make a subfolder named like the filename to hold all the programs we split out of it
        string outputFolder = Path.Combine(backupFile.DirectoryName ?? string.Empty, Path.GetFileNameWithoutExtension(backupFile.Name));
        try
        {
            Directory.CreateDirectory(outputFolder);
        } 
        catch 
        {  // Fail gracefully
            outputFolder = backupFile.DirectoryName ?? ".\\";
        }

        // Do the splitting and write out separate files
        Fanuc.SplitAllProgTxt(backupFile, outputFolder);

        // Success
        return 0;
    }

    private static void NotFoundError(string fileName)
    {
        Console.WriteLine($"File not found: {fileName}");
    }

    private static void DisplayHelp()
    {
        Console.WriteLine($"""

                          At least one argument required. Enter only the full path to the backup file you would like to split.

                          EXAMPLE: 
                              
                          {ThisExecutableName} "C:\temp\ALL-PROG.TXT"
                          
                          """);
    }

    private static void DisplayHeader()
    {
        Console.WriteLine();
        Console.WriteLine($"Split FANUC Program Backup Version {VersionNumber} Build Date: {BuildDate}");
    }
}