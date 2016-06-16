using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace altvisngs
{
    /// <summary>Abstract class containg methods called when the "rarefaction" option is passed to altvisngs </summary>
    /// <remarks>Methods herein are applied to individual samples, after initialization</remarks>
    abstract class altvisngs_rarefaction
    {
        /// <summary>Method to build the rarefaction curve and write data to _rarecurve.csv </summary>
        /// <param name="sample"></param>
        /// <param name="n_rare_curve_segs"></param>
        public static void RarefactionCurve(
            string filepath_output,
            Sample sample,
            int n_rare_curve_segs)
        {
            //first, build the rarefaction curve
            Console.WriteLine("Building rarefaction curve...");
            long N = 0;//total number of reads for the sample
            List<long> xs = new List<long>();//list containing the counts of each taxanomic classification; only non-zero entries
            for (int i = 0; i < sample.TaxonObservations.Length; i++)
            {
                if (sample.TaxonObservations[i].Observation.Abundance == 0) continue;
                N += sample.TaxonObservations[i].Observation.Abundance;
                xs.Add(sample.TaxonObservations[i].Observation.Abundance);
            }

            long n;
            double multby = ((double)N) / ((double)n_rare_curve_segs);//save some time
            Tuple<long, double>[] raredata = new Tuple<long, double>[n_rare_curve_segs + 1];
            for (int i = 0; i <= n_rare_curve_segs; i++)
            {
                if (i == 0) n = 0;
                else if (i == n_rare_curve_segs) n = N;
                else n = (int)(((double)i) * multby);

                raredata[i] = new Tuple<long, double>(n, ESn(N, xs, n));
            }

            Console.WriteLine("Saving rarefaction curve to `" + Path.GetFileName(filepath_output) + "'");
            using (StreamWriter sw = new StreamWriter(filepath_output))//this is the output for the rarefaction curve
            {
                sw.WriteLine("n,Es");
                for (int i = 0; i < raredata.Length; i++)
                    sw.WriteLine(raredata[i].Item1.ToString() + "," + raredata[i].Item2.ToString());
            }
        }

        /// <summary> Get the expected number of taxa at n reads for the passed taxa vector with maximal N reads </summary>
        /// <param name="N">The maximum number of reads</param>
        /// <param name="xs">List of the reads for each taxonomic classification (assumed only non-zero entries)</param>
        /// <param name="n">The number of reads (less than or equal to N) at which the number of taxa is to be estimated</param>
        /// <returns></returns>
        private static double ESn(long N, List<long> xs, long n)
        {
            double rslt = 0d;
            for (int i = 0; i < xs.Count; i++) rslt += 1d - qsi(N, xs[i], n);
            return rslt;
        }
        /// <summary> Function to evaluate q_si in the rarefaction analysis </summary>
        /// <remarks> By inspection, result will be between 0 and 1 </remarks>
        /// <param name="N">The maximum number of reads</param>
        /// <param name="x"></param>
        /// <param name="n">The number of reads (less than or equal to N) at which the number of taxa is to be estimated</param>
        /// <returns></returns>
        private static double qsi(long N, long x, long n)
        {
            if (N - x - n < 0) return 0;
            //general approach: eliminate common terms from the factorials to avoid overflows
            double rslt = 1d;
            if (n < x)//then pair (N-n)! with N! and (N-x)! with (N-x-n)! (fewest iterations)
            {
                ////(N-n)!/N! => 1/(N*(N-1)*(N-2)*...*(N-n+1))
                //double[] denom = new double[n];//n is the number of factors to be multiplied in the denominator (n=4 yields 1/(N*(N-1)*(N-2)*(N-3)); n=0 yields N!/N!=1)
                //for (int i = 0; i < n; i++) denom[i] = N - i;
                ////note that it may be zero (gives unity)
                ////(N-x)!/(N-x-n)! => (N-x)*(N-x-1)*(N-x-2)*...*(N-x-n)!
                //double[] numer = new double[n];//n is the number of factors to be multiplied in the numerator (n=4 yields (N-x)*(N-x-1)*(N-x-2)*(N-x-3); n=0 yields (N-x)!/(N-x)!=1)
                //for (int i = 0; i < n; i++) numer[i] = N - x - i;
                for (long i = 0; i < n; i++) rslt *= ((double)((N - x - i))) / ((double)((N - i)));
            }
            else//pair (N-x)! with N! and (N-n)! with (N-x-n)! (fewest iterations)
            {
                ////(N-x)!/N! => 1/(N*(N-1)*(N-2)*...*(N-x+1))
                //double[] denom = new double[x];//x is the number of factors to be multiplied in the denominator (x=4 yields 1/(N*(N-1)*(N-2)*(N-3)); x=0 yields N!/N!=1)
                //for (int i = 0; i < x; i++) denom[i] = N - i;
                ////note that it may be zero (gives unity)
                ////(N-n)!/(N-x-n)! => (N-x)*(N-x-1)*(N-x-2)*...*(N-x-n)!
                //double[] numer = new double[n];//n is the number of factors to be multiplied in the numerator (n=4 yields (N-x)*(N-x-1)*(N-x-2)*(N-x-3); n=0 yields (N-x)!/(N-x)!=1)
                //for (int i = 0; i < n; i++) numer[i] = N - x - i;
                for (long i = 0; i < x; i++) rslt *= ((double)((N - n - i))) / ((double)((N - i)));
            }
            return rslt;
        }
    }
}
