using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOGWAI_RUNTIME.Classes
{
    internal class AppGlobal
    {
        public static string RootFolder => FileSystem.Current.AppDataDirectory;

        public static string CodeFolder => Path.Combine(RootFolder, "Code");

        public static string DataFolder => Path.Combine(RootFolder, "Data");

        public static bool CreateDataStructure()
        {
            Debug.WriteLine($"AppDataDirectory = {FileSystem.Current.AppDataDirectory}");

            try
            {
                Directory.CreateDirectory(RootFolder);
                Directory.CreateDirectory(CodeFolder);
                Directory.CreateDirectory(DataFolder);

                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
