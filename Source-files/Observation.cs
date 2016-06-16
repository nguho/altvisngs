using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace altvisngs
{
    public struct Observation
    {
        public double RelativeAbundance;
        public int Abundance;

        public Observation(double relativeAbundance, int abundance)
        {
            RelativeAbundance = relativeAbundance;
            Abundance = abundance;
            if (RelativeAbundance < 0d || Abundance < 0) throw new ArgumentOutOfRangeException("The relative abundance and abundance cannot be less than zero.");
        }

        #region Equality
        public override bool Equals(object obj)
        {
            if (!(obj is Observation)) return false;
            Observation obs = (Observation)obj;

            //compare the values
            return (this.RelativeAbundance == obs.RelativeAbundance && this.Abundance == obs.Abundance);
        }
        public static bool operator ==(Observation A, Observation B) { return A.Equals(B); }
        public static bool operator !=(Observation A, Observation B) { return !A.Equals(B); }

        public static Observation operator +(Observation A, Observation B) { return new Observation(A.RelativeAbundance + B.RelativeAbundance, A.Abundance + B.Abundance); }
        public static Observation operator -(Observation A, Observation B) { return new Observation(A.RelativeAbundance - B.RelativeAbundance, A.Abundance - B.Abundance); }
        /// <summary>Return the hashcode for this observation</summary>
        /// <remarks>Implementation based on: http://stackoverflow.com/a/263416</remarks>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + this.RelativeAbundance.GetHashCode();
                hash = hash * 23 + this.Abundance.GetHashCode();
                return hash;
            }
        }
        #endregion
    }
}
