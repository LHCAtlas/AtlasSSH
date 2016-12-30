using AtlasWorkFlows;
using PSAtlasDatasetCommands.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var listener = new PSListener(this);
            Trace.Listeners.Add(listener);
            try
            {
                var list = GRIDDatasetLocator.GetActiveLocations();
                foreach (var l in list)
                {
                    using (var pl = listener.PauseListening())
                    {
                        WriteObject(l.Name);
                    }
                }
            } finally
            {
                Trace.Listeners.Remove(listener);
            }
        }
    }
}
