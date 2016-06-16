using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace altvisngs
{
    /// <summary> Abstract class containing the methods for data handling and manipulation </summary>
    abstract class altvisngs_data
    {
        #region Keys
        /// <summary> Returns all of the IDs in the first line of the key file. </summary>
        /// <param name="filepath_key"></param>
        /// <returns></returns>
        public static string[] AttributesInKey(string filepath_key)
        {
            string[] parsed = null;
            using (StreamReader sr = new StreamReader(filepath_key))
            {
                string line = sr.ReadLine();
                parsed = line.Split(new char[] { ',' }, StringSplitOptions.None);
            }
            return parsed;
        }

        #endregion

        #region Open
        /// <summary> Returns an array of samples satisfying the passed criteria found in the directory of the keyFilePath</summary>
        /// <param name="keyFilePath"></param>
        /// <param name="ID"></param>
        /// <param name="satisfies"></param>
        /// <returns></returns>
        public static Sample[] OpenSamples(string keyFilePath, string ID = "ID", Func<Sample, bool> criteria = null)
        {
            List<string> sampleIDs = new List<string>();
            using (StreamReader sr = new StreamReader(keyFilePath))
            {
                string line = sr.ReadLine();
                string[] parsed = line.Split(new char[] { ',' }, StringSplitOptions.None);
                int ididx = -1;
                for (int i = 0; i < parsed.Length; i++)
                    if (parsed[i] == ID)
                    {
                        ididx = i;
                        break;
                    }
                if (ididx == -1) throw new FormatException("Sample ID column not found in `" + keyFilePath + "'; expecting a heading of `" + ID + "'.");
                while (!sr.EndOfStream)//read each of the following lines, adding if it satisfies
                {
                    line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;
                    parsed = line.Split(new char[] { ',' }, StringSplitOptions.None);
                    sampleIDs.Add(parsed[ididx]);
                }
            }
            List<Sample> samples = new List<Sample>();
            for (int i = 0; i < sampleIDs.Count; i++)
            {
                Sample test = Sample.ReadFromFile(Path.GetDirectoryName(keyFilePath) + "\\" + sampleIDs[i] + "_ngsdata.xml");
                if (criteria != null)
                    if (!criteria(test))
                        continue;
                samples.Add(test);
            }
            return samples.ToArray();
        }

        #endregion

        #region Refine
        /// <summary> Returns the unsorted array of samples satisfying the attribute test </summary>
        /// <param name="samples"></param>
        /// <param name="satisfies">Method to assess a sample</param>
        /// <returns></returns>
        public static Sample[] SamplesSatisfying_Unsorted(Sample[] samples, Func<Sample, bool> criteria) { return Array.FindAll(samples, (s) => criteria(s)); }
        public static Sample[][] GroupedSamplesSatisfying_Unsorted(Sample[] avail_samples, params SampleKeyFilter[] filters)
        {
            if (filters == null) return new Sample[0][];
            Sample[][] rslt = new Sample[filters.Length][];
            for (int i = 0; i < filters.Length; i++)
            {
                SampleKeyFilter filt = filters[i];
                rslt[i] = SamplesSatisfying_Unsorted(avail_samples, (s) => (s.GetAttr(filt.PropertyName) == filt.PropertyValue));
            }
            return rslt;
        }
        
        #endregion

        #region Sort
        /// <summary> Sort TaxonObservations based on the Taxon (hierarchically, then alphabetically) </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public static int SortHeadObservation_Taxon(GroupedObservations A, GroupedObservations B, params string[] unknowns)
        {
            if (!A.IsOverridden && !B.IsOverridden)//then based on the taxon
                return SortTaxon(A.Taxon, B.Taxon, unknowns);
            //one or both overridden by string...
            //always put the taxon FIRST
            if (B.IsOverridden) return -1;
            if (A.IsOverridden) return 1;//
            return string.Compare(A.TaxonOverride, B.TaxonOverride);
        }
        /// <summary> Method to sort two taxa (hierarchically, then alphabetically) </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="unknowns"></param>
        /// <returns></returns>
        public static int SortTaxon(Taxon A, Taxon B, params string[] unknowns)
        {
            int alphasort = 0;
            bool isatlast = false;
            int atidx = 0;
            for (int i = 0; i < A.Hierarchy.Length; i++)
                if (A.Hierarchy[i] != B.Hierarchy[i])
                {
                    if (string.IsNullOrEmpty(A.Hierarchy[i]) && string.IsNullOrEmpty(B.Hierarchy[i]))
                        alphasort = 0;//same
                    else if (!string.IsNullOrEmpty(A.Hierarchy[i]))
                        alphasort = A.Hierarchy[i].CompareTo(B.Hierarchy[i]);
                    else//this is null, other is not => invert.
                        alphasort = -B.Hierarchy[i].CompareTo(A.Hierarchy[i]);
                    isatlast = (i == A.Hierarchy.Length - 1);
                    atidx = i;
                    break;
                }

            //assess unid and prop IF at the first level OR at the last level
            //always put the unidentified first
            if ((atidx == 0 || atidx == A.Hierarchy.Length - 1) && unknowns != null)
            {
                if (unknowns.Contains(A.Hierarchy[atidx]) && !unknowns.Contains(B.Hierarchy[atidx])) return -1;
                if (!unknowns.Contains(A.Hierarchy[atidx]) && unknowns.Contains(B.Hierarchy[atidx])) return 1;
            }
            return alphasort;
        }

        #endregion

        #region Taxon
        /// <summary> Method to get all of the taxons in a sample (all levels, includes zeros) </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static Taxon[] GetAllTaxons(Sample sample)
        {
            Taxon[] taxons = new Taxon[sample.TaxonObservationsCount];
            for (int i = 0; i < sample.TaxonObservationsCount; i++)
                taxons[i] = sample.TaxonObservations[i].Taxon;
            return taxons;
        }
        /// <summary> Method to get all of the nonzero taxons in a sample (all levels) </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static Taxon[] GetAllTaxons_NonZero(Sample sample)
        {
            List<Taxon> taxons = new List<Taxon>();
            for (int i = 0; i < sample.TaxonObservations.Length; i++)
                if (sample.TaxonObservations[i].Observation.Abundance != 0)
                    taxons.Add(sample.TaxonObservations[i].Taxon);
            return taxons.ToArray();
        }

        /// <summary> Method to get all of the taxons which are defined at the passed level</summary>
        /// <param name="availtaxons">Array of available taxons (presumed defined to higher levels than that desired)</param>
        /// <param name="level">The desired level of the taxons</param>
        /// <param name="unknown">The string used to indicate an unknown taxon name (default = "unknown")</param>
        /// <returns></returns>
        public static Taxon[] GetTaxonsAtLevel_Unsorted(Taxon[] availtaxons, int level, string unknown = "unknown")
        {
            List<Taxon> taxons = new List<Taxon>();
            for (int i = 0; i < availtaxons.Length; i++)
            {
                string[] taxonhier = new string[level + 1];
                for (int j = 0; j < taxonhier.Length; j++)
                    taxonhier[j] = availtaxons[i].Hierarchy[j];
                Taxon taxon = new Taxon(taxonhier,unknown);
                if (!taxons.Contains(taxon))
                    taxons.Add(taxon);
            }
            return taxons.ToArray();
        }

        /// <summary> Method to consolidate the available taxons based on the passed approach </summary>
        /// <param name="availTaxons">UNIQUE array of taxons all defined to the same level (no duplicates assumed)</param>
        /// <param name="consolType"></param>
        /// <returns></returns>
        public static Taxon[] ConsolidateUnknownTaxa(Taxon[] availTaxons, UnknownTaxonMatchType consolType, string unknown="unknown")
        {
            if (availTaxons == null) return null;
            if (availTaxons.Length == 0) return new Taxon[] { };
            List<Taxon> consoltaxons= new List<Taxon>();
            int level = -1;
            switch (consolType)
            {
                case(UnknownTaxonMatchType.NoLumping):
                    return availTaxons;
                case(UnknownTaxonMatchType.AllInOne)://All unknowns are represented by a "taxon" with hierarchy {"*", "*", ...,"*", "unknown"} w/ unknown at the level of availTaxons
                    for (int i = 0; i < availTaxons.Length; i++)
                    {
                        if (level == -1) level = availTaxons[i].Hierarchy.Length -1;
                        if (level != availTaxons[i].Hierarchy.Length -1) throw new ArgumentException(@"Consolidation of unknown taxa assumes that ALL taxons are ""defined"" to the same taxonomic level");
                        if (availTaxons[i].Hierarchy[level] != unknown)
                            consoltaxons.Add(availTaxons[i]);
                    }
                    if (availTaxons.Length > consoltaxons.Count)//some unkowns
                    {
                        string[] hierarch = new string[level + 1];
                        for (int i = 0; i < level - 1; i++)
                            hierarch[i] = "*";
                        hierarch[hierarch.Length - 1] = unknown;
                        consoltaxons.Add(new Taxon(hierarch, unknown));
                    }
                    return consoltaxons.ToArray();
                case(UnknownTaxonMatchType.ByLastDefinedTaxaLevel)://Unknowns are grouped by the last taxonomic level at which they are defined.
                    bool[] unknownatlevel = null;
                    for (int i = 0; i < availTaxons.Length; i++)
                    {
                        if (level == -1)
                        {
                            level = availTaxons[i].Hierarchy.Length - 1;
                            unknownatlevel = new bool[level + 1];//last level at which it is known. Extra level for not known.
                        }
                        if (level != availTaxons[i].Hierarchy.Length - 1) throw new ArgumentException(@"Consolidation of unknown taxa assumes that ALL taxons are ""defined"" to the same taxonomic level");
                        if (availTaxons[i].Hierarchy[level] != unknown)
                            consoltaxons.Add(availTaxons[i]);
                        else//unknown...fill lastknownatlevel array
                            for(int j=0;j<level+1;j++)
                                if(availTaxons[i].Hierarchy[j] == unknown)
                                {
                                    unknownatlevel[j] = true;
                                    break;
                                }
                    }
                    for (int i = 0; i < unknownatlevel.Length; i++)
                    {
                        if (!unknownatlevel[i]) continue;//don't add an unknown for this level
                        string[] hierarch = new string[level+1];//i + 1];
                        for (int j = 0; j < i; j++)
                            hierarch[j] = "*";
                        for(int j=i; j<level+1; j++)
                            hierarch[j] = unknown;
                        consoltaxons.Add(new Taxon(hierarch, unknown));
                    }
                    return consoltaxons.ToArray();
                default:
                    throw new NotImplementedException("UnknownTaxonMatchType `" + consolType.ToString() + "' not implemented.");
            }
        }
        #endregion

        #region TaxonObservations
        /// <summary> Method to collapse the taxon observations to the passed level </summary>
        /// <remarks> Go through and determine the total associated with each taxon at the level passed</remarks>
        /// <param name="sample"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static TaxonObservation[] CollapseHierarchy_Unsorted(Sample sample, int level, string unknown = "unknown")
        {
            Taxon[] taxonsatlvl = GetTaxonsAtLevel_Unsorted(GetAllTaxons(sample), level, unknown);
            TaxonObservation[] rslt = new TaxonObservation[taxonsatlvl.Length];
            for (int i = 0; i < rslt.Length; i++)
            {
                Observation collapsed = new Observation(0d, 0);
                for (int j = 0; j < sample.TaxonObservationsCount; j++)
                    if (sample.TaxonObservations[j].Taxon.IsMemberOf(taxonsatlvl[i], unknown))
                        collapsed += sample.TaxonObservations[j].Observation;
                rslt[i] = new TaxonObservation(taxonsatlvl[i], collapsed);
            }
            return rslt;
        }
        /// <summary> Method to build a master taxonobservation matrix for the samples </summary>
        /// <remarks> First dimension is the taxon, the observations are the second dimension and ordered as in samples</remarks>
        /// <param name="samples"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static GroupedObservations[] BuildTaxonObservations(Sample[] samples, int level, string unknown = "unknown")
        {
            Dictionary<Taxon, Observation[]> rslts = new Dictionary<Taxon,Observation[]>();
            for (int i = 0; i < samples.Length; i++)
            {
                TaxonObservation[] collapsed = CollapseHierarchy_Unsorted(samples[i], level, unknown);
                for (int j = 0; j < collapsed.Length; j++)
                {
                    if(!rslts.ContainsKey(collapsed[j].Taxon))
                        rslts.Add(collapsed[j].Taxon, new Observation[samples.Length]);
                    rslts[collapsed[j].Taxon][i] = collapsed[j].Observation;
                }
            }
            //build array
            List<GroupedObservations> rslt = new List<GroupedObservations>();
            foreach (KeyValuePair<Taxon, Observation[]> kvp in rslts)
                rslt.Add(new GroupedObservations(kvp.Key, kvp.Value));
            return rslt.ToArray();
        }

        /// <summary>Method to consolidate a GroupedObservations wherein ALL of the observations that are less than the passed minorProportion are placed into a single GroupedObservations at the end </summary>
        /// <param name="taxonObs"></param>
        /// <param name="unknown"></param>
        /// <returns></returns>
        public static GroupedObservations[] ConsolidateMinorTaxa(GroupedObservations[] taxonObs, double minorProportion, string unknown = "unknown")
        {
            if (taxonObs == null) return null;
            Observation[] minorobs = new Observation[taxonObs[0].Observations.Length];//same length
            int minorcount = 0;
            List<GroupedObservations> rslt = new List<GroupedObservations>();
            for (int i = 0; i < taxonObs.Length; i++)
            {
                bool isminor = true;
                for (int j = 0; j < taxonObs[i].Observations.Length; j++)
                    if (taxonObs[i].Observations[j].RelativeAbundance > minorProportion)
                    {
                        isminor = false;
                        break;
                    }
                if (!isminor)
                    rslt.Add(taxonObs[i]);
                else
                {
                    minorcount++;
                    for (int j = 0; j < taxonObs[i].Observations.Length; j++)
                        minorobs[j] += taxonObs[i].Observations[j];
                }
            }
            rslt.Add(new GroupedObservations(Taxon.Empty, minorobs, "Minor taxa (" + minorcount.ToString() + ")"));
            return rslt.ToArray();
        }

        #endregion
    }
}
