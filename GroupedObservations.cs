using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace altvisngs
{
    public struct GroupedObservations
    {
        #region Fields
        public string TaxonOverride;
        public Taxon Taxon;
        public Observation[] Observations;

        #endregion

        #region Constructors
        public GroupedObservations(Taxon heading, Observation[] observations, string headOverride = null)
        {
            Taxon = heading;
            Observations = observations;
            TaxonOverride = headOverride;
            if (IsOverridden) Taxon = Taxon.Empty;
        }

        #endregion

        #region Properties
        public bool IsOverridden { get { return !string.IsNullOrEmpty(TaxonOverride) || Taxon.IsEmpty; } }

        #endregion

    }
}
