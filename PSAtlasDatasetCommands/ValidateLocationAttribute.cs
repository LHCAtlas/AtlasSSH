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
    /// Add for a location argument, makes sure that only the availible locations can
    /// be x-checked.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    sealed class ValidateLocationAttribute : ValidateArgumentsAttribute
    {
        /// <summary>
        /// Setup defaults, prime with legal items.
        /// </summary>
        public ValidateLocationAttribute()
        {
            _validLocations = GRIDDatasetLocator.GetActiveLocations().Select(loc => loc.Name).ToArray();
        }

        /// <summary>
        /// Check against legal values.
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="engineIntrinsics"></param>
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            var s = arguments as string;
            if (s == null)
            {
                throw new ValidationMetadataException("Argument is not a string.");
            }
            if (!_validLocations.Contains(s))
            {
                var err = _validLocations.Aggregate(new StringBuilder(), (bld, loc) => bld.Append($" {loc}"));
                throw new ValidationMetadataException($"Illegal value for Location - possible values:{err.ToString()}");
            }
        }

        public IList<string> ValidValues { get { return _validLocations; } }
        private string[] _validLocations;
    }
}
