using AtlasSSH;
using AtlasWorkFlows.Locations;
using AtlasWorkFlows.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlowsTest.Location
{
    [TestClass]
    public class PlaceGRIDTest
    {
        /// <summary>
        /// A good dataset name we use for testing.
        /// </summary>
        private const string _good_dsname = "mc15_13TeV.361032.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ12W.merge.DAOD_EXOT15.e3668_s2608_s2183_r7772_r7676_p2711";

        /// <summary>
        /// A good file that exists in the dataset.
        /// </summary>
        private const string _good_dsfile = "DAOD_EXOT15.09201449._000001.pool.root.1";

        /// <summary>
        /// internet name of machine we will be accessing to run tests against.
        /// </summary>
        public string _remote_name;

        /// <summary>
        /// Absolute path on that machine of where the repro is to be put.
        /// </summary>
        public string _remote_path;

        /// <summary>
        /// Username we should use to access the remote fellow.
        /// </summary>
        public string _remote_username;

        [TestInitialize]
        public void TestSetup()
        {
            // Clean out all caches.
            DiskCache.RemoveCache("PlaceGRIDDSCatalog");

            // Load parameters that we can use to access the test machine.
            var cFile = new FileInfo("location_test_params.txt");
            Assert.IsTrue(cFile.Exists, $"Unable to locate test file {cFile.FullName}");
            var p = Config.ParseConfigFile(cFile);
            Assert.IsTrue(p.ContainsKey("LinuxRemoteTest"), "Unable to find machine info in LinuxRemoteTest");
            var lrtInfo = p["LinuxRemoteTest"];

            _remote_name = lrtInfo["LinuxHost"];
            _remote_path = lrtInfo["LinuxPath"];
            _remote_username = lrtInfo["LinuxUserName"];
        }

        /// <summary>
        /// Close down the ssh connection
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            if (_connection != null)
            {
                _connection.Dispose();
            }
        }

        private static SSHConnection _connection = null;


        [TestMethod]
        public void GetExistingDatasetFileList()
        {
            var local_p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            var files = grid_p.GetListOfFilesForDataset(_good_dsname);
            Assert.AreEqual(197, files.Length);
            // This might not be safe b.c. file order might not be idempotent, but try this for now.
            Assert.AreEqual(_good_dsfile, files[0]);
        }

        [TestMethod]
        public void GetNonDatasetFileList()
        {
            var local_p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            var files = grid_p.GetListOfFilesForDataset("mc15_13TeV.361032.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ12W.merge.DAOD_EXOT15.bogus");
            Assert.IsNull(files);
        }

        [TestMethod]
        public void HasExpectedFile()
        {
            var local_p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            Assert.IsTrue(grid_p.HasFile(new Uri($"gridds://{_good_dsname}/{_good_dsfile}")));
        }

        [TestMethod]
        public void DoesNotHaveWeirdFile()
        {
            var local_p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            Assert.IsFalse(grid_p.HasFile(new Uri($"gridds://{_good_dsname}/myfile.root")));
        }
        [TestMethod]
        public void DoesNotHaveWeirdDataset()
        {
            var local_p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            Assert.IsFalse(grid_p.HasFile(new Uri($"gridds://{_good_dsname}_bogus/{_good_dsfile}")));
        }

        // Who can we copy to
        // CopyTo Stuff
    }
}
