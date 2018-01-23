using Renci.SshNet;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AtlasSSH
{
    /// <summary>
    /// Throw when we can't find the linux shell prompt.
    /// </summary>
    [Serializable]
    public class NoLinuxShellPromptSeenException : Exception
    {
        public NoLinuxShellPromptSeenException() { }
        public NoLinuxShellPromptSeenException(string message) : base(message) { }
        public NoLinuxShellPromptSeenException(string message, Exception inner) : base(message, inner) { }
        protected NoLinuxShellPromptSeenException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

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
        public static async Task<string> ReadRemainingText(this ShellStream shell, int msToWaitAfterText)
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

            await Task.Delay(msToWaitAfterText);

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
        public static async Task<string> WaitTillPromptText(this ShellStream shell)
        {
            const int promptTimeout = 20;
            var timeout = DateTime.Now + TimeSpan.FromSeconds(promptTimeout);
            var allText = new StringBuilder();
            while (true)
            {
                while (shell.Length == 0 && timeout > DateTime.Now)
                {
                    await Task.Delay(10);
                }
                if (shell.Length == 0)
                {
                    throw new NoLinuxShellPromptSeenException($"It could be that the remote machine isn't responding - didn't get anything that looked like a prompt in {promptTimeout} seconds");
                }

                string line;
                while ((line = await shell.ReadLineAsync(TimeSpan.FromMilliseconds(10))) != null)
                {
                    //Trace.WriteLine("WaitTillPromptText: read text: " + line, "SSHConnection");
                    allText.AppendLine(line);
                }

                if (shell.Length > 0)
                    return allText.ToString();
            }
        }
    }

    internal static class ShellStreamUtils
    {
        public static Task<string> ReadLineAsync(this ShellStream stream, TimeSpan timeout)
        {
            return Task.Factory.StartNew(() => stream.ReadLine(timeout));
        }
    }
}
