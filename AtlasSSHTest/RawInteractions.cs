using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CredentialManagement;
using System.Linq;
using Renci.SshNet;
using System.Threading;
using System.IO;

namespace AtlasSSHTest
{
    [TestClass]
    public class RawInteractions
    {
#if false
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
            var host = info.Item1;
            var username = info.Item2;
            var password = passwordInfo.Password;

            // Do the work.
            var con = new SshClient(host, username, password);
            con.Connect();

            var s = con.CreateShellStream("Commands", 240, 200, 132, 80, 1024);

            var reader = new StreamReader(s);
            var writer = new StreamWriter(s);
            writer.AutoFlush = true;

            // Do the read command
            writer.WriteLine("read -p \"hi there: \" bogus");
            Thread.Sleep(200);
            writer.Write("Life Is Great\n");
            Thread.Sleep(200);
            writer.WriteLine("set | grep bogus");

            Thread.Sleep(200);

            string l = "";
            while ((l = reader.ReadLine()) != null)
            {
                Console.WriteLine(l);
            }

            Assert.Inconclusive();
        }
#endif
    }
}
