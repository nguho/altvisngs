using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace altvisngs
{
    /// <summary> Struct serving as both the structure for an individual phylotype in a sample (for which Observations.Length=1) AND the aggregation of phylotypes from multiple samples (for which Observations.Length>1)</summary>
    public struct TaxonObservation
    {
        public Taxon Taxon;
        public Observation Observation;

        public TaxonObservation(Taxon taxon, Observation observation)
        {
            Taxon = taxon;
            Observation = observation;
        }

        #region Equality
        public override bool Equals(object obj)
        {
            if (!(obj is TaxonObservation)) return false;
            TaxonObservation taxonObs = (TaxonObservation)obj;

            //compare the hierarchy
            if (this.Taxon != taxonObs.Taxon || this.Observation != taxonObs.Observation) return false;
            return true;
        }
        public static bool operator ==(TaxonObservation A, TaxonObservation B) { return A.Equals(B); }
        public static bool operator !=(TaxonObservation A, TaxonObservation B) { return !A.Equals(B); }
        /// <summary>Return the hashcode for this TaxonObservations</summary>
        /// <remarks>Implementation based on: http://stackoverflow.com/a/263416</remarks>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + this.Taxon.GetHashCode();
                hash = hash * 23 + this.Observation.GetHashCode();
                return hash;
            }
        }

        #endregion

        #region Sorting
        /// <summary> Comparator used in sorting </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public static int Compare_TaxonomicBar(TaxonObservation A, TaxonObservation B, string unknown="unknown", double minor_cutoff=0d)
        {
            //<0 A preceeds B
            // 0 A is at the same position as B
            //>0 A is after B
            //want the undefined to be at the top, followed by the alphabetically-sorted minor, then the alphabetically-sorted majors
            //find the index at which they differ
            bool an_unknown = false;
            int alpha_sort = 0;//if the same throughout, then this will be it
            int diff_at = -1;
            for (int i = 0; i < A.Taxon.Hierarchy.Length; i++)
                if (A.Taxon.Hierarchy[i] != B.Taxon.Hierarchy[i])
                {
                    if (!string.IsNullOrEmpty(A.Taxon.Hierarchy[i]) && A.Taxon.Hierarchy[i] != unknown && !string.IsNullOrEmpty(B.Taxon.Hierarchy[i]) && B.Taxon.Hierarchy[i] != unknown)//both not unknown
                    {
                        alpha_sort = A.Taxon.Hierarchy[i].CompareTo(B.Taxon.Hierarchy[i]);//alphabetically compare
                        diff_at = i;
                        break;
                    }
                    else if (string.IsNullOrEmpty(A.Taxon.Hierarchy[i]) || A.Taxon.Hierarchy[i] == unknown) //one or both is unknown; 
                        //A is unknown
                        if (string.IsNullOrEmpty(B.Taxon.Hierarchy[i]) || B.Taxon.Hierarchy[i] == unknown)//B is also unkown
                            alpha_sort = 0;//same
                        else//B is known
                            alpha_sort = -1;//A before B
                    else//B is unknown; A is known
                        alpha_sort = 1;//B before A
                    diff_at = i;
                    an_unknown = true;
                    break;
                }
            if (diff_at == 0 || diff_at == A.Taxon.Hierarchy.Length - 1)//!an_unknown)//check if minor comes into play
            {
                //if ((string.IsNullOrEmpty(A.Taxon.Hierarchy[diff_at]) || A.Taxon.Hierarchy[diff_at] == unknown) && (!string.IsNullOrEmpty(B.Taxon.Hierarchy[diff_at]) && B.Taxon.Hierarchy[diff_at] != unknown)) return -1;
                //if ((!string.IsNullOrEmpty(A.Taxon.Hierarchy[diff_at]) && A.Taxon.Hierarchy[diff_at] != unknown) && (string.IsNullOrEmpty(B.Taxon.Hierarchy[diff_at]) || B.Taxon.Hierarchy[diff_at] == unknown)) return 1;
                if (A.Observation.RelativeAbundance < minor_cutoff && B.Observation.RelativeAbundance >= minor_cutoff) return -1;//if (this.IsMinor && !other.IsMinor) return -1;
                if (B.Observation.RelativeAbundance < minor_cutoff && A.Observation.RelativeAbundance >= minor_cutoff) return 1; //if (other.IsMinor && !this.IsMinor) return 1;
            }
            return alpha_sort;
            //int alphasort = 0;
            //bool isatlast = false;
            //int atidx = 0;
            //for (int i = 0; i < A.Taxon.Hierarchy.Length; i++)
            //    if (A.Taxon.Hierarchy[i] != B.Taxon.Hierarchy[i])
            //    {
            //        if (string.IsNullOrEmpty(A.Taxon.Hierarchy[i]) && string.IsNullOrEmpty(B.Taxon.Hierarchy[i]))
            //            alphasort = 0;//same
            //        else if (!string.IsNullOrEmpty(A.Taxon.Hierarchy[i]))
            //            alphasort = A.Taxon.Hierarchy[i].CompareTo(B.Taxon.Hierarchy[i]);
            //        else//this is null, other is not => invert.
            //            alphasort = -B.Taxon.Hierarchy[i].CompareTo(A.Taxon.Hierarchy[i]);
            //        isatlast = (i == A.Taxon.Hierarchy.Length - 1);
            //        atidx = i;
            //        break;
            //    }

            ////assess unid and prop IF at the first level OR at the last level
            //if (atidx == 0 || atidx == A.Taxon.Hierarchy.Length - 1)
            //{
            //    //always put the unidentified first
            //    if ((string.IsNullOrEmpty(A.Taxon.Hierarchy[atidx]) || A.Taxon.Hierarchy[atidx] == unknown) && (!string.IsNullOrEmpty(B.Taxon.Hierarchy[atidx]) && B.Taxon.Hierarchy[atidx] != unknown)) return -1;
            //    if ((!string.IsNullOrEmpty(A.Taxon.Hierarchy[atidx]) && A.Taxon.Hierarchy[atidx] != unknown) && (string.IsNullOrEmpty(B.Taxon.Hierarchy[atidx]) || B.Taxon.Hierarchy[atidx] == unknown)) return 1;
            //    if (A.Observation.RelativeAbundance < minor_cutoff && B.Observation.RelativeAbundance >= minor_cutoff) return -1;//if (this.IsMinor && !other.IsMinor) return -1;
            //    if (B.Observation.RelativeAbundance < minor_cutoff && A.Observation.RelativeAbundance >= minor_cutoff) return 1; //if (other.IsMinor && !this.IsMinor) return 1;
            //}
            //return alphasort;
        }

        #endregion
    }
}
