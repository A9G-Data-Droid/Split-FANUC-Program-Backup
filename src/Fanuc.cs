using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SplitFANUCProgramBackup;

public static class Fanuc
{
    /// <summary>
    /// The left side of the OR (|) describes an "O Number", the original CNC program name structure, consisting of an "O" followed by 4 to 8 numbers
    /// The right side of the OR (|) describes the new FANUC 32 character alphanumeric program name. Must end in either .NC or .CNC
    /// </summary>
    public static readonly Regex ONumberPattern = new(@"^(O\d{4,8}|<\w+.c?nc>)", RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Matches subdirectory flags dispersed throughout the backup file.
    /// </summary>
    private static readonly Regex DirectoryFlag = new("(&F=)", RegexOptions.Compiled);

    /// <summary>
    /// Character found at the top and bottom of each program.
    /// The CNC control uses this to determine when a program begins and ends.
    /// </summary>
    public const char ProgramDelimiter = '%';
    private const string CncProgramFileExtension = ".CNC";
    private const string DefaultCncProgramName = "Unknown";
    private static readonly char[] SubFolderTrim = [' ', '/'];
    private const int MinimumProgramSize = 7;
    private const char LineFeed = '\n';

    /// <summary>
    /// Split cnc programs found in the text file and save them as individual files. 
    /// File subdirectories reflect those on the source CNC controller. 
    /// </summary>
    /// <param name="backupFile">Full path to "ALL-PROG.TXT"</param>
    /// <param name="outputFolder">Destination output will be written to.</param>
    public static void SplitAllProgTxt(FileInfo backupFile, string outputFolder)
    {
        // Use parallel to decouple reading of the old file from writing the new ones. This allows reading at full speed.
        Parallel.ForEach(GetCncPrograms(backupFile.FullName, outputFolder), newFile =>
        {
            string programFileName = GetProgramNameFromHeader(newFile.ProgramText);
            if (programFileName.Length < 1)
                programFileName = DefaultCncProgramName;

            string outputFilename =
                Path.Combine(outputFolder, newFile.SubFolder, programFileName + CncProgramFileExtension);
            try
            {
                File.WriteAllText(outputFilename, newFile.ProgramText);
                Console.WriteLine($"CREATED FILE: {outputFilename}");
            }
            catch (Exception err)
            {
                Console.WriteLine($"ERROR {err.HResult}: {err.Message}");
                Console.WriteLine($"FAILED TO CREATE FILE: {outputFilename}");
            }
        });
    }

    /// <summary>
    /// Searches for program names in CNC program headers
    /// </summary>
    /// <param name="cncProgramText">The full text of a CNC program</param>
    /// <returns>The program name from the header</returns>
    private static string GetProgramNameFromHeader(string cncProgramText)
    {
        return ONumberPattern.Match(cncProgramText).Value;
    }

    /// <summary>
    /// Reads a text file and attempts to split out CNC programs found within, 
    /// while capturing each program's subdirectory as in the source CNC controller.
    /// </summary>
    /// <param name="fileName">Full path to "ALL-PROG.TXT"</param>
    /// <param name="outputFolder">Destination folder.</param>
    /// <returns>Each CNC program as a string, and any associated subdirectory</returns>
    private static IEnumerable<(string SubFolder, string ProgramText)> GetCncPrograms(string fileName, string outputFolder)
    {
        StringBuilder content = new();
        string subFolder = "";

        // Searches for CNC programs between program name symbols
        var lines = File.ReadLines(fileName);
        foreach(string line in lines)
        {
            // Checks for subdirectory notation
            if (DirectoryFlag.IsMatch(line))
            {
                if (content.Length > MinimumProgramSize)
                {
                    // Return file in buffer, as new subdirectory only applies to subsequent programs
                    yield return (subFolder, CncProgramText(content));

                    // Start a new file
                    content.Clear();
                }

                // Strip out the directory flag and slashes to get just the folder name.
                subFolder = DirectoryFlag.Replace(line, string.Empty).Trim(SubFolderTrim);
                Directory.CreateDirectory(Path.Combine(outputFolder, subFolder));

                // Don't append notation to next program
                continue;
            }

            if (ONumberPattern.IsMatch(line))
            {                     
                if (content.Length > MinimumProgramSize)
                {   // Return the file we have in the buffer
                    yield return (subFolder, CncProgramText(content));
                }

                // Start a new file
                content.Clear();
            }

            // Add unix style line terminators
            content.Append(line)
                .Append(LineFeed);
        }

        // Once we reach the end we will have the final program in the buffer.
        yield return (subFolder, CncProgramText(content));
    }

    private static string CncProgramText(StringBuilder content)
    {
        // Prevent IndexOutOfBounds exceptions if final program is empty
        if (content.Length <= MinimumProgramSize) 
            return content.ToString();

        // Add % to the top 
        if (content[0] != ProgramDelimiter)
        {
            content.Insert(0, LineFeed);
            content.Insert(0, ProgramDelimiter);
        }

        content.TrimEnd();

        // Add % to the bottom when missing
        if (content[content.Length - 1] != ProgramDelimiter)
        {
            content.Append(ProgramDelimiter)
                .Append(LineFeed);
        }

        return content.ToString();
    }
}