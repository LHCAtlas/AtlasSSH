using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CredentialManagement;
using System.Linq;
using Renci.SshNet;
using System.Threading;

namespace AtlasSSHTest
{
    [TestClass]
    public class RawInteractions
    {
        [TestMethod]
        public void ReadBack()
        {
            var info = util.GetUsernameAndPassword();

            var sclist = new CredentialSet(info.Item1);
            var passwordInfo = sclist.Load().Where(c => c.Username == info.Item2).FirstOrDefault();
            if (passwordInfo == null)
            {
                throw new ArgumentException(string.Format("Please create a generic windows credential with '{0}' as the target address, '{1}' as the username, and the password for remote SSH access to that machine.", info.Item1, info.Item2));
            }

            // Create the connection, but do it lazy so we don't do anything if we aren't used.
            var con = new SshClient(info.Item1, info.Item2, passwordInfo.Password);
            con.Connect();

            // And create a shell stream. Initialize to find the prompt so we can figure out, later, when
            // a task has finished.
            var s = con.CreateShellStream("Commands", 240, 200, 132, 80, 1024);

            s.WriteLine("read -p \"hi there\" bogus1 bogus2 bogus3 bogus4 bogus5");
            Thread.Sleep(200);
            s.WriteLine("dork is a winner");
            s.WriteLine("dork is a winner");
            Thread.Sleep(200);
            s.WriteLine("set");

            Thread.Sleep(200);

            string l = "";
            while ((l = s.ReadLine()) != null)
            {
                Console.WriteLine(l);
            }

            Assert.Inconclusive();
        }
    }
}
