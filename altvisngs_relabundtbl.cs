using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace altvisngs
{
    /// <summary> Enum to indicate how the taxaObs are counted for a given taxon name. </summary>
    public enum UnknownTaxonMatchType 
    {
        /// <summary> To be counted, the taxon name at the current level must be unknown, as well as each taxon name for all superior levels (the Exact option) </summary>
        /// <remarks> Most unique types (lines in table)</remarks>
        NoLumping,
        /// <summary> To be counted, the taxon name at the current level must be unkown, as well as the requisite number up to a given level </summary>
        /// <remarks> With this option, unknowns are grouped based on the taxonomic level where they are last classified; Intermediate number of lines in table</remarks>
        ByLastDefinedTaxaLevel,
        /// <summary> To be counted, the taxon name must simply be unknown (ALL unknowns are grouped into a single line) </summary>
        AllInOne 
    };
    /// <summary> Abstract class handling the generation of tables of relative abundance from the samples </summary>
    /// <remarks> This produces tables for the relative abundance at different taxonomic levels</remarks>
    abstract class altvisngs_relabundtbl
    {
        public static void AutoTable(
            string output_directory,
            string fileName_NoExtension,
            Sample[][] samples,
            Func<Sample, string> samplecolhead,
            Func<Sample, string> posttexhead,
            int level,
            string label,
            string long_caption,
            string short_caption = "",
            string unknown = "unknown", params string[] unknowns)
        {
            Console.WriteLine("Building relative abundance table `" + label + "'...");
            string dest = output_directory + "\\" + fileName_NoExtension;
            string stackedbarlbl = string.Empty;
            if (samples.Length == 1)//only one...simplew/avgstddev
            {
                if (samples[0].Length == 0) return;//nothing to do
                altvisngs_relabundtbl.SimpleSamplesWithAverageStdDev(dest + ".tex", dest + ".csv", dest + "_stackedbar.tex",
                    level, samples[0],
                    UnknownTaxonMatchType.NoLumping,
                    long_caption,
                    short_caption,
                    label,
                    stackedbarlbl,
                    samplecolhead, true);
            }
            else
                altvisngs_relabundtbl.MultipleAveragesStdDev(dest + ".tex", dest + ".csv", dest + "_stackedbar.tex",
                    level, samples,
                    UnknownTaxonMatchType.NoLumping,
                    long_caption,
                    short_caption,
                    label,
                    stackedbarlbl,
                    samplecolhead, posttexhead);
        }
        
        #region Default Table Types
        /// <summary> Create a simple summary of multiple samples with an optional average+/- stddev column at the right hand side </summary>
        /// <param name="filePath_tex"></param>
        /// <param name="filePath_csv"></param>
        /// <param name="level"></param>
        /// <param name="samples"></param>
        /// <param name="handleunknowns"></param>
        /// <param name="delimitedcaptionargs">The delimited arguments for \caption (e.g., "[Short table caption.]{Long table caption.}")</param>
        /// <param name="captionafterfirstpage">The "caption" to appear at the top of the table after the first page (e.g., "Long table caption continued.")</param>
        /// <param name="label">The argument for \label for the table (e.g., "tbl:testtable")</param>
        /// <param name="includeMeanStdDev"></param>
        /// <param name="not_detected">The string used in place of zero for a taxa that is not detected in a sample (defualt = "N. D.")</param>
        public static void SimpleSamplesWithAverageStdDev(string filePath_tex, string filePath_csv, string filePath_stackedbar, int level,
            Sample[] samples, UnknownTaxonMatchType handleunknowns,
            string mandatory_caption,
            string optional_caption,
            string label,
            string stackedbarxlabel,
            Func<Sample, string> samplecolhead,
            bool includeMeanStdDev = true, string not_detected = "N. D.", string NDequiv = "Not detected",
            string minor_phylotypes = @"Minor phylotypes", string unknown="unknown", params string[] unknowns)
        {
            //test the table output
            List<altvisngs_relabundtbl.relabundtbl_column> cols = new List<altvisngs_relabundtbl.relabundtbl_column>();
            //string[] unknowns = new string[] { "unknown", null, string.Empty };

            string head = altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(level);
            head = char.ToUpper(head[0]).ToString() + head.Substring(1);
            //Taxon taxon, int level, string postIncertaeSedis, bool consolidate
            cols.Add(new altvisngs_relabundtbl.relabundtbl_column_taxa(level,
                (t, l, c) => altvisngs_relabundtbl.TaxonName(t, l, c, (x, e, p, o) => (altvisngs_relabundtbl.FormatTaxa_itall(x, e, p, o, minor_phylotypes, unknown, unknown)),
                    altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons,
                    handleunknowns != UnknownTaxonMatchType.NoLumping, @"\ref{tblfoot:" + label + ".incsed}", @"\ref{tblfoot:" + label + ".minor}", minor_phylotypes, unknown, unknown, unknowns),
                @">{\hangindent=1em}p{1.75in}", new string[] { head + @"\ref{tblfoot:" + label + ".sort}" + @"\textsuperscript{,}\ref{tblfoot:" + label + ".taxon}" },
                new string[][] { new string[] { head } }));

            string prcnt = @"\%";
            Dictionary<string, string> tablenotes = new Dictionary<string, string>();
            if (includeMeanStdDev)
                tablenotes.Add("tblfoot:" + label + @".sort", @"Phylotypes were sorted in descending order of the mean relative abundance.");
            else
                tablenotes.Add("tblfoot:" + label + @".sort", @"Phylotypes were sorted in descending order of the maximum relative abundance in all samples.");

            switch (handleunknowns)
            {
                case (UnknownTaxonMatchType.NoLumping):
                    tablenotes.Add("tblfoot:" + label + @".taxon", @"Unknown phylotypes are differentiated based on their classification at superior taxonomic levels. " +
                        @"Where available, the most specific taxonomic level and taxon assigned to the phylotype are included under the unknown. " +
                        @"Taxonomic levels are abbreviated as d:~domain, p:~phylum, c:~class, o:~order, and f:~family.");
                    break;
                default:
                    throw new NotImplementedException("UnknownTaxonMatchType `" + handleunknowns.ToString() + "' not implemented.");
            }
            if (!string.IsNullOrEmpty(not_detected) || !string.IsNullOrEmpty(NDequiv))
            {
                tablenotes.Add("tblfoot:" + label + ".ND", not_detected + " = " + NDequiv + ".");
                prcnt += @"\ref{tblfoot:" + label + ".ND}";
            }
            if (includeMeanStdDev)
                tablenotes.Add("tblfoot:" + label + ".stddev", @"Sample standard deviation.");// +Environment.NewLine;// with $n=" + samples.Length.ToString() + "$.

            tablenotes.Add("tblfoot:" + label + @".minor", @"" + minor_phylotypes + @" is the aggregation of the phylotypes individually constituting less than \SI{1}{\percent} of the total relative abundance for all of the operational days shown; the number of included phylotypes is indicated in parentheses.");// +Environment.NewLine; 

            for (int i = 0; i < samples.Length; i++)
                cols.Add(new altvisngs_relabundtbl.relabundtbl_column_sample(samples[i], altvisngs_relabundtbl.RelativeAbundance,
                    "S", new string[] { samplecolhead(samples[i]), prcnt },
                    new string[][] { new string[] { samplecolhead(samples[i]), "%" } }));

            if (includeMeanStdDev)
                cols.Add(new altvisngs_relabundtbl.relabundtbl_column_samplestat(samples,
                    (m, c) => (altvisngs_relabundtbl.MeanpmStdDev(m, true, c, not_detected)), altvisngs_relabundtbl.SortOnMean,
                    new string[] {@"S@{\,$\pm$\,}", @"S"},
                    "Average", new string[] { @"{Average}&{SD\ref{tblfoot:" + label + ".stddev}}", prcnt },
                    new string[][] { new string[] { "Average", "%" }, new string[] {"SD", "%"} }));

            siunitx_Sdefault Sdefaults = new siunitx_Sdefault(2, 2, 1, true);

            tablenotes.Add(@"tblfoot:" + label + @".incsed", @"To conserve space, \textit{incertae sedis} has been abbreviated \textit{inc.\@ sed.\@} with the original capitalization and interceding characters preserved.");

            altvisngs_relabundtbl.BuildTexTable tbl = ((t, h, b, i) => LaTeX_Table.ThreePartTable_longtable(
                tabular_cols: t,
                table_heading: h,
                table_body: b,
                table_notes: tablenotes,
                mandatory_caption: mandatory_caption,
                table_label: label,
                caption_option: optional_caption,
                si_setup: @"table-number-alignment=center,tight-spacing=true," + Sdefaults.ToString(),
                new_tab_col_sep_length:"5pt"));

            altvisngs_relabundtbl.RelativeAbundanceTable(filePath_tex, filePath_csv, filePath_stackedbar,
                cols,
                (A,B) => (altvisngs_relabundtbl.SortByStatistic_Rev(A, B , unknowns)),
                tbl,
                handleunknowns, Sdefaults, level,
                stackedbarxlabel, true, 0.01, minor_phylotypes, "unknown", not_detected, "&",@"\tabularnewline", unknowns);
        }

        /// <summary> Create a summary of averages of samples </summary>
        /// <param name="filePath_tex"></param>
        /// <param name="filePath_csv"></param>
        /// <param name="level"></param>
        /// <param name="samples"></param>
        /// <param name="handleunknowns"></param>
        /// <param name="delimitedcaptionargs">The delimited arguments for \caption (e.g., "[Short table caption.]{Long table caption.}")</param>
        /// <param name="captionafterfirstpage">The "caption" to appear at the top of the table after the first page (e.g., "Long table caption continued.")</param>
        /// <param name="label">The argument for \label for the table (e.g., "tbl:testtable")</param>
        /// <param name="not_detected">The string used in place of zero for a taxa that is not detected in a sample (defualt = "N. D.")</param>
        public static void MultipleAveragesStdDev(string filePath_tex, string filePath_csv, string filePath_stackedbar, int level,
            Sample[][] samples, UnknownTaxonMatchType handleunknowns,
            string mandatory_caption,
            string optional_caption,
            string label,
            string stackedbarxlabel,
            Func<Sample, string> samplecolhead,
            Func<Sample, string> postheadtex,
            string not_detected = "N. D.", string NDequiv = "Not detected",
            string minor_phylotypes = @"Minor phylotypes",
            string unknown = "unknown",
            params string[] unknowns)
        {
            //test the table output
            List<altvisngs_relabundtbl.relabundtbl_column> cols = new List<altvisngs_relabundtbl.relabundtbl_column>();

            string head = altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(level);
            head = char.ToUpper(head[0]).ToString() + head.Substring(1);
            //Taxon taxon, int level, string postIncertaeSedis, bool consolidate
            cols.Add(new altvisngs_relabundtbl.relabundtbl_column_taxa(level,
                (t, l, c) => altvisngs_relabundtbl.TaxonName(t, l, c, (x, e, p, o) => (altvisngs_relabundtbl.FormatTaxa_itall(x, e, p, o, minor_phylotypes, unknown, unknown)),
                    altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons,
                    handleunknowns != UnknownTaxonMatchType.NoLumping,
                    @"\ref{tblfoot:" + label + ".incsed}",
                    @"\ref{tblfoot:" + label + ".minor}",
                    minor_phylotypes,
                    unknown,
                    unknown,
                    unknowns),
                @">{\hangindent=1em}p{1.75in}", new string[] { head + @"\ref{tblfoot:" + label + ".sort}" + @"\textsuperscript{,}\ref{tblfoot:" + label + ".taxon}" },
                new string[][] { new string[] { head } }));

            string prcnt = @"\%";
            Dictionary<string, string> tablenotes = new Dictionary<string, string>();// = @"      \footnotesize" + Environment.NewLine;
            tablenotes.Add("tblfoot:" + label + @".sort", @"Phylotypes were sorted in descending order of the maximum mean relative abundance.");// + Environment.NewLine;

            switch (handleunknowns)
            {
                case (UnknownTaxonMatchType.NoLumping):
                    tablenotes.Add("tblfoot:" + label + @".taxon", @"Unknown phylotypes are differentiated based on their classification at superior taxonomic levels. " +
                        @"Where available, the most specific taxonomic level and taxon assigned to the phylotype are included under the unknown. " +
                        @"Taxonomic levels are abbreviated as d:~domain, p:~phylum, c:~class, o:~order, and f:~family.");
                    break;
                default:
                    throw new NotImplementedException("UnknownTaxonMatchType `" + handleunknowns.ToString() + "' not implemented.");
            }
            if (!string.IsNullOrEmpty(not_detected) || !string.IsNullOrEmpty(NDequiv))
            {
                tablenotes.Add("tblfoot:" + label + ".ND", not_detected + " = " + NDequiv + ".");// + Environment.NewLine;
                prcnt += @"\ref{tblfoot:" + label + ".ND}";
            }
            tablenotes.Add("tblfoot:" + label + ".stddev", @"Sample standard deviation.");// + Environment.NewLine;// with $n=" + samples.Length.ToString() + "$.

            tablenotes.Add("tblfoot:" + label + @".minor", minor_phylotypes + @" is the aggregation of the phylotypes individually constituting less than \SI{1}{\percent} of the total relative abundance for all of the operational days shown; the number of included phylotypes is indicated in parentheses.");// + Environment.NewLine;
            
            for(int i=0;i<samples.Length;i++)
                if(samples[i].Length>0)
                    cols.Add(new altvisngs_relabundtbl.relabundtbl_column_samplestat(samples[i],
                        (m, c) => (altvisngs_relabundtbl.MeanpmStdDev(m, true, c, not_detected)), altvisngs_relabundtbl.SortOnMean,
                        new string[] {@"S@{\,$\pm$\,}", @"S"},
                        samplecolhead(samples[i][0]), new string[] { samplecolhead(samples[i][0]) + postheadtex(samples[i][0]), @"{Average}&{SD\ref{tblfoot:" + label + ".stddev}}", prcnt },
                        new string[][] { new string[] { samplecolhead(samples[i][0]), "Average", "%" }, new string[] { samplecolhead(samples[i][0]), "SD", "%" } }));

            siunitx_Sdefault Sdefaults = new siunitx_Sdefault(2, 2, 1, true);
            tablenotes.Add("tblfoot:" + label + @".incsed", @"To conserve space, \textit{incertae sedis} has been abbreviated \textit{inc.\@ sed.\@} with the original capitalization and interceding characters preserved.");
            altvisngs_relabundtbl.BuildTexTable tbl = ((t, h, b, i) => LaTeX_Table.ThreePartTable_longtable(
                tabular_cols: t,
                table_heading: h,
                table_body: b,
                table_notes: tablenotes,
                mandatory_caption: mandatory_caption,
                table_label: label,
                caption_option: optional_caption,
                si_setup: @"table-number-alignment=center,tight-spacing=true," + Sdefaults.ToString(),
                new_tab_col_sep_length: "5pt"));//custom command from longtablesup package

            altvisngs_relabundtbl.RelativeAbundanceTable(filePath_tex, filePath_csv, filePath_stackedbar,
                cols,
                (A, B) => (altvisngs_relabundtbl.SortByStatistic_Rev(A, B, unknowns)),
                tbl,
                handleunknowns, Sdefaults, level,
                stackedbarxlabel,
                true,
                0.01,
                minor_phylotypes,
                unknown, not_detected, "&", @"\tabularnewline", unknowns);
        }

        #endregion
        
        /// <summary> Method to create a tex table from the passed colums and write the result to the passed file </summary>
        /// <param name="texfilePath">The file path for the .tex file (if empty or null, not written)</param>
        /// <param name="csvfilePath">The file path for the .csv file (if empty or null, not written)</param>
        /// <param name="columns">List of the columns of the table (some restrictions apply)</param>
        /// <param name="RowSortComparison">The comparator used to sort the rows</param>
        /// <param name="buildTexTable">Delegate for building the tex table from the heading and body</param>
        /// <param name="ND">The abbreviation used in place of "0" for a taxa that is not detected in a sample</param>
        /// <param name="tabularcolsep">The string used for column separator (default = "&")</param>
        /// <param name="tabularnewline">The string used for new lines (default = "\tabularnewline")</param>
        public static void RelativeAbundanceTable(string texfilePath, string csvfilePath, string stackfilePath, List<relabundtbl_column> columns,
            System.Comparison<relabundtbl_row> RowSortComparison,
            BuildTexTable buildTexTable,
            UnknownTaxonMatchType handleunknowns,
            siunitx_Sdefault S_sisetup,
            int level,
            string stackedbarxlabel,
            bool consolidateMinors,
            double minorcutoff,
            string minor_phylotypes = @"Minor phylotypes",
            string unknown = "unknown",
            string ND = "N. D.", string tabularcolsep = "&", string tabularnewline = @"\tabularnewline", params string[] unknowns)
        {
            stackfilePath = null;
            //determine the entries in the column
            //first, the taxon_level
            relabundtbl_column_taxa[] taxon_cols = columns.OfType<relabundtbl_column_taxa>().ToArray();
            if(taxon_cols == null) throw new FormatException("RelativeAbundanceTable: Must have one taxa type column");
            if(taxon_cols.Length != 1) throw new FormatException("RelativeAbundanceTable: Must have one and only one taxa type column");
            int taxon_col_idx = columns.IndexOf(taxon_cols[0]);//only one... no need for a dictionary

            //next, the samples
            relabundtbl_column_sample[] sample_cols = columns.OfType<relabundtbl_column_sample>().ToArray();
            if (sample_cols == null) sample_cols = new relabundtbl_column_sample[0];
            Dictionary<int, relabundtbl_column_sample> sample_cols_dict = new Dictionary<int,relabundtbl_column_sample>();//speed thing sup w/ a dictionary
            for (int i = 0; i < sample_cols.Length; i++) 
                sample_cols_dict.Add(columns.IndexOf(sample_cols[i]), sample_cols[i]);

            //finally, the averages
            relabundtbl_column_samplestat[] stat_cols = columns.OfType<relabundtbl_column_samplestat>().ToArray();
            if (stat_cols == null) stat_cols = new relabundtbl_column_samplestat[0];
            Dictionary<int, relabundtbl_column_samplestat> stat_cols_dict = new Dictionary<int, relabundtbl_column_samplestat>();
            for (int i = 0; i < stat_cols.Length; i++) 
                stat_cols_dict.Add(columns.IndexOf(stat_cols[i]), stat_cols[i]);

            //Build a list of ALL samples referenced
            List<Sample> AllSamples = new List<Sample>();
            for (int i = 0; i < sample_cols.Length; i++)
                if(!AllSamples.Contains(sample_cols[i].Sample))
                    AllSamples.Add(sample_cols[i].Sample);
            for(int i=0;i<stat_cols.Length;i++)
                for(int j=0;j<stat_cols[i].Samples.Length;j++)
                    if (!AllSamples.Contains(stat_cols[i].Samples[j]))
                        AllSamples.Add(stat_cols[i].Samples[j]);

            //Build a list of All taxons
            List<Taxon> AllTaxons = new List<Taxon>();
            for (int i = 0; i < AllSamples.Count; i++)
            {
                Taxon[] rslt = altvisngs_data.GetAllTaxons(AllSamples[i]);
                for(int j=0;j<rslt.Length;j++)
                    if(!AllTaxons.Contains(rslt[j]))
                        AllTaxons.Add(rslt[j]);
            }

            //Refine the list of taxons
            Taxon[] taxons = altvisngs_data.GetTaxonsAtLevel_Unsorted(AllTaxons.ToArray(), level, unknown);
            taxons = altvisngs_data.ConsolidateUnknownTaxa(taxons, handleunknowns, unknown);

            //Build the rows: Split at major and minor
            List<Taxon> major = new List<Taxon>();
            List<Taxon> minor = new List<Taxon>();
            if (!consolidateMinors) 
                major.AddRange(taxons);//quick
            else
                for (int i = 0; i < taxons.Length; i++)
                {
                    bool is_major = false;
                    for (int j = 0; j < AllSamples.Count; j++)
                    {
                        Observation obs = AllSamples[j].SumAllMembersOf(taxons[i], unknown, handleunknowns == UnknownTaxonMatchType.AllInOne);
                        if (obs.RelativeAbundance > minorcutoff)//if ANY sample included has this as a non-minor taxon, then include in the majors
                        {
                            is_major = true;
                            break;
                        }
                    }
                    if (is_major) major.Add(taxons[i]);
                    else minor.Add(taxons[i]);
                }

            //Finally, build the rows.
            List<relabundtbl_row> rows = new List<relabundtbl_row>();
            relabundtbl_rowentry[] rowentries;
            relabundtbl_rowentry_taxon taxon_row_entry;
            bool hasIncertaeSedis = false;
            for (int i = 0; i < major.Count; i++)
            {
                if (level < major[i].Hierarchy.Length && !hasIncertaeSedis)
                    hasIncertaeSedis = IsIncertaeSedis(major[i].Hierarchy[level]);

                rowentries = new relabundtbl_rowentry[columns.Count];
                taxon_row_entry = null;
                for (int j = 0; j < columns.Count; j++)
                {
                    if (j == taxon_col_idx)
                    {
                        taxon_row_entry = new relabundtbl_rowentry_taxon(major[i], level, taxon_cols[0].TaxonMeth);
                        rowentries[j] = taxon_row_entry;
                    }
                    else
                    {
                        if (sample_cols_dict.ContainsKey(j))
                            rowentries[j] = new relabundtbl_rowentry_relabund(sample_cols_dict[j].Sample.SumAllMembersOf(major[i], unknown, handleunknowns == UnknownTaxonMatchType.AllInOne).RelativeAbundance, sample_cols_dict[j].RelativeAbundanceMeth, ND);
                        else
                        {
                            if (stat_cols_dict.ContainsKey(j))
                            {
                                double[] relabunds = new double[stat_cols_dict[j].Samples.Length];
                                for (int k = 0; k < relabunds.Length; k++)
                                    relabunds[k] = stat_cols_dict[j].Samples[k].SumAllMembersOf(major[i], unknown, handleunknowns == UnknownTaxonMatchType.AllInOne).RelativeAbundance;
                                rowentries[j] = new relabundtbl_rowentry_stat(relabunds, stat_cols_dict[j].StatMethod, stat_cols_dict[j].SortOnMethod);
                            }
                        }
                    }
                }
                relabundtbl_row row = new relabundtbl_row(taxon_row_entry, rowentries);
                rows.Add(row);
            }

            //now, the minor
            relabundtbl_row minorRow = null;
            if (minor.Count > 0)
            {
                taxon_row_entry = new relabundtbl_rowentry_taxon(new Taxon(new string[] { minor_phylotypes + " (" + minor.Count.ToString() + ")" }), level, taxon_cols[0].TaxonMeth);
                rowentries = new relabundtbl_rowentry[columns.Count];
                rowentries[taxon_col_idx] = taxon_row_entry;
                for (int i = 0; i < minor.Count; i++)
                    for (int j = 0; j < columns.Count; j++)
                        if (sample_cols_dict.ContainsKey(j))
                        {
                            if (rowentries[j] == null) rowentries[j] = new relabundtbl_rowentry_relabund(0d, sample_cols_dict[j].RelativeAbundanceMeth, ND);
                            rowentries[j].RelAbund += sample_cols_dict[j].Sample.SumAllMembersOf(minor[i], unknown, handleunknowns == UnknownTaxonMatchType.AllInOne).RelativeAbundance;
                        }
                        else
                        {
                            if (stat_cols_dict.ContainsKey(j))
                            {
                                double[] relabunds = new double[stat_cols_dict[j].Samples.Length];
                                for (int k = 0; k < relabunds.Length; k++)
                                    relabunds[k] = stat_cols_dict[j].Samples[k].SumAllMembersOf(minor[i], unknown, handleunknowns == UnknownTaxonMatchType.AllInOne).RelativeAbundance;
                                if (rowentries[j] == null) rowentries[j] = new relabundtbl_rowentry_stat(relabunds, stat_cols_dict[j].StatMethod, stat_cols_dict[j].SortOnMethod);
                                else
                                {
                                    relabundtbl_rowentry_stat st = rowentries[j] as relabundtbl_rowentry_stat;
                                    for (int k = 0; k < relabunds.Length; k++)
                                        st.RelativeAbundances[k] += relabunds[k];
                                }
                            }
                        }
                minorRow = new relabundtbl_row(taxon_row_entry, rowentries);
                rows.Add(minorRow);
            }

            //rows populated. Sort.
            rows.Sort(RowSortComparison);

            if (!string.IsNullOrEmpty(stackfilePath))//then save the bar plot
            {
                List<relabundtbl_row> alphsort = new List<relabundtbl_row>();
                alphsort.AddRange(rows);
                if (minorRow != null)
                {
                    alphsort.Remove(minorRow);//ALWAYS have this at the bottom (top of the stack)
                    alphsort.Add(minorRow);
                }

                List<string> legendentries = new List<string>();
                for (int i = 0; i < alphsort.Count; i++)
                    legendentries.Add(TaxonName_StackedBar(alphsort[i].Taxon, level, altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons,minor_phylotypes,unknown,unknown,unknowns));

                string currplot;
                //string minorplot = @"\addplot coordinates" + Environment.NewLine + "{";
                int sampleidx;
                List<string> addplots = new List<string>();
                for (int i = 0; i < alphsort.Count; i++)
                {
                    sampleidx = 0;
                    currplot = @"\addplot coordinates" + Environment.NewLine;
                    currplot += "{";
                    for (int j = 0; j < alphsort[i].RowEntries.Length; j++)
                    {
                        relabundtbl_rowentry_relabund rela = alphsort[i].RowEntries[j] as relabundtbl_rowentry_relabund;
                        double valtoplot;
                        if (rela != null)
                            valtoplot = rela.RelAbund;
                        else
                        {
                            relabundtbl_rowentry_stat stat = alphsort[i].RowEntries[j] as relabundtbl_rowentry_stat;
                            if (stat == null) continue;//nothing to do with this collumn;
                            valtoplot = _Mean(stat.RelativeAbundances);
                        }
                        currplot += "(" + sampleidx.ToString() + "," + (100d * valtoplot).ToString("0.###############") + ")";
                        //if (i == alphsort.Count - 1)
                        //    minorplot += "(" + sampleidx.ToString() + "," + (100d * minor[j]).ToString("0.###############") + ")";
                        sampleidx++;
                        if (sampleidx < sample_cols.Length)
                        {
                            currplot += " ";
                            //if (i == nonminorrows.Count - 1) minorplot += " ";
                        }
                    }
                    currplot += "};" + Environment.NewLine;
                    addplots.Add(currplot);
                }
                //minorplot += "};" + Environment.NewLine;
                //addplots.Add(minorplot);

                int[] xpositions = new int[sample_cols.Length + stat_cols.Length];
                string[] xlabels = new string[sample_cols.Length + stat_cols.Length];
                int idx = 0;
                for (int i = 0; i < columns.Count; i++)
                {
                    relabundtbl_column_sample rela = columns[i] as relabundtbl_column_sample;
                    string lbl = string.Empty;
                    if (rela != null)
                    {
                        lbl = rela.GetHeadingBarPlot();
                    }
                    else
                    {
                        relabundtbl_column_samplestat stat = columns[i] as relabundtbl_column_samplestat;
                        if (stat == null) continue;
                        lbl = stat.StackedBarHeading;
                        //if (sample_cols.Length == 0)
                        //    lbl = stat.GetHeadingAtLine_csv(0);//,tabularcolsep);//should be what the heading is titled for multi
                        //else
                        //    lbl = "Average";
                    }
                    xpositions[idx] = idx;
                    xlabels[idx] = lbl;
                    idx++;
                }

                altvisngs_stackedbar.DefaultStackedBar(stackfilePath, legendentries.ToArray(), xlabels, stackedbarxlabel, xpositions, addplots.ToArray());
            }

            //Build the heading and body of the table.
            List<string> tabularcoltypes = new List<string>();//string[columns.Count];
            List<string> headingtex = new List<string>();// string.Empty;
            string headingdummy = string.Empty;
            string headingcsv = string.Empty;
            int linesinhead = 0;
            for (int i = 0; i < columns.Count; i++)
            {
                linesinhead = Math.Max(linesinhead, columns[i].HeadingLines);
                if (columns[i].TabularColumnType == null) continue;
                for (int j = 0; j < columns[i].TabularColumnType.Length; j++)
                    tabularcoltypes.Add(columns[i].TabularColumnType[j]); //tabularcoltypes[i] = columns[i].TabularColumnType;
            }
            if (linesinhead != 0)
                for (int i = 0; i < linesinhead; i++)
                    for (int j = 0; j < columns.Count; j++)
                    {
                        headingdummy += columns[j].GetHeadingAtLine_tex(i, tabularcolsep);
                        if (j < columns.Count - 1)
                            headingdummy += tabularcolsep;
                        else
                        {
                            headingtex.Add(headingdummy + tabularnewline);
                            headingdummy = string.Empty;
                        }
                        headingcsv += columns[j].GetHeadingAtLine_csv(i) +
                            ((j < columns.Count - 1) ? (",") : (Environment.NewLine));
                    }
            else
                headingtex.Add(tabularnewline);// += tabularnewline + Environment.NewLine;//have a blank line for the heading

            List<string> bodytex = new List<string>();// string.Empty;
            string bodycsv = string.Empty;
            if (rows.Count != 0)
                for (int i = 0; i < rows.Count; i++)
                {
                    string bod = string.Empty;
                    for (int j = 0; j < rows[i].RowEntries.Length; j++)
                    {
                        bod += rows[i].RowEntries[j].Result_tex(tabularcolsep) + ((j < rows[i].RowEntries.Length - 1) ? (tabularcolsep) : (tabularnewline));
                        bodycsv += rows[i].RowEntries[j].Result_csv() + ((j < rows[i].RowEntries.Length - 1) ? (",") : (Environment.NewLine));
                    }
                    bodytex.Add(bod);
                }
            else bodytex.Add(tabularnewline);// +Environment.NewLine;//have a blank line for the body at least

            
            if (!string.IsNullOrEmpty(texfilePath))
            {
                texfilePath = texfilePath.Replace(" ", "");
                //Combine heading and body via delegate
                string rslt = buildTexTable(tabularcoltypes.ToArray(), headingtex.ToArray(), bodytex.ToArray(), hasIncertaeSedis);
                Console.WriteLine("Writing tex table to `" + Path.GetFileName(texfilePath) + "'");
                using (StreamWriter sw = new StreamWriter(texfilePath))
                {
                    sw.Write(rslt);
                }
            }
            if (!string.IsNullOrEmpty(csvfilePath))
            {
                csvfilePath = csvfilePath.Replace(" ", "");
                Console.WriteLine("Writing csv table to `" + Path.GetFileName(csvfilePath) + "'");
                using (StreamWriter sw = new StreamWriter(csvfilePath))
                {
                    sw.Write(headingcsv);
                    sw.Write(bodycsv);
                }
            }
        }

        /// <summary> Delegate to build a tex Table from the Heading and Body </summary>
        /// <remarks> This approach allows for the variety of table approaches in tex to be accomodated</remarks>
        /// <param name="TabularColumnTypes">The array of tabular column types for the columns</param>
        /// <param name="Heading">The string for the heading</param>
        /// <param name="Body">The string for the body of the table</param>
        /// <returns></returns>
        public delegate string BuildTexTable(string[] TabularColumnTypes, string[] Heading, string[] Body, bool hasIncertaeSedis);

        #region Column Classes
        public abstract class relabundtbl_column 
        {
            public string[] TabularColumnType;
            private string[] HeadingTex;
            private string[][] HeadingCSV;
            //public int EQCols;
            public relabundtbl_column(string[] tabularColumnType, string[] headinglinestex, string[][] headinglinescsv)//, int equivCols)
            {
                this.HeadingTex = headinglinestex;
                this.HeadingCSV = headinglinescsv;
                if(headinglinescsv !=null && this.HeadingCSV!=null && this.HeadingTex!=null)
                for (int i = 0; i < headinglinescsv.Length; i++)
                    if (this.HeadingCSV[i].Length != this.HeadingTex.Length)
                        throw new ArgumentException("Each of the csv entries MUST be the same length as the tex entry!");
                this.TabularColumnType = tabularColumnType;
                //this.EQCols = equivCols;
            }

            public int HeadingLines { get { return HeadingTex.Length; } }
            public string GetHeadingAtLine_tex(int index, string tabularcolsep)
            {
                string rslt = string.Empty;
                if (this.TabularColumnType != null)
                {
                    if (index < this.HeadingLines)
                    {
                        if (this.TabularColumnType.Length > 1)//multi
                            if (this.HeadingTex[index].Contains(tabularcolsep)) return this.HeadingTex[index];//custom override
                            else return @"\multicolumn{" + TabularColumnType.Length.ToString() + "}{c}{" + this.HeadingTex[index] + "}";
                        else
                            return "{" + this.HeadingTex[index] + "}";
                    }
                    for (int i = 0; i < TabularColumnType.Length - 1; i++)
                        rslt += tabularcolsep;
                }
                return rslt;
            }
            public string GetHeadingAtLine_csv(int index)
            {
                string rslt = string.Empty;
                if (this.TabularColumnType != null)
                {
                    if (index < this.HeadingLines)
                    {
                        for (int i = 0; i < this.TabularColumnType.Length; i++)
                            rslt += this.HeadingCSV[i][index] + ((i < this.TabularColumnType.Length - 1) ? (",") : (""));
                        return rslt;
                    }
                    for (int i = 0; i < TabularColumnType.Length - 1; i++)
                        rslt += ",";
                }
                return rslt;
            }
        }

        /// <summary> Method to translate a taxon name and potentially the superior taxon names required to differentiate it from an otherwise equivalent entry in the table to a string for the tex table </summary>
        /// <param name="taxon_name">The taxon name</param>
        /// <param name="reqsuperiortaxon">Taxon above the taxon_name required for the entry to be unique in the table (if not null, then another entry in the table has the same taxon_name)</param>
        /// <returns>The formated string for the tex table</returns>
        public delegate string ColumnMeth_TaxonName(Taxon taxon, int level, bool consolidate);
        public class relabundtbl_column_taxa : relabundtbl_column
        {
            public int Level;
            public ColumnMeth_TaxonName TaxonMeth;

            public relabundtbl_column_taxa(int level, ColumnMeth_TaxonName taxonMeth, string tabularColumnType, string[] headinglinestex, string[][] headinglinescsv)
                : base(new string[] { tabularColumnType }, headinglinestex, headinglinescsv)
            {
                this.Level = level;
                this.TaxonMeth = taxonMeth;
            }
        }

        /// <summary> Methd to translate a relative abundace to a string for the text table </summary>
        /// <param name="relativeAbundance">The relative abundance</param>
        /// <returns>The formated string for the tex table</returns>
        public delegate string ColumnMeth_RelativeAbundance(double relativeAbundance, string ND);
        public class relabundtbl_column_sample : relabundtbl_column
        {
            public Sample Sample;
            public ColumnMeth_RelativeAbundance RelativeAbundanceMeth;

            public relabundtbl_column_sample(Sample sample, ColumnMeth_RelativeAbundance relativeAbundanceMeth, string tabularColumnType, string[] headinglinestex, string[][] headinglinescsv)
                : base(new string[] { tabularColumnType }, headinglinestex, headinglinescsv)
            {
                this.Sample = sample;
                this.RelativeAbundanceMeth = relativeAbundanceMeth;
            }

            public string GetHeadingBarPlot() { return this.GetHeadingAtLine_tex(0, ""); }
        }

        /// <summary> Method to process an array of relative abundances and return the string for the tex table </summary>
        /// <param name="relativeAbundances">Array of relative abundances from the passed samples for the associated taxon in the row</param>
        /// <returns>The formated string for the tex table</returns>
        public delegate string ColumnMeth_Statistics(double[] relativeAbundances, string tabularcolsep);
        public delegate double ColumnMeth_Statistics_SortOn(double[] relativeAbundances);
        public class relabundtbl_column_samplestat : relabundtbl_column
        {
            /// <summary> The samples on which the statistics are to be determined </summary>
            public Sample[] Samples;
            public ColumnMeth_Statistics StatMethod;
            public ColumnMeth_Statistics_SortOn SortOnMethod;

            public string StackedBarHeading;

            /// <summary> Initialize a new instance of a statistics column </summary>
            /// <remarks> Column may be used to determine statistics on the relative abundances of multiple samples (e.g., mean, stddev, etc.)</remarks>
            /// <param name="samples">The samples for which the statistics will be determined</param>
            /// <param name="statMethod">Delegate to process the array of relative abundances and return the string for the TeX table</param>
            /// <param name="headinglinestex">The lines in the heading of the table for this column</param>
            public relabundtbl_column_samplestat(Sample[] samples, ColumnMeth_Statistics statMethod, ColumnMeth_Statistics_SortOn sortOnMethod,
                string[] tabularColumnType, string stackedBarheading, string[] headinglinestex, string[][] headinglinescsv)
                : base(tabularColumnType, headinglinestex, headinglinescsv)
            {
                this.StackedBarHeading = stackedBarheading;
                this.Samples = samples;
                this.StatMethod = statMethod;
                this.SortOnMethod = sortOnMethod;
            }
        }

        /// <summary> Class used as a placeholder in a column collection to indicate that additional horizontal space should be added. </summary>
        public class relabundtbl_column_addspace : relabundtbl_column { public relabundtbl_column_addspace() : base(null, null, null) { } }
        #endregion

        #region Row Classes
        public class relabundtbl_row
        {
            /// <summary> The taxon entry for the row (used for sorting) </summary>
            private relabundtbl_rowentry_taxon _taxonentry;
            /// <summary> Array of values for the row in the order of the columns </summary>
            public relabundtbl_rowentry[] RowEntries;

            /// <summary> Initialize a new instance of the relabundtbl_row </summary>
            /// <param name="taxon"></param>
            /// <param name="rowEntries"></param>
            public relabundtbl_row(relabundtbl_rowentry_taxon taxonentry, relabundtbl_rowentry[] rowEntries)
            {
                RowEntries = rowEntries;
                _taxonentry = taxonentry;
            }
            public Taxon Taxon { get { return _taxonentry.Taxon; } }
        }

        public abstract class relabundtbl_rowentry
        {
            public virtual string Result_tex(string tabularcolsep) { throw new NotImplementedException(); }
            public virtual string Result_csv() { throw new NotImplementedException(); }
            public virtual double RelAbund { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        }
        public class relabundtbl_rowentry_taxon : relabundtbl_rowentry
        {
            public int Taxon_Level;
            public Taxon Taxon;
            public ColumnMeth_TaxonName TaxonNameMeth;
            public relabundtbl_rowentry_taxon(Taxon taxon, int level, ColumnMeth_TaxonName taxonNameMeth)
            {
                this.Taxon_Level = level;
                Taxon = taxon;
                this.TaxonNameMeth = taxonNameMeth;
            }
            public override string Result_tex(string tabularcolsep) { return this.TaxonNameMeth(this.Taxon, this.Taxon_Level, true); }
            public override string Result_csv() { return this.TaxonNameMeth(this.Taxon, this.Taxon_Level, false); }
        }
        public class relabundtbl_rowentry_relabund : relabundtbl_rowentry
        {
            private double _RelAbund;
            public ColumnMeth_RelativeAbundance relAbundMeth;
            public string ND;
            public relabundtbl_rowentry_relabund(double relativeAbundance, ColumnMeth_RelativeAbundance method, string nd)
            {
                _RelAbund = relativeAbundance;
                this.relAbundMeth = method;
                this.ND = nd;
            }
            public override string Result_tex(string tabularcolsep) { return this.relAbundMeth(this.RelAbund, this.ND); }
            public override string Result_csv() 
            {
                if (RelAbund == 0d) return ND;
                return (RelAbund * 100).ToString("0.###############");
            }//output the full float this.relAbundMeth(this.RelAbund, this.ND); }
            public override double RelAbund { get { return _RelAbund; } set { _RelAbund = value; } }
        }
        public class relabundtbl_rowentry_stat : relabundtbl_rowentry
        {
            private double _meanrelativeabunde;
            private double[] _relativeAbundances;
            public ColumnMeth_Statistics StatMethod;
            public ColumnMeth_Statistics_SortOn SortOnMethod;
            
            public relabundtbl_rowentry_stat(double[] relativeAbundances, ColumnMeth_Statistics statMethod, ColumnMeth_Statistics_SortOn sortOnMethod)
            {
                RelativeAbundances = relativeAbundances;
                this.StatMethod = statMethod;
                this.SortOnMethod = sortOnMethod;
            }
            public double[] RelativeAbundances
            {
                get { return _relativeAbundances; }
                set
                {
                    _relativeAbundances = value;
                    if (_relativeAbundances == null) _meanrelativeabunde = double.NaN;
                    else if (_relativeAbundances.Length == 0) _meanrelativeabunde = double.NaN;
                    else _meanrelativeabunde = _Mean(this.RelativeAbundances);
                }
            }

            public override string Result_tex(string tabularcolsep) { return this.StatMethod(this.RelativeAbundances, tabularcolsep); }
            public override string Result_csv() { return this.StatMethod(this.RelativeAbundances, ","); }
            public double SortOn { get { return this.SortOnMethod(this.RelativeAbundances); } }

            public override double RelAbund { get { return this._meanrelativeabunde; } }
        }

        /// <summary>  Struct containing the default settings for the siunitx S column. This may be then compared against to see if spacing can be adjusted in the table to accomodate the data automatically.</summary>
        public struct siunitx_Sdefault
        {
            /// <summary>Option corresponding to the "table-figures-integer" option (siunitx default = 3)</summary>
            public int Integers;
            /// <summary>Option corresponding to the "table-figures-decimal" option (siunitx default = 2)</summary>
            public int Decimal;
            /// <summary>Option corresponding to the "table-figures-exponent" option (siunitx default = 0)</summary>
            public int Exponent;
            /// <summary> Option corresponding to the "table-sign-exponent" flag (siunitx default = false)</summary>
            public bool SignExponent;

            /// <summary> Initialize a new instance of the siunitx_Sdefault </summary>
            /// <param name="inte"></param>
            /// <param name="deci"></param>
            /// <param name="expo"></param>
            /// <param name="signExponent"></param>
            public siunitx_Sdefault(int inte = 3, int deci=2, int expo=0, bool signExponent = false)
            {
                Integers = inte;
                Decimal = deci;
                Exponent = expo;
                SignExponent = signExponent;
            }

            /// <summary> Method to determine the required format to display this pre-formatted value </summary>
            /// <param name="formattedrslt"></param>
            /// <param name="exponentind"></param>
            /// <returns></returns>
            public static siunitx_Sdefault GetFromFormattedResult(string formattedrslt, string[] exponentind)
            {
                int inte = 0;
                int deci = 0;
                int expo = 0;
                bool sign = false;
                if (exponentind[0] != "{" && exponentind[exponentind.Length - 1] != "}")
                {
                    string mantissa = formattedrslt;
                    string exponent = string.Empty;
                    for (int i = 0; i < exponentind.Length; i++)
                        if (formattedrslt.Contains(exponentind[i]))
                        {
                            mantissa = formattedrslt.Substring(0, formattedrslt.IndexOf(exponentind[i]));
                            exponent = formattedrslt.Substring(formattedrslt.IndexOf(exponentind[i]) + exponentind[i].Length);
                            break;
                        }
                    if (!string.IsNullOrEmpty(exponent))
                    {
                        int right = int.Parse(exponent);
                        if (right < 0) sign = true;
                        expo = right.ToString().Length;
                    }
                    if (!string.IsNullOrEmpty(mantissa))
                    {
                        if (mantissa.Contains("."))
                        {
                            inte = mantissa.Substring(0, mantissa.IndexOf(".")).Length;
                            deci = mantissa.Substring(mantissa.IndexOf(".") + 1).Length;
                        }
                        else
                        {
                            inte = mantissa.ToString().Length;
                        }
                    }
                }
                return new siunitx_Sdefault(inte, deci, expo, sign);
            }

            /// <summary> Return the widest siunitX_S options of the two </summary>
            /// <param name="A"></param>
            /// <param name="B"></param>
            /// <returns></returns>
            public static siunitx_Sdefault GetWidest(siunitx_Sdefault A, siunitx_Sdefault B)
            {
                return new siunitx_Sdefault(Math.Max(A.Integers, B.Integers), Math.Max(A.Decimal, B.Decimal), Math.Max(A.Exponent, B.Exponent), A.SignExponent || B.SignExponent);
            }
            /// <summary> Return the string containing the options which are narrower than the passed default. </summary>
            /// <param name="defaultS"></param>
            /// <returns></returns>
            public string NarrowerOptions(siunitx_Sdefault defaultS)
            {
                string rslt = string.Empty;
                if (this.Integers < defaultS.Integers)
                    rslt += "table-figures-integer=" + this.Integers.ToString();
                if (this.Decimal < defaultS.Decimal)
                    rslt += (string.IsNullOrEmpty(rslt)) ? ("") : (",") + "table-figures-decimal=" + this.Decimal.ToString();
                if (this.Exponent < defaultS.Exponent)
                    rslt += (string.IsNullOrEmpty(rslt)) ? ("") : (",") + "table-figures-exponent=" + this.Exponent.ToString();
                if (!this.SignExponent && defaultS.SignExponent)
                    rslt += (string.IsNullOrEmpty(rslt)) ? ("") : (",") + "table-sign-exponent=false";
                return rslt;
            }

            public override string ToString()
            {
                return "table-figures-integer=" + this.Integers.ToString() + "," +
                    "table-figures-decimal=" + this.Decimal.ToString() + "," +
                    "table-figures-exponent=" + this.Exponent.ToString() + "," +
                    "table-sign-exponent=" + ((this.SignExponent) ? ("true") : ("false"));
            }
        }

        #endregion

        #region Column/Entry Formatters - Taxon_Name
        public delegate string TaxonNameAtLevel(int level);
        public delegate string FormatTaxonName(Taxon taxon, int level, string postIncertaeSedis, bool consolidate);
        /// <summary> Basic formatter which italicies all taxonomic levels (except unknown) </summary>
        /// <param name="taxon_name"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static string FormatTaxa_itall(Taxon taxon, int level, string postIncertaeSedis, bool consolidate,
            string minor_phylotypes = @"Minor phylotypes", string print_unknown = "unknown", string data_unknown = "unknown") 
        {
            string topass = string.Empty;
            if (taxon.Hierarchy.Length == 1) topass = taxon.Hierarchy[0];//allow minor phylotypes to be handled...
            else topass = taxon.Hierarchy[level];
            return FormatTaxaString_itall(topass, postIncertaeSedis, consolidate, minor_phylotypes, print_unknown, data_unknown);
            //if(taxon.Hierarchy.Length == 1)//look at minor first.
            //    if (taxon.Hierarchy[0].Contains(minor_phylotypes))
            //        if (taxon.Hierarchy[0].IndexOf(minor_phylotypes) == 0)
            //            return taxon.Hierarchy[0];//return unformatted.
            //if (taxon.Hierarchy.Length <= level) return print_unknown;
            //if (taxon.Hierarchy[level] == data_unknown) 
            //    return print_unknown;

            //string taxon_name = taxon.Hierarchy[level];
            ////if (string.IsNullOrEmpty() || taxon_name == "unknown" || taxon_name == "unclassified") return "unknown";
            //if (IsIncertaeSedis(taxon_name) && consolidate) taxon_name = AbbrIncertaeSedis(taxon_name, postIncertaeSedis);//abbreviate incertae sedis (reduce width of tables if consolidate; full appears in csv)
            //taxon_name=taxon_name.Replace("_", @"_\allowbreak ");
            //return @"\textit{" + taxon_name + "}";
        }

        public static string FormatTaxaString_itall(string taxon_name, string postIncertaeSedis, bool consolidate,
            string minor_phylotypes = @"Minor phylotypes",
            string print_unknown = "unknown",
            string data_unknown = "unknown",
            string print_wild = @"\textasteriskcentered",
            string data_wild = "*") 
        {
            if (string.IsNullOrEmpty(taxon_name)) return print_unknown;
            if (taxon_name == data_unknown) return print_unknown;
            if (taxon_name.Contains(minor_phylotypes))
                if (taxon_name.IndexOf(minor_phylotypes) == 0)
                    return taxon_name;//return unformatted

            if (taxon_name == data_wild) return print_wild;//don't italicize *

            if (IsIncertaeSedis(taxon_name) && consolidate) taxon_name = AbbrIncertaeSedis(taxon_name, postIncertaeSedis);//abbreviate incertae sedis (reduce width of tables if consolidate; full appears in csv)
            taxon_name = taxon_name.Replace("_", @"_\allowbreak ");
            return @"\textit{" + taxon_name + "}";
        }
        
        /// <summary> incertae_sedis variants which will be abbreviated to Inc.Sed. or equiv</summary>
        private static string[] incsed = { "incertae_sedis", "incertae sedis", "Incertae_Sedis", "Incertae Sedis", "Incertae_sedis", "Incertae sedis" };
        private static string[] incsedabbr = { "inc._sed.", "inc. sed.", "Inc._Sed.", "Inc. Sed.", "Inc._sed.", "Inc. sed." };
        public static bool IsIncertaeSedis(string taxon_name)
        {
            if (!taxon_name.Contains("ncertae") || !taxon_name.Contains("edis")) return false;//common elements
            for (int i = 0; i < incsed.Length; i++)
                if (taxon_name.Contains(incsed[i])) return true;
            return false;
        }
        public static string AbbrIncertaeSedis(string taxon_name, string postIncertaeSedis = null)
        {
            for (int i = 0; i < incsed.Length; i++)
                if (taxon_name.Contains(incsed[i]))
                {
                    string pre = taxon_name.Substring(0,taxon_name.IndexOf(incsed[i]));
                    string post = taxon_name.Substring(taxon_name.IndexOf(incsed[i]) + incsed[i].Length);
                    return pre + incsedabbr[i] + post + ((string.IsNullOrEmpty(postIncertaeSedis))?(""):(@"{\normalfont " + postIncertaeSedis+ "}"));
                }
            return taxon_name;
        }

        /// <summary> General TaxonName formatter produces "taxon_name (reqsuperiortaxon[0], reqsuperiortaxon[1], ... reqsuperiortaxon[reqsuperiortaxon.Length-1])"  w/ each formatted with formatter</summary>
        /// <param name="taxon_name"></param>
        /// <param name="level"></param>
        /// <param name="reqsuperiortaxon"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static string TaxonName(Taxon taxon, int level, bool consolidate,
            FormatTaxonName formatter, TaxonNameAtLevel taxonNameAtLevel, bool abbreviateallunkown,
            string postIncertaeSedis, string postMinor,
            string minor_phylotypes = @"Minor phylotypes", string print_unknown = "unknown", string data_unknown="unknown", params string[] unknowns)
        {
            string rslt = formatter(taxon, level, postIncertaeSedis, consolidate);
            if (rslt == print_unknown)
            {
                for (int i = taxon.Hierarchy.Length - 1; i > -1; i--)
                    if (taxon.Hierarchy[i] != data_unknown)
                    {
                        rslt += @"\linebreak[4]\mbox{" + taxonNameAtLevel(i)[0] + ": " + formatter(taxon, i, postIncertaeSedis, consolidate) + "}";
                        break;
                    }
            }
            else
            {
                if (rslt.Contains(minor_phylotypes))
                    rslt += postMinor;
            }
            return rslt;
        }

        public static string TaxonName_StackedBar(Taxon taxon, int level, TaxonNameAtLevel taxonNameAtLevel, string minor_phylotypes = @"Minor phylotypes", string print_unknown = "unknown", string data_unknown = "unknown", params string[] unknowns)
        {
            string rslt = FormatTaxa_itall(taxon, level, string.Empty, true, minor_phylotypes, print_unknown, data_unknown);
            if (rslt == print_unknown)
            {
                for (int i = taxon.Hierarchy.Length - 1; i > -1; i--)
                    if (taxon.Hierarchy[i] != data_unknown)
                    {
                        rslt += @" (" + taxonNameAtLevel(i)[0] + ": " + FormatTaxa_itall(taxon, i, string.Empty, true, minor_phylotypes, print_unknown, data_unknown) + ")";
                        break;
                    }
            }
            return rslt;
        }
        #endregion

        #region Double To Sting
        /// <summary>
        /// Format the result to display as a percent with three sigfigs
        /// </summary>
        /// <param name="fractionalval"></param>
        /// <returns></returns>
        public static string FormattedRelativeAbundance(double fractionalval)
        {
            double rslt = fractionalval * 100d;//first mult by 100...show as percent
            //return rslt.ToString("0.###############");
            if (rslt >= 10d)//show as
                return rslt.ToString("0.0");
            if (rslt >= 1d)//integer component
                return rslt.ToString("0.00");
            return rslt.ToString("E2");
        }

        #endregion

        #region Column/Entry Formatters - RelativeAbundance
        public static string RelativeAbundance(double relativeAbundance, string ND)
        {
            if (relativeAbundance == 0d) return "{" + ND + "}";
            if (double.IsNaN(relativeAbundance)) return "{NaN}";
            else return FormattedRelativeAbundance(relativeAbundance);
        }

        #endregion

        #region Column/Entry Formatters - Statistics

        public static double SortOnMean(double[] relativeAbundances) { return _Mean(relativeAbundances); }

        public static string Mean(double[] relativeAbundances, string tabularcolsep = "&") { return FormattedRelativeAbundance(_Mean(relativeAbundances)); }
        public static string StdDev(double[] relativeAbundances, string tabularcolsep = "&") { return FormattedRelativeAbundance(_StdDev(relativeAbundances)); }
        public static string StdErr(double[] relativeAbundances, string tabularcolsep = "&") { return FormattedRelativeAbundance(_StdErr(relativeAbundances)); }
        public static string Min(double[] relativeAbundances, string tabularcolsep = "&") { return FormattedRelativeAbundance(_Min(relativeAbundances)); }
        public static string Max(double[] relativeAbundances, string tabularcolsep = "&") { return FormattedRelativeAbundance(_Max(relativeAbundances)); }

        /// <summary> The mean +/- stddev. Assumes two columns correspond to this entry </summary>
        /// <param name="relativeAbundances"></param>
        /// <param name="strform"></param>
        /// <param name="tabularcolsep"></param>
        /// <returns></returns>
        public static string MeanpmStdDev(double[] relativeAbundances, bool substitue_not_detected, string tabularcolsep = "&", string not_detected = "N. D.") 
        {
            if(substitue_not_detected)
                for(int i=0;i<relativeAbundances.Length;i++)
                    if (relativeAbundances[i] != 0d)
                    {
                        substitue_not_detected = false;
                        break;
                    }
            if (!substitue_not_detected)
                return Mean(relativeAbundances) + tabularcolsep + StdDev(relativeAbundances);
            else
                return @"\multicolumn{2}{c}{" + not_detected + "}";
        }
        /// <summary> The mean +/- stderr. Assumes the associated column type is "S[]@{\,$\pm$\,}S[]" or a similar two column equivalent </summary>
        /// <param name="relativeAbundances"></param>
        /// <param name="strform"></param>
        /// <param name="tabularcolsep"></param>
        /// <returns></returns>
        public static string MeanpmStdErr(double[] relativeAbundances, bool substitue_not_detected, string tabularcolsep = "&", string not_detected = "N. D.")
        {
            if (substitue_not_detected)
                for (int i = 0; i < relativeAbundances.Length; i++)
                    if (relativeAbundances[i] != 0d)
                    {
                        substitue_not_detected = false;
                        break;
                    }
            if (!substitue_not_detected)
                return Mean(relativeAbundances) + tabularcolsep + StdErr(relativeAbundances);
            else
                return @"\multicolumn{2}{c}{" + not_detected + "}";
        }
        

        /// <summary> The mean of the relative abundances </summary>
        /// <param name="relativeAbundances"></param>
        /// <returns></returns>
        public static double _Mean(double[] relativeAbundances) 
        {
            if (relativeAbundances == null) return double.NaN;
            if (relativeAbundances.Length == 0) return double.NaN;
            return (relativeAbundances.Sum() / ((double)(relativeAbundances.Length)));
        }
        /// <summary> The sample standard deviation of the relative abundances </summary>
        /// <param name="relativeAbundances"></param>
        /// <returns></returns>
        public static double _StdDev(double[] relativeAbundances)
        {
            if (relativeAbundances == null) return double.NaN;
            if (relativeAbundances.Length < 2) return double.NaN;
            double mean = _Mean(relativeAbundances);
            double stddev = 0d;
            for (int i = 0; i < relativeAbundances.Length; i++)
                stddev += Math.Pow(relativeAbundances[i] - mean, 2d);
            return Math.Sqrt(1d / ((double)(relativeAbundances.Length - 1)) * stddev);
        }
        /// <summary> The standard error of the mean  of the relative abundances</summary>
        /// <param name="relativeAbundances"></param>
        /// <returns></returns>
        private static double _StdErr(double[] relativeAbundances)
        {
            if (relativeAbundances == null) return double.NaN;
            if (relativeAbundances.Length < 2) return double.NaN;
            return _StdDev(relativeAbundances) / Math.Sqrt(relativeAbundances.Length);
        }
        /// <summary> The minimum of the relative abundances </summary>
        /// <param name="relativeAbundances"></param>
        /// <returns></returns>
        public static double _Min(double[] relativeAbundances)
        {
            if (relativeAbundances == null) return double.NaN;
            if (relativeAbundances.Length == 0) return double.NaN;
            double min = relativeAbundances[0];
            for (int i = 1; i < relativeAbundances.Length; i++)
                min = Math.Min(min, relativeAbundances[i]);
            return min;
        }
        /// <summary> The maximum of the relative abundances </summary>
        /// <param name="relativeAbundances"></param>
        /// <returns></returns>
        public static double _Max(double[] relativeAbundances)
        {
            if (relativeAbundances == null) return double.NaN;
            if (relativeAbundances.Length == 0) return double.NaN;
            double max = relativeAbundances[0];
            for (int i = 1; i < relativeAbundances.Length; i++)
                max = Math.Max(max, relativeAbundances[i]);
            return max;
        }
        #endregion

        #region Sorting
        /// <summary> Sort rows by the maximum relative abundance of a sample in the row </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static int SortByAllValues_Rev(relabundtbl_row A, relabundtbl_row B, params string[] unknowns)
        {
            double maxA = double.NaN;
            double maxB = double.NaN;
            for (int i = 0; i < A.RowEntries.Length; i++)
            {
                relabundtbl_rowentry_relabund relabund = A.RowEntries[i] as relabundtbl_rowentry_relabund;
                if (relabund != null)
                {
                    if (double.IsNaN(maxA)) maxA = relabund.RelAbund;
                    maxA = Math.Max(relabund.RelAbund, maxA);
                    continue;
                }
                relabundtbl_rowentry_stat stat = A.RowEntries[i] as relabundtbl_rowentry_stat;
                if (stat != null)
                {
                    if(double.IsNaN(maxA)) maxA = stat.SortOn;
                    maxA = Math.Max(stat.SortOn, maxA);
                    continue;
                }
            }
            for (int i = 0; i < B.RowEntries.Length; i++)
            {
                relabundtbl_rowentry_relabund relabund = B.RowEntries[i] as relabundtbl_rowentry_relabund;
                if (relabund != null)
                {
                    if (double.IsNaN(maxB)) maxB = relabund.RelAbund;
                    maxB = Math.Max(relabund.RelAbund, maxB);
                    continue;
                }
                relabundtbl_rowentry_stat stat = B.RowEntries[i] as relabundtbl_rowentry_stat;
                if (stat != null)
                {
                    if(double.IsNaN(maxB)) maxB = stat.SortOn;
                    maxB = Math.Max(stat.SortOn, maxB);
                    continue;
                }
            }
            //if <0 then A is less than B
            //if =0 then A = B
            //if >0 then A is greater than B
            if ((double.IsNaN(maxA) && double.IsNaN(maxB)) || maxA == maxB) return altvisngs_data.SortTaxon(A.Taxon, B.Taxon, unknowns);
            if (maxA < maxB) return 1;
            return -1;
        }
        /// <summary> Sort rows by the value of the first statistic column if present, if not, then by the maximum value </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static int SortByStatistic_Rev(relabundtbl_row A, relabundtbl_row B, params string[] unknowns)
        {
            double maxA = double.NaN;
            double maxB = double.NaN;
            relabundtbl_rowentry_stat statA = null;
            relabundtbl_rowentry_stat statB = null;

            bool foundA = false;
            bool foundB = false;
            for (int i = 0; i < A.RowEntries.Length; i++)
            {
                statA = A.RowEntries[i] as relabundtbl_rowentry_stat;//og
                if (statA == null) continue;//og
                foundA = true;
                if (double.IsNaN(maxA)) maxA = statA.SortOn;
                else maxA = Math.Max(maxA, statA.SortOn);
            }
                //statB = B.Values[i] as relabundtbl_rowentry_stat;
                //if (statB != null) break;
                //statA = null;
            //}
            for (int i = 0; i < B.RowEntries.Length; i++)
            {
                statB = B.RowEntries[i] as relabundtbl_rowentry_stat;//og
                if (statB == null) continue;//og
                foundB = true;
                if (double.IsNaN(maxB)) maxB = statB.SortOn;
                else maxB = Math.Max(maxB, statB.SortOn);
            }
            //if (statA == null || statB == null) return SortByAllValues_Rev(A, B);
            if (!foundA && !foundB) return SortByAllValues_Rev(A, B, unknowns);//no stat cols
            //if <0 then A is less than B
            //if =0 then A = B
            //if >0 then A is greater than B
            if ((double.IsNaN(maxA) && double.IsNaN(maxB)) || maxA == maxB) return altvisngs_data.SortTaxon(A.Taxon, B.Taxon, unknowns);
            if (maxA < maxB) return 1;
            return -1;
        }
        /// <summary> Sort rows taxonomically, placing undefined at the top, followed alphabetically through each level of the hierarchy </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="unknowns">Taxon names which are considered equal and unknown (e.g., "unknown", null, "", "uncategorized", "unidentified") </param>
        /// <returns></returns>
        public static int TaxanomicSort_Rev(relabundtbl_row A, relabundtbl_row B, params string[] unknowns)
        {
            return altvisngs_data.SortTaxon(A.Taxon, B.Taxon, unknowns);
            //int alphasort = 0;
            //bool isatlast = false;
            //int atidx = 0;
            //for (int i = 0; i < A.Taxon_Lineage.Length; i++)
            //    if (A.Taxon_Lineage[i] != B.Taxon_Lineage[i])
            //    {
            //        if (string.IsNullOrEmpty(A.Taxon_Lineage[i]) && string.IsNullOrEmpty(B.Taxon_Lineage[i]))
            //            alphasort = 0;//same
            //        else if (!string.IsNullOrEmpty(A.Taxon_Lineage[i]))
            //            alphasort = A.Taxon_Lineage[i].CompareTo(B.Taxon_Lineage[i]);
            //        else//this is null, other is not => invert.
            //            alphasort = -B.Taxon_Lineage[i].CompareTo(A.Taxon_Lineage[i]);
            //        isatlast = (i == A.Taxon_Lineage.Length - 1);
            //        atidx = i;
            //        break;
            //    }

            ////assess unid and prop IF at the first level OR at the last level
            ////always put the unidentified first
            //if (atidx == 0 || atidx == A.Taxon_Lineage.Length - 1)
            //{
            //    if (unknowns.Contains(A.Taxon_Lineage[atidx]) && !unknowns.Contains(B.Taxon_Lineage[atidx])) return -1;
            //    if (!unknowns.Contains(A.Taxon_Lineage[atidx]) && unknowns.Contains(B.Taxon_Lineage[atidx])) return 1;
            //}
            //return alphasort;
        }
        
        #endregion
    }
}
