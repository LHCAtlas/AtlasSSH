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
        private const string _good_dsfile_1 = "DAOD_EXOT15.09201449._000001.pool.root.1";
        private const string _good_dsfile_2 = "DAOD_EXOT15.09201449._000002.pool.root.1";

        UtilsForBuildingLinuxDatasets _ssh;

        [TestInitialize]
        public void TestSetup()
        {
            _ssh = new UtilsForBuildingLinuxDatasets();
        }

        /// <summary>
        /// Close down the ssh connection
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            _ssh.TestCleanup();
        }

        [TestMethod]
        public void GetExistingDatasetFileList()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            var files = grid_p.GetListOfFilesForDataset(_good_dsname);
            Assert.AreEqual(197, files.Length);
            // This might not be safe b.c. file order might not be idempotent, but try this for now.
            Assert.AreEqual(_good_dsfile_1, files[0]);
        }

        [TestMethod]
        public void GetExistingDatasetFileListTunnel()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            GetExistingDatasetFileList();
        }

        [TestMethod]
        public void GetNonDatasetFileList()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            var files = grid_p.GetListOfFilesForDataset("mc15_13TeV.361032.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ12W.merge.DAOD_EXOT15.bogus");
            Assert.IsNull(files);
        }

        [TestMethod]
        public void HasExpectedFile()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            Assert.IsTrue(grid_p.HasFile(new Uri($"gridds://{_good_dsname}/{_good_dsfile_1}")));
        }

        [TestMethod]
        public void DoesNotHaveWeirdFile()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            Assert.IsFalse(grid_p.HasFile(new Uri($"gridds://{_good_dsname}/myfile.root")));
        }
        [TestMethod]
        public void DoesNotHaveWeirdDataset()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            Assert.IsFalse(grid_p.HasFile(new Uri($"gridds://{_good_dsname}_bogus/{_good_dsfile_1}")));
        }

        [TestMethod]
        public void CanCopyToProperLocation()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            Assert.IsTrue(grid_p.CanSourceCopy(local_p));
        }

        [TestMethod]
        public void CanCopyToProperLocationTunnel()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            CanCopyToProperLocation();
        }

        [TestMethod]
        public void CanNotCopyToProperLocation()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var local_p_other = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            Assert.IsFalse(grid_p.CanSourceCopy(local_p_other));
        }

        /// <summary>
        /// Ignored because it will take way too long to run normally. But we should check it when debugging
        /// things. :-)
        /// </summary>
        [TestMethod]
        [Ignore]
        public void CopyTwoFiles()
        {
            _ssh.CreateRepro();
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);

            var uris = new Uri[]
            {
                new Uri($"gridds://{_good_dsname}/{_good_dsfile_1}"),
                new Uri($"gridds://{_good_dsname}/{_good_dsfile_2}"),
            };

            grid_p.CopyTo(local_p, uris);

            var files = local_p.GetListOfFilesForDataset(_good_dsname);
            Assert.AreEqual(197, files.Length);
            Assert.IsTrue(local_p.HasFile(uris[0]));
            Assert.IsTrue(local_p.HasFile(uris[1]));
        }

        /// <summary>
        /// Ignored because it will take way too long to run normally. But we should check it when debugging
        /// things. :-)
        /// </summary>
        [TestMethod]
        [Ignore]
        public void CopyOneFilesTunnel()
        {
            // Should be setup to deal with about 4 GB of data in one file.
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnelBigData");
            _ssh.CreateRepro();
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);

            var uris = new Uri[]
            {
                new Uri($"gridds://{_good_dsname}/{_good_dsfile_1}"),
            };

            grid_p.CopyTo(local_p, uris);

            var files = local_p.GetListOfFilesForDataset(_good_dsname);
            Assert.AreEqual(197, files.Length);
            Assert.IsTrue(local_p.HasFile(uris[0]));
        }
    }
}
