using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenPartFile
{

    static class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetConsoleProcessList(
            uint[] ProcessList,
            uint ProcessCount
        );



        enum OutputType
        {
            None = 0,
            Form = 1,
            MsgBox = 1,
            Console = 2,
            Log = 4
        }

        private static OutputType OUTPUT_TYPE = OutputType.Form;
        static int SYMLINK_FLAG_DIRECTORY = 1;
        private static readonly IEnumerable<string> FILETYPES = new List<string>() { ".temp", ".tmp", ".part", ".crdownload" };


        static bool GetConsoleCountAndReturnTrueIfInConsoleMode()
        {
            string msg = "";

            try
            {

                uint[] procIDs = new uint[64];

                var count = GetConsoleProcessList(procIDs, 64);
                msg+=($"\r\n****({Process.GetCurrentProcess().Id})***** " + count + "****** \r\n");
                msg+=(string.Join(" ", procIDs));
                msg+=("\r\n**end*of*--* " + count + "****** \r\n");
                if (count > 1) return true;
            }
            catch
            {

            }

            try
            {
                var count = GetConsoleProcessList(null, 0);
                msg+=("\r\n*******2nd*count******* " + count + "***END*** \r\n");
                if (count > 1) return true;
            }
            catch (Exception e)
            { }
            Program.LogOutput(msg);
            return false;
        }



        [STAThread]

        static void Main(string[] args)
        {
            //if (Debugger.IsAttached == false)
            //{
            //    Debugger.Launch();
            //    Debugger.Break();
            //}

            if (args.Length < 1)
            {
                LogOutput("No arguments supplied. e.g.  OpenPartFile.exe \"myvideo.mkv.part\"");
                return;
            }

            //if (GetConsoleCountAndReturnTrueIfInConsoleMode()
            //    || args.Contains("/C", StringComparer.InvariantCultureIgnoreCase)
            //    || args.Contains("/console", StringComparer.InvariantCultureIgnoreCase))
            //{
            //    OUTPUT_TYPE = OutputType.Console;
            //}

            if (args[0].ToLowerInvariant() == "/install")
            {
                SetupAssociations();
                return;
            }

            var filePath = CheckFilePath(String.Join(" ", args));
            var suffix = "";
            suffix = CheckForSuffix(filePath, suffix);
            if (suffix == "")
            {
                LogOutput($"Only {String.Join(" , ", FILETYPES)} files are supported.");
                return;
            }

            var newLink = Path.Combine(Path.GetDirectoryName(filePath), "tmpLink_"
                + Path.GetFileNameWithoutExtension(filePath));

            if (!File.Exists(newLink))
            {
                LogOutput($">mklink {newLink} {filePath}");
                if (CreateLink(/*"\""+*/newLink/*.Replace(" ","\\ ")*/ /*+ "\""*/ , /*"\""+*/filePath/*.Replace(" ", "\\ ")*/ /*+ "\""*/ , 0))
                    LogOutput($"symbolic link created for {newLink } <<===>> { filePath}");
                else
                {
                    LogOutput($"Failed to create file {newLink}. Attempting to use temp folder...");
                    newLink = Path.Combine(Path.GetTempPath(), "tmpLink_"
                        + Path.GetFileNameWithoutExtension(filePath));

                    if (!File.Exists(newLink))
                    {

                        LogOutput($">mklink {newLink} {filePath}");

                        if (CreateLink(/*"\""+*/newLink/*+"\""*/  , /*"\""+*/filePath/*+"\"" */, 0))
                            LogOutput($"symbolic link created for {newLink} <<===>> {filePath}");
                        else
                        {
                            LogOutput($"Failed to create file {newLink}. Aborting...");
                            return;
                        }
                    }
                }

            }

            var startUsingDefaultProgram = false;
            MessageBox.Show($"Attempting to load {(startUsingDefaultProgram ? "default application" : "Open-With dialog")} for {newLink}");
            OpenAs(filename: newLink, startUsingDefaultProgram: startUsingDefaultProgram);

        }

        static void OpenAs(string filename, bool startUsingDefaultProgram = false)
        {
            var filePath = CheckFilePath(filename);
            if (startUsingDefaultProgram) Process.Start("\"" + filePath + "\"");
            else Process.Start("rundll32.exe", "shell32.dll, OpenAs_RunDLL " + filePath);
        }


        private static void SetupAssociations()
        {
            LogOutput("Setting up associations:");
            try
            {
                FileAssociations.EnsureAssociationsSet(FILETYPES);


            }
            catch (Exception e)
            {
                LogOutput($"Failed to register associations. ({e.Message})");
            }
            LogOutput("Done.");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lnk"></param>
        /// <param name="target"></param>
        /// <param name="type">0 = file, 1=dir, only with symbolic</param>
        /// <param name="hard"></param>
        /// <returns></returns>
        private static bool CreateLink(string lnk, string target, int type = 0, bool hard = true)
        {
            if (hard)
            {
                return CreateHardLink(lnk, target, IntPtr.Zero);
            }
            else
            {
                return CreateSymbolicLink(lnk, target, type);
            }
        }

        private static void RegisterAssociation(object filetype)
        {
            // throw new NotImplementedException();
        }

        private static string CheckForSuffix(string filePath, string suffix)
        {
            foreach (string filetype in FILETYPES)
            {
                if (filePath.EndsWith(filetype, StringComparison.InvariantCultureIgnoreCase))
                {
                    suffix = filetype;
                }
            }
            return suffix;
        }


        private static string CheckFilePath(string filename)
        {
            string filePath = filename;
            if (!File.Exists(filePath))
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    filePath)))
                {
                    filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        filePath);
                }
                else
                {
                    var msg = "Locations searched:  \r\n "
                              + filePath + " \r\n "
                              + Path.Combine(
                                  Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                  filePath)


                        ;
                    LogOutput("File not found.\r\n" + msg);
                    throw new FileNotFoundException(msg);
                }
            }

            if (string.IsNullOrEmpty(Path.GetDirectoryName(filename)))
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            }

            return filePath;
        }

        public static void LogOutput(string msg)
        {
            try
            {
                switch (OUTPUT_TYPE)
                {
                    case OutputType.Console:
                    case OutputType.Log:
                        Console.WriteLine(msg);
                        break;

                    case OutputType.MsgBox: // MsgBox or Form
                        System.Windows.Forms.MessageBox.Show(msg);
                        break;

                    case OutputType.None:
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.Print(msg);
                Debug.Fail(e.Message, e.InnerException?.Message);
            }
        }
    }
}
