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
using static AtlasWorkFlowsTest.DatasetManagerTest;

namespace AtlasWorkFlowsTest.Location
{
    /// <summary>
    /// Test out the local disk
    /// </summary>
    [TestClass]
    public class PlaceLocalWindowsDiskTest
    {
        UtilsForBuildingLinuxDatasets _ssh = null;

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
        public async Task ExistingFileInExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var u = new Uri("gridds://ds1/f1.root");
            Assert.IsTrue(await place.HasFileAsync(u));
        }

        [TestMethod]
        public async Task NonExistingFileInExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            File.Delete($"{repro.FullName}\\ds1\\files\\f1.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var u = new Uri("gridds://ds1/f1.root");
            Assert.IsFalse(await place.HasFileAsync(u));
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public async Task BadFileInExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var u = new Uri("gridds://ds1/f3.root");

            // This is undefined behavior - HasFile can only take URI's for files that exist in the dataset.
            Assert.IsFalse(await place.HasFileAsync(u));
        }

        [TestMethod]
        public async Task FileInNonExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var u = new Uri("gridds://ds2/f3.root");
            Assert.IsFalse(await place.HasFileAsync(u));
        }

        [TestMethod]
        public async Task GetDSFileListForExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var files = await place.GetListOfFilesForDataSetAsync("ds1");
            Assert.IsNotNull(files);
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual("f1.root", files[0]);
            Assert.AreEqual("f2.root", files[1]);
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public async Task GetDSFileListForExistingDatasetButMissingOneFile()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            File.Delete($"{repro.FullName}\\ds1\\files\\f1.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var files = await place.GetListOfFilesForDataSetAsync("ds1");
            Assert.IsNotNull(files);
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual("f1.root", files[0]);
            Assert.AreEqual("f2.root", files[1]);
        }

        [TestMethod]
        public async Task GetDSFileListForNonExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var files = await place.GetListOfFilesForDataSetAsync("ds2");
            Assert.IsNull(files);
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public async Task GetLocalPathsForDSFiles()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var files = await place.GetLocalFileLocationsAsync(new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") });
            Assert.AreEqual(2, files.Count());
            foreach (var f in files)
            {
                Assert.IsTrue(new FileInfo(f.LocalPath).Exists);
            }
        }

        [TestMethod]
        public async Task GetLocalPathsForPartialDSFiles()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            File.Delete($"{repro.FullName}\\ds1\\files\\f2.root");
            var files = await place.GetLocalFileLocationsAsync(new Uri[] { new Uri("gridds://ds1/f1.root") });
            Assert.AreEqual(1, files.Count());
            foreach (var f in files)
            {
                Assert.IsTrue(new FileInfo(f.LocalPath).Exists);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(DataSetFileNotLocalException))]
        public async Task GetLocalPathsForMissingDSFiles()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            File.Delete($"{repro.FullName}\\ds1\\files\\f2.root");
            var files = (await place.GetLocalFileLocationsAsync(new Uri[] { new Uri("gridds://ds1/f2.root") })).ToArray();
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        [ExpectedException(typeof(DataSetDoesNotExistInThisReproException))]
        public async Task GetLocalPathsForMissingDS()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var files = (await place.GetLocalFileLocationsAsync(new Uri[] { new Uri("gridds://ds3/f2.root") })).ToArray();
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public void CanCopyFromAnotherLikeThis()
        {
            var repro1 = BuildRepro("repro1");
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");
            var repro2 = BuildRepro("repro2");

            var place1 = new PlaceLocalWindowsDisk("test1", repro1);
            var place2 = new PlaceLocalWindowsDisk("test2", repro2);

            Assert.IsTrue(place1.CanSourceCopy(place2));
        }

        [TestMethod]
        [DeploymentItem("location_test_unc.txt")]
        [DeploymentItem("location_test_params.txt")]
        public async Task TestUNCLocation()
        {
            // Create a UNC repro
            string uncRoot = File.ReadLines("location_test_unc.txt").First();
            var repro1 = new DirectoryInfo(uncRoot);
            if (repro1.Exists)
            {
                repro1.Delete(true);
            }
            repro1.Create();
            repro1.Refresh();

            // Now build a dataset there.
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");
            var place1 = new PlaceLocalWindowsDisk("test1", repro1);
            var files = (await place1.GetLocalFileLocationsAsync(new[] { new Uri("gridds://ds1/f1.root") })).ToArray();
            Assert.AreEqual(1, files.Length);
            Assert.AreEqual($"{uncRoot}\\ds1\\files\\f1.root", files[0].LocalPath);

        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public void CantCopyFromAnotherTypeOfRepro()
        {
            var repro1 = BuildRepro("repro1");
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");

            var place1 = new PlaceLocalWindowsDisk("test1", repro1);
            var place2 = new DummyPlace("dork");

            Assert.IsFalse(place1.CanSourceCopy(place2));
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public void CanCopyFromAlwaysVisibleSCP()
        {
            var repro1 = BuildRepro("repro1");
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");

            var place1 = new PlaceLocalWindowsDisk("test1", repro1);
            var place2 = new SCPPlaceCanCopyTestJig() { ReturnSCPIsVisibleFrom = true };

            Assert.IsTrue(place1.CanSourceCopy(place2));
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public void CanCopyFromNeverVisibleSCP()
        {
            var repro1 = BuildRepro("repro1");
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");

            var place1 = new PlaceLocalWindowsDisk("test1", repro1);
            var place2 = new SCPPlaceCanCopyTestJig() { ReturnSCPIsVisibleFrom = false };

            Assert.IsFalse(place1.CanSourceCopy(place2));
        }

        [TestMethod]
        public async Task CopyFrom()
        {
            var repro1 = BuildRepro("repro1");
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");
            var repro2 = BuildRepro("repro2");

            var place1 = new PlaceLocalWindowsDisk("test1", repro1);
            var place2 = new PlaceLocalWindowsDisk("test2", repro2);
            DataSetManager.ResetDSM(place1, place2);

            Assert.IsNull(await place2.GetListOfFilesForDataSetAsync("ds1"));
            await place2.CopyFromAsync(place1, (await place1.GetListOfFilesForDataSetAsync("ds1")).Select(f => new Uri($"gridds://ds1/{f}")).ToArray());
            Assert.AreEqual(2, (await place2.GetListOfFilesForDataSetAsync("ds1")).Length);
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public async Task CopyFromSCPTarget()
        {
            // Build remote dataset up on linux
            _ssh.CreateRepro();
            _ssh.CreateDS("ds1", "f1.root", "f2.root");
            var place1 = new PlaceLinuxRemote("test1", _ssh.RemotePath, _ssh.RemoteHostInfo);

            var repro1 = BuildRepro("repro2");
            var place2 = new PlaceLocalWindowsDisk("test1", repro1);
            DataSetManager.ResetDSM(place1, place2);

            var uris = new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") };
            await place2.CopyFromAsync(place1, uris);
            var files = await place2.GetListOfFilesForDataSetAsync("ds1");
            Assert.AreEqual(2, files.Length);
            Assert.IsTrue(await place2.HasFileAsync(uris[0]));
            Assert.IsTrue(await place2.HasFileAsync(uris[1]));
        }

        [TestMethod]
        [DeploymentItem("location_test_params.txt")]
        public async Task CopyToSCPTarget()
        {
            // Build remote dataset up on linux
            _ssh.CreateRepro();
            var place1 = new PlaceLinuxRemote("test1", _ssh.RemotePath, _ssh.RemoteHostInfo);

            var repro1 = BuildRepro("repro2");
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");
            var place2 = new PlaceLocalWindowsDisk("test1", repro1);
            DataSetManager.ResetDSM(place1, place2);

            var uris = new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") };
            await place2.CopyToAsync(place1, uris);
            var files = await place1.GetListOfFilesForDataSetAsync("ds1");
            Assert.AreEqual(2, files.Length);
            Assert.IsTrue(await place1.HasFileAsync(uris[0]));
            Assert.IsTrue(await place1.HasFileAsync(uris[1]));
        }

        [TestMethod]
        public async Task CopyTo()
        {
            var repro1 = BuildRepro("repro1");
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");
            var repro2 = BuildRepro("repro2");

            var place1 = new PlaceLocalWindowsDisk("test1", repro1);
            var place2 = new PlaceLocalWindowsDisk("test2", repro2);
            DataSetManager.ResetDSM(place1, place2);

            Assert.IsNull(await place2.GetListOfFilesForDataSetAsync("ds1"));
            await place1.CopyToAsync(place2, (await place1.GetListOfFilesForDataSetAsync("ds1")).Select(f => new Uri($"gridds://ds1/{f}")).ToArray());
            Assert.AreEqual(2, (await place2.GetListOfFilesForDataSetAsync("ds1")).Length);
        }

        [TestMethod]
        public async Task FileNameColonsAreIgnored()
        {
            var repro = BuildRepro("repro");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            AddNamespaceToDatasetText(repro, "ds1", "user.gwatts");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var files = await place.GetListOfFilesForDataSetAsync("ds1");
            Assert.AreEqual("f1.root", files[0]);
            Assert.AreEqual("f2.root", files[1]);
        }

        #region Setup Routines
        /// <summary>
        /// Build a repro at a particular location. Destory anything that was at that location previously.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private DirectoryInfo BuildRepro(string v)
        {
            var d = new DirectoryInfo(v);
            if (d.Exists)
            {
                d.Delete(true);
            }
            d.Create();
            d.Refresh();
            return d;
        }

        /// <summary>
        /// Build a dataset at root directory with given files.
        /// </summary>
        /// <param name="rootDir">Root directory name for the repo layout.</param>
        /// <param name="dsName"></param>
        /// <param name="v3"></param>
        /// <param name="v4"></param>
        private void BuildDatset(DirectoryInfo rootDir, string dsName, params string[] files)
        {
            var dsDir = new DirectoryInfo($"{rootDir.FullName}\\{dsName}");
            dsDir.Create();
            var fileDir = new DirectoryInfo($"{dsDir.FullName}\\files");
            fileDir.Create();

            using (var listingFileWr = File.CreateText(Path.Combine(dsDir.FullName, "aa_dataset_complete_file_list.txt")))
            {
                foreach (var f in files)
                {
                    var finfo = new FileInfo($"{fileDir.FullName}\\{f}");
                    using (var wr = finfo.CreateText())
                    {
                        wr.WriteLine("hi");
                        listingFileWr.WriteLine(f);
                    }
                }
            }
        }

        /// <summary>
        /// Add a namespace to the text file for a dataset.
        /// </summary>
        /// <param name="repro"></param>
        /// <param name="dsName"></param>
        /// <param name="nameSpace"></param>
        private void AddNamespaceToDatasetText(DirectoryInfo repro, string dsName, string nameSpace)
        {
            var dsDir = new DirectoryInfo($"{repro.FullName}\\{dsName}");

            var infoFile = new FileInfo(Path.Combine(dsDir.FullName, "aa_dataset_complete_file_list.txt"));
            var fnames = new List<string>();
            using (var rdr = infoFile.OpenText())
            {
                while (true)
                {
                    var l = rdr.ReadLine();
                    if (l == null)
                    {
                        break;
                    }
                    fnames.Add(l);
                }
            }

            using (var listingFileWr = infoFile.CreateText())
            {
                foreach (var f in fnames)
                {
                    listingFileWr.WriteLine($"{nameSpace}:{f}");
                }
            }
        }

        class SCPPlaceCanCopyTestJig : IPlace, ISCPTarget
        {
            public SCPPlaceCanCopyTestJig ()
            {
                ReturnForCanSourceCopy = false;
                ReturnSCPIsVisibleFrom = false;
            }

            public bool ReturnForCanSourceCopy { get; set; }
            public bool CanSourceCopy(IPlace destination)
            {
                return ReturnForCanSourceCopy;
            }

            public bool ReturnSCPIsVisibleFrom { get; set; }

            public string Name => throw new NotImplementedException();

            public bool IsLocal => throw new NotImplementedException();

            public int DataTier => throw new NotImplementedException();

            public bool NeedsConfirmationCopy => throw new NotImplementedException();

            public string SCPMachineName => throw new NotImplementedException();

            public string SCPUser => throw new NotImplementedException();

            public bool SCPIsVisibleFrom(string internetLocation)
            {
                return ReturnSCPIsVisibleFrom;
            }

            public Task ResetConnectionsAsync()
            {
                return Task.FromResult(true);
            }

            public Task<IEnumerable<Uri>> GetLocalFileLocationsAsync(IEnumerable<Uri> uris)
            {
                throw new NotImplementedException();
            }

            public Task<string[]> GetListOfFilesForDataSetAsync(string dsname, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                throw new NotImplementedException();
            }

            public Task CopyDataSetInfoAsync(string dsName, string[] files, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                throw new NotImplementedException();
            }

            public Task<bool> HasFileAsync(Uri u, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                throw new NotImplementedException();
            }

            public Task CopyToAsync(IPlace destination, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeoutMinutes = 60)
            {
                throw new NotImplementedException();
            }

            public Task CopyFromAsync(IPlace origin, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeoutMinutes = 60)
            {
                throw new NotImplementedException();
            }

            public Task<string> GetSCPFilePathAsync(Uri f)
            {
                throw new NotImplementedException();
            }

            public Task<string> GetPathToCopyFilesAsync(string dsName)
            {
                throw new NotImplementedException();
            }

            public Task CopyFromRemoteToLocalAsync(string dsName, string[] files, DirectoryInfo ourpath, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                throw new NotImplementedException();
            }

            public Task CopyFromLocalToRemoteAsync(string dsName, IEnumerable<FileInfo> files, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
