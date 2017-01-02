﻿using AtlasWorkFlows;
using AtlasWorkFlows.Locations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using static AtlasWorkFlows.DatasetManager;

namespace AtlasWorkFlowsTest
{
    [TestClass]
    public class DatasetManagerTest
    {
        [TestInitialize]
        public void TestInit()
        {
            DatasetManager.ResetDSM(new EmptyNonLocalPlace());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DatasetManager.ResetDSM();
        }

        [TestMethod]
        [ExpectedException(typeof(DatasetManager.DatasetDoesNotExistException))]
        public void NoPlaces()
        {
            var files = DatasetManager.ListOfFilesInDataset("bogus");
        }

        [TestMethod]
        public void PlaceWithDataset()
        {
            DatasetManager.ResetDSM(new DummyPlace("bogus") { { "ds1", "f1", "f2" } });
            var files = DatasetManager.ListOfFilesInDataset("ds1");
            Assert.IsNotNull(files);
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual("gridds://ds1/f1", files[0].OriginalString);
            Assert.AreEqual("gridds://ds1/f2", files[1].OriginalString);
        }

        [TestMethod]
        public void PlaceWithSecondDataset()
        {
            DatasetManager.ResetDSM(
                new DummyPlace("bogus1") { { "ds1", "f1", "f2" } },
                new DummyPlace("bogus2") { { "ds2", "f1", "f2" } }
                );
            var files = DatasetManager.ListOfFilesInDataset("ds2");
            Assert.IsNotNull(files);
            Assert.AreEqual(2, files.Length);
            Assert.AreEqual("gridds://ds2/f1", files[0].OriginalString);
            Assert.AreEqual("gridds://ds2/f2", files[1].OriginalString);
        }

        [TestMethod]
        public void FindLocalDatasetThatIsLocal()
        {
            DatasetManager.ResetDSM(new DummyPlace("bogus") { { "ds1", "f1", "f2" } });
            var files = DatasetManager.ListOfFilesInDataset("ds1");
            var localFiles = DatasetManager.MakeFilesLocal(files);
            Assert.IsNotNull(localFiles);
            Assert.AreEqual(2, localFiles.Length);
            Assert.AreEqual(@"c:\junk\f1.txt", localFiles[0].OriginalString);
            Assert.AreEqual(@"c:\junk\f2.txt", localFiles[1].OriginalString);
        }

        [TestMethod]
        [ExpectedException(typeof(DatasetDoesNotExistException))]
        public void FindLocalDatasetWhenNotAround()
        {
            DatasetManager.ResetDSM(new DummyPlace("bogus") { { "ds1", "f1", "f2" } });
            var localFiles = DatasetManager.MakeFilesLocal(new Uri("gridds://ds2/f2"));
        }

        [TestMethod]
        [ExpectedException(typeof(UnknownUriSchemeException))]
        public void FindLocalDatasetWithBadURIs()
        {
            var localFiles = DatasetManager.MakeFilesLocal(new Uri("http://www.nytimes.com"));
        }

        [TestMethod]
        public void CopyToLocal()
        {
            var loc1 = new DummyPlace("bogusLocal") { NeedsConfirmationCopy = false, CanSourceACopy = true };
            var loc2 = new DummyPlace("bogusNonLocal") { IsLocal = false, DataTier = 10 };
            loc2.Add("ds1", "f1", "f2");
            DatasetManager.ResetDSM(loc1, loc2);
            var files = DatasetManager.ListOfFilesInDataset("ds1");
            var localFiles = DatasetManager.MakeFilesLocal(files);
            Assert.IsNotNull(localFiles);
            Assert.AreEqual(2, localFiles.Length);
            Assert.AreEqual(@"c:\junk\f1.txt", localFiles[0].OriginalString);
            Assert.AreEqual(@"c:\junk\f2.txt", localFiles[1].OriginalString);
        }

