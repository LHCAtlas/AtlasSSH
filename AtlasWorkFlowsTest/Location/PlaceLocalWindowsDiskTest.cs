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
        [TestMethod]
        public void ExistingFileInExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var u = new Uri("gridds://ds1/f1.root");
            Assert.IsTrue(place.HasFile(u));
        }

        [TestMethod]
        public void NonExistingFileInExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            File.Delete($"{repro.FullName}\\ds1\\files\\f1.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var u = new Uri("gridds://ds1/f1.root");
            Assert.IsFalse(place.HasFile(u));
        }

        [TestMethod]
        public void BadFileInExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var u = new Uri("gridds://ds1/f3.root");

            // This is undefined behavior - HasFile can only take URI's for files that exist in the dataset.
            Assert.IsFalse(place.HasFile(u));
        }

        [TestMethod]
        public void FileInNonExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var u = new Uri("gridds://ds2/f3.root");
            Assert.IsFalse(place.HasFile(u));
        }

        [TestMethod]
        public void GetDSFileListForExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var files = place.GetListOfFilesForDataset("ds1");
            Assert.IsNotNull(files);
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual("f1.root", files[0]);
            Assert.AreEqual("f2.root", files[1]);
        }

        [TestMethod]
        public void GetDSFileListForExistingDatasetButMissingOneFile()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            File.Delete($"{repro.FullName}\\ds1\\files\\f1.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var files = place.GetListOfFilesForDataset("ds1");
            Assert.IsNotNull(files);
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual("f1.root", files[0]);
            Assert.AreEqual("f2.root", files[1]);
        }

        [TestMethod]
        public void GetDSFileListForNonExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var files = place.GetListOfFilesForDataset("ds2");
            Assert.IsNull(files);
        }

        [TestMethod]
        public void GetLocalPathsForDSFiles()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var files = place.GetLocalFileLocations(new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") });
            Assert.AreEqual(2, files.Count());
            foreach (var f in files)
            {
                Assert.IsTrue(new FileInfo(f.LocalPath).Exists);
            }
        }

        [TestMethod]
        public void GetLocalPathsForPartialDSFiles()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            File.Delete($"{repro.FullName}\\ds1\\files\\f2.root");
            var files = place.GetLocalFileLocations(new Uri[] { new Uri("gridds://ds1/f1.root") });
            Assert.AreEqual(1, files.Count());
            foreach (var f in files)
            {
                Assert.IsTrue(new FileInfo(f.LocalPath).Exists);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(DatasetFileNotLocalException))]
        public void GetLocalPathsForMissingDSFiles()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            File.Delete($"{repro.FullName}\\ds1\\files\\f2.root");
            var files = place.GetLocalFileLocations(new Uri[] { new Uri("gridds://ds1/f2.root") }).ToArray();
        }

        [TestMethod]
        [ExpectedException(typeof(DatasetDoesNotExistInThisReproException))]
        public void GetLocalPathsForMissingDS()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var files = place.GetLocalFileLocations(new Uri[] { new Uri("gridds://ds3/f2.root") }).ToArray();
        }

        [TestMethod]
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
        public void CantCopyFromAnotherTypeOfRepro()
        {
            var repro1 = BuildRepro("repro1");
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");

            var place1 = new PlaceLocalWindowsDisk("test1", repro1);
            var place2 = new DummyPlace("dork");

            Assert.IsFalse(place1.CanSourceCopy(place2));
        }

        [TestMethod]
        public void CanCopyFromAlwaysVisibleSCP()
        {
            var repro1 = BuildRepro("repro1");
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");

            var place1 = new PlaceLocalWindowsDisk("test1", repro1);
            var place2 = new SCPPlaceCanCopyTestJig() { ReturnSCPIsVisibleFrom = true };

            Assert.IsTrue(place1.CanSourceCopy(place2));
        }

        [TestMethod]
        public void CanCopyFromNeverVisibleSCP()
        {
            var repro1 = BuildRepro("repro1");
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");

            var place1 = new PlaceLocalWindowsDisk("test1", repro1);
            var place2 = new SCPPlaceCanCopyTestJig() { ReturnSCPIsVisibleFrom = false };

            Assert.IsFalse(place1.CanSourceCopy(place2));
        }

        [TestMethod]
        public void CopyFrom()
        {
            var repro1 = BuildRepro("repro1");
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");
            var repro2 = BuildRepro("repro2");

            var place1 = new PlaceLocalWindowsDisk("test1", repro1);
            var place2 = new PlaceLocalWindowsDisk("test2", repro2);

            Assert.IsNull(place2.GetListOfFilesForDataset("ds1"));
            place2.CopyFrom(place1, place1.GetListOfFilesForDataset("ds1").Select(f => new Uri($"gridds://ds1/{f}")).ToArray());
            Assert.AreEqual(2, place2.GetListOfFilesForDataset("ds1").Length);
        }

        [TestMethod]
        public void CopyTo()
        {
            var repro1 = BuildRepro("repro1");
            BuildDatset(repro1, "ds1", "f1.root", "f2.root");
            var repro2 = BuildRepro("repro2");

            var place1 = new PlaceLocalWindowsDisk("test1", repro1);
            var place2 = new PlaceLocalWindowsDisk("test2", repro2);

            Assert.IsNull(place2.GetListOfFilesForDataset("ds1"));
            place1.CopyTo(place2, place1.GetListOfFilesForDataset("ds1").Select(f => new Uri($"gridds://ds1/{f}")).ToArray());
            Assert.AreEqual(2, place2.GetListOfFilesForDataset("ds1").Length);
        }

        [TestMethod]
        public void FileNameColonsAreIgnored()
        {
            var repro = BuildRepro("repro");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            AddNamespaceToDatasetText(repro, "ds1", "user.gwatts");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var files = place.GetListOfFilesForDataset("ds1");
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
                        wr.Close();
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

            public int DataTier
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsLocal
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string Name
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool NeedsConfirmationCopy
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string SCPMachineName
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string SCPUser
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool ReturnForCanSourceCopy { get; set; }
            public bool CanSourceCopy(IPlace destination)
            {
                return ReturnForCanSourceCopy;
            }

            public void CopyDataSetInfo(string dsName, string[] files)
            {
                throw new NotImplementedException();
            }

            public void CopyFrom(IPlace origin, Uri[] uris)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(IPlace destination, Uri[] uris)
            {
                throw new NotImplementedException();
            }

            public string[] GetListOfFilesForDataset(string dsname)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<Uri> GetLocalFileLocations(IEnumerable<Uri> uris)
            {
                throw new NotImplementedException();
            }

            public string GetPathToCopyFiles(string dsName)
            {
                throw new NotImplementedException();
            }

            public string GetSCPFilePath(Uri f)
            {
                throw new NotImplementedException();
            }

            public bool HasFile(Uri u)
            {
                throw new NotImplementedException();
            }

            public bool ReturnSCPIsVisibleFrom { get; set; }
            public bool SCPIsVisibleFrom(string internetLocation)
            {
                return ReturnSCPIsVisibleFrom;
            }
        }
        #endregion
    }
}
