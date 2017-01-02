using AtlasWorkFlows;
using AtlasWorkFlows.Locations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

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

            public string[] GetListOfFilesForDataset(string dsname)
            {
                return null;
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

            public bool CanSourceCopy(IPlace destination)
            {
                return false;
            }

            public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
