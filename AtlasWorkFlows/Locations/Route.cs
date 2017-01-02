﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Tuple;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Route between IPlaces. A path a file can take to get from point A to point B.
    /// </summary>
    class Route
    {
        public object Name { get; internal set; }

        /// <summary>
        /// A list of steps along the way, starting at the source and ending with the last one.
        /// </summary>
        private List<IPlace> _steps = new List<IPlace>();

        /// <summary>
        /// How many steps is this route?
        /// </summary>
        public int Length { get { return _steps.Count; } }

        /// <summary>
        /// Return the last destination in the route
        /// </summary>
        public IPlace LastPlace { get { return _steps.Last(); } }

        /// <summary>
        /// Returns true if a particualr step is already on this route.
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        public bool Contains (IPlace step)
        {
            return _steps.Contains(step);
        }

        /// <summary>
        /// Create a new route
        /// </summary>
        /// <param name="source">The location of the item</param>
        public Route(IPlace source)
        {
            _steps.Add(source);
        }

        /// <summary>
        /// Build a route from the old one, but add a new step on.
        /// </summary>
        /// <param name="oldRoute"></param>
        /// <param name="nextStep"></param>
        public Route(Route oldRoute, IPlace nextStep)
        {
            _steps = new List<IPlace>(oldRoute._steps);
            _steps.Add(nextStep);
        }

        /// <summary>
        /// Add a stop along the way. The first stop added is the source, the last the final desitation that should
        /// give us a location.
        /// </summary>
        /// <param name="location"></param>
        public void Add(IPlace location)
        {
            _steps.Add(location);
        }

        /// <summary>
        /// Process files, and return the locations.
        /// </summary>
        /// <param name="datasetURIs"></param>
        /// <returns></returns>
        internal IEnumerable<Uri> ProcessFiles(IEnumerable<Uri> datasetURIs)
        {
            // Make sure objet state is sane
            if (_steps.Count == 0)
            {
                throw new InvalidOperationException("Route created with zero steps!");
            }

            // We are going to do a simple copy of each one to the next one.
            var stepsNoLastDes = _steps.Concat(new IPlace[] { null });
            var stepsNoFirstSource = new IPlace[] { null }.Concat(_steps);

            // Track the URI's as we go
            var uris = datasetURIs.ToArray();

            foreach (var pairing in stepsNoFirstSource.Zip(stepsNoLastDes, (f, l) => Create(f, l)))
            {
                if (pairing.Item1 != null && pairing.Item2 != null)
                {
                    if (pairing.Item1.CanSourceCopy(pairing.Item2))
                    {
                        pairing.Item1.Copy(pairing.Item2, uris);
                    }
                    else
                    {
                        pairing.Item2.Copy(pairing.Item1, uris);
                    }
                }
                else if (pairing.Item2 == null)
                {
                    // Last item in the guy
                    return pairing.Item1.GetLocalFileLocations(uris);
                }
            }

            // Code should never reach this point.
            throw new InvalidOperationException();
        }
    }
}
