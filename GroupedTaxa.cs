using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace altvisngs
{
    class GroupedTaxa
    {
        public string DisplayedName;
        public double TotalProportion;
        public TaxonObservation[] ContainedTaxa;
        public TaxaClassLevel refer;

        public GroupedTaxa(string displayedName, double ttlprop, TaxonObservation[] contained, TaxaClassLevel inref = null)
        {
            DisplayedName = displayedName;
            TotalProportion = ttlprop;
            ContainedTaxa = contained;
            refer = inref;
        }
    }
}
