using AtlasSSH;
using AtlasWorkFlows;
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
            DataSetManager.ResetDSM();
        }

        /// <summary>
        /// Close down the ssh connection
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            _ssh.TestCleanup();
            DataSetManager.ResetDSM();
        }

        [TestMethod]
        public async Task GetExistingDatasetFileList()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            DataSetManager.ResetDSM(local_p, grid_p);

            var files = await grid_p.GetListOfFilesForDataSetAsync(_good_dsname);
            Assert.AreEqual(197, files.Length);
            // This might not be safe b.c. file order might not be idempotent, but try this for now.
            Assert.AreEqual(_good_dsfile_1, files[0]);
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public async Task GetExistingDatasetFileListTunnel()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            await GetExistingDatasetFileList();
        }

        [TestMethod]
        public async Task GetNonDatasetFileList()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            DataSetManager.ResetDSM(local_p, grid_p);

            var files = await grid_p.GetListOfFilesForDataSetAsync("mc15_13TeV.361032.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ12W.merge.DAOD_EXOT15.bogus");
            Assert.IsNull(files);
        }

        [TestMethod]
        public async Task HasExpectedFile()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            DataSetManager.ResetDSM(local_p, grid_p);

            Assert.IsTrue(await grid_p.HasFileAsync(new Uri($"gridds://{_good_dsname}/{_good_dsfile_1}")));
        }

        [TestMethod]
        public async Task DoesNotHaveWeirdFile()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            DataSetManager.ResetDSM(local_p, grid_p);

            Assert.IsFalse(await grid_p.HasFileAsync(new Uri($"gridds://{_good_dsname}/myfile.root")));
        }
        [TestMethod]
        public async Task DoesNotHaveWeirdDataset()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            DataSetManager.ResetDSM(local_p, grid_p);

            Assert.IsFalse(await grid_p.HasFileAsync(new Uri($"gridds://{_good_dsname}_bogus/{_good_dsfile_1}")));
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public void CanCopyToProperLocation()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            DataSetManager.ResetDSM(local_p, grid_p);

            Assert.IsTrue(grid_p.CanSourceCopy(local_p));
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public void CanCopyToProperLocationTunnel()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            CanCopyToProperLocation();
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public void CanNotCopyToProperLocation()
        {
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var local_p_other = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            DataSetManager.ResetDSM(local_p, grid_p, local_p_other);

            Assert.IsFalse(grid_p.CanSourceCopy(local_p_other));
        }

        /// <summary>
        /// Ignored because it will take way too long to run normally. But we should check it when debugging
        /// things. :-)
        /// </summary>
        [TestMethod]
        [Ignore]
        public async Task CopyTwoFiles()
        {
            _ssh.CreateRepro();
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            DataSetManager.ResetDSM(local_p, grid_p);

            var uris = new Uri[]
            {
                new Uri($"gridds://{_good_dsname}/{_good_dsfile_1}"),
                new Uri($"gridds://{_good_dsname}/{_good_dsfile_2}"),
            };

            await grid_p.CopyToAsync(local_p, uris);

            var files = await local_p.GetListOfFilesForDataSetAsync(_good_dsname);
            Assert.AreEqual(197, files.Length);
            Assert.IsTrue(await local_p.HasFileAsync(uris[0]));
            Assert.IsTrue(await local_p.HasFileAsync(uris[1]));
        }

        /// <summary>
        /// Ignored because it will take way too long to run normally. But we should check it when debugging
        /// things. :-)
        /// </summary>
        [TestMethod]
        [Ignore]
        public async Task CopyOneFilesTunnel()
        {
            // Should be setup to deal with about 4 GB of data in one file.
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnelBigData");
            _ssh.CreateRepro();
            var local_p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var grid_p = new PlaceGRID("test-GRID", local_p);
            DataSetManager.ResetDSM(local_p, grid_p);

            var uris = new Uri[]
            {
                new Uri($"gridds://{_good_dsname}/{_good_dsfile_1}"),
            };

            await grid_p.CopyToAsync(local_p, uris);

            var files = await local_p.GetListOfFilesForDataSetAsync(_good_dsname);
            Assert.AreEqual(197, files.Length);
            Assert.IsTrue(await local_p.HasFileAsync(uris[0]));
        }
    }
}
