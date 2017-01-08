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
        UtilsForBuildingLinuxDatasets _ssh = null;

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
        public void GetReproDatasetFileListForBadDS()
        {
            _ssh.CreateRepro();
            _ssh.CreateDS("ds1", "f1.root", "f2.root");
            var p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var files = p.GetListOfFilesForDataset("ds2");
            Assert.IsNull(files);
        }
        [TestMethod]
        public void GetReproDatasetFileListForBadDSTunnel()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            GetReproDatasetFileListForBadDS();
        }

        [TestMethod]
        public void GetReproDatasetFileList()
        {
            _ssh.CreateRepro();
            _ssh.CreateDS("ds1", "f1.root", "f2.root");
            var p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var files = p.GetListOfFilesForDataset("ds1");
            Assert.IsNotNull(files);
            Assert.AreEqual("f1.root", files[0]);
            Assert.AreEqual("f2.root", files[1]);
        }

        [TestMethod]
        public void GetReproDatasetFileListTunnel()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            GetReproDatasetFileList();
        }

        [TestMethod]
        public void HasFileGoodFile()
        {
            _ssh.CreateRepro();
            _ssh.CreateDS("ds1", "f1.root", "f2.root");
            var p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            Assert.IsTrue(p.HasFile(new Uri("gridds://ds1/f1.root")));
        }

        [TestMethod]
        public void HasGoodFileTunnel()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            HasFileGoodFile();
        }

        [TestMethod]
        public void HasFileMissingFileInGoodDataset()
        {
            _ssh.CreateRepro();
            _ssh.CreateDS("ds1", "f1.root", "f2.root");
            _ssh.RemoveFileInDS("ds1", "f1.root");
            var p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            Assert.IsFalse(p.HasFile(new Uri("gridds://ds1/f1.root")));
        }

        [TestMethod]
        public void HasFileMissingFileInGoodDatastTunnel()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            HasFileMissingFileInGoodDataset();
        }

        [TestMethod]
        public void HasFileMissingDataset()
        {
            _ssh.CreateRepro();
            _ssh.CreateDS("ds1", "f1.root", "f2.root");
            _ssh.RemoveFileInDS("ds1", "f1.root");
            var p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            Assert.IsFalse(p.HasFile(new Uri("gridds://ds2/f1.root")));
        }

        [TestMethod]
        public void HasFileMissingDatasetTunnel()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            HasFileMissingDataset();
        }

        [TestMethod]
        public void CanSourceCopyToISCPTarget()
        {
            var p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var other = new ScpTargetDummy() { DoVisibility = true };
            Assert.IsTrue(p.CanSourceCopy(other));
        }

        [TestMethod]
        public void CanNotSourceCopyToISCPTarget()
        {
            var p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var other = new ScpTargetDummy() { DoVisibility = false };
            Assert.IsFalse(p.CanSourceCopy(other));
        }

        [TestMethod]
        public void CanNotSourceCopyToNonISCPTarget()
        {
            var p = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var other = new DummyPlace("testmeout");
            Assert.IsFalse(p.CanSourceCopy(other));
        }

#if false
        [TestMethod]
        public void CanSourceCopyFromSelf()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            var p1 = new PlaceLinuxRemote("test1", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var p2 = new PlaceLinuxRemote("test1", _ssh.RemotePath, _ssh.RemoteHostInfo);
            Assert.IsTrue(p1.CanSourceCopy(p2));
        }
