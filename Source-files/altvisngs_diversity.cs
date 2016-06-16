using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace altvisngs
{
    abstract class altvisngs_diversity
    {
        /// <summary> Method to output a diversity summary table </summary>
        /// <param name="output_directory"></param>
        /// <param name="samples"></param>
        /// <param name="Func"></param>
        public static void DiversityTable(
            string filepath_output,
            string table_mandatory_caption,
            string table_optional_caption,
            string table_label,
            Sample[][] grouped_samples,
            string[] group_headings,
            string[] tabular_column_types,
            string[][] table_heading,
            Func<Sample,string>[] table_columns,
            Dictionary<string,string> table_footnotes,
            string tblcolsep="&",
            string tblnewline=@"\tabularnewline",
            string sisetup_default="table-number-alignment=center,tight-spacing=true,group-minimum-digits=4")
        {
            Console.WriteLine("Building diversity table `" + table_label + "'...");
            int ncol = table_columns.Length;
            if (tabular_column_types.Length != ncol) throw new ArgumentOutOfRangeException("Column number mismatch");

            //build heading
            List<string> heading = new List<string>();
            for (int i = 0; i < table_heading.Length; i++)
                if (table_heading[i].Length != ncol)
                    throw new ArgumentOutOfRangeException("Column number mismatch");
                else
                {
                    string temp = string.Empty;
                    for (int j = 0; j < table_heading[i].Length; j++)
                    {
                        temp += "{" + table_heading[i][j] + "}";
                        if (j == ncol - 1)
                        {
                            temp += tblnewline;
                            heading.Add(temp);
                            temp = string.Empty;
                        }
                        else
                            temp += tblcolsep;
                    }
                }

            //build body
            if (group_headings.Length != grouped_samples.Length) throw new ArgumentOutOfRangeException("Mismatch in grouped samples and headings");
            List<string> body = new List<string>();// string.Empty;
            for (int i = 0; i < group_headings.Length; i++)
            {
                if (group_headings.Length != 1)//more than one...have a single multicolumn to summarize
                {
                    if (i > 0) body.Add(@"\midrule");//\breakablemidrule//don't add a top line for the first...duplicate
                    body.Add(@"\multicolumn{" + ncol.ToString() + "}{c}{" + group_headings[i] + "}" + tblnewline);// +Environment.NewLine;
                    body.Add(@"\midrule");// +Environment.NewLine;
                }
                for (int j = 0; j < grouped_samples[i].Length; j++)
                {
                    string bod = string.Empty;
                    for (int k = 0; k < ncol; k++)
                        bod += table_columns[k](grouped_samples[i][j]) + ((k == ncol - 1) ? (tblnewline) : (tblcolsep));
                    body.Add(bod);
                }
            }

            LaTeX_Table.ThreePartTable_longtable(
                file_path:filepath_output,
                tabular_cols: tabular_column_types,
                table_heading: heading.ToArray(),
                table_body: body.ToArray(),
                table_notes: table_footnotes,
                mandatory_caption: table_mandatory_caption,
                table_label: table_label,
                caption_option: table_optional_caption,
                si_setup: sisetup_default,
                tabular_new_line: tblnewline);
        }

        /// <summary> Get the total number of reads </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static int TotalReads(Sample sample) { return sample.TaxonObservations.Sum((d) => (d.Observation.Abundance)); }
        /// <summary> Get the total number of phylotypes with non-zero abundance </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static int TotalPhylotypes(Sample sample) { return sample.TaxonObservations.Sum((d) => ((d.Observation.Abundance == 0) ? (0) : (1))); }
        /// <summary> Get the total number of phylotypes with a relative abundance greater than or equal to the minimum </summary>
        /// <param name="sample"></param>
        /// <param name="min_rel_abund"></param>
        /// <returns></returns>
        public static int TotalMajorPhylotypes(Sample sample, double min_rel_abund) { return sample.TaxonObservations.Sum((d) => ((d.Observation.RelativeAbundance >= min_rel_abund && d.Observation.Abundance != 0) ? (1) : (0))); }
        /// <summary> Get the total number of phylotypes with a relative abundance greater than zero and less than the maximum </summary>
        /// <param name="sample"></param>
        /// <param name="max_rel_abund"></param>
        /// <returns></returns>
        public static int TotalMinorPhylotypes(Sample sample, double max_rel_abund) { return sample.TaxonObservations.Sum((d) => ((d.Observation.RelativeAbundance < max_rel_abund && d.Observation.Abundance != 0) ? (1) : (0))); }

        #region Diversity Indices
        /// <summary> Get the Shannon Diversity Index ($H'$), given by $H'=-\sum\limits_{i=1}^S\left(p_i\cdot\ln p_i\right)$ where $S$ is the total number of phylotypes and $p_i$ is the relative abundance of the \ith{} phylotype. </summary>
        /// <remarks> Reference: </remarks>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static double ShannonDiversityIdx(Sample sample) {return -1d * sample.TaxonObservations.Sum((d) => ((d.Observation.Abundance == 0)?(0d):(d.Observation.RelativeAbundance * Math.Log(d.Observation.RelativeAbundance, Math.E))));}
        /// <summary> Get the Pielou Evenness ($R$), given by $R = \dfrac{H'}{\ln S}$ where $H'$ is the Shannon diversity index and $S$ is the total number of phylotypes. </summary>
        /// <remarks> Reference: </remarks>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static double PielouEvenness(Sample sample) { return altvisngs_diversity.ShannonDiversityIdx(sample) / Math.Log(sample.TaxonObservations.Sum((d) => ((d.Observation.Abundance == 0) ? (0d) : (1d))), Math.E); }
        /// <summary> Get the Simpson's Diversity Index ($D$), given by $D = 1-\sum\limits_{i=1}^S{p_{i}^{2}}$ where $S$ is the total number of phylotypes and $p_i$ is the relative abundance of the \ith{} phylotype</summary>
        /// <remarks> Reference: </remarks>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static double SimpsonDiversityIdx(Sample sample) { return 1d - sample.TaxonObservations.Sum((d) => Math.Pow(d.Observation.RelativeAbundance, 2d)); }

        #endregion

        #region Richness Estimators
        /// <summary> Get the bias-corrected and small sample size corrected Chao1 Richness Estimate for the sample, given by $S_{\text{Chao1}} = S + \dfrac{n+1}{n}\cdot\dfrac{F_{1}\cdot(F_{1} -1)}{2\cdot(F_{2} + 1)}$ where $S$ is the total number of phylotypes, $n$ is the total number of reads, $F_{1}$ is the total number of phylotypes to which only one read was assigned, and $F_{2}$ is the total number of phylotypes to which only two reads were assigned. </summary>
        /// <remarks> Reference: http://viceroy.eeb.uconn.edu/estimates/EstimateSPages/EstSUsersGuide/EstimateSUsersGuide.htm#Chao1 </remarks>
        /// <param name="sample"></param>
        /// <returns></returns>
        public static double Chao1(Sample sample)
        {
            double F1,F2;
            F1 = (double)(sample.TaxonObservations.Sum((d) => (d.Observation.Abundance == 1)?(1):(0)));//singletons
            F2 = (double)(sample.TaxonObservations.Sum((d) => (d.Observation.Abundance == 2)?(1):(0)));//doubletons
            double n = (double)(altvisngs_diversity.TotalReads(sample));//number of individuals

            return altvisngs_diversity.TotalPhylotypes(sample) + ((n - 1d) / n) * (F1 * (F1 - 1d)) / (2d * (F2 + 1d));
        }

        public static double ACE(Sample sample)
        {
            double F_1 = (double)(sample.TaxonObservations.Sum((d) => (d.Observation.Abundance == 1) ? (1) : (0)));
            double F_leq10 = (double)(sample.TaxonObservations.Sum((d) => (d.Observation.Abundance <= 10 && d.Observation.Abundance > 0)?(1):(0)));
            double F_gt10 = (double)(sample.TaxonObservations.Sum((d) => (d.Observation.Abundance > 10) ? (1) : (0)));
            return double.NaN;
        }

        #endregion
    }
}
