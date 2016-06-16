using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ComponentModel;
using Excel = Microsoft.Office.Interop.Excel;//Excel used to output the 
using System.Reflection;
using System.Runtime.InteropServices;

namespace altvisngs
{
    /* altvisngs
     * Description: Program to translate `table.abundance.txt', `table.proportions.txt', and `table.taxa_info.txt' files into hierarchical lineage bar plots, rarefaction curves, heatmaps, bar plots,  and diversity measures tables
     * Version: 1.0.0
     * Date: 14 JUN 2016
     * Author: Nicholas M. Guho
     * Input: Command line arguments
     *  #1: "subroutine"
     *  #2: "options"
     */
    class Program
    {
        public static string SForm { get { return "0.###############"; } }
        /// <summary>Main program</summary>
        /// <param name="args">Arguments passed at the command line.</param>
        static void Main(string[] args)
        {
            if (args == null) return;
            if (args.Length == 0) return;//nothing to run
            string[] permitted = new string[] { "-help", "-sub", "-group_attr", "-group_attr_lower", "-groups", "-subgroup_attr", "-subgroup_attr_lower", "-primers" };
            bool ishelp = false;
            //1. sort the arguments
            Dictionary<string, string[]> parsed_args = new Dictionary<string, string[]>();
            string curr_dec = string.Empty;
            List<string> curr_dec_args = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.IsNullOrEmpty(args[i]))
                {
                    if (!string.IsNullOrEmpty(curr_dec))
                        curr_dec_args.Add(args[i]);
                    else
                    {
                        Console.WriteLine("Invalid null/empty argument.");
                        return;
                    }
                }
                else
                {
                    if (args[i][0] == '-')//new dec
                    {
                        if (!string.IsNullOrEmpty(curr_dec))
                        {
                            if (parsed_args.ContainsKey(curr_dec))//allow repeated keys
                                parsed_args[curr_dec] = curr_dec_args.ToArray();
                            parsed_args.Add(curr_dec, curr_dec_args.ToArray());
                            curr_dec_args = new List<string>();
                        }
                        if (args[i] == "-help") ishelp = true;
                        if (args[i].Contains("="))//allow -dec=arg
                        {
                            string[] parsed = args[i].Split(new char[] { '=' });
                            curr_dec = parsed[0];
                            curr_dec_args.Add(parsed[1]);
                        }
                        else
                            curr_dec = args[i];
                        if (!permitted.Contains(curr_dec))
                        {
                            Console.WriteLine("The argument `" + curr_dec + "' was not recognized.");
                            return;
                        }
                    }
                    else//not new dec
                    {
                        if (args[i] == "=") continue;//allow -dec = arg
                        curr_dec_args.Add(args[i]);
                    }
                }
            }
            if (!string.IsNullOrEmpty(curr_dec))
                parsed_args.Add(curr_dec, curr_dec_args.ToArray());

