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
                System.Windows.Forms.MessageBox.Show("No arguments supplied. e.g.  OpenPartFile.exe \"myvideo.mkv.part\"");
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
                System.Windows.Forms.MessageBox.Show($"Only {String.Join(" , ", FILETYPES)} files are supported.");
                return;
            }

            var newLink = Path.Combine(Path.GetDirectoryName(filePath) , "tmpLink_"
                + Path.GetFileNameWithoutExtension(filePath));

            if (!File.Exists(newLink))
            {
                System.Windows.Forms.MessageBox.Show($">mklink {newLink} {filePath}"  );
                if (CreateLink(/*"\""+*/newLink/*.Replace(" ","\\ ")*/ /*+ "\""*/ , /*"\""+*/filePath/*.Replace(" ", "\\ ")*/ /*+ "\""*/ , 0))
                    System.Windows.Forms.MessageBox.Show($"symbolic link created for {newLink } <<===>> { filePath}" );
                else
                {
                    System.Windows.Forms.MessageBox.Show($"Failed to create file {newLink}. Attempting to use temp folder...");
                    newLink = Path.Combine(Path.GetTempPath(), "tmpLink_"
                        + Path.GetFileNameWithoutExtension(filePath));

                    if (!File.Exists(newLink))
                    {

                        System.Windows.Forms.MessageBox.Show($">mklink {newLink} {filePath}" );

                        if (CreateLink(/*"\""+*/newLink/*+"\""*/  , /*"\""+*/filePath/*+"\"" */, 0))
                            System.Windows.Forms.MessageBox.Show($"symbolic link created for {newLink} <<===>> {filePath}" );
                        else
                        {
                            System.Windows.Forms.MessageBox.Show($"Failed to create file {newLink}. Aborting...");
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
            System.Windows.Forms.MessageBox.Show("Setting up associations:");
            try
            {
                foreach (var filetype in FILETYPES)
                {
                    RegisterAssociation(filetype);
                    System.Windows.Forms.MessageBox.Show($" * Registered {filetype}");
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to register associations. ({e.Message})");
            }
            System.Windows.Forms.MessageBox.Show("Done.");

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
                    MessageBox.Show("File not found.\r\n"+msg);
                    throw new FileNotFoundException(msg);
                }
            }

            if (string.IsNullOrEmpty(Path.GetDirectoryName(filename)))
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            }

            return filePath;
        }
    }
}
