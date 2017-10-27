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
    /// Add for a location argument, makes sure that only the available locations can
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
            if (!_validLocations.Value.Contains(s))
            {
                var err = _validLocations.Value.Aggregate(new StringBuilder(), (bld, loc) => bld.Append($" {loc}"));
                throw new ValidationMetadataException($"Illegal value for Location ({s}) - possible values:{err.ToString()}");
            }
        }

        public IList<string> ValidValues { get { return _validLocations.Value; } }

        /// <summary>
        /// The list of valid location.
        /// </summary>
        /// <remarks>
        /// Lazy so we determine it once. Also, can't put it in the ctor as that will cause
        /// it to be created on a simple command completion operation in powershell!
        /// Note: If the computer changes location after this has been initialized, it won't
        ///       get updated!
        /// </remarks>
        private Lazy<string[]> _validLocations = new Lazy<string[]>(() => DatasetManager.ValidLocations);
    }
}