            if (ishelp)
            {
                Console.Write(
@"altvisngs Version: 1.0.0
Date: 14 JUN 2016
Nicholas M. Guho
https://github.com/nguho/altvisngs

Recognized commands:
-help:          Display help for altvisngs
-sub:           Subroutine(s):
    all: run all subroutines
    initialize: initialize the dataset
    taxatables: generate the raw tables
    taxabars: generate the taxonomic hierarchy bars
    autotables: generate the tabulated data
    heatmaps: generate the heatmaps
    summarybars: generate the summary bar plots
    clusters: generate the cluster analysis
    cluster_dissmats: generate the dissimilarity matrices
    diversity: generate the diversity tables
-group_attr:    Declare the group attribute
***incomplete***
");
                return;
            }           

            string _key_ID = "Name";
            string minor_phylotypes = @"Minor phylotypes";
            string not_detected = @"N. D.";
            string _unknown = "unknown";
            string _wild = @"\textasteriskcentered";
            string[] _unknowns = new string[] { _unknown, string.Empty, null };
            string[] _taxa_names = new string[] { "domain", "phylum", "class", "order", "family", "genus" };

            string[] _subroutines = parsed_args["-sub"];
            if (_subroutines.Length == 0) return;//nothing to do

            if(_subroutines.Contains("all"))
                if (_subroutines.Length != 1)
                {
                    Console.WriteLine("If `all' is selected as a subroutine, it must be alone");
                    return;
                }
                else
                    _subroutines = new string[] {"initialize", "taxatables", "taxabars", "autotables", "heatmaps", "summarybars", "clusters", "cluster_dissmats", "diversity"};
            if(_subroutines.Contains("initialize"))
                if(_subroutines[0] != "initialize" || _subroutines.Count((s)=>(s=="initialize"))!=1)
                {
                    Console.WriteLine("The `initialize' subroutine must be the first in a sequence and can only be used once.");
                    return;
                }

            string _group_attr = ((parsed_args.ContainsKey("-group_attr"))?(parsed_args["-group_attr"][0]):(""));//"Reactor";
            string _group_attr_lower = ((parsed_args.ContainsKey("-group_attr_lower")) ? (parsed_args["-group_attr_lower"][0]) : (""));//"system";
            if (string.IsNullOrEmpty(_group_attr_lower) && !string.IsNullOrEmpty(_group_attr))
                _group_attr_lower = char.ToLower(_group_attr[0]) + _group_attr.Substring(1);
            string[] _groups = ((parsed_args.ContainsKey("-groups")) ? (parsed_args["-groups"]) : (null));//new string[] { "S-EBPR", "V-EBPR", "G-EBPR", "R-EBPR", "Moscow WRRF" };
            string _subgroup_attr = ((parsed_args.ContainsKey("-subgroup_attr")) ? (parsed_args["-subgroup_attr"][0]) : (""));//"Operational day";
            string _subgroup_attr_lower = ((parsed_args.ContainsKey("-subgroup_attr_lower")) ? (parsed_args["-subgroup_attr_lower"][0]) : (""));//"operational day";
            if (string.IsNullOrEmpty(_subgroup_attr_lower) && !string.IsNullOrEmpty(_subgroup_attr))
                _subgroup_attr_lower = char.ToLower(_subgroup_attr[0]) + _subgroup_attr.Substring(1);
            string[] primers = parsed_args["-primers"];

            for (int i = 0; i < primers.Length; i++)//Directories within the working directory
            {
                string input_directory = primers[i];//directory w/in the working directory
                string filepath_abundance = input_directory + @"\table.abundance.txt";
                string filepath_proportions = input_directory + @"\table.proportions.txt";
                string filepath_taxainfo = input_directory + @"\table.taxa_info.txt";
                string filepath_key = input_directory + @"\_key.csv";
                string primer = primers[i];// input_directory.Substring(input_directory.LastIndexOf(@"\") + 1);//primer from the folder name

                string[] sample_properties = altvisngs_data.AttributesInKey(filepath_key);//available properties for samples.

                Sample[] all_avail = null;
                Sample[] all_reactors = null;
                Sample[][] grouped = null;//<=== 1st dim = reactor, 2nd dim = samples in that reactor

                if (_subroutines[0] == "initialize")
                {
                    altvisngs_initialize.Initialize_dbcAmplicons(input_directory, filepath_key, filepath_abundance, filepath_proportions, filepath_taxainfo, ",", _key_ID);
                    if (_subroutines.Length == 1) continue;//done w/ this primer
                }

                all_avail = altvisngs_data.OpenSamples(filepath_key, _key_ID, null);
                all_reactors = altvisngs_data.SamplesSatisfying_Unsorted(all_avail, (s) => (_groups.Contains(s.GetAttr(_group_attr))));
                grouped = altvisngs_data.GroupedSamplesSatisfying_Unsorted(all_avail, SampleKeyFilter.MultipleNames(_group_attr, _groups));

                for (int g = ((_subroutines[0] == "initialize") ? (1) : (0)); g < _subroutines.Length; g++)
                    switch (_subroutines[g])
                    {
                        //case ("initialize")://initialize the results (must be called before any of the following)
                        //    altvisngs_initialize.Initialize_dbcAmplicons(input_directory, filepath_key, filepath_abundance, filepath_proportions, filepath_taxainfo, ",", _key_ID);
                        //    break;
                        case ("taxatables")://generate the "correct" tables (both individually as .txt files and collectively as .xlsx files) from dbcAmplicons output (called AFTER initialize)
                            Excel.Application xlApp = new Excel.Application();
                            xlApp.DisplayAlerts = false;
                            Excel.Workbooks xlAppWBs = xlApp.Workbooks;

                            xlApp.Visible = false;
                            xlApp.ScreenUpdating = false;
                            xlApp.EnableEvents = false;

                            Excel.Workbook wbAbund = xlAppWBs.Add(Excel.XlWBATemplate.xlWBATWorksheet);
                            Excel.Workbook wbRelAbund = xlAppWBs.Add(Excel.XlWBATemplate.xlWBATWorksheet);
                            Excel.Worksheet ws = null;

                            object[,] data;
                            List<string> taxaranks = new List<string>();
                            for (int j = 0; j < 6; j++)
                            {
                                string rank = altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(j);
                                taxaranks.Add(rank);
                                data = altvisngs_taxaranktbl.TaxaRankTable_Abundance(
                                    filepath_key,
                                    input_directory + "\\" + "table.abundance." + rank + ".txt",
                                    all_avail,
                                    j,
                                    taxaranks.ToArray(),
                                    new string[] { _group_attr, _subgroup_attr },
                                    new Type[] { typeof(string), typeof(DateTime), typeof(int) },
                                    ('\t').ToString(),
                                    _key_ID, _unknown, _unknowns);

                                Console.WriteLine("Creating Excel Worksheet `" + taxaranks[j] + "'.");
                                if (j != 0) wbAbund.Worksheets.Add(Type.Missing, wbAbund.Worksheets[wbAbund.Worksheets.Count]);
                                ws = (Excel.Worksheet)wbAbund.Worksheets[wbAbund.Worksheets.Count];
                                ws.Name = taxaranks[j];
                                Excel.Range rng = ws.Range[ws.Cells[1, 1], ws.Cells[data.GetLength(0), data.GetLength(1)]];
                                rng.Value = data;
                                Marshal.ReleaseComObject(rng);
                                rng = null;

                                data = altvisngs_taxaranktbl.TaxaRankTable_RelativeAbundance(
                                    filepath_key,
                                    input_directory + "\\" + "table.proportions." + altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(j) + ".txt",
                                    all_avail,
                                    j,
                                    taxaranks.ToArray(),
                                    new string[] { _group_attr, _subgroup_attr },
                                    new Type[] { typeof(string), typeof(DateTime), typeof(int) },
                                    ('\t').ToString(),
                                    _key_ID, _unknown, _unknowns);

                                Console.WriteLine("Creating Excel Worksheet `" + taxaranks[j] + "'.");
                                if (j != 0) wbRelAbund.Worksheets.Add(Type.Missing, wbRelAbund.Worksheets[wbRelAbund.Worksheets.Count]);
                                ws = (Excel.Worksheet)wbRelAbund.Worksheets[wbRelAbund.Worksheets.Count];
                                ws.Name = taxaranks[j];
                                rng = ws.Range[ws.Cells[1, 1], ws.Cells[data.GetLength(0), data.GetLength(1)]];
                                rng.Value = data;
                                Marshal.ReleaseComObject(rng);
                                rng = null;
                            }
                            wbAbund.Close(SaveChanges: true, Filename: Directory.GetCurrentDirectory() + "\\" + input_directory + "\\" + "table.abundance.ranks.xlsx");
                            Marshal.ReleaseComObject(wbAbund);
                            wbAbund = null;

                            wbRelAbund.Close(SaveChanges: true, Filename: Directory.GetCurrentDirectory() + "\\" + input_directory + "\\" + "table.proportions.ranks.xlsx");
                            Marshal.ReleaseComObject(wbRelAbund);
                            wbRelAbund = null;

                            Marshal.ReleaseComObject(ws);
                            ws = null;
                            Marshal.ReleaseComObject(xlAppWBs);
                            xlAppWBs = null;
                            Marshal.ReleaseComObject(xlApp);
                            xlApp = null;
                            GC.Collect();
                            break;
                        case ("taxabars"):
                            /* Option to create the tabulated summary within sample groups and between seample groups
                             * Verified 16.05.23
                             */
                            List<string> reactors_with_unpositioned_labels = new List<string>();
                            string not_identified_abbr = "No ID";
                            string minor_abbr = "Minor";
                            for (int k = 0; k < grouped.Length; k++)//each reactor
                            {
                                List<TaxonObservation[]> observ = new List<TaxonObservation[]>();
                                bool un_positioned_labels;
                                for (int l = 0; l < grouped[k].Length; l++)//for each op-day
                                {
                                    observ.Add(grouped[k][l].TaxonObservations);
                                    un_positioned_labels = false;
                                    string subgroup = grouped[k][l].GetAttr(_subgroup_attr);
                                    altvisngs_taxabar.TaxaBar(
                                        filepath_output: input_directory + (@"\_taxabar_" + _groups[k] + "_" + subgroup + ".tex").Replace(" ", ""),
                                        taxon_observations: grouped[k][l].TaxonObservations,
                                        figure_title: _groups[k] + " " + _subgroup_attr + " " + subgroup,
                                        taxa_levels: 6,
                                        TaxaNames: _taxa_names,
                                        minor_cutoff: 0.01d,
                                        figure_label: @"fig:" + "taxabar." + primer + "." + _groups[k] + "." + grouped[k][l].GetAttr(_subgroup_attr) + "",
                                        caption_mandatory: @"Relative abundance and taxonomic classification of the 16S rRNA gene sequencing " + primer + " primer set results for " + _groups[k] + " " + _subgroup_attr_lower + " " + subgroup + ". " +
                                            @"Phylotypes which were not identified or those whose identification at a specific taxonomic level did not exceed the bootstrap threshold were aggregated, denoted ``" + not_identified_abbr + @"'', and depicted in {\color{red}red}. " +
                                            @"Identified phylotypes with less than \SI{1}{\percent} of the total relative abundance were aggregated, denoted ``" + minor_abbr + @"'', and depicted in {\color{gray}gray}. " +
                                            @"Phylotypes with at least \SI{1}{\percent} relative abundance are labeled. " +
                                            @"To conserve space, \textit{incertae sedis}, when present in taxon names, has been abbreviated as \textit{inc.\@ sed.\@} with the original capitalization and interceding characters preserved.",//primer + " primer set
                                        figure_pdf_relative_path: primer + @"/",
                                        caption_optional: "Relative abundance and taxonomic classification of " + _groups[k] + " " + subgroup + " with the " + primer + " primer set",
                                        refresh_tex_measures: true,
                                        un_positioned_labels: out un_positioned_labels,
                                        min_relabund_to_show_label: 0.01,
                                        scale_mult_cm: 13d,
                                        row_offset_cm: 0.4,
                                        sample_label_x_cm: -1.5,
                                        permitted_right_overhang_cm: 0.75d,
                                        minor_noid_lbl_wid_cm: 1.25,
                                        bar_ht_cm: 0.375,
                                        min_Del_X_cm: 0.125,
                                        max_leader_length_cm: 2d,
                                        label_min_sep_cm: 0.125,
                                        leader_buffer_cm: 0.05,
                                        max_extra_rows: 3,
                                        row_Penalty_cm: 10d,
                                        label_cover_max: 8,
                                        not_identified_abbr: not_identified_abbr,
                                        minor_abbr: minor_abbr,
                                        unknown: _unknown);
                                    if (un_positioned_labels)
                                        reactors_with_unpositioned_labels.Add(grouped[k][l].GetAttr(_key_ID) + ":" + _groups[k] + " " + grouped[k][l].GetAttr(_subgroup_attr));
                                }
                                //Summary for this reactor
                                //1. summarize into one list of taxon
                                Dictionary<Taxon, double[]> taxons = new Dictionary<Taxon, double[]>();
                                for (int l = 0; l < observ.Count; l++)
                                    for (int m = 0; m < observ[l].Length; m++)
                                    {
                                        if (!taxons.ContainsKey(observ[l][m].Taxon)) taxons.Add(observ[l][m].Taxon, new double[observ.Count]);
                                        taxons[observ[l][m].Taxon][l] = observ[l][m].Observation.RelativeAbundance;
                                    }
                                List<TaxonObservation> taxon_obs = new List<TaxonObservation>();
                                foreach (KeyValuePair<Taxon, double[]> kvp in taxons)
                                    taxon_obs.Add(new TaxonObservation(kvp.Key, new Observation(altvisngs_relabundtbl._Mean(kvp.Value), int.MaxValue)));

                                un_positioned_labels = false;
                                altvisngs_taxabar.TaxaBar(
                                    filepath_output: input_directory + (@"\_taxabar_" + _groups[k] + "_summary.tex").Replace(" ", ""),
                                    taxon_observations: taxon_obs.ToArray(),
                                    figure_title: _groups[k] + " average",
                                    taxa_levels: 6,
                                    TaxaNames: _taxa_names,
                                    minor_cutoff: 0.01d,
                                    figure_label: @"fig:" + "taxabar." + primer + "." + _groups[k] + ".summary",
                                    caption_mandatory: @"Average relative abundance and taxonomic classification of the 16S rRNA gene sequencing " + primer + " primer set results for " + _groups[k] + ". " +
                                        @"Phylotypes which were not identified or those whose identification at a specific taxonomic level did not exceed the bootstrap threshold were aggregated, denoted ``" + not_identified_abbr + @"'', and depicted in {\color{red}red}. " +
                                        @"Identified phylotypes less than \SI{1}{\percent} of the average total relative abundance were aggregated, denoted ``" + minor_abbr + @"'', and depicted in {\color{gray}gray}. " +
                                        @"Phylotypes with at least \SI{1}{\percent} average relative abundance are labeled. " +
                                        @"To conserve space, \textit{incertae sedis}, when present in taxon names, has been abbreviated as \textit{inc.\@ sed.\@} with the original capitalization and interceding characters preserved.",//primer + " primer set
                                    figure_pdf_relative_path: primer + @"/",
                                    caption_optional: "Average relative abundance and taxonomic classification of " + _groups[k] + " with the " + primer + " primer set",
                                    refresh_tex_measures: true,
                                    un_positioned_labels: out un_positioned_labels,
                                    min_relabund_to_show_label: 0.01,
                                    scale_mult_cm: 13d,
                                    row_offset_cm: 0.4,
                                    sample_label_x_cm: -1.5,
                                    permitted_right_overhang_cm: 0.75d,
                                    minor_noid_lbl_wid_cm: 1.25,
                                    bar_ht_cm: 0.375,
                                    min_Del_X_cm: 0.125,
                                    max_leader_length_cm: 2d,
                                    label_min_sep_cm: 0.125,
                                    leader_buffer_cm: 0.05,
                                    max_extra_rows: 3,
                                    row_Penalty_cm: 10d,
                                    label_cover_max: 8,
                                    not_identified_abbr: not_identified_abbr,
                                    minor_abbr: minor_abbr,
                                    unknown: _unknown);
                                if (un_positioned_labels)
                                    reactors_with_unpositioned_labels.Add(_groups[k] + " average");
                            }
                            if (reactors_with_unpositioned_labels.Count != 0)
                            {
                                Console.WriteLine("Taxabars with unpositioned labels (see `_taxabar_unpositionedlabels.txt'):");
                                using (StreamWriter sw = new StreamWriter(input_directory + @"\_taxabar_unpositionedlabels.txt"))
                                {
                                    sw.WriteLine("Run ending: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString());
                                    for (int j = 0; j < reactors_with_unpositioned_labels.Count; j++)
                                    {
                                        Console.WriteLine(reactors_with_unpositioned_labels[j]);
                                        sw.WriteLine(reactors_with_unpositioned_labels[j]);
                                    }
                                }
                            }
                            break;
                        case ("autotables"):
                            /* Option to create the tabulated summary within sample groups and between seample groups
                             * Verified 16.05.23
                             */
                            for (int k = 0; k < 6; k++)//domain to family.
                            {
                                string level = altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(k);
                                //individual reactor table
                                for (int l = 0; l < _groups.Length; l++)
                                {
                                    string reactor = _groups[l];
                                    altvisngs_relabundtbl.AutoTable(
                                        input_directory,
                                        ("_relabundtbl_" + reactor + "_" + level).Replace(" ", ""),
                                        new Sample[][] { grouped[l] },
                                        (s) => ("Day " + s.GetAttr(_subgroup_attr)), null,
                                        k,
                                        "tbl:" + reactor + "." + primer + "." + level,
                                        reactor + " " + level + "-level relative abundance summary by " + _subgroup_attr_lower + " of the 16S rRNA gene sequencing results with the " + primer + " primer set.",
                                        reactor + " " + level + "-level " + primer + " results summary.",
                                        _unknown, _unknowns);
                                }
                                //summary table of reactors at this rank
                                altvisngs_relabundtbl.AutoTable(
                                    input_directory,
                                    "_relabundtbl_summary_" + level,
                                    grouped,
                                    (s) => (s.GetAttr(_group_attr)),
                                    (s) => (@" (\ref{tbl:" + s.GetAttr(_group_attr) + "." + primer + "." + level + "})"),
                                    k,
                                    "tbl:summary." + primer + "." + level,
                                    "Summary of" + " " + level + "-level relative abundance by " + _group_attr_lower + " of the 16S rRNA gene sequencing results with the " + primer + " primer set.",
                                    "Summary of" + " " + level + "-level " + primer + " results.",
                                    _unknown, _unknowns);
                            }
                            break;
                        case ("heatmaps"):
                            /* Option to create a summary heat map from the grouped samples
                             * Verified 16.05.23
                             */
                            CubeHelix cubehe = new CubeHelix();
                            cubehe.IsReversed = true;
                            cubehe.StartFraction = 0.1;
                            cubehe.EndFraction = 0.1;
                            for (int k = 0; k < 6; k++)//domain to genus
                            {
                                string level = altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(k);

                                //Build the option variants (different types of heatmaps run)
                                string[] opt_filepaths = new string[] {
                                //none
                                input_directory + @"\_heatmap_" + level + ".tex" ,
                                //anysamplegeq1
                                input_directory + @"\_heatmap_" + level + "_anysamplegeq1.tex",
                                //anysamplegeq1
                                input_directory + @"\_heatmap_" + level + "_anysamplegeq25.tex",
                                //anysamplegeq5
                                input_directory + @"\_heatmap_" + level + "_anysamplegeq5.tex"
                            };
                                string reltbls = string.Empty;//\cref string for all of the tables at this level and primer set
                                for (int l = 0; l < _groups.Length; l++)
                                    reltbls += "tbl:" + _groups[l] + "." + primer + "." + level + ((l < _groups.Length - 1) ? (",") : (""));
                                string caption_base =
                                    @"Heat map showing the relative abundance of phylotypes identified at the " + level + @" level using the " + primer + @" primer set (see \cref{" + reltbls + "}). " +
                                    @"Samples are organized by " + _group_attr_lower + " and " + _subgroup_attr_lower + " and the phylotypes sorted according to taxonomic hierarchy. " +
                                    @"Unidentified phylotypes (denoted as ``" + _unknown + @"'') were aggregated by the most specific taxonomic level at which they were identified (the ``" + _wild + @"'' is used to indicate any identified taxon name). " +
                                    @"Note that the adopted color scheme (based on \citet{Green11_Cubehelix}) is nonlinear at \SI{0}{\percent} relative abundance to differentiate phylotypes which were not detected (" + not_detected + @") in a sample from those that were. " +
                                    @"To conserve space, \textit{incertae sedis}, when present in taxon names, has been abbreviated as \textit{inc.\@ sed.\@} with the original capitalization and interceding characters preserved. ";

                                string[] opt_caption_mandatory = new string[] {
                                //none
                                caption_base,
                                //anysamplegeq1
                                caption_base +
                                @"Any identified phylotypes with less than \SI{1}{\percent} relative abundance in all samples were aggregated and denoted ``" + minor_phylotypes + @"'' at the bottom of the heat map. " +
                                @"The number of phylotypes categorized accordingly is indicated in the parenthesis following ``" + minor_phylotypes + @"''. " +
                                @"If no phylotypes were so categorized, the line has been omitted.",
                                //anysamplegeq25
                                caption_base +
                                @"Any identified phylotypes with less than \SI{2.5}{\percent} relative abundance in all samples were aggregated and denoted ``" + minor_phylotypes + @"'' at the bottom of the heat map. " +
                                @"The number of phylotypes categorized accordingly is indicated in the parenthesis following ``" + minor_phylotypes + @"''. " +
                                @"If no phylotypes were so categorized, the line has been omitted.",
                                //anysamplegeq5
                                caption_base +
                                @"Any identified phylotypes with less than \SI{5}{\percent} relative abundance in all samples were aggregated and denoted ``" + minor_phylotypes + @"'' at the bottom of the heat map. " +
                                @"The number of phylotypes categorized accordingly is indicated in the parenthesis following ``" + minor_phylotypes + @"''. " +
                                @"If no phylotypes were so categorized, the line has been omitted."
                            };
                                string[] opt_label = new string[] {
                                //none
                                "fig:" + primer + "." + level + ".heat",
                                //anysamplegeq1
                                "fig:" + primer + "." + level + ".heat_anysamplegeq1",
                                //anysamplegeq25
                                "fig:" + primer + "." + level + ".heat_anysamplegeq25",
                                //anysamplegeq5
                                "fig:" + primer + "." + level + ".heat_anysamplegeq5"
                            };
                                MajorMinorCutoff[] opt_majorminorcutoff = new MajorMinorCutoff[] {
                                //none
                                MajorMinorCutoff.None_AllMajor,
                                //anysamplegeq1
                                MajorMinorCutoff.AnyGroup,
                                //anysamplegeq1
                                MajorMinorCutoff.AnyGroup,
                                //anysamplegeq5
                                MajorMinorCutoff.AnyGroup
                            };
                                double[] opt_minorcutoff = new double[] {
                                //none
                                0d,
                                //anysamplegeq1
                                0.01d,
                                //anysamplegeq25
                                0.025d,
                                //anysamplegeq1
                                0.05d
                            };
                                string[] opt_optionalcaption = new string[] {
                                //none
                                primer + " primer set heat map at the " + level + @" level",
                                //anysamplegeq1
                                primer + " primer set heat map at the " + level + @" level, $\geq 1\,\%$ in any sample",
                                //anysamplegeq25
                                primer + " primer set heat map at the " + level + @" level, $\geq 2.5\,\%$ in any sample",
                                //anysamplegeq5
                                primer + " primer set heat map at the " + level + @" level, $\geq 5\,\%$ in any sample"
                            };

                                //run the variants
                                for (int l = 0; l < opt_filepaths.Length; l++)
                                    altvisngs_heatmap.Heatmap(
                                        filePath: opt_filepaths[l],
                                        relative_filepath_prefix: primer + @"/",
                                        grouped_samples: grouped,
                                        legend_entries: _groups,
                                        taxa_level: k,
                                        heading_attr: _subgroup_attr,
                                        colorscheme: cubehe,
                                        format_taxon_string_at_level: (s) => altvisngs_relabundtbl.FormatTaxaString_itall(s, string.Empty, true, minor_phylotypes, _unknown, _unknown, _wild, "*"),
                                        row_offset_cm: 0.4,
                                        cell_width_cm: 0.4,
                                        caption_mandatory: opt_caption_mandatory[l],
                                        figure_label: opt_label[l],
                                        cutoff_criteria: opt_majorminorcutoff[l],
                                        include_group_means: true,
                                        minor_cutoff: opt_minorcutoff[l],
                                        caption_optional: opt_optionalcaption[l],
                                        minor_phylotypes: minor_phylotypes,
                                        not_detected: not_detected,
                                        unknown: _unknown,
                                        unknowns: _unknowns);
                            }
                            break;
                        case ("summarybars"):
                            /* Option to create a summary heat map from the grouped samples
                             * Verified 16.05.23
                             */
                            for (int k = 0; k < 6; k++)//domain to genus.
                            {
                                string level = altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(k);

                                //Build the option variants (different types of heatmaps run)
                                string[] opt_filepaths = new string[] {
                                //none
                                input_directory + @"\_summarybar_" + level + ".tex" ,
                                //anysamplegeq1
                                input_directory + @"\_summarybar_" + level + "_anysamplegeq1.tex",
                                //anysamplegeq5
                                input_directory + @"\_summarybar_" + level + "_anysamplegeq5.tex"
                            };

                                string[] opt_caption_mandatory = new string[] {
                                //none
                                @"Mean relative abundance for phylotypes identified using the " + primer + " primer set at the " + level + @" level (see \cref{tbl:summary." + primer + "." + level + "}). " +
                                //@"Phylotypes with less than \SI{1}{\percent} in all of the samples were lumped into ``" + minor_phylotypes + @"'' at the right hand side of the plot. " +
                                //@"The number of phylotypes categorized accordingly is indicated in the parenthesis following ``" + minor_phylotypes + @"''. If no phylotypes were so categorized, the category has been omitted. " +
                                @"Phylotypes which were not identified at the " + level + " level are indicated by ``" + _unknown + "'' followed by the most specific identified taxonomic level (if available) in parenthesis (where d:~domain, p:~phylum, c:~class, o:~order, f:~family). " +
                                @"Note that the bars for phylotypes which were not detected in a given " + _group_attr_lower + @" (i.e., \SI{0}{\percent} relative abundance in all of the associated samples) are omitted. " +
                                @"Error bars indicate the sample standard deviation.",
                                //anysamplegeq1
                                @"Mean relative abundance for phylotypes identified using the " + primer + " primer set at the " + level + @" level (see \cref{tbl:summary." + primer + "." + level + "}). " +
                                @"Phylotypes with less than \SI{1}{\percent} in all of the samples were lumped into ``" + minor_phylotypes + @"'' at the right hand side of the plot. " +
                                @"The number of phylotypes categorized accordingly is indicated in the parenthesis following ``" + minor_phylotypes + @"''. If no phylotypes were so categorized, the category has been omitted. " +
                                @"Non-minor phylotypes which were not identified at the " + level + " level are indicated by ``" + _unknown + "'' followed by the most specific identified taxonomic level (if available) in parenthesis (where d:~domain, p:~phylum, c:~class, o:~order, f:~family). " +
                                @"Note that the bars for phylotypes which were not detected in a given " + _group_attr_lower + @" (i.e., \SI{0}{\percent} relative abundance in all of the associated samples) are omitted. " +
                                @"Error bars indicate the sample standard deviation.",
                                //anysamplegeq5
                                @"Mean relative abundance for phylotypes identified using the " + primer + " primer set at the " + level + @" level (see \cref{tbl:summary." + primer + "." + level + "}). " +
                                @"Phylotypes with less than \SI{5}{\percent} in all of the samples were lumped into ``" + minor_phylotypes + @"'' at the right hand side of the plot. " +
                                @"The number of phylotypes categorized accordingly is indicated in the parenthesis following ``" + minor_phylotypes + @"''. If no phylotypes were so categorized, the category has been omitted. " +
                                @"Non-minor phylotypes which were not identified at the " + level + " level are indicated by ``" + _unknown + "'' followed by the most specific identified taxonomic level (if available) in parenthesis (where d:~domain, p:~phylum, c:~class, o:~order, f:~family). " +
                                @"Note that the bars for phylotypes which were not detected in a given " + _group_attr_lower + @" (i.e., \SI{0}{\percent} relative abundance in all of the associated samples) are omitted. " +
                                @"Error bars indicate the sample standard deviation."
                            };
                                string[] opt_label = new string[] {
                                //none
                                "fig:" + primer + "." + level + ".summarybar",
                                //anysamplegeq1
                                "fig:" + primer + "." + level + ".summarybar_anysamplegeq1",
                                //anysamplegeq5
                                "fig:" + primer + "." + level + ".summarybar_anysamplegeq5"
                            };
                                MajorMinorCutoff[] opt_majorminorcutoff = new MajorMinorCutoff[] {
                                //none
                                MajorMinorCutoff.None_AllMajor,
                                //anysamplegeq1
                                MajorMinorCutoff.AnyGroup,
                                //anysamplegeq5
                                MajorMinorCutoff.AnyGroup
                            };
                                double[] opt_minorcutoff = new double[] {
                                //none
                                0d,
                                //anysamplegeq1
                                0.01d,
                                //anysamplegeq1
                                0.05d
                            };
                                string[] opt_optionalcaption = new string[] {
                                //none
                                primer + " primer set mean relative abundance at the " + level + @" level",
                                //anysamplegeq1
                                primer + " primer set mean relative abundance at the " + level + @" level, $\geq 1\,\%$ in any sample",
                                //anysamplegeq5
                                primer + " primer set mean relative abundance at the " + level + @" level, $\geq 5\,\%$ in any sample"
                            };

                                //run the variants
                                for (int l = 0; l < opt_filepaths.Length; l++)
                                    altvisngs_sidebar.SideBarMeanRelativeAbundance(
                                        filepath_output: opt_filepaths[l],
                                        grouped_samples: grouped,
                                        legend_entries: _groups,
                                        taxa_level: k,
                                        cutoff_criteria: opt_majorminorcutoff[l],
                                        minimum_relabund_major: opt_minorcutoff[l],
                                        caption_mandatory: opt_caption_mandatory[l],
                                        figure_label: opt_label[l],
                                        relative_filepath_prefix: primer + @"/",
                                        caption_optional: opt_optionalcaption[l],
                                        minor_phylotypes: minor_phylotypes,
                                        unknown: _unknown,
                                        unknowns: _unknowns);
                            }
                            break;
                        case ("clusters"):
                            /* Option to perform hierarchical cluster analysis on the grouped samples
                             * Verified 16.05.23
                             */
                            for (int k = 0; k < 6; k++)//domain to genus.
                            {
                                string level = altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(k);

                                //Build the option variants
                                string[] opt_filepaths = new string[] {
                                //none
                                input_directory + @"\_cluster_" + level + ".tex"
                            };

                                string[] opt_caption_mandatory = new string[] {
                                @"Hierarchical agglomerative cluster analysis of the samples with phylotypes identified using the " + primer + @" primer set at the " + level + @" level. " + 
                                @"Sample dissimiliarity was quantified according to Bray-Curtis " + 
                                    @"\citep[given by $1 - 2\cdot\frac{\sum_{i=0}^{n} {\min(A_i, B_i)}}{\sum_{i=0}^{n}{A_i + B_i}}$ where $A_i$ and $B_i$ are the number of joined sequences assigned to phylotype $i$ in samples $A$ and $B$, repsectively, and $n$ is the total number of phylotypes][]{Bray57}. " + 
                                @"The average linkage method was used to define the dissimilarity between clusters."
                            };
                                string[] opt_label = new string[] {
                                //none
                                "fig:" + primer + "." + level + ".cluster"
                            };
                                string[] opt_optionalcaption = new string[] {
                                //none
                                primer + " primer set cluster analysis at the " + level + @" level"
                            };

                                //run the variants
                                for (int l = 0; l < opt_filepaths.Length; l++)
                                    altvisngs_cluster.ClusterAnalysis(
                                        filepath_key: filepath_key,
                                        filePath_output: input_directory + @"\cluster_" + altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(k) + ".tex",
                                        samples: all_reactors,
                                        groups: _groups,
                                        color_on_attr: _group_attr,
                                        level: k,
                                        labeltext: (s) => (s.GetAttr(_group_attr) + " " + s.GetAttr(_subgroup_attr)),
                                        caption_mandatory: opt_caption_mandatory[l],
                                        figure_label: opt_label[l],
                                        relative_filepath_prefix: primer + @"/",
                                        caption_optional: opt_optionalcaption[l],
                                        distmeth: DistanceMethod.BrayCurtis,
                                        linkmeth: LinkageMethod.AverageLinkage,
                                        beta: double.NaN,
                                        ID: _key_ID);
                            }
                            break;
                        case ("cluster_dissmats"):
                            /* Option to get the table with the dissimilarity matrix from the hierarchical cluster analysis on the grouped samples
                             * Verified 16.05.23
                             */
                            for (int j = 0; j < 6; j++)//domain to genus
                            {
                                string level = altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(j);
                                altvisngs_cluster.DissimilarityMatrix(
                                    filePath_output: input_directory + @"\_cluster_dissmat_" + altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(j) + ".tex",
                                    grouped_samples: grouped,
                                    attrvals_dim_0: _groups,
                                    attrname_dim_1: _subgroup_attr,
                                    distanceMethod: DistanceMethod.BrayCurtis,
                                    linkageMethod: LinkageMethod.AverageLinkage,
                                    mandatory_caption: "Bray-Curtis dissimilarity matrix for samples with phylotypes identified using the " + primer + " primer set at the " + level + " level, grouped by " + _group_attr_lower + " and " + _subgroup_attr_lower + ". " +
                                        @"Dissimilarity values have been multiplied by 100 to yield the percent dissimilarity.",
                                    optional_caption: "Bray-Curtis dissimilarity matrix, " + primer + " primer set, " + level + " level",
                                    label: "tbl:dissmat:" + primer + ":" + altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(j),
                                    beta: double.NaN);
                            }
                            break;
                        case ("diversity")://make a diversity table
                            string label = "tbl:" + primer + ":diversity";
                            Dictionary<string, string> table_footnotes = new Dictionary<string, string>();
                            table_footnotes.Add("tblfoot:" + label + @".Major", @"Major phylotypes constitute at least 1\% of the total relative abundance.");
                            table_footnotes.Add("tblfoot:" + label + @".Minor", @"Minor phylotypes constitute less than 1\% of the total relative abundance.");
                            table_footnotes.Add("tblfoot:" + label + @".Shannon", @"Shannon diversity index, given by $H'=-\sum\limits_{i=1}^S\left(p_i\cdot\ln p_i\right)$ where $S$ is the total number of phylotypes and $p_i$ is the relative abundance of the \ith{} phylotype \citep{Shannon48}.");
                            table_footnotes.Add("tblfoot:" + label + @".Pielou", @"Pielou evenness index, given by $R = \dfrac{H'}{\ln S}$ where $H'$ is the Shannon diversity index and $S$ is the total number of phylotypes.");
                            table_footnotes.Add("tblfoot:" + label + @".SimpsonD", @"Simpson's diversity index, given by $D = 1-\sum\limits_{i=1}^S{p_{i}^{2}}$ where $S$ is the total number of phylotypes and $p_i$ is the relative abundance of the \ith{} phylotype \citep{Simpson49}.");
                            table_footnotes.Add("tblfoot:" + label + @".Chao1", @"Bias-corrected Chao1 richness estimate, given by $S_{\text{Chao1}} = S + \dfrac{n+1}{n}\cdot\dfrac{F_{1}\cdot(F_{1} -1)}{2\cdot(F_{2} + 1)}$ where $S$ is the total number of phylotypes, $n$ is the total number of sequences, $F_{1}$ is the total number of phylotypes to which only one sequence was assigned, and $F_{2}$ is the total number of phylotypes to which only two sequences were assigned.");
                            altvisngs_diversity.DiversityTable(
                                filepath_output: input_directory + @"\diversity.tex",
                                table_mandatory_caption: "Sample diversity and evenness indices and richness estimates using the " + primer + " primer set.",
                                table_optional_caption: "Sample diversity and evenness indices and richness estimates using the " + primer + " primer set.",
                                table_label: label,
                                grouped_samples: grouped,
                                group_headings: _groups,
                                tabular_column_types: new string[] { "c", "S", "S", "S", "S", "S", "S", "S", "S" },
                                table_heading: new string[][] {
                                    new string[] { _subgroup_attr, "Total", "Total", @"Major\ref{tblfoot:" + label + ".Major}", @"Minor\ref{tblfoot:" + label + ".Minor}", @"$H'$\ref{tblfoot:" + label + ".Shannon}", @"$R$\ref{tblfoot:" + label + ".Pielou}", @"$D$\ref{tblfoot:" + label + ".SimpsonD}", @"$S_{\text{Chao1}}$\ref{tblfoot:" + label + ".Chao1}" },
                                    new string[] { "", "reads", "phylotypes", "phylotypes", "phylotypes", "", "", "", "" } },
                                table_columns: new Func<Sample, string>[] {
                                (s) => (s.GetAttr(_subgroup_attr)),
                                (s) => altvisngs_diversity.TotalReads(s).ToString(),
                                (s) => altvisngs_diversity.TotalPhylotypes(s).ToString(),
                                (s) => altvisngs_diversity.TotalMajorPhylotypes(s, 0.01).ToString(),
                                (s) => altvisngs_diversity.TotalMinorPhylotypes(s, 0.01).ToString(),
                                (s) => altvisngs_diversity.ShannonDiversityIdx(s).ToString("#.00"),
                                (s) => altvisngs_diversity.PielouEvenness(s).ToString("#.00"),
                                (s) => altvisngs_diversity.SimpsonDiversityIdx(s).ToString("#.00"),
                                (s) => altvisngs_diversity.Chao1(s).ToString("#.00")},
                                table_footnotes: table_footnotes);
                            break;
                        default:
                            {
                                Console.WriteLine("Unrecognized subroutine: " + _subroutines[g]);
                                return;
                            }
                    }
            }
            return;
        }
    }
}
