using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace altvisngs
{
    public class CommandLineProc
    {
        private System.Diagnostics.Process process;
        private System.Diagnostics.ProcessStartInfo startInfo;
        /// <summary> Get or set the excecutable name </summary>
        public string Executable { get; protected set; }

        /// <summary> Get if the run was successful </summary>
        public bool SuccessfulRun { get; protected set; }
        /// <summary> The string describing the first error encountered in the log file </summary>
        public string FirstError { get; protected set; }

        public CommandLineProc(string pExecutable)
        {
            this.Executable = pExecutable;
        }

        public void RunSync(string arguments, string logpath)
        {
            ProcessStartParams psp = new ProcessStartParams(this.Executable, arguments, logpath);
            startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;

            startInfo.FileName = psp.Executable;
            startInfo.Arguments = psp.Arguments;
            process = new System.Diagnostics.Process();
            process.StartInfo = startInfo;

            process.Start();
            process.WaitForExit();
        }

        protected class ProcessStartParams
        {
            public string Executable { get; protected set; }
            public string Arguments { get; protected set; }
            public string LogPath { get; protected set; }
            public ProcessStartParams(string pExecutable, string pArguments, string pLogPath)
            {
                this.Executable = pExecutable;
                this.Arguments = pArguments;
                this.LogPath = pLogPath;
            }
        }
    }
    public class latex : CommandLineProc
    {
        public latex() : base("latex.exe") { }
        public void RunSync(string filePath, string output_directory = "")
        {
            Console.WriteLine("Running " + this.Executable + " on `" + Path.GetFileName(filePath) + "'.");
            if (string.IsNullOrEmpty(output_directory))
                output_directory = Path.GetDirectoryName(filePath);
            base.RunSync("-interaction=nonstopmode" + " --output-directory=" + '\"' + output_directory + '\"' + " " +
                '\"' + filePath + '\"', output_directory + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".log");

            string err = ExitedWithError(output_directory + "\\" + Path.GetFileNameWithoutExtension(filePath) + ".log");
            SuccessfulRun = string.IsNullOrEmpty(err);
            FirstError = err;
            if (SuccessfulRun) Console.WriteLine(this.Executable + " exited normally.");
            else Console.WriteLine(this.Executable + " exited with error(s): `" + FirstError + "'.");
        }
        private string ExitedWithError(string logfile)
        {
            if (!File.Exists(logfile)) return "Log file `" + logfile + "' not found.";
            using (StreamReader sr = new StreamReader(logfile))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.Length == 0) continue;
                    if (line[0] == '!') return "`" + line + "'";
                }
            }
            return string.Empty;//no errors encountered.
        }
    }
    public class pdflatex : latex { public pdflatex() : base() { this.Executable = "pdflatex"; } }
}
