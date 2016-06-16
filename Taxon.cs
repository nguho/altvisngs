using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace altvisngs
{
    /// <summary> Struct containing a taxon </summary>
    public struct Taxon
    {
        #region Fields
        public string BestClassification;
        public string[] Hierarchy;
        public int BestKnownLevel;
        public bool IsEmpty;
        #endregion

        #region Constructors
        /// <summary> Create a new instance of a taxon </summary>
        /// <param name="hierarchy"></param>
        /// <param name="unknown">For consistency, all taxon names in the hierarchy which are unknown are presumed to be equal to unknown. Empty or null entries will throw an error. All else will be considered valid taxon names.</param>
        public Taxon(string[] hierarchy, string unknown = "unknown")
        {
            Hierarchy = hierarchy;
            BestClassification = string.Empty;
            BestKnownLevel = -1;

            if (this.Hierarchy == null) throw new ArgumentNullException("Null taxon hierarchy.");
            if (this.Hierarchy.Length == 0) throw new ArgumentOutOfRangeException("Empty taxon hierarchy.");
            //determine the best classification
            for (int i = 0; i < this.Hierarchy.Length; i++)
                if (string.IsNullOrEmpty(this.Hierarchy[i])) throw new ArgumentNullException("Null or empty Taxon Name in Hierarchy!");
                else if (this.Hierarchy[i] == unknown)
                {
                    if (i > 0) i--;//back to the previous level
                    BestKnownLevel = i;
                    BestClassification = this.Hierarchy[i];
                    break;
                }
            if (string.IsNullOrEmpty(BestClassification))
            {
                BestClassification = this.Hierarchy[this.Hierarchy.Length - 1];
                BestKnownLevel = this.Hierarchy.Length - 1;
            }
            IsEmpty = false;
        }
        /// <summary> Create a new instance of the empty taxon </summary>
        /// <param name="isEmpty"></param>
        private Taxon(bool isEmpty)
        {
            if (!isEmpty) throw new ArgumentException("Invalid use of the private Taxon constructor");
            BestClassification = string.Empty;
            Hierarchy = new string[] { };
            BestKnownLevel = -1;
            IsEmpty = true;
        }

        #endregion

        #region Properties
        /// <summary>Get an instance of an empty taxon </summary>
        public static Taxon Empty { get { return new Taxon(true); } }

        #endregion

        #region Methods
        /// <summary> Method to assess if this taxon belongs to the passed taxon </summary>
        /// <remarks> Assessment for equality is made at each level within the passed taxon
        /// If the wild card ("*") is at a level in the supertaxon hierarchy, and known taxon at that level will be considered a member (unknowns will not by default) </remarks>
        /// <param name="supertaxon">The superordinate taxon</param>
        /// <param name="unknownTaxon">The string used to indicate the taxon is unknown</param>
        /// <param name="includeunknownsinwild">True if unknown taxons should be included in wild card ("*") entries (default is false)</param>
        /// <returns>True if this taxon is a member of the passed taxon, false if not</returns>
        public bool IsMemberOf(Taxon supertaxon, string unknownTaxon, bool include_unknowns_in_wild = false)
        {
            if (this.IsEmpty || supertaxon.IsEmpty) return false;//empties cannot belong.
            if(this.Hierarchy.Length < supertaxon.Hierarchy.Length) return false;//cannot possibly match.
            for (int i = 0; i < supertaxon.Hierarchy.Length; i++)
                if (supertaxon.Hierarchy[i] == "*")
                {
                    if (this.Hierarchy[i] == unknownTaxon)
                        if (include_unknowns_in_wild) continue;
                        else return false;
                }
                else
                {
                    if (this.Hierarchy[i] != supertaxon.Hierarchy[i])
                        return false;
                }
            return true;//at this point, they all match
        }

        #endregion

        #region Equality
        public override bool Equals(object obj)
        {
            if (!(obj is Taxon)) return false;
            Taxon taxon = (Taxon)obj;
            if (this.IsEmpty && taxon.IsEmpty) return true;
            if (this.IsEmpty && !taxon.IsEmpty) return false;
            if (!this.IsEmpty && taxon.IsEmpty) return false;

            //compare the hierarchy
            if (this.Hierarchy.Length != taxon.Hierarchy.Length) return false;
            for (int i = 0; i < this.Hierarchy.Length; i++)
                if (this.Hierarchy[i] != taxon.Hierarchy[i]) return false;

            return true;
        }
        public static bool operator ==(Taxon A, Taxon B) { return A.Equals(B); }
        public static bool operator !=(Taxon A, Taxon B) { return !A.Equals(B); }
        /// <summary>Return the hashcode for this taxon</summary>
        /// <remarks>Implementation based on: http://stackoverflow.com/a/263416</remarks>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < Hierarchy.Length; i++)
                    hash = hash * 23 + Hierarchy[i].GetHashCode();
                hash = hash * 23 + IsEmpty.GetHashCode();
                return hash;
            }
        }

        #endregion
    }
}
