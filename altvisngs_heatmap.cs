using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace altvisngs
{
    abstract class altvisngs_heatmap
    {
        #region Fields
        /// <summary> Required preamble for the heatmap </summary>
        private const string _heatmap_reqd_preamble =
@"
\usepackage[scaled]{helvet}
\renewcommand\familydefault{\sfdefault} 
\usepackage[T1]{fontenc}
\usepackage{sansmath}
\sansmath

\usepackage{tikz}
\usetikzlibrary{calc}
%%% Taxa Label and Line Styles %%%
\tikzstyle{taxalbl}=[anchor=mid west,fill=white,font=\footnotesize]%node style for taxa labels
\tikzstyle{minortaxalbl}=[anchor=mid east,font=\footnotesize]%node style for minor label
\tikzstyle{taxatblhrule}=[thin]%the style used for leaders
\tikzstyle{taxahead}=[font=\footnotesize\bfseries]%node style for the taxa headings
\tikzstyle{samplelbl}=[rotate=90,anchor=mid west,font=\footnotesize]%node style for the sample headings
\tikzstyle{meanboxline}=[thin]%the style used for the boxes surrounding the meansamples (drawn before samp
\tikzstyle{groupboxline}=[thick]%style of the box surrounding groups
\tikzstyle{groupheading}=[font=\footnotesize\bfseries]%node style for the group headings; Note: Also change style in rotateheadifreq!
\tikzstyle{colorbarlbl}=[anchor=mid west,inner sep=0pt,font=\footnotesize]%node style for color bar labels

%%% Dimensions %%%
\newlength{\thisLeftX}
\setlength{\thisLeftX}{0cm}
\newlength{\nextLeftX}
\newlength{\headingY}
\setlength{\headingY}{0cm}
\edef\colorscaleticklen{5pt}%The length of the ticks for the color bar
\edef\samplegroupsep{2pt}%Separation between sample groups

\newlength{\intwid}%the maximum width of an integer component
\newlength{\deltintwid}%the measured width of the integer

%%% Custom commands %%%
\makeatletter
	%%% Set \nextleftX to the maximum of it and the east side of the node passed in #1 %%%
	%%% Adapted from: http://tex.stackexchange.com/a/8761/89497 %%%
	\newcommand\updatenextleftX[1]{%
		\pgfextractx{\pgf@xa}{\pgfpointanchor{#1}{east}}% absolute x coord of the east of the node
		\pgfmathsetlength{\nextLeftX}{max(\nextLeftX,\pgf@xa)}%
		}
    \newcommand\updateheadingY[1]{%
        \pgfextracty{\pgf@xa}{\pgfpointanchor{#1}{east}}% absolute x coord of the east (north, presuming rotated) of the node
		\pgfmathsetlength{\headingY}{max(\headingY,\pgf@xa)}%
		}
	%%% rotate the heading if it won't fit horizontally %%%
	\newcommand\rotateheadifreq[2]{%
		\settowidth{\pgf@xa}{\pgfinterruptpicture\footnotesize\bfseries #1\endpgfinterruptpicture}
		\ifdim\pgf@xa>#2
			\node[groupheading, anchor=mid west, rotate=90, inner sep=0pt] at (\headX,\headingY + 5pt) {#1};		
		\else
			\node[groupheading, anchor=base, inner sep=0pt] at (\headX,\headingY + 5pt) {#1};
		\fi}
    %%% Used for RGB color = {} in nodes %%%
    \tikzset{RGB color/.code={\pgfutil@definecolor{.}{RGB}{#1}\tikzset{color=.}}}
\makeatother

%%% Patch to allow _ to be underscore in text mode. Requires font encoding to be T1. %%%
%%% Ref: egreg soln: http://tex.stackexchange.com/a/38720/89497 %%%
\catcode`_=12
\begingroup\lccode`~=`_\lowercase{\endgroup\let~\sb}
\mathcode`_=""8000";

        #endregion

        /// <summary> Method to produce a heatmap of the samples at the passed taxonomic level </summary>
        /// <param name="fileName"></param>
        /// <param name="samples"></param>
        /// <param name="colorscheme"></param>
        public static void Heatmap(string filePath,
            Sample[][] grouped_samples,
            string[] legend_entries,
            int taxa_level,
            string heading_attr,
            ColorScheme colorscheme,
            Func<string, string> format_taxon_string_at_level,
            double row_offset_cm,
            double cell_width_cm,
            string caption_mandatory,
            string figure_label,
            MajorMinorCutoff cutoff_criteria,
            bool include_group_means = true,
            double minor_cutoff = 0.01d,
            string relative_filepath_prefix = "Figures",
            string caption_optional = "",
            string minor_phylotypes = @"Minor phylotypes",
            string not_detected = "N. D.",
            string unknown="unknown",
            string document_class=@"\documentclass[10pt,border=1pt]{standalone}",
            string add_packages = @"",
            params string[] unknowns)
        {
            Console.WriteLine("Building heatmap `" + figure_label + "'...");
            int n_columns = 0;//the number of columns (sample + means) in the heatmap
            int n_samples = grouped_samples.Length;
            if (n_samples != legend_entries.Length) throw new ArgumentOutOfRangeException("legend-samples dimension mismatch");

            //1.   Build the summary dictionary and list of taxa
            //1.1. Get the dictionary and list of all available nonzero taxons (regardless of level)
            Dictionary<Taxon, double[][]> all_non_zero_taxons_at_lvl = new Dictionary<Taxon, double[][]>();//dims follow grouped_samples
            for (int i = 0; i < n_samples; i++)
                if (grouped_samples[i] != null)
                {
                    n_columns += grouped_samples[i].Length + 1;//the number of samples in this group plus one for the mean.
                    for (int j = 0; j < grouped_samples[i].Length; j++)
                    {
                        Taxon[] taxons = altvisngs_data.GetTaxonsAtLevel_Unsorted(altvisngs_data.GetAllTaxons_NonZero(grouped_samples[i][j]), taxa_level, unknown);
                        for (int k = 0; k < taxons.Length; k++)
                            if (!all_non_zero_taxons_at_lvl.ContainsKey(taxons[k]))
                                all_non_zero_taxons_at_lvl.Add(taxons[k], new double[n_samples][]);
                    }
                }

            //1.2. Go through each of the phylotypes and fill the relative abundance array for each of the samples
            //     Also, set the maximum fraction from the individual values
            double maxfrac = 0d;
            foreach (KeyValuePair<Taxon, double[][]> kvp in all_non_zero_taxons_at_lvl)
                for (int i = 0; i < n_samples; i++)
                    if (grouped_samples[i] == null) kvp.Value[i] = new double[0];
                    else
                    {
                        kvp.Value[i] = new double[grouped_samples[i].Length];
                        for (int j = 0; j < grouped_samples[i].Length; j++)
                        {
                            kvp.Value[i][j] = grouped_samples[i][j].SumAllMembersOf(kvp.Key, unknown, true).RelativeAbundance;
                            maxfrac = Math.Max(maxfrac, kvp.Value[i][j]);
                        }
                    }

            //2.   Consolidate the phylotypes based on unknown and minor
            //     Combine the unknowns at each taxonomic level together
            //2.1. Aggregate the unknowns
            //     Group such that unknowns at a level a grouped according to the last level at which they were known.
            //     Therefore, proceed through the hierarchy
            for (int i = 0; i <= taxa_level; i++)
            {
                string[] temp = new string[taxa_level + 1];
                for (int j = 0; j <= taxa_level; j++) temp[j] = (j < i) ? ("*") : (unknown);
                Taxon unknown_at_this_level = new Taxon(temp, unknown);
                List<Taxon> consolidated = new List<Taxon>();
                //consolidate all taxon in the dictionary that satisfy this unknown taxon
                foreach (KeyValuePair<Taxon, double[][]> kvp in all_non_zero_taxons_at_lvl)
                    if (kvp.Key.IsMemberOf(unknown_at_this_level, unknown, false))
                        consolidated.Add(kvp.Key);
                //build the consolidated relative abundance arrays and remove individuals from dictionary
                double[][] consol_unknowns = new double[n_samples][];
                for (int j = 0; j < n_samples; j++)
                    if (grouped_samples[j] == null) consol_unknowns[j] = new double[0];
                    else consol_unknowns[j] = new double[grouped_samples[j].Length];
                for (int j = 0; j < consolidated.Count; j++)
                {
                    for (int k = 0; k < n_samples; k++)
                        for (int l = 0; l < grouped_samples[k].Length; l++)
                        {
                            consol_unknowns[k][l] += all_non_zero_taxons_at_lvl[consolidated[j]][k][l];
                            maxfrac = Math.Max(maxfrac, consol_unknowns[k][l]);
                        }
                    all_non_zero_taxons_at_lvl.Remove(consolidated[j]);
                }
                //add the consolidated entry to the dictionary
                all_non_zero_taxons_at_lvl.Add(unknown_at_this_level, consol_unknowns);
            }
            //2.2.  Consolidate remaining minors
            List<Taxon> minors = new List<Taxon>();
            foreach (KeyValuePair<Taxon, double[][]> kvp in all_non_zero_taxons_at_lvl)
            {
                bool ismajor = false;
                switch (cutoff_criteria)
                {
                    case (MajorMinorCutoff.AnyGroup):
                        ismajor = AnyReactor_geq(kvp.Value, minor_cutoff);
                        break;
                    case (MajorMinorCutoff.AnyAverage):
                        throw new NotImplementedException("The MajorMinorCutoff `" + cutoff_criteria.ToString() + "' is not recognized.");
                        break;
                    case (MajorMinorCutoff.AllAverages):
                        throw new NotImplementedException("The MajorMinorCutoff `" + cutoff_criteria.ToString() + "' is not recognized.");
                        break;
                    case (MajorMinorCutoff.None_AllMajor):
                        ismajor = true;
                        break;
                    default:
                        throw new NotImplementedException("The MajorMinorCutoff `" + cutoff_criteria.ToString() + "' is not recognized.");
                }
                if (!ismajor) minors.Add(kvp.Key);
            }
            double[][] minorvals = new double[n_samples][];
            for (int i = 0; i < n_samples; i++)
                if (grouped_samples[i] == null) minorvals[i] = new double[0];
                else minorvals[i] = new double[grouped_samples[i].Length];
            for (int i = 0; i < minors.Count; i++)
            {
                for (int j = 0; j < n_samples; j++)
                    if (grouped_samples[j] != null)
                        for (int k = 0; k < grouped_samples[j].Length; k++)
                            minorvals[j][k] += all_non_zero_taxons_at_lvl[minors[i]][j][k];
                all_non_zero_taxons_at_lvl.Remove(minors[i]);
            }

            //2.3. Develop the sorted taxa list
            List<Taxon> taxa = new List<Taxon>(all_non_zero_taxons_at_lvl.Keys);//list of the non-zero taxa
            taxa.Sort((A, B) => altvisngs_data.SortTaxon(A, B, unknowns));

            //3.   Round the maximum relative abundance for display
            int stepsize = 0;
            int maxstepto = 0;
            maxfrac = Math.Min(maxfrac, 1d);//copensate for rounding errors.
            int[] permittedmax = new int[] { 10, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 600, 700, 800, 900, 1000 };//of 1000
            int[] stepsizes = new int[] { 1, 10, 10, 25, 20, 25, 50, 50, 50, 50, 50, 100, 100, 100, 100, 100 };//step size = value/1000. integer to keep from rounding
            for (int i = 0; i < permittedmax.Length; i++)
                if (maxfrac <= ((double)permittedmax[i]) / 1000d)
                {
                    maxstepto = permittedmax[i];
                    maxfrac = ((double)maxstepto) / (1000d);
                    stepsize = stepsizes[i];
                    break;
                }
            if (stepsize == 0) throw new Exception("Can't have a zero step size.");//safety

            //4.   Produce the TeX file
            //     Strategy: place each column of taxonomic rank prior to the next, this way the widths may be determined automatically
            //4.1.  Add the tikz nodes at each level
            string[] tikz_nodes = new string[taxa_level + 1];
            int[][] hrules = new int[taxa_level + 1][];//array containing the requisite hrules dim 0 = taxlvl, dim 1 = the row index above which the rule is to be written.
            for (int i = 0; i <= taxa_level; i++)//for each level
            {
                tikz_nodes[i] += @"      %%% " + altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(i) + " taxon names" + Environment.NewLine;
                tikz_nodes[i] += @"      \node[taxahead,anchor=mid west] (A) at (\thisLeftX, " + (row_offset_cm).ToString("0.###############") + "cm) " +
                            "{" + altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(i) + "};" + Environment.NewLine;

                tikz_nodes[i] += @"      \coordinate (Lvl" + i.ToString() + @") at (\thisLeftX, 0 cm);" + Environment.NewLine + Environment.NewLine;//record the Left side of each taxonomic level.
                string currtaxon = string.Empty;
                List<int> rules = new List<int>();
                for (int j = 0; j < taxa.Count; j++)//for each row
                    if (taxa[j].Hierarchy[i] != currtaxon || j == 0 || unknowns.Contains(currtaxon))
                    {
                        currtaxon = taxa[j].Hierarchy[i];
                        tikz_nodes[i] += @"      \node[taxalbl] (A) at (\thisLeftX, " + (-((double)j) * row_offset_cm).ToString("0.###############") + "cm) " +
                            "{" + format_taxon_string_at_level(currtaxon) + "};" + Environment.NewLine;
                        tikz_nodes[i] += @"      \updatenextleftX{A}" + Environment.NewLine;
                        //see if a rule at this row has already been called for by a more general rank
                        bool found = false;
                        for (int k = 0; k < i; k++)//go through all of the levels that have already been assessed
                        {
                            for (int l = 0; l < hrules[k].Length; l++)
                                if (hrules[k][l] == j)
                                {
                                    found = true;
                                    break;
                                }
                            if (found) break;
                        }
                        if (!found)//needed
                            rules.Add(j);
                    }
                if (i == taxa_level && minors.Count != 0)//<========Minor label
                    tikz_nodes[i] += @"      \node[minortaxalbl] (A) at (\nextLeftX, " + (-((double)taxa.Count) * row_offset_cm).ToString("0.###############") + @"cm) " +
                        "{" + minor_phylotypes + " (" + minors.Count.ToString() + ")};" + Environment.NewLine;
                if (i == 0)//domain...add a rule at the bottom
                    rules.Add(taxa.Count);
                tikz_nodes[i] += @"      \setlength{\thisLeftX}{\nextLeftX}" + Environment.NewLine;
                hrules[i] = rules.ToArray();//set the rules.
            }

            //4.2.  Add the horizontal rules
            string[] tikz_hrules = new string[taxa_level + 1];
            for (int i = 0; i < taxa_level + 1; i++)
            {
                tikz_hrules[i] += @"      %%% " + altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(i) + " hrules" + Environment.NewLine;
                for (int j = 0; j < hrules[i].Length; j++)
                {
                    string y = ((0.5 - ((double)hrules[i][j])) * row_offset_cm).ToString("0.###############");//go to the top of the row.
                    tikz_hrules[i] += @"      \draw[taxatblhrule] let \p1 = (Lvl" + i.ToString() + @") in (\x1, " + y + @"cm) -- (\thisLeftX, " + y + "cm);" + Environment.NewLine;
                }
            }

            //4.3.  Add the sample headings, draw the rectangles, draw the boxes and inner rules
            //      Strategy: Proceed column-wise through the data, adding the heading, filled rectangles for each taxon.sample, and outer group rectangles
            string[] tikz_samples = new string[n_columns];
            string[] tikz_headings = new string[n_samples];
            string leftX = @"\thisLeftX";
            double offsetfromstart = 0d;
            string groupstart = string.Empty;
            double groupoffsetfromstart = 0d;
            int colidx = 0;
            string bottomy = (minors.Count == 0) ? (((-0.5 - ((double)(taxa.Count - 1))) * row_offset_cm).ToString("0.###############") + "cm") : (((-0.5 - ((double)taxa.Count)) * row_offset_cm).ToString("0.###############") + @"cm");
            for (int i = 0; i < grouped_samples.Length; i++)
            {
                if (grouped_samples[i] == null) continue;//here for safety
                if (grouped_samples[i].Length == 0) continue;//no samples in group
                leftX += @"+ \samplegroupsep";//add space between groups
                groupstart = leftX + " + " + offsetfromstart.ToString("0.###############") + "cm";
                groupoffsetfromstart = offsetfromstart;
                //evaluate each sample
                for (int j = 0; j <= grouped_samples[i].Length; j++)
                {
                    if (j == grouped_samples[i].Length && (!include_group_means || grouped_samples[i].Length == 1)) continue;//don't include mean

                    //add column heading
                    string head = ((j == grouped_samples[i].Length) ? ("Mean") : (grouped_samples[i][j].GetAttr(heading_attr)));
                    tikz_samples[colidx] += @"      %%% Sample: " + legend_entries[i] + "." + head + Environment.NewLine;
                    tikz_samples[colidx] += @"      \node[samplelbl] (A) at (" + leftX + " + " + (offsetfromstart + 0.5 * cell_width_cm).ToString("0.###############") + "cm," + (0.5 * row_offset_cm).ToString("0.###############") + "cm) {" + head + "};" + Environment.NewLine;
                    tikz_samples[colidx] += @"      \updateheadingY{A}" + Environment.NewLine + Environment.NewLine;

                    //draw rectangle for each taxa
                    for (int k = 0; k < taxa.Count; k++)
                    {
                        double[] relabunds = all_non_zero_taxons_at_lvl[taxa[k]][i];
                        double val = ((j == grouped_samples[i].Length) ? (altvisngs_relabundtbl._Mean(relabunds)) : (relabunds[j]));
                        _color color = _color.Empty;
                        color = (val / maxfrac == 0d) ? (_color.White) : (colorscheme.GetColor(val / maxfrac));
                        tikz_samples[colidx] += @"      \fill[" + color.TikZString + "]" +
                            @"(" + leftX + " + " + offsetfromstart.ToString("0.###############") + "cm," + ((-0.5 - ((double)k)) * row_offset_cm).ToString("0.###############") + "cm) rectangle " +
                            @"(" + leftX + " + " + (offsetfromstart + cell_width_cm).ToString("0.###############") + "cm," + ((0.5 - ((double)k)) * row_offset_cm).ToString("0.###############") + "cm);" + Environment.NewLine;
                    }

                    //add the minors
                    if (minors.Count != 0)
                    {
                        double[] relabunds = minorvals[i];
                        double val = ((j == grouped_samples[i].Length) ? (altvisngs_relabundtbl._Mean(relabunds)) : (relabunds[j]));
                        _color color = _color.Empty;
                        color = (val / maxfrac == 0d) ? (_color.White) : (colorscheme.GetColor(val / maxfrac));
                        tikz_samples[colidx] += @"      \fill[" + color.TikZString + "]" +
                            @"(" + leftX + " + " + offsetfromstart.ToString("0.###############") + "cm," + bottomy + ") rectangle " +
                            @"(" + leftX + " + " + (offsetfromstart + cell_width_cm).ToString("0.###############") + "cm," + ((0.5 - ((double)taxa.Count)) * row_offset_cm).ToString("0.###############") + @"cm);" + Environment.NewLine;
                    }
                    //draw the left vert for the means
                    if (j == grouped_samples[i].Length)
                        tikz_samples[colidx] += @"      \draw[meanboxline] (" + leftX + " + " + offsetfromstart.ToString("0.###############") + "cm," + bottomy + ") rectangle " +
                            @"(" + leftX + " + " + (offsetfromstart + cell_width_cm).ToString("0.###############") + "cm," + ((0.5) * row_offset_cm).ToString("0.###############") + "cm);" + Environment.NewLine;
                    offsetfromstart += cell_width_cm;//next column
                    colidx++;
                }
                //draw the outer box
                if (minors.Count != 0)
                    tikz_samples[colidx - 1] += @"      \draw[meanboxline] (" + groupstart + "," + ((-0.5 - ((double)(taxa.Count - 1))) * row_offset_cm).ToString("0.###############") + "cm)--" +
                        @"(" + leftX + " + " + offsetfromstart.ToString("0.###############") + "cm," + ((-0.5 - ((double)(taxa.Count - 1))) * row_offset_cm).ToString("0.###############") + "cm);" + Environment.NewLine;
                tikz_samples[colidx - 1] += Environment.NewLine + @"      %%% Draw rectangle around " + legend_entries[i] + " group" + Environment.NewLine +
                    @"      \draw[groupboxline] (" + groupstart + "," + bottomy + ") rectangle " +
                    @"(" + leftX + " + " + offsetfromstart.ToString("0.###############") + "cm," + ((0.5) * row_offset_cm).ToString("0.###############") + "cm);" + Environment.NewLine;
                //add the heading entry
                tikz_headings[i] +=
                    @"      \edef\headX{\the\dimexpr(" + groupstart + " + " + leftX + " + " + offsetfromstart.ToString("0.###############") + @"cm)/(2)\relax}" + Environment.NewLine +
                    @"      \rotateheadifreq{" + legend_entries[i] + "}{" + (offsetfromstart - groupoffsetfromstart).ToString("0.###############") + @"cm}" + Environment.NewLine;
                    //@"      \node[groupheading, anchor=base] at (\headX,\headingY + 5pt) {" + legend_entries[i] + "};" + Environment.NewLine;
            }

            //4.4.  Add the color bar
            string tikz_colorbar = string.Empty;
            offsetfromstart += 0.5 * cell_width_cm;//offset for the color bar.
            //finally, add the color bar
            tikz_colorbar += @"      %%% Color scale" + Environment.NewLine;
            string maxints = maxstepto.ToString();
            if (maxints.Length == 2) maxints = "5";//1% or 5%
            if (maxints.Length == 3) maxints = "55";//10-90%
            if (maxints.Length == 4) maxints = "100";//100%
            string outputformat = ((stepsize == 1 || stepsize == 25 || stepsize == 25) ? ("0.0#") : ("0"));// (int)(((double)stepsize) / 1000d * 10d) != (int)((double)((int)(((double)stepsize) / 1000d)) * 10d);
            tikz_colorbar += @"      \settowidth{\intwid}{\footnotesize $" + maxints + "$}%The width of the largest integer component of a label" + Environment.NewLine;
            tikz_colorbar += @"      \draw[thin] (" + leftX + " +" + offsetfromstart.ToString("0.###############") + "cm," + ((-0.5 - ((double)0)) * row_offset_cm).ToString("0.###############") + "cm) rectangle " +
                        @"(" + leftX + " +" + (offsetfromstart + cell_width_cm).ToString("0.###############") + "cm," + ((0.5 - ((double)0)) * row_offset_cm).ToString("0.###############") + "cm);" + Environment.NewLine;
            tikz_colorbar += @"      \node [colorbarlbl] at (" + leftX + " +" + (offsetfromstart + cell_width_cm).ToString("0.###############") + @"cm + \colorscaleticklen, 0cm) {" + not_detected + "};" + Environment.NewLine;
            for (int i = 0; i <= 100; i++)
            {
                double frac = ((double)i) / ((double)100) * maxfrac;
                _color color = colorscheme.GetColor(frac / maxfrac);
                tikz_colorbar += @"      \fill[" + color.TikZString + "]" +
                        @"(" + leftX + " +" + offsetfromstart.ToString("0.###############") + "cm," + ((-0.5 - ((double)i) - 20d) * 0.1 * row_offset_cm).ToString("0.###############") + "cm) rectangle " +
                        @"(" + leftX + " +" + (offsetfromstart + cell_width_cm).ToString("0.###############") + "cm," + ((0.5 - ((double)i) - 20d) * 0.1 * row_offset_cm).ToString("0.###############") + "cm);" + Environment.NewLine;
            }
            int currval = 0;
            while (currval <= maxstepto)
            {
                double ypos = (-currval / 1000d * 100d / maxfrac - 20d) * 0.1 * row_offset_cm;
                tikz_colorbar += @"      \draw [thin] (" + leftX + " +" + (offsetfromstart + cell_width_cm).ToString("0.###############") + "cm, " + ypos.ToString("0.###############") + "cm)--" +
                    @"(" + leftX + " +" + (offsetfromstart + cell_width_cm).ToString("0.###############") + @"cm + \colorscaleticklen, " + ypos.ToString("0.###############") + "cm);" + Environment.NewLine;
                tikz_colorbar += @"      \settowidth{\deltintwid}{\footnotesize $" + (currval / 1000d * 100d).ToString("0") + "$}%The width of the largest integer component of a label" + Environment.NewLine;
                tikz_colorbar += @"      \node [colorbarlbl] at (" + leftX + " +" + (offsetfromstart + cell_width_cm).ToString("0.###############") + @"cm +\colorscaleticklen, " + ypos.ToString("0.###############") + "cm) " +
                    @"{\hspace{\dimexpr\intwid-\deltintwid}$" + (currval / 1000d * 100d).ToString(outputformat) + ((currval == 0) ? (@"\,\%") : ("")) + "$};" + Environment.NewLine;
                currval += stepsize;
            }

            Console.WriteLine("Saving `" + Path.GetFileName(filePath) + "'");
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine(document_class);
                sw.WriteLine(add_packages);
                sw.WriteLine(_heatmap_reqd_preamble);
                sw.WriteLine(@"\begin{document}");
                sw.WriteLine(@"    \begin{tikzpicture}");
                for (int i = 0; i < tikz_nodes.Length; i++)
                    sw.WriteLine(tikz_nodes[i]);
                for (int i = 0; i < tikz_hrules.Length; i++)
                    sw.WriteLine(tikz_hrules[i]);
                for (int i = 0; i < tikz_samples.Length; i++)
                    sw.WriteLine(tikz_samples[i]);
                for (int i = 0; i < tikz_headings.Length; i++)
                    sw.WriteLine(tikz_headings[i]);
                sw.WriteLine(tikz_colorbar);
                sw.WriteLine(@"    \end{tikzpicture}");
                sw.WriteLine(@"\end{document}");
            }
            if (((double)(taxa.Count - 1)) * row_offset_cm > 20d * 12d * 2.54)
                Console.WriteLine("pdflatex not run on `" + Path.GetFileName(filePath) + "'. Heatmap length exceeds 19 feet (too long).");
            else
            {
                pdflatex tex = new pdflatex();
                tex.RunSync(filePath);
            }
            LaTeX_Figure.figure_inclgrphx(
                filepath_output_caption: filePath.Replace(".tex", "_float.tex"),
                filepath_output_captionof: filePath.Replace(".tex", "_nofloat.tex"),
                figure_pdf_relative_path: relative_filepath_prefix + Path.GetFileNameWithoutExtension(filePath),
                caption_mandatory: caption_mandatory,
                figure_label: figure_label,
                caption_optional: caption_optional);
        }

        public static bool AnyReactor_geq(double[][] vals, double cutoff)
        {
            for (int i = 0; i < vals.Length; i++)
                for (int j = 0; j < vals[i].Length; j++)
                    if (vals[i][j] >= cutoff) return true;
            return false;
        }
    }
}
