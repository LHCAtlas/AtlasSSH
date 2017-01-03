using AtlasWorkFlows.Locations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        [ExpectedException(typeof(DatasetDoesNotExistInThisReproException))]
        public void FileInNonExistingDataset()
        {
            var repro = BuildRepro("ExistingFileInExistingDataset");
            BuildDatset(repro, "ds1", "f1.root", "f2.root");
            var place = new PlaceLocalWindowsDisk("test", repro);
            var u = new Uri("gridds://ds2/f3.root");
            place.HasFile(u);
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
        #endregion
    }
}
