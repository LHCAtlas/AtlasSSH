using AtlasWorkFlows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands
{
    /// <summary>
    /// Return the list of locations that are active right now where a GRID dataset
    /// can be downloaded to.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "GRIDDataLocations")]
    public class GetGRIDDataLocations : PSCmdlet
    {
        /// <summary>
        /// Get the list and write it out as objects.
        /// </summary>
        protected override void ProcessRecord()
        {
            var list = GRIDDatasetLocator.GetActiveLocations();
            foreach (var l in list)
            {
                WriteObject(l.Name);
            }
        }
    }
}
