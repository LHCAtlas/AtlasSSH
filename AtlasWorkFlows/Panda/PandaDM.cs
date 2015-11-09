using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Panda
{
    /// <summary>
    /// File contains the various objects that we will get back from a panda query.
    /// </summary>

    // Actual datasets
    public class Dataset
    {
        public string containername { get; set; }
        public string datasetname { get; set; }
        public int nfilesfinished { get; set; }
        public int nfilesonhold { get; set; }
        public string creationtime { get; set; }
        public string site { get; set; }
        public string vo { get; set; }
        public object frozentime { get; set; }
        public int nfilesfailed { get; set; }
        public string statechecktime { get; set; }
        public string cloud { get; set; }
        public int neventsused { get; set; }
        public string destination { get; set; }
        public string state { get; set; }
        public string storagetoken { get; set; }
        public int? provenanceid { get; set; }
        public int jeditaskid { get; set; }
        public string lockedby { get; set; }
        public object lockedtime { get; set; }
        public string status { get; set; }
        public int? nfilestobeused { get; set; }
        public object masterid { get; set; }
        public int? templateid { get; set; }
        public int nfiles { get; set; }
        public int nfilesused { get; set; }
        public int neventstobeused { get; set; }
        public string type { get; set; }
        public string streamname { get; set; }
        public string modificationtime { get; set; }
        public int nevents { get; set; }
        public int datasetid { get; set; }
        public string attributes { get; set; }
        public object statecheckexpiration { get; set; }
    }

    /// Results of running (files, pass, fail, etc.).
    public class Dsinfo
    {
        public int nfilesfailed { get; set; }
        public int nfiles { get; set; }
        public int pctfailed { get; set; }
        public int pctfinished { get; set; }
        public int nfilesfinished { get; set; }
    }

    /// <summary>
    /// Task list
    /// </summary>
    public class PandaTask
    {
        public string termcondition { get; set; }
        public string username { get; set; }
        public string statechangetime { get; set; }
        public string ticketid { get; set; }
        public string transuses { get; set; }
        public string site { get; set; }
        public string vo { get; set; }
        public int reqid { get; set; }
        public object frozentime { get; set; }
        public object iointensity { get; set; }
        public int ramcount { get; set; }
        public string taskname { get; set; }
        public int workdiskcount { get; set; }
        public string cloud { get; set; }
        public string workinggroup { get; set; }
        public object failurerate { get; set; }
        public int workqueue_id { get; set; }
        public string prodsourcelabel { get; set; }
        public string walltimeunit { get; set; }
        public string workdiskunit { get; set; }
        public int corecount { get; set; }
        public string oldstatus { get; set; }
        public int basewalltime { get; set; }
        public string cputimeunit { get; set; }
        public string transhome { get; set; }
        public object progress { get; set; }
        public int currentpriority { get; set; }
        public string lockedby { get; set; }
        public string lockedtime { get; set; }
        public string status { get; set; }
        public string campaign { get; set; }
        public string ramunit { get; set; }
        public string ticketsystemtype { get; set; }
        public string processingtype { get; set; }
        public int cpuefficiency { get; set; }
        public int totevrem { get; set; }
        public int? cputime { get; set; }
        public string splitrule { get; set; }
        public int eventservice { get; set; }
        public string errordialog { get; set; }
        public int parent_tid { get; set; }
        public string superstatus { get; set; }
        public string endtime { get; set; }
        public string iointensityunit { get; set; }
        public int walltime { get; set; }
        public List<Dataset> datasets { get; set; }
        public int jeditaskid { get; set; }
        public string outdiskunit { get; set; }
        public int outdiskcount { get; set; }
        public string modificationtime { get; set; }
        public string countrygroup { get; set; }
        public string architecture { get; set; }
        public string starttime { get; set; }
        public string tasktype { get; set; }
        public Dsinfo dsinfo { get; set; }
        public string transpath { get; set; }
        public string creationdate { get; set; }
        public int taskpriority { get; set; }
        public int totev { get; set; }
    }
}
