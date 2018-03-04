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

   static  class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes
        );


        enum OutputType
        {
            None=0,
            MsgBox=1,
            Console=2,
            Log=4
        }

        private static OutputType OUTPUT_TYPE = OutputType.None;
        static int SYMLINK_FLAG_DIRECTORY = 1;
        private static readonly IEnumerable<string> FILETYPES = new List<string>() { ".temp", ".tmp", ".part", ".crdownload" };

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

            if (args[0].ToLowerInvariant() == "/install")
            {
                SetupAssociations();
                return;
            }

            var filePath = CheckFilePath(String.Join(" ",args));
            var suffix = "";
            suffix = CheckForSuffix(filePath, suffix);
            if (suffix == "")
            {
                LogOutput($"Only {String.Join(" , ", FILETYPES)} files are supported.");
                return;
            }

            var newLink = Path.Combine(Path.GetDirectoryName(filePath) , "tmpLink_"
                + Path.GetFileNameWithoutExtension(filePath));

            if (!File.Exists(newLink))
            {
                LogOutput($">mklink {newLink} {filePath}"  );
                if (CreateLink(/*"\""+*/newLink/*.Replace(" ","\\ ")*/ /*+ "\""*/ , /*"\""+*/filePath/*.Replace(" ", "\\ ")*/ /*+ "\""*/ , 0))
                    LogOutput($"symbolic link created for {newLink } <<===>> { filePath}" );
                else
                {
                    LogOutput($"Failed to create file {newLink}. Attempting to use temp folder...");
                    newLink = Path.Combine(Path.GetTempPath(), "tmpLink_"
                        + Path.GetFileNameWithoutExtension(filePath));

                    if (!File.Exists(newLink))
                    {

                        LogOutput($">mklink {newLink} {filePath}" );

                        if (CreateLink(/*"\""+*/newLink/*+"\""*/  , /*"\""+*/filePath/*+"\"" */, 0))
                            LogOutput($"symbolic link created for {newLink} <<===>> {filePath}" );
                        else
                        {
                            LogOutput($"Failed to create file {newLink}. Aborting...");
                            return;
                        }
                    }
                }

            }

            var startUsingDefaultProgram = false;
            MessageBox.Show($"Attempting to load {(startUsingDefaultProgram?"default application": "Open-With dialog")} for {newLink}");
            OpenAs(filename: newLink,startUsingDefaultProgram: startUsingDefaultProgram);

        }

        static void OpenAs(string filename,bool startUsingDefaultProgram=false)
        {
            var filePath = CheckFilePath(filename);
            if(startUsingDefaultProgram) Process.Start("\""+filePath+"\"");
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
        private static bool CreateLink(string lnk, string target, int type=0, bool hard = true)
        {
            if (hard)
            {
               return CreateHardLink(lnk, target, IntPtr.Zero);
            }
            else
            {
              return  CreateSymbolicLink(lnk, target, type);
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
                    LogOutput("File not found.\r\n"+msg);
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
            switch (OUTPUT_TYPE)
            {
                case OutputType.Console:
                case OutputType.Log:
                case OutputType.MsgBox:
                case OutputType.None:
                    try
                    {
                        System.Windows.Forms.MessageBox.Show(msg);
                    }
                    catch (Exception e)
                    {
                        Debug.Print(msg);
                        Debug.Fail(e.Message,e.InnerException?.Message);
                    }
                    break;
                    

                    default:
                    break;
                    
            }
        }
    }
}