        [TestMethod]
        [ExpectedException(typeof(NoLocalPlaceToCopyToException))]
        public void CopyToLocalWithNoNonConfirmLocalAvailible()
        {
            var loc1 = new DummyPlace("bogusLocal") { NeedsConfirmationCopy = true, CanSourceACopy = true };
            var loc2 = new DummyPlace("bogusNonLocal") { IsLocal = false, DataTier = 10 };
            loc2.Add("ds1", "f1", "f2");
            DatasetManager.ResetDSM(loc1, loc2);
            var files = DatasetManager.ListOfFilesInDataset("ds1");
            var localFiles = DatasetManager.MakeFilesLocal(files);
        }

        [TestMethod]
        public void CopyToLocalFromMultipleDatasets()
        {
            Assert.Inconclusive();
            // Make sure we can copy from multiple datasets without messing up the internal copy.
        }

        #region Places
        class EmptyNonLocalPlace : IPlace
        {
            public EmptyNonLocalPlace(string name = "noway")
            {
                Name = name;
            }
            public int DataTier { get { return 500; }  }

            public bool IsLocal { get { return false; } }

            public string Name { get; private set; }

            public bool NeedsConfirmationCopy { get { return true; } }

            public bool CanSourceCopy(IPlace destination)
            {
                return false;
            }

            public void Copy(IPlace origin, Uri[] uris)
            {
                throw new NotImplementedException();
            }

            public string[] GetListOfFilesForDataset(string dsname)
            {
                return null;
            }

            public IEnumerable<Uri> GetLocalFileLocations(IEnumerable<Uri> uris)
            {
                throw new NotImplementedException();
            }

            public bool HasFile(Uri u)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// A local place, with perhaps some datasets in it.
        /// </summary>
        class DummyPlace : IPlace, IEnumerable<KeyValuePair<string, string[]>>
        {
            /// <summary>
            /// By default setup as a dummy local place
            /// </summary>
            /// <param name="name"></param>
            public DummyPlace(string name)
            {
                Name = name;
                DataTier = 1;
                IsLocal = true;
                NeedsConfirmationCopy = true;
                CanSourceACopy = false;
            }

            private Dictionary<string, string[]> _dataset_list = new Dictionary<string, string[]>();

            /// <summary>
            /// Add a dataset with some files
            /// </summary>
            /// <param name="dsName"></param>
            /// <param name="files"></param>
            public void Add(string dsName, params string[] files)
            {
                _dataset_list[dsName] = files;
            }
            public string[] GetListOfFilesForDataset(string dsname)
            {
                if (_dataset_list.ContainsKey(dsname))
                {
                    return _dataset_list[dsname];
                }
                return null;
            }

            public int DataTier { get; set; }

            public bool IsLocal { get; set; }

            public string Name { get; private set; }

            public bool NeedsConfirmationCopy { get; set; }

            public bool CanSourceACopy { get; set; }

            public bool CanSourceCopy(IPlace destination)
            {
                return CanSourceACopy;
            }

            public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Return a set of local files.
            /// </summary>
            /// <param name="uris"></param>
            /// <returns></returns>
            public IEnumerable<Uri> GetLocalFileLocations(IEnumerable<Uri> uris)
            {
                Assert.IsTrue(IsLocal, "Can't get local file locations unless this is a local repo!");
                return uris
                    .Select(u => new Uri($"c:\\junk\\{u.Segments.Last()}.txt"));
            }

            public bool HasFile(Uri u)
            {
                // Make sure the file is contained in one of our datasets
                if (!_dataset_list.ContainsKey(u.Authority))
                    return false;

                return _dataset_list[u.Authority].Any(fname => fname == u.Segments.Last());
            }

            /// <summary>
            /// Pretend to copy - make sure all URIs are in the same dataset.
            /// </summary>
            /// <param name="origin"></param>
            /// <param name="uris"></param>
            public void Copy(IPlace origin, Uri[] uris)
            {
                Assert.AreEqual(1, uris.Select(u => u.Authority).Distinct().Count(), "Number of different datasets");
            }
        }
        #endregion
    }
}
