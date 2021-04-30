using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Split_FANUC_Program_Backup
{
    class Program
    {
        private const string cncProgramFileExtension = ".CNC";
        private const string defaultCNCprogramName = "Uknown.CNC";

        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                DisplayHelp();
                return 1;
            } 

            if (!File.Exists(args[0]))
            {
                NotFoundError(args[0]);
                return 2;
            }

            SplitALLPROGtxt(args[0]);
            return 0;
        }

        /// <summary>
        /// Split cnc programs found in the text file and save them as individual files
        /// </summary>
        /// <param name="fileName">Full path to "ALL-PROG.TXT"</param>
        private static void SplitALLPROGtxt(string fileName)
        {
            foreach (string cncProgramText in SplitCNCProgams(fileName))
            {
                string programFileName = GetProgramNameFromHeader(cncProgramText);
                if (programFileName.Length < 1) { programFileName = defaultCNCprogramName; }
                File.WriteAllTextAsync(programFileName + cncProgramFileExtension, cncProgramText);
            }
        }

        /// <summary>
        /// Searches for program names in CNC program headers
        /// </summary>
        /// <param name="cncProgramText">The full text of a CNC program</param>
        /// <returns>The program name from the header</returns>
        private static string GetProgramNameFromHeader(string cncProgramText)
        {
            /// Searches for O#### formatted program names
            string pattern = "^O\\d+";
            return Regex.Match(cncProgramText, pattern, RegexOptions.Multiline).Value;
        }

        /// <summary>
        /// Reads a text file and attempts to split out CNC programs found within
        /// </summary>
        /// <param name="fileName">Full path to "ALL-PROG.TXT"</param>
        /// <returns>An array where each string is a whole CNC program</returns>
        static string[] SplitCNCProgams(string fileName)
        {
            /// Searches for CNC programs between % symbols
            string pattern = "^%((.|\n|\r)*)%$";
            return Regex.Split(File.ReadAllText(fileName), pattern,
                                    RegexOptions.Multiline,
                                    TimeSpan.FromMilliseconds(5000));
        }

        private static void NotFoundError(string fileName)
        {
            Console.WriteLine("File not found: " + fileName);
        }

        static void DisplayHelp()
        {
            Console.WriteLine("One argument required. Enter only the path of the file you would like to split.");
        }
    }
}