#endif

        [TestMethod]
        public void CanSourceCopyFromSomeoneSomewhereElse()
        {
            var _ssh1 = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            var p1 = new PlaceLinuxRemote("test1", _ssh1.RemotePath, _ssh1.RemoteHostInfo);
            var _ssh2 = new UtilsForBuildingLinuxDatasets();
            var p2 = new PlaceLinuxRemote("test1", _ssh2.RemotePath, _ssh2.RemoteHostInfo);
            Assert.IsTrue(p1.CanSourceCopy(p2));
        }

        [TestMethod]
        public void CanNotSourceCopyFromSomeoneSomewhereElse()
        {
            var _ssh1 = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            var p1 = new PlaceLinuxRemote("test1", _ssh1.RemotePath, _ssh1.RemoteHostInfo);
            var _ssh2 = new UtilsForBuildingLinuxDatasets();
            var p2 = new PlaceLinuxRemote("test1", _ssh2.RemotePath, _ssh2.RemoteHostInfo);
            Assert.IsFalse(p2.CanSourceCopy(p1));
        }

        [TestMethod]
        public void CopyTo()
        {
            _ssh.CreateRepro();
            _ssh.CreateDS("ds1", "f1.root", "f2.root");
            var p1 = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var p2 = new PlaceLinuxRemote("test", _ssh.RemotePath + "2", _ssh.RemoteHostInfo);

            var fileList = new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") };
            p1.CopyTo(p2, fileList);

            Assert.IsTrue(p2.HasFile(fileList[0]));
        }

        [TestMethod]
        public void CopyToViaTunnel()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnel");
            _ssh.CreateRepro();
            _ssh.CreateDS("ds1", "f1.root", "f2.root");
            var p1 = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);

            var sshRemote = new UtilsForBuildingLinuxDatasets();
            sshRemote.CreateRepro();
            var p2 = new PlaceLinuxRemote("test", sshRemote.RemotePath, sshRemote.RemoteHostInfo);

            var fileList = new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") };
            p1.CopyTo(p2, fileList);
            Assert.IsTrue(p2.HasFile(fileList[0]));
        }

        [TestMethod]
        public void CopyToViaTunnelWithPassword()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnelBigData");
            _ssh.CreateRepro();
            _ssh.CreateDS("ds1", "f1.root", "f2.root");
            var p1 = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);

            var sshRemote = new UtilsForBuildingLinuxDatasets();
            sshRemote.CreateRepro();
            var p2 = new PlaceLinuxRemote("test", sshRemote.RemotePath, sshRemote.RemoteHostInfo);

            var fileList = new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") };
            p1.CopyTo(p2, fileList);
            Assert.IsTrue(p2.HasFile(fileList[0]));
        }

        [TestMethod]
        public void CopyToTwoStep()
        {
            _ssh.CreateRepro();
            _ssh.CreateDS("ds1", "f1.root", "f2.root");
            var p1 = new PlaceLinuxRemote("test1", _ssh.RemotePath, _ssh.RemoteHostInfo);
            _ssh.CreateRepro(_ssh.RemotePath + "2");
            var p2 = new PlaceLinuxRemote("test2", _ssh.RemotePath + "2", _ssh.RemoteHostInfo);

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
            _ssh.CreateRepro();
            _ssh.CreateDS("ds1", "f1.root", "f2.root");
            var p1 = new PlaceLinuxRemote("test1", _ssh.RemotePath, _ssh.RemoteHostInfo);
            var p2 = new PlaceLinuxRemote("test2", _ssh.RemotePath + "2", _ssh.RemoteHostInfo);

            var fileList = new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") };
            p2.CopyFrom(p1, fileList);

            Assert.IsTrue(p2.HasFile(fileList[0]));
            Assert.AreEqual(2, p2.GetListOfFilesForDataset("ds1").Length);
        }

        [TestMethod]
        public void CopyFromTwoStep()
        {
            _ssh.CreateRepro();
            _ssh.CreateDS("ds1", "f1.root", "f2.root");
            var p1 = new PlaceLinuxRemote("test1", _ssh.RemotePath, _ssh.RemoteHostInfo);
            _ssh.CreateRepro(_ssh.RemotePath + "2");
            var p2 = new PlaceLinuxRemote("test2", _ssh.RemotePath + "2", _ssh.RemoteHostInfo);

            var fileList = new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") };
            p2.CopyFrom(p1, fileList.Take(1).ToArray());
            Assert.IsTrue(p2.HasFile(fileList[0]));
            Assert.IsFalse(p2.HasFile(fileList[1]));

            p2.CopyFrom(p1, fileList.Skip(1).ToArray());
            Assert.IsTrue(p2.HasFile(fileList[1]));
        }

        [TestMethod]
        public void CopyFromViaTunnelWithPassword()
        {
            _ssh = new UtilsForBuildingLinuxDatasets("LinuxRemoteTestTunnelBigData");
            _ssh.CreateRepro();
            var p1 = new PlaceLinuxRemote("test", _ssh.RemotePath, _ssh.RemoteHostInfo);

            var sshRemote = new UtilsForBuildingLinuxDatasets();
            sshRemote.CreateRepro();
            sshRemote.CreateDS("ds1", "f1.root", "f2.root");
            var p2 = new PlaceLinuxRemote("test", sshRemote.RemotePath, sshRemote.RemoteHostInfo);

            var fileList = new Uri[] { new Uri("gridds://ds1/f1.root"), new Uri("gridds://ds1/f2.root") };
            p1.CopyFrom(p2, fileList);
            Assert.IsTrue(p1.HasFile(fileList[0]));
        }
        
        #region Helper Items

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

            public void CopyFrom(IPlace origin, Uri[] uris, Action<string> statusUpdate = null)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(IPlace destination, Uri[] uris, Action<string> statusUpdate = null)
            {
                throw new NotImplementedException();
            }

            public string[] GetListOfFilesForDataset(string dsname, Action<string> statusUpdate = null)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<Uri> GetLocalFileLocations(IEnumerable<Uri> uris)
            {
                throw new NotImplementedException();
            }

            public bool HasFile(Uri u, Action<string> statusUpdate = null)
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

            public void CopyDataSetInfo(string key, string[] v, Action<string> statusUpdate = null)
            {
                throw new NotImplementedException();
            }

            public string GetPathToCopyFiles(string key)
            {
                throw new NotImplementedException();
            }

            public void CopyFromRemoteToLocal(string dsName, string[] files, DirectoryInfo ourpath, Action<string> statusUpdate = null)
            {
                throw new NotImplementedException();
            }

            public void CopyFromLocalToRemote(string dsName, IEnumerable<FileInfo> files, Action<string> statusUpdate = null)
            {
                throw new NotImplementedException();
            }
        }
#endregion
    }
}
