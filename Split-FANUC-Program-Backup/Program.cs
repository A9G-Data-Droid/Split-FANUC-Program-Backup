using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Split_FANUC_Program_Backup
{
    static class Program
    {
        private static string ThisExecutableName => AppDomain.CurrentDomain.FriendlyName;
        private static string VersionNumber => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static string BuildDate
        {
            get {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return new DateTime(2000, 1, 1)
                    .AddDays(version.Build)
                    .AddSeconds(version.Revision * 2).ToString("o");
            }
        }

        private const string cncProgramFileExtension = ".CNC";
        private const string defaultCNCprogramName = "Uknown.CNC";
        private const char programDelimiter = '%';
        private const int minimumProgramSize = 7;

        /// <summary>
        /// The left side of the OR (|) describes an "O Number", the original CNC program name structure, consisting of an "O" followed by 4 to 8 numbers
        /// The right side of the OR (|) describes the new FANUC 32 character alphanumeric program name. Must end in either .NC or .CNC
        /// </summary>
        private const string oNumberPattern = @"^(O\d{4,8}|<\w+.c?nc>)";

        static int Main(string[] args)
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
                backupFile = new(args[0]);
            } catch (Exception err)
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
            string outputFolder = Path.Combine(backupFile.DirectoryName, Path.GetFileNameWithoutExtension(backupFile.Name));
            try
            {
                Directory.CreateDirectory(outputFolder);
            } catch 
            {  // Fail gracefully
                outputFolder = backupFile.DirectoryName;
            }

            SplitALLPROGtxt(backupFile, outputFolder);

            // Success
            return 0;
        }

        /// <summary>
        /// Split cnc programs found in the text file and save them as individual files
        /// </summary>
        /// <param name="fileName">Full path to "ALL-PROG.TXT"</param>
        private static void SplitALLPROGtxt(FileInfo backupFile, string outputFolder)
        {
            foreach (string cncProgramText in GetCNCProgams(backupFile.FullName))
            {
                string programFileName = GetProgramNameFromHeader(cncProgramText);
                if (programFileName.Length < 1) { programFileName = defaultCNCprogramName; }

                string outputFilename = Path.Combine(outputFolder, programFileName + cncProgramFileExtension);
                try
                {
                    File.WriteAllTextAsync(outputFilename, cncProgramText);
                    Console.WriteLine("CREATED FILE: " + outputFilename);
                } catch (Exception err)
                {
                    Console.WriteLine("ERROR " + err.HResult + ": " + err.Message);
                    Console.WriteLine("FAILED TO CREATE FILE: " + outputFilename);
                }
            }
        }

        /// <summary>
        /// Searches for program names in CNC program headers
        /// </summary>
        /// <param name="cncProgramText">The full text of a CNC program</param>
        /// <returns>The program name from the header</returns>
        private static string GetProgramNameFromHeader(string cncProgramText)
        {       
            return Regex.Match(cncProgramText, oNumberPattern, RegexOptions.Multiline).Value;
        }

        /// <summary>
        /// Reads a text file and attempts to split out CNC programs found within
        /// </summary>
        /// <param name="fileName">Full path to "ALL-PROG.TXT"</param>
        /// <returns>Each string is a whole CNC program</returns>
        static IEnumerable<string> GetCNCProgams(string fileName)
        {
            StringBuilder content = new();

            /// Searches for CNC programs between program name symbols
            foreach(string line in File.ReadLines(fileName))
            {
                if (Regex.IsMatch(line, oNumberPattern))
                { 
                    if (content.Length > minimumProgramSize)
                    {   // Return the file we have in the buffer
                        yield return CncProgramText(content);
                    }

                    // Start a new file
                    content = new();
                }

                content.AppendLine(line);
            }

            // Once we reach the end we will have the final program in the buffer.
            yield return CncProgramText(content);
        }

        static string CncProgramText(StringBuilder content)
        {
            // Add % to the top 
            content.Insert(0, Environment.NewLine);
            content.Insert(0, programDelimiter);

            // Add % to the bottom when missing
#pragma warning disable S1854 // Unused assignments should be removed
            int lastCharIndex = Environment.NewLine.Length + 1;
#pragma warning restore S1854 // Unused assignments should be removed
            if (content[^lastCharIndex] != programDelimiter) { content.AppendLine(programDelimiter.ToString()); }

            return content.ToString();
        }

        private static void NotFoundError(string fileName)
        {
            Console.WriteLine("File not found: " + fileName);
        }

        static void DisplayHelp()
        {
            Console.WriteLine(@"
At least one argument required. Enter only the path of the file you would like to split.

EXAMPLE: 
    " + ThisExecutableName + " \"C:\\temp\\ALL-PROG.TXT\"" + Environment.NewLine);
        }

        private static void DisplayHeader()
        {
            Console.WriteLine(Environment.NewLine + "Split FANUC Program Backup Version " + VersionNumber + " Build Date: " + BuildDate);
        }
    }
}
