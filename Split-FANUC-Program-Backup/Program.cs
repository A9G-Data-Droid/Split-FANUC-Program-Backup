using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Split_FANUC_Program_Backup
{
    class Program
    {
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

        private static void SplitALLPROGtxt(string fileName)
        {
            string pattern = "^%((.|\n|\r)*)%$";
            string[] result = Regex.Split(File.ReadAllText(fileName), pattern,
                                    RegexOptions.Multiline,
                                    TimeSpan.FromMilliseconds(5000));
            throw new NotImplementedException();
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
