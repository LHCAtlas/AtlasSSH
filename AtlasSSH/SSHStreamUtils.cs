using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AtlasSSH
{
    static class SSHStreamUtils
    {
        /// <summary>
        /// Read text that is sitting in the buffer. But don't do it until
        /// the computer has had a chance to dump it. There is a hardwired 10 second
        /// wait for the first text to show up.
        /// </summary>
        /// <param name="shell">The shell stream to look for text</param>
        /// <param name="msToWaitAfterText">How long to wait after text appears in the buffer before reading everything.</param>
        /// <returns></returns>
        public static string ReadRemainingText(this ShellStream shell, int msToWaitAfterText)
        {
            var timeout = DateTime.Now + TimeSpan.FromSeconds(10);
            while (shell.Length == 0 && timeout > DateTime.Now)
            {
                Thread.Sleep(20);
            }

            if (shell.Length == 0)
            {
                throw new InvalidOperationException("Waited 10 seconds for any output from shell; nothing seen. Possible hang?");
            }

            Thread.Sleep(msToWaitAfterText);

            // Sometimes two lines are sent. No idea why.
            var r = shell.ReadLine(TimeSpan.FromMilliseconds(1));
            if (r != null && r.Length > 0)
            {
                return r;
            }
            else
            {
                return shell.Read();
            }
        }

        /// <summary>
        /// Move through everything in the input stream until it looks like we are looking at a prompt.
        /// </summary>
        /// <param name="shell"></param>
        public static string WaitTillPromptText(this ShellStream shell)
        {
            var timeout = DateTime.Now + TimeSpan.FromSeconds(10);
            var allText = new StringBuilder();
            while (true)
            {
                while (shell.Length == 0 && timeout > DateTime.Now)
                {
                    Thread.Sleep(10);
                }
                if (shell.Length == 0)
                {
                    throw new InvalidOperationException("It could be that the remote machine isn't responding - didn't get anything that looked like a prompt in 10 seconds");
                }

                string line;
                while ((line = shell.ReadLine(TimeSpan.FromMilliseconds(10))) != null)
                {
                    Trace.WriteLine("WaitTillPromptText: read text: " + line, "SSHConnection");
                    allText.AppendLine(line);
                }

                if (shell.Length > 0)
                    return allText.ToString();
            }
        }
    }
}
