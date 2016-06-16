using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace altvisngs
{
    /* Taxanomic Level Summary Tables
     * 24 APR 2016
     * Nick Guho 
     * 
     */
    /// <summary>Abstract class containing methods to output the csv files for a collection of samples at a specific taxonomic rank (i.e., the expected output from sequencing pipelines) </summary>
    /// <remarks>The methods herein are meant to output the EXPECTED results from dbcAmplicons (i.e., to translate what is actually output to what is wanted)</remarks>
    abstract class altvisngs_taxaranktbl
    {
        public static object[,] TaxaRankTable_Abundance(string key_filepath, string output_filepath, Sample[] samples, int level, string[] taxaranks, string[] addtlAttrs = null, Type[] addtlAttrsTypes = null, string sep = ",", string ID = "ID", string unknown = "unknown", params string[] unknowns)
        {
            return TaxaRankTable_General(key_filepath, output_filepath, samples, level, taxaranks, false, addtlAttrs, addtlAttrsTypes, sep, ID, unknown, unknowns);
        }
        public static object[,] TaxaRankTable_RelativeAbundance(string key_filepath, string output_filepath, Sample[] samples, int level, string[] taxaranks, string[] addtlAttrs = null, Type[] addtlAttrsTypes=null, string sep = ",", string ID = "ID", string unknown = "unknown", params string[] unknowns)
        {
            return TaxaRankTable_General(key_filepath, output_filepath, samples, level, taxaranks, true, addtlAttrs, addtlAttrsTypes, sep, ID, unknown, unknowns);
        }
        
        /// <summary> General method to create a table at the specified taxa level </summary>
        /// <param name="key_filepath">The path to the key file the directory of which contains the sample files to be assessed</param>
        /// <param name="output_filepath">The path to the .txt file resulting from this run</param>
        /// <param name="samples">The samples</param>
        /// <param name="level">The taxonomic level to which the hierarchy should be collapsed</param>
        /// <param name="taxaranks">String array yielding the names for the ranks up to level (e.g., "domain", "phylum", "class", "order")</param>
        /// <param name="relabund">true if the table should be Relative Abundance (proportion); false for Abundance</param>
        /// <param name="addtlAttrs">The names of the additional attributes to be included in the outputted data (not in the .txt file) </param>
        /// <param name="addtlAttrsTypes">The types of the additional attributes to be included in the outputted (not in the .txt file; used for parsing to the appropriate type)</param>
        /// <param name="sep">The separator in the .txt file</param>
        /// <param name="ID">The attribute name for the sample ID used in the sequencing (i.e., the name of the file)</param>
        /// <param name="unknown">The string assigned to unknown taxons for consistency</param>
        /// <param name="unknowns">strings considered to be unknown</param>
        /// <returns>2D array of objects which may be written to an .xlsx file</returns>
        private static object[,] TaxaRankTable_General(string key_filepath, string output_filepath,
            Sample[] samples,
            int level, string[] taxaranks,
            bool relabund, string[] addtlAttrs = null, Type[] addtlAttrsTypes=null, string sep = ",",
            string ID = "ID",
            string unknown = "unknown", params string[] unknowns)
        {
            Console.WriteLine("Creating TaxaRankTable `" + Path.GetFileName(output_filepath) + "'.");
            if (level != taxaranks.Length - 1) throw new ArgumentOutOfRangeException("Taxa level/heading mismatch");
            //Sample[] samples = altvisngs_data.OpenSamples(key_filepath, ID, criteria);
            List<GroupedObservations> rslts = altvisngs_data.BuildTaxonObservations(samples, level, unknown).ToList();
            rslts.Sort((A, B) => altvisngs_data.SortHeadObservation_Taxon(A, B, unknown));
            Console.WriteLine("Writing `" + Path.GetFileName(output_filepath) + "'.");
            int addl = 1;
            if (addtlAttrs != null)
                addl += addtlAttrs.Length;
            object[,] data = new object[rslts.Count + addl, taxaranks.Length + samples.Length];
            using (StreamWriter sw = new StreamWriter(output_filepath))
            {
                sw.WriteLine(string.Join(sep, taxaranks) + sep + string.Join(sep, samples.Select((s) => (s.GetAttr(ID)))));

                for(int i = 0; i < samples.Length; i++)
                    for(int j = 0; j < addl - 1; j++)
                    {
                        var parse = addtlAttrsTypes[j].GetMethod("Parse", new [] {typeof(string)});
                        if (parse != null)
                            try { data[j, i + taxaranks.Length] = parse.Invoke(null, new object[] { samples[i].GetAttr(addtlAttrs[j]) }); }
                            catch { data[j, i + taxaranks.Length] = samples[i].GetAttr(addtlAttrs[j]); }
                        else
                            data[j, i + taxaranks.Length] = samples[i].GetAttr(addtlAttrs[j]);
                    }
                for (int i = 0; i < taxaranks.Length; i++)
                    data[addl - 1, i] = taxaranks[i];
                for (int i = 0; i < samples.Length; i++)
                    data[addl - 1, i + taxaranks.Length] = samples[i].GetAttr(ID);


                for (int i = 0; i < rslts.Count; i++)
                {
                    sw.WriteLine(string.Join(sep, rslts[i].Taxon.Hierarchy) + sep + string.Join(sep, rslts[i].Observations.Select((o) => ((relabund) ? (o.RelativeAbundance.ToString("0.###############")) : (o.Abundance.ToString())))));
                    for (int j = 0; j < taxaranks.Length; j++)
                        data[i + addl, j] = rslts[i].Taxon.Hierarchy[j];
                    for (int j = 0; j < samples.Length; j++)
                        data[i + addl, j + taxaranks.Length] = ((relabund) ? (rslts[i].Observations[j].RelativeAbundance) : (rslts[i].Observations[j].Abundance));
                }
            }
            return data;
        }
    }
}
