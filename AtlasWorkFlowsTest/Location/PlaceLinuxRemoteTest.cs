﻿using AtlasSSH;
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
    /// Test out a linux remote. Note that you will need a config file
    /// for this to work at all:
    /// </summary>
    [TestClass]
    public class PlaceLinuxRemoteTest
    {
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
            DiskCache.RemoveCache("PlaceLinuxDatasetFileList");

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

        [TestMethod]
        public void GetReproDatasetFileListForBadDS()
        {
            CreateRepro();
            CreateDS("ds1", "f1.root", "f2.root");
            var p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            var files = p.GetListOfFilesForDataset("ds2");
            Assert.IsNull(files);
        }

        [TestMethod]
        public void GetReproDatasetFileList()
        {
            CreateRepro();
            CreateDS("ds1", "f1.root", "f2.root");
            var p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            var files = p.GetListOfFilesForDataset("ds1");
            Assert.IsNotNull(files);
            Assert.AreEqual("f1.root", files[0]);
            Assert.AreEqual("f2.root", files[1]);
        }

        [TestMethod]
        public void HasFileGoodFile()
        {
            CreateRepro();
            CreateDS("ds1", "f1.root", "f2.root");
            var p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            Assert.IsTrue(p.HasFile(new Uri("gridds://ds1/f1.root")));
        }

        [TestMethod]
        public void HasFileMissingFileInGoodDataset()
        {
            CreateRepro();
            CreateDS("ds1", "f1.root", "f2.root");
            RemoveFileInDS("ds1", "f1.root");
            var p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            Assert.IsFalse(p.HasFile(new Uri("gridds://ds1/f1.root")));
        }

        [TestMethod]
        public void HasFileMissingDataset()
        {
            CreateRepro();
            CreateDS("ds1", "f1.root", "f2.root");
            RemoveFileInDS("ds1", "f1.root");
            var p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            Assert.IsFalse(p.HasFile(new Uri("gridds://ds1/f1.root")));
        }

        [TestMethod]
        public void CanSourceCopyToISCPTarget()
        {
            var p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            var other = new ScpTargetDummy() { DoVisibility = true };
            Assert.IsTrue(p.CanSourceCopy(other));
        }

        [TestMethod]
        public void CanNotSourceCopyToISCPTarget()
        {
            var p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            var other = new ScpTargetDummy() { DoVisibility = false };
            Assert.IsFalse(p.CanSourceCopy(other));
        }

        [TestMethod]
        public void CanNotSourceCopyToNonISCPTarget()
        {
            var p = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            var other = new DummyPlace("testmeout");
            Assert.IsFalse(p.CanSourceCopy(other));
        }

        [TestMethod]
        public void CopyTo()
        {
            CreateRepro();
            CreateDS("ds1", "f1.root", "f2.root");
            var p1 = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path);
            var p2 = new PlaceLinuxRemote("test", _remote_name, _remote_username, _remote_path + "2");

            var fileList = new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") };
            p1.CopyTo(p2, fileList);

            Assert.IsTrue(p2.HasFile(fileList[0]));
        }

        [TestMethod]
        public void CopyToTwoStep()
        {
            CreateRepro();
            CreateDS("ds1", "f1.root", "f2.root");
            var p1 = new PlaceLinuxRemote("test1", _remote_name, _remote_username, _remote_path);
            CreateRepro(_remote_path + "2");
            var p2 = new PlaceLinuxRemote("test2", _remote_name, _remote_username, _remote_path + "2");

            var fileList = new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") };
            p1.CopyTo(p2, fileList.Take(1).ToArray());
            Assert.IsTrue(p2.HasFile(fileList[0]));
            Assert.IsFalse(p2.HasFile(fileList[1]));

            p1.CopyTo(p2, fileList.Skip(1).ToArray());
            Assert.IsTrue(p2.HasFile(fileList[1]));
        }

        [TestMethod]
        public void CopyFrom()
        {
            CreateRepro();
            CreateDS("ds1", "f1.root", "f2.root");
            var p1 = new PlaceLinuxRemote("test1", _remote_name, _remote_username, _remote_path);
            var p2 = new PlaceLinuxRemote("test2", _remote_name, _remote_username, _remote_path + "2");

            var fileList = new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") };
            p2.CopyFrom(p1, fileList);

            Assert.IsTrue(p2.HasFile(fileList[0]));
            Assert.AreEqual(2, p2.GetListOfFilesForDataset("ds1").Length);
        }

        [TestMethod]
        public void CopyFromTwoStep()
        {
            CreateRepro();
            CreateDS("ds1", "f1.root", "f2.root");
            var p1 = new PlaceLinuxRemote("test1", _remote_name, _remote_username, _remote_path);
            CreateRepro(_remote_path + "2");
            var p2 = new PlaceLinuxRemote("test2", _remote_name, _remote_username, _remote_path + "2");

            var fileList = new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") };
            p2.CopyFrom(p1, fileList.Take(1).ToArray());
            Assert.IsTrue(p2.HasFile(fileList[0]));
            Assert.IsFalse(p2.HasFile(fileList[1]));

            p2.CopyFrom(p1, fileList.Skip(1).ToArray());
            Assert.IsTrue(p2.HasFile(fileList[1]));
        }

        #region Helper Items
        private void CreateDS(string ds, params string[] filenames)
        {
            var dsDir = $"{_remote_path}/{ds}";
            var dsFileDir = $"{dsDir}/files";

            // Create the directories.
            _connection.ExecuteLinuxCommand($"mkdir -p {dsFileDir}");

            // For the files create them and add them to the whole thing.
            foreach (var f in filenames)
            {
                _connection
                    .ExecuteLinuxCommand($"echo {f} >> {dsDir}/aa_dataset_complete_file_list.txt")
                    .ExecuteLinuxCommand($"echo hi > {dsFileDir}/{f}");                
            }
        }

        /// <summary>
        /// Remove a file in a dataset
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        private void RemoveFileInDS(string dsname, string fname)
        {
            _connection.ExecuteLinuxCommand($"rm {_remote_path}/{dsname}/files/{fname}");
        }

        private static SSHConnection _connection = null;

        /// <summary>
        /// Create a fresh, clean, repro on the remote machine
        /// </summary>
        private void CreateRepro(string remote_path = null)
        {
            if (remote_path == null)
                remote_path = _remote_path;

            // Make sure no one is debing a dick by accident.
            Assert.AreNotEqual(".", remote_path);
            Assert.IsFalse(string.IsNullOrWhiteSpace(remote_path));
            Assert.IsFalse(remote_path.Contains("*"));

            // Create the new repro
            _connection = new SSHConnection(_remote_name, _remote_username);
            _connection.ExecuteLinuxCommand($"rm -rf {remote_path}")
                .ExecuteLinuxCommand($"mkdir -p {remote_path}");
        }

        /// <summary>
        /// Dummy that implements the target.
        /// </summary>
        class ScpTargetDummy : IPlace, ISCPTarget
        {
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

            public bool CanSourceCopy(IPlace destination)
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

            public bool HasFile(Uri u)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Teach the obj how to respond.
            /// </summary>
            public bool DoVisibility { get; set; }

            public string SCPMachineName
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
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

                set
                {
                    throw new NotImplementedException();
                }
            }

            public bool SCPIsVisibleFrom(string internetLocation)
            {
                return DoVisibility;
            }

            public string GetSCPFilePath(Uri f)
            {
                throw new NotImplementedException();
            }

            public void CopyDataSetInfo(string key, string[] v)
            {
                throw new NotImplementedException();
            }

            public string GetPathToCopyFiles(string key)
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}