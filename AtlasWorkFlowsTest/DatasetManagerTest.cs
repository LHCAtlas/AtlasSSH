using AtlasWorkFlows;
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
            Assert.AreEqual(0, DummyPlace.CopyLogs.Count);
        }

        /// <summary>
        /// Seen in the wild. We have a single file request, and it is already local. But
        /// system still queries other non-local locatoins.
        /// </summary>
        [TestMethod]
        public void LocalDatasetQueryDoesNotForceOtherQueries()
        {
            var p1 = new DummyPlace("bogusLocal") { { "ds1", "f1", "f2" } };
            p1.IsLocal = true;
            p1.DataTier = 1;
            var p2 = new DummyPlace("bogusRemote") { { "ds1", "f1", "f2" } };
            p2.IsLocal = false;
            p2.DataTier = 10;
            DatasetManager.ResetDSM(p1, p2);

            DatasetManager.MakeFilesLocal(new Uri[] { new Uri("gridds://ds1/f1") });
            Assert.AreEqual(0, p2.GetListOfFilesForDatasetCalled);
            Assert.AreEqual(0, p2.HasFileCalled);
        }

        [TestMethod]
        public void LocalPartialDatasetQueryDoesNotForceOtherQueries()
        {
            var p1 = new DummyPlace("bogusLocal") { { "ds1", "f1", "f2" } };
            p1.IsLocal = true;
            p1.DataTier = 1;
            p1.BlockHasFileFor("f2");
            var p2 = new DummyPlace("bogusRemote") { { "ds1", "f1", "f2" } };
            p2.IsLocal = false;
            p2.DataTier = 10;
            DatasetManager.ResetDSM(p1, p2);

            DatasetManager.MakeFilesLocal(new Uri[] { new Uri("gridds://ds1/f1") });
            Assert.AreEqual(0, p2.GetListOfFilesForDatasetCalled);
            Assert.AreEqual(0, p2.HasFileCalled);
        }

        [TestMethod]
        public void LocalPartialDatasetQueryForcesOtherQueries()
        {
            var p1 = new DummyPlace("bogusLocal") { { "ds1", "f1", "f2" } };
            p1.IsLocal = true;
            p1.DataTier = 1;
            p1.NeedsConfirmationCopy = false;
            p1.CanSourceACopy = true;
            p1.BlockHasFileFor("f2");
            var p2 = new DummyPlace("bogusRemote") { { "ds1", "f1", "f2" } };
            p2.IsLocal = false;
            p2.DataTier = 10;
            DatasetManager.ResetDSM(p1, p2);

            DatasetManager.MakeFilesLocal(new Uri[] { new Uri("gridds://ds1/f2") });
            Assert.AreEqual(0, p2.GetListOfFilesForDatasetCalled);
            Assert.AreEqual(1, p2.HasFileCalled);
        }

        [TestMethod]
        [ExpectedException(typeof(DatasetDoesNotExistException))]
        public void FindLocalDatasetWhenNotAround()
        {
            DatasetManager.ResetDSM(new DummyPlace("bogus") { { "ds1", "f1", "f2" } });
            var localFiles = DatasetManager.MakeFilesLocal(new Uri[] { new Uri("gridds://ds2/f2") });
        }

        [TestMethod]
        [ExpectedException(typeof(UnknownUriSchemeException))]
        public void FindLocalDatasetWithBadURIs()
        {
            var localFiles = DatasetManager.MakeFilesLocal(new Uri[] { new Uri("http://www.nytimes.com") });
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

            Assert.AreEqual(1, DummyPlace.CopyLogs.Count);
            Assert.AreEqual("bogusNonLocal -> *bogusLocal (2 files)", DummyPlace.CopyLogs[0]);
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
        public void CopyDatasetsWithSameFilenames()
        {
            var loc1 = new DummyPlace("bogusLocal") { NeedsConfirmationCopy = false, CanSourceACopy = true };
            var loc2 = new DummyPlace("bogusNonLocal") { IsLocal = false, DataTier = 10 };
            loc2.Add("ds1", "f1", "f2");
            loc2.Add("ds2", "f1", "f2");
            DatasetManager.ResetDSM(loc1, loc2);
            var files = DatasetManager.ListOfFilesInDataset("ds1").Concat(DatasetManager.ListOfFilesInDataset("ds2")).ToArray();
            var localFiles = DatasetManager.MakeFilesLocal(files);

            Assert.AreEqual(4, localFiles.Length);
            Assert.AreEqual(@"c:\junk\f1.txt", localFiles[0].OriginalString);
            Assert.AreEqual(@"c:\junk\f2.txt", localFiles[1].OriginalString);
            Assert.AreEqual(@"c:\junk\f1.txt", localFiles[2].OriginalString);
            Assert.AreEqual(@"c:\junk\f2.txt", localFiles[3].OriginalString);
        }

        [TestMethod]
        public void CopyToLocalFromMultipleDatasets()
        {
            var loc1 = new DummyPlace("bogusLocal") { NeedsConfirmationCopy = false, CanSourceACopy = true };
            var loc2 = new DummyPlace("bogusNonLocal") { IsLocal = false, DataTier = 10 };
            loc2.Add("ds1", "f1", "f2");
            loc2.Add("ds2", "f3", "f4");
            DatasetManager.ResetDSM(loc1, loc2);
            var files = DatasetManager.ListOfFilesInDataset("ds1").Concat(DatasetManager.ListOfFilesInDataset("ds2")).ToArray();
            var localFiles = DatasetManager.MakeFilesLocal(files);

            Assert.AreEqual(4, localFiles.Length);
            Assert.AreEqual(@"c:\junk\f1.txt", localFiles[0].OriginalString);
            Assert.AreEqual(@"c:\junk\f2.txt", localFiles[1].OriginalString);
            Assert.AreEqual(@"c:\junk\f3.txt", localFiles[2].OriginalString);
            Assert.AreEqual(@"c:\junk\f4.txt", localFiles[3].OriginalString);
        }

        [TestMethod]
        public void CopyFromFireWalledPlace()
        {
            // Make sure it finds a 3 step routing, from CERN, to the tev machines, back to a local.
            var loc1 = new DummyPlace("LocalDisk") { IsLocal = true, NeedsConfirmationCopy = false, CanSourceACopy = true };
            var loc2 = new DummyPlace("tev") { IsLocal = false, DataTier = 10, CanSourceACopy = true, NeedsConfirmationCopy = false };
            var loc3 = new DummyPlace("CERN") { IsLocal = false, DataTier = 10, CanSourceACopy = true, NeedsConfirmationCopy = false };

            loc1.ExplicitlyNotAllowed.Add(loc3);
            loc2.ExplicitlyNotAllowed.Add(loc3);
            loc2.ExplicitlyNotAllowed.Add(loc1);
            loc3.ExplicitlyNotAllowed.Add(loc1);

            loc3.Add("ds1", "f1", "f2");
            DatasetManager.ResetDSM(loc1, loc2, loc3);
            var files = DatasetManager.ListOfFilesInDataset("ds1");
            var localFiles = MakeFilesLocal(files);
            Assert.AreEqual(2, localFiles.Length);

            foreach (var c in DummyPlace.CopyLogs)
            {
                Console.WriteLine(c);
            }
            Assert.AreEqual(2, DummyPlace.CopyLogs.Count);
            Assert.AreEqual("*CERN -> tev (2 files)", DummyPlace.CopyLogs[0]);
            Assert.AreEqual("tev -> *LocalDisk (2 files)", DummyPlace.CopyLogs[1]);
        }

        [TestMethod]
        public void CopyFromGRIDExample()
        {
            // Make sure it finds a 3 step routing, from CERN, to the tev machines, back to a local.
            var loc1 = new DummyPlace("LocalDisk") { IsLocal = true, NeedsConfirmationCopy = false, CanSourceACopy = true };
            var loc2 = new DummyPlace("tev") { IsLocal = false, DataTier = 10, CanSourceACopy = true, NeedsConfirmationCopy = false };
            var loc3 = new DummyPlace("tev-GRID") { IsLocal = false, DataTier = 100, CanSourceACopy = true, NeedsConfirmationCopy = false };

            // The tev-GRID can copy only to tev
            loc3.ExplicitlyNotAllowed.Add(loc1);
            loc1.ExplicitlyNotAllowed.Add(loc3);

            // loc1 can only source a copy
            loc2.ExplicitlyNotAllowed.Add(loc1);

            loc3.Add("ds1", "f1", "f2");
            DatasetManager.ResetDSM(loc1, loc2, loc3);

            var files = DatasetManager.ListOfFilesInDataset("ds1");
            var localFiles = MakeFilesLocal(files);
            Assert.AreEqual(2, localFiles.Length);

            foreach (var c in DummyPlace.CopyLogs)
            {
                Console.WriteLine(c);
            }
            Assert.AreEqual(2, DummyPlace.CopyLogs.Count);
            Assert.AreEqual("*tev-GRID -> tev (2 files)", DummyPlace.CopyLogs[0]);
            Assert.AreEqual("tev -> *LocalDisk (2 files)", DummyPlace.CopyLogs[1]);
        }

        [TestMethod]
        public void CopyToFromGRIDWithConfirmation()
        {
            // Make sure it finds a 3 step routing, from CERN, to the tev machines, back to a local.
            var loc1 = new DummyPlace("LocalDisk") { IsLocal = true, NeedsConfirmationCopy = true, CanSourceACopy = true };
            var loc2 = new DummyPlace("tev") { IsLocal = false, DataTier = 10, CanSourceACopy = true, NeedsConfirmationCopy = false };
            var loc3 = new DummyPlace("tev-GRID") { IsLocal = false, DataTier = 100, CanSourceACopy = true, NeedsConfirmationCopy = false };

            // The tev-GRID can copy only to tev
            loc3.ExplicitlyNotAllowed.Add(loc1);
            loc1.ExplicitlyNotAllowed.Add(loc3);

            // loc1 can only source a copy
            loc2.ExplicitlyNotAllowed.Add(loc1);

            loc3.Add("ds1", "f1", "f2");
            DatasetManager.ResetDSM(loc1, loc2, loc3);

            var files = DatasetManager.ListOfFilesInDataset("ds1");
            var localFiles = DatasetManager.CopyFilesTo(loc1, files);
            Assert.AreEqual(2, localFiles.Length);

            foreach (var c in DummyPlace.CopyLogs)
            {
                Console.WriteLine(c);
            }
            Assert.AreEqual(2, DummyPlace.CopyLogs.Count);
            Assert.AreEqual("*tev-GRID -> tev (2 files)", DummyPlace.CopyLogs[0]);
            Assert.AreEqual("tev -> *LocalDisk (2 files)", DummyPlace.CopyLogs[1]);
        }

        [TestMethod]
        public void CopyFromGRIDWithTwoOptions()
        {
            // Make sure it finds a 3 step routing, from CERN, to the tev machines, back to a local.
            var loc1 = new DummyPlace("LocalDisk") { IsLocal = true, NeedsConfirmationCopy = false, CanSourceACopy = true };
            var loc2 = new DummyPlace("tev") { IsLocal = false, DataTier = 10, CanSourceACopy = true, NeedsConfirmationCopy = false };
            var loc3 = new DummyPlace("tev-GRID") { IsLocal = false, DataTier = 100, CanSourceACopy = true, NeedsConfirmationCopy = false };
            var loc4 = new DummyPlace("cern") { IsLocal = false, DataTier = 10, CanSourceACopy = true, NeedsConfirmationCopy = false };
            var loc5 = new DummyPlace("cern-GRID") { IsLocal = false, DataTier = 100, CanSourceACopy = true, NeedsConfirmationCopy = false };

            // The tev-GRID can copy only to tev
            loc3.ExplicitlyNotAllowed.Add(loc1);
            loc3.ExplicitlyNotAllowed.Add(loc4);
            loc3.ExplicitlyNotAllowed.Add(loc5);

            // tev-CERN can copy only to cern
            loc5.ExplicitlyNotAllowed.Add(loc1);
            loc5.ExplicitlyNotAllowed.Add(loc2);
            loc5.ExplicitlyNotAllowed.Add(loc3);

            // loc1 can only access tev and cern
            loc1.ExplicitlyNotAllowed.Add(loc3);
            loc1.ExplicitlyNotAllowed.Add(loc5);

            // loc1 can only source a copy
            loc2.ExplicitlyNotAllowed.Add(loc1);
            loc4.ExplicitlyNotAllowed.Add(loc1);

            loc3.Add("ds1", "f1", "f2");
            loc5.Add("ds1", "f1", "f2");
            DatasetManager.ResetDSM(loc1, loc2, loc3, loc4, loc5);

            var files = DatasetManager.ListOfFilesInDataset("ds1");
            var localFiles = MakeFilesLocal(files);
            Assert.AreEqual(2, localFiles.Length);

            foreach (var c in DummyPlace.CopyLogs)
            {
                Console.WriteLine(c);
            }
            Assert.AreEqual(2, DummyPlace.CopyLogs.Count);
            Assert.AreEqual("*tev-GRID -> tev (2 files)", DummyPlace.CopyLogs[0]);
            Assert.AreEqual("tev -> *LocalDisk (2 files)", DummyPlace.CopyLogs[1]);
        }

        [TestMethod]
        public void CopyFromLocalPlaceToLocalPlace()
        {
            var loc1 = new DummyPlace("l1") { IsLocal = true, NeedsConfirmationCopy = false, CanSourceACopy = true };
            var loc2 = new DummyPlace("l2") { IsLocal = true, NeedsConfirmationCopy = false, CanSourceACopy = true };

            loc1.Add("ds1", "f1", "f2");
            DatasetManager.ResetDSM(loc1, loc2);
            var f = DatasetManager.CopyFilesTo(loc2, DatasetManager.ListOfFilesInDataset("ds1"));
            Assert.AreEqual(2, f.Length);
            Assert.AreEqual(1, DummyPlace.CopyLogs.Count);
            Assert.AreEqual("*l1 -> l2 (2 files)", DummyPlace.CopyLogs[0]);
        }

        [TestMethod]
        public void CopyFromNonLocalPlaceToNonLocalPlace()
        {
            var loc1 = new DummyPlace("l1") { IsLocal = false, NeedsConfirmationCopy = false, CanSourceACopy = true };
            var loc2 = new DummyPlace("l2") { IsLocal = false, NeedsConfirmationCopy = false, CanSourceACopy = true };

            loc1.Add("ds1", "f1", "f2");
            DatasetManager.ResetDSM(loc1, loc2);
            var f = DatasetManager.CopyFilesTo(loc2, DatasetManager.ListOfFilesInDataset("ds1"));
            Assert.AreEqual(2, f.Length);
            Assert.AreEqual(1, DummyPlace.CopyLogs.Count);
            Assert.AreEqual("*l1 -> l2 (2 files)", DummyPlace.CopyLogs[0]);
        }

        [TestMethod]
        public void CopyToLocalWithoutConfirm()
        {
            var loc1 = new DummyPlace("l1") { IsLocal = true, NeedsConfirmationCopy = true, CanSourceACopy = true };
            var loc2 = new DummyPlace("l2") { IsLocal = true, NeedsConfirmationCopy = true, CanSourceACopy = true };

            loc1.Add("ds1", "f1", "f2");
            DatasetManager.ResetDSM(loc1, loc2);
            var f = DatasetManager.CopyFilesTo(loc2, DatasetManager.ListOfFilesInDataset("ds1"));
            Assert.AreEqual(2, f.Length);
            Assert.AreEqual(1, DummyPlace.CopyLogs.Count);
            Assert.AreEqual("*l1 -> l2 (2 files)", DummyPlace.CopyLogs[0]);
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

            public void CopyDataSetInfo(string dsName, string[] files, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                throw new NotImplementedException();
            }

            public void CopyFrom(IPlace origin, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(IPlace destination, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                throw new NotImplementedException();
            }

            public string[] GetListOfFilesForDataset(string dsname, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                return null;
            }

            public IEnumerable<Uri> GetLocalFileLocations(IEnumerable<Uri> uris)
            {
                throw new NotImplementedException();
            }

            public bool HasFile(Uri u, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                throw new NotImplementedException();
            }

            public void ResetConnections()
            {
            }
        }

        /// <summary>
        /// A local place, with perhaps some datasets in it.
        /// </summary>
        internal class DummyPlace : IPlace, IEnumerable<KeyValuePair<string, string[]>>
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
                CopyLogs = new List<string>();
                HasFileCalled = 0;
                GetListOfFilesForDatasetCalled = 0;
            }

            public static List<string> CopyLogs = new List<string>();

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
            public string[] GetListOfFilesForDataset(string dsname, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                GetListOfFilesForDatasetCalled++;
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
            public int HasFileCalled { get; private set; }
            public int GetListOfFilesForDatasetCalled { get; private set; }

            /// <summary>
            /// Add a pairing and no matter CanSourceACopy this will return false.
            /// </summary>
            public HashSet<IPlace> ExplicitlyNotAllowed = new HashSet<IPlace>();

            public bool CanSourceCopy(IPlace destination)
            {
                if (ExplicitlyNotAllowed.Contains(destination))
                {
                    return false;
                }
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
                    .Select(u => new Uri($"c:\\junk\\{u.DatasetFilename()}.txt"));
            }

            private List<string> _filesWeDontHaveLocally = new List<string>();
            /// <summary>
            /// so that a file that is in the dataset isn't on disk for this location.
            /// </summary>
            /// <param name="fname"></param>
            public void BlockHasFileFor(string fname)
            {
                _filesWeDontHaveLocally.Add(fname);
            }

            /// <summary>
            /// See if this file is here.
            /// </summary>
            /// <param name="u"></param>
            /// <param name="statusUpdate"></param>
            /// <param name="failNow"></param>
            /// <returns></returns>
            public bool HasFile(Uri u, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                HasFileCalled++;
                // Make sure the file is contained in one of our datasets
                if (!_dataset_list.ContainsKey(u.DatasetName()))
                    return false;

                if (_filesWeDontHaveLocally.Contains(u.DatasetFilename()))
                {
                    return false;
                }

                return _dataset_list[u.DatasetName()].Any(fname => fname == u.DatasetFilename());
            }

            /// <summary>
            /// Pretend to copy - make sure all URIs are in the same dataset.
            /// </summary>
            /// <param name="origin"></param>
            /// <param name="uris"></param>
            public void CopyFrom(IPlace origin, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                Assert.AreEqual(1, uris.Select(u => u.DatasetName()).Distinct().Count(), "Number of different datasets");
                CopyLogs.Add($"{origin.Name} -> *{Name} ({uris.Length} files)");
            }
            public void CopyTo(IPlace dest, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                Assert.AreEqual(1, uris.Select(u => u.DatasetName()).Distinct().Count(), "Number of different datasets");
                CopyLogs.Add($"*{Name} -> {dest.Name} ({uris.Length} files)");
            }

            public void CopyDataSetInfo(string dsName, string[] files, Action<string> statusUpdate = null, Func<bool> failNow = null)
            {
                throw new NotImplementedException();
            }

            public void ResetConnections()
            {
            }
        }
        #endregion
    }
}
