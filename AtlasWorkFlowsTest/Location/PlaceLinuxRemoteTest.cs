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

        [TestMethod]
        public void GetReproDatasetFileListForBadDS()
        {
            CreateRepro();
            CreateDS("ds1", "f1.root", "f2.root");
            var p = new PlaceLinuxRemote("test", _remote_name, _remote_path);
            var files = p.GetListOfFilesForDataset("ds2");
            Assert.IsNull(files);
        }

        [TestMethod]
        public void GetReproDatasetFileList()
        {
            CreateRepro();
            CreateDS("ds1", "f1.root", "f2.root");
            var p = new PlaceLinuxRemote("test", _remote_name, _remote_path);
            var files = p.GetListOfFilesForDataset("ds");
            Assert.IsNotNull(files);
            Assert.AreEqual("f1.root", files[0]);
            Assert.AreEqual("f2.root", files[1]);
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

        private static SSHConnection _connection = null;

        /// <summary>
        /// Create a fresh, clean, repro on the remote machine
        /// </summary>
        private void CreateRepro()
        {
            // Make sure no one is debing a dick by accident.
            Assert.AreNotEqual(".", _remote_path);
            Assert.IsFalse(string.IsNullOrWhiteSpace(_remote_path));
            Assert.IsFalse(_remote_path.Contains("*"));

            // Create the new repro
            _connection = new SSHConnection(_remote_name, _remote_username);
            _connection.ExecuteLinuxCommand($"rm -rf {_remote_path}")
                .ExecuteLinuxCommand($"mkdir -p {_remote_path}");
        }
        #endregion
    }
}
