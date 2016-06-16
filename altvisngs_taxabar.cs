using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.ComponentModel;

namespace altvisngs
{
    /// <summary> </summary>
    abstract class altvisngs_taxabar
    {
        #region Fields
        /// <summary> String format for number outputs to tex </summary>
        public static string SForm { get { return "0.###############"; } }

        private static string tikzrsltpreamble =
@"%%% altvisngs output
\documentclass[tikz,10pt]{standalone}
\usepackage[scaled]{helvet}
\renewcommand\familydefault{\sfdefault} 
\usepackage[T1]{fontenc}
\usepackage{sansmath}
\sansmath

%Patch to allow _ to be underscore in text mode. Requires font encoding to be T1.
%Ref: egreg soln: http://tex.stackexchange.com/a/38720/89497
\catcode`_=12
\begingroup\lccode`~=`_\lowercase{\endgroup\let~\sb}
\mathcode`_=""8000

%%% Colors Used %%%
\def\noidcolor{red}
\def\minorcolor{gray}

%%% Heading Styles %%%
\tikzstyle{samplelbl}=[font=\footnotesize\bfseries,rotate=90]%node style for the sample label
\tikzstyle{taxabarlbl}=[font=\footnotesize\bfseries]%node style for the taxa bar labels (i.e., domain, phylum, etc.)
\tikzstyle{minorbarlbl}=[font=\footnotesize\color{\minorcolor}]%node style for the Minor portion labels
\tikzstyle{noidbarlbl}=[font=\footnotesize\color{\noidcolor}]%node style for the No ID portion labels

%%% Taxa Label Styles %%%
\tikzstyle{taxalbl}=[fill=white,inner sep=0pt,font=\footnotesize]%base node style for taxa labels
\tikzstyle{taxalbl0}=[taxalbl]%node style for taxa level 0 (typically domain)
\tikzstyle{taxalbl1}=[taxalbl]%node style for taxa level 1 (typically phylum)
\tikzstyle{taxalbl2}=[taxalbl]%node style for taxa level 2 (typically class)
\tikzstyle{taxalbl3}=[taxalbl]%node style for taxa level 3 (typically order)
\tikzstyle{taxalbl4}=[taxalbl]%node style for taxa level 4 (typically family)
\tikzstyle{taxalbl5}=[taxalbl,font=\footnotesize\itshape]%node style for taxa level 5 (typically genus)
\tikzstyle{taxalbl6}=[taxalbl,font=\footnotesize\itshape]%node style for taxa level 6 (typically species)

\tikzstyle{leaderstyle}=[thin]%the style used for leaders

%%% Taxa Bar Styles %%%
\tikzstyle{minorbackground}=[\minorcolor!50!white]%fill style used for minor background
\tikzstyle{noidbackground}=[\noidcolor!50!white]%fill style used for no id background
\tikzstyle{minorbar}=[\minorcolor]%fill style used for minor bar segments
\tikzstyle{noidbar}=[\noidcolor]%fill style used for noid bar segments
\tikzstyle{barrectangle}=[thin]%the style used for the outer rectangle surrounding each bar
\tikzstyle{barinner}=[thin]%the style used for the vertical lines within a bar
\tikzstyle{bartobar}=[densely dotted]%the style used for the lines between bars to denote taxa hierarchy
";//this is the preamble for the generic lineage diagram

        private static string tikzmeasaddpreamble =
@"
\usepackage{tikz,fp}
\usetikzlibrary{calc}
\makeatletter
%%from http://tex.stackexchange.com/a/38500/89497
%#1 and #2 are the coordinate names
%#3 is the length csname
\def\calcLength(#1,#2)#3{%Length is in pt
	\pgfpointdiff{\pgfpointanchor{#1}{center}}%
	             {\pgfpointanchor{#2}{center}}%
	\pgf@xa=\pgf@x%
	\pgf@ya=\pgf@y%
	\FPeval\@temp@a{\pgfmath@tonumber{\pgf@xa}}%
	\FPeval\@temp@b{\pgfmath@tonumber{\pgf@ya}}%
	\FPeval\@temp@sum{(\@temp@a*\@temp@a+\@temp@b*\@temp@b)}%
	\FProot{\FPMathLen}{\@temp@sum}{2}%
	\FPround\FPMathLen\FPMathLen5\relax
	\global\expandafter\edef\csname #3\endcsname{\FPMathLen}
	}
\makeatother

\newwrite\altvisngsfile
\immediate\openout\altvisngsfile=\jobname rdprslt.txt

%%allow for tab delimited output
\begingroup
\catcode`\^^I=12
\gdef\mytab{^^I}
\endgroup

%#1 is the unique ID
%#2 is the node style
%#3 is the node contents
\newcommand{\MeasureNode}[3]{%
	\node[#2] (test) at (0,0){#3};%
	\coordinate (left) at (test.base west);%
	\coordinate (bottomleft) at (test.south west);%
	\coordinate (right) at (test.base east);%
	\coordinate (top) at (test.north);%
	\coordinate (bottom) at (test.south);%
	\calcLength(left,right){testwidth};%
	\calcLength(top,bottom){testheight};%
	\calcLength(left,bottomleft){testdepth};%
	\immediate\write\altvisngsfile{#1\mytab\testwidth\mytab\testheight\mytab\testdepth}%
    }
";
        private static string tikzmeaspreend =
@"\immediate\closeout\altvisngsfile
";
        #endregion
        /// <summary> Create a Taxa bar</summary>
        /// <param name="filepath_output"></param>
        /// <param name="sample"></param>
        /// <param name="figure_label"></param>
        /// <param name="taxa_levels"></param>
        /// <param name="TaxaNames"></param>
        /// <param name="minor_cutoff"></param>
        /// <param name="scale_mult_cm">width of the bar in (cm; default = 13)</param>
        /// <param name="row_offset_cm">baseline to baseline vertical space between label rows (cm; default = 0.4)</param>
        /// <param name="sample_label_x_cm">the x location of the sample label for the lineage chart (c; default = -1.5)</param>
        /// <param name="permitted_right_overhang_cm">amount by which labels may protrude to the right of the figure bounds (cm; default = 0.5).</param>
        /// <param name="minor_noid_lbl_wid_cm">the width of the minor and no id totals label width (cm; default = -1.25)</param>
        /// <param name="bar_ht_cm">the height of the bar (cm; default - 0.375)</param>
        /// <param name="min_Del_X_cm">the minimum absolute horizontal distance between the start and end of a leader (cm; to prevent horizontal leaders; default = 0.125)</param>
        /// <param name="max_leader_length_cm">the maximum permitted length of a leader (cm; default = 2)</param>
        /// <param name="label_min_sep_cm">the minimum separation between labels in the same row (cm; default = 0.125) </param>
        /// <param name="leader_buffer_cm">The minimimum space between a leader and a label (cm; default = 0.05)</param>
        /// <param name="row_fluff">fluff factor to increase the upper limit on the anticipated number of rows (default = 1.25)</param>
        /// <param name="row_Penalty_cm">the factor, multiplied by the number of rows - 1 used to score label arrangements; equivalent to leader length (cm) added per row (default = 10)</param>
        /// <param name="label_cover_max">The maximum number of labels a given label may cover. Used to prevent a long label from covering too many future labels. This will effectively force a long label to the opposite side of the bar. </param>
        /// <param name="unknown"></param>
        public static void TaxaBar(
            string filepath_output,
            TaxonObservation[] taxon_observations,
            string figure_title,
            int taxa_levels,
            string[] TaxaNames,
            double minor_cutoff,
            string figure_label,
            string caption_mandatory,
            string figure_pdf_relative_path,
            string caption_optional,
            bool refresh_tex_measures,
            out bool un_positioned_labels,
            double min_relabund_to_show_label = 0d,
            double scale_mult_cm= 13d,
            double row_offset_cm = 0.4,
            double sample_label_x_cm = -1.5,
            double permitted_right_overhang_cm = 0.5,
            double minor_noid_lbl_wid_cm = 1.25,
            double bar_ht_cm = 0.375d,
            double min_Del_X_cm = 0.125,
            double max_leader_length_cm = 2d,
            double label_min_sep_cm = 0.125,
            double leader_buffer_cm = 0.05,
            int max_extra_rows = 3,
            double row_Penalty_cm = 10d,
            int label_cover_max = 8,
            string not_identified_abbr = "No ID",
            string minor_abbr = "Minor",
            string unknown = "unknown")
        {
            Console.WriteLine("Building taxonomic bars `" + figure_label + "'...");
            //**Create lineage bar charts**
            //now, go through and sort the entries
            //sort list
            Console.WriteLine("Sorting taxa...");
            List<TaxonObservation> taxobs = new List<TaxonObservation>();
            taxobs.AddRange(taxon_observations);
            //taxobs.Sort((A, B) => TaxonObservation.Compare_TaxonomicBar(A, B, unknown, minor_cutoff));

            TaxaClassLevel life = new TaxaClassLevel(taxobs.ToArray(), 0, taxa_levels, minor_cutoff, unknown);
            List<TikzLabel> allLabels = new List<TikzLabel>();
            List<TikzLabel[]> lblsatlevels = new List<TikzLabel[]>();
            int viabletaxalevels = -1;
            for (int i = 0; i < taxa_levels; i++)//evaluate each taxa level
            {
                TikzLabel[] lblsatlevel = GetLabelsAtTaxaLevel(life, i, scale_mult_cm, unknown, min_relabund_to_show_label);
                //if (lblsatlevel.Length == 0) break;//no unique labels at this level => cannot be any further down => stop here. 
                allLabels.AddRange(lblsatlevel);
                lblsatlevels.Add(lblsatlevel);
                viabletaxalevels = i + 1;
            }
            if (viabletaxalevels == -1) { un_positioned_labels = false; return; }//no data here.

            //measure labels
            double midtobaseline = 0d;
            double all_labels_depth_cm = 0d;
            double all_labels_height_cm = 0d;
            MeasureLabels(allLabels.ToArray(), filepath_output.Replace(".tex", "_meas.tex"), tikzrsltpreamble + tikzmeasaddpreamble, tikzmeaspreend, refresh_tex_measures, out midtobaseline, out all_labels_depth_cm, out all_labels_height_cm);

            //Position the labels and determine the number rows
            un_positioned_labels = false;
            List<PositionedTikzLabel[]> positionedLabels = new List<PositionedTikzLabel[]>();
            int[] nrowsabove = new int[viabletaxalevels];
            int[] nrowsbelow = new int[viabletaxalevels];
            double[] barcenterY = new double[viabletaxalevels];
            double minY = 0d;
            double maxY = 0d;
            for (int i = 0; i < viabletaxalevels; i++)
            {
                int nabove = 0;
                int nbelow = 0;
                if (lblsatlevels[i].Length == 0) positionedLabels.Add(new PositionedTikzLabel[] { });
                else
                {
                    bool valid_run = true;
                    for (int j = 0; j < max_extra_rows + 1; j++)
                    {
                        PositionedTikzLabel[] rslt = PlaceLabelsAtLevel(
                            lblsatlevels[i],
                            i,
                            sample_label_x_cm,
                            scale_mult_cm + permitted_right_overhang_cm,
                            row_offset_cm,
                            min_Del_X_cm,
                            label_min_sep_cm,
                            leader_buffer_cm,
                            midtobaseline,
                            row_Penalty_cm,
                            j,
                            max_leader_length_cm,
                            all_labels_depth_cm,
                            all_labels_height_cm,
                            label_cover_max,
                            out nabove,
                            out nbelow,
                            out valid_run);
                        if (valid_run || j == max_extra_rows)
                        {
                            positionedLabels.Add(rslt);
                            break;
                        }
                        Console.WriteLine("Label positioning failed for " + TaxaNames[i] + " at row fluff = " + j.ToString());
                    }
                    if (!valid_run)
                    {
                        Console.WriteLine("Failed to place labels at `" + TaxaNames[i] + "'");
                        un_positioned_labels = true;
                    }
                }
                nrowsabove[i] = nabove;
                nrowsbelow[i] = nbelow;
                if (i == 0) barcenterY[i] = -1 * ((double)(nbelow + 1)) * row_offset_cm;
                else barcenterY[i] = barcenterY[i - 1] - ((double)(nrowsabove[i] + nrowsbelow[i - 1] + 1)) * row_offset_cm;
                if (i == 0) minY = barcenterY[0] + midtobaseline + ((double)nabove * row_offset_cm);
                if (i == viabletaxalevels - 1)
                    maxY = barcenterY[i] - midtobaseline;
            }

            //now draw the figure:
            //start with the background
            string tikzrslt = tikzrsltpreamble;
            tikzrslt += @"\begin{document}" + Environment.NewLine;
            tikzrslt += @"  \begin{tikzpicture}" + Environment.NewLine;
            tikzrslt += @"      %%% Sample Label and Headings %%%" + Environment.NewLine;
            tikzrslt += @"      \node[samplelbl,anchor=south] at (" + sample_label_x_cm.ToString(SForm) + "," + (0.5 * (minY + maxY)).ToString(SForm) + ")" +
                "{" + figure_title + "};" + Environment.NewLine;

            //unidentified and minor amount labels
            if (barcenterY.Length > 0)
            {
                //minor amount label
                tikzrslt += @"      \node[minorbarlbl,anchor=base] at (" +
                    (scale_mult_cm + label_min_sep_cm + 0.5 * minor_noid_lbl_wid_cm).ToString(SForm) + "," + (barcenterY[0] + (nrowsabove[0] + 1) * row_offset_cm).ToString(SForm) + ") " +
                    "{" + minor_abbr + "};" + Environment.NewLine;
                //unidentified amount label
                tikzrslt += @"      \node[noidbarlbl,anchor=base] at (" +
                    (scale_mult_cm + label_min_sep_cm + minor_noid_lbl_wid_cm + label_min_sep_cm + 0.5 * minor_noid_lbl_wid_cm).ToString(SForm) + "," + (barcenterY[0] + (nrowsabove[0] + 1) * row_offset_cm).ToString(SForm) + ") " +
                    "{" + not_identified_abbr + "};" + Environment.NewLine;
            }
            tikzrslt += Environment.NewLine;
            tikzrslt += @"      %%% Background Fills (All Taxa Levels) %%%" + Environment.NewLine;
            tikzrslt += life.GetNestedBackgrounds(scale_mult_cm, scale_mult_cm, bar_ht_cm, barcenterY, viabletaxalevels, true, unknown) + Environment.NewLine;
            for (int i = 0; i < viabletaxalevels; i++)//
            {
                List<double> horiz = new List<double>();
                List<double> bardelim = new List<double>();
                GroupedTaxa[] rslt = life.GetAtLevel(i, true, unknown);
                tikzrslt += @"      %%% Taxa Level: " + TaxaNames[i] + " %%%" + Environment.NewLine;
                tikzrslt += @"      \begin{scope}[yshift=" + barcenterY[i].ToString(SForm) + "cm]" + Environment.NewLine;
                double unid = 0d;
                double minor = 0d;
                double right = scale_mult_cm;
                for (int j = 0; j < rslt.Length; j++)
                {
                    string colorcmd = "";
                    if (rslt[j].DisplayedName == "Unidentified") colorcmd = @"noidbar";
                    else if (rslt[j].DisplayedName == "Minor Taxa") colorcmd = @"minorbar";
                    if (!string.IsNullOrEmpty(colorcmd))// ((rslt[j].DisplayedName == "Unidentified") ? ("\\unidentcolor") : ((rslt[j].DisplayedName == "Minor Taxa") ? ("\\minorcolor") : ("white")))
                    {
                        tikzrslt += @"          \fill[" + colorcmd + "] ";
                        tikzrslt += "(" + (right - rslt[j].TotalProportion * scale_mult_cm).ToString(SForm) + "," + (0.5 * bar_ht_cm).ToString(SForm) + ") rectangle (" + right.ToString(SForm) + ",-" + (0.5 * bar_ht_cm).ToString(SForm) + ");" + Environment.NewLine;
                    }
                    if (j > 0) bardelim.Add(right);
                    right = right - rslt[j].TotalProportion * scale_mult_cm;

                    switch (rslt[j].DisplayedName)
                    {
                        case ("Minor Taxa"):
                            minor += rslt[j].TotalProportion;
                            break;
                        case ("Unidentified"):
                            unid += rslt[j].TotalProportion;
                            break;
                        default:
                            if (!horiz.Contains(right)) horiz.Add(right);
                            if (!horiz.Contains(right + rslt[j].TotalProportion * scale_mult_cm)) horiz.Add(right + rslt[j].TotalProportion * scale_mult_cm);
                            break;
                    }
                }
                //draw dotted lines to the next level
                if (i != viabletaxalevels - 1)
                    for (int j = 0; j < horiz.Count; j++)
                        tikzrslt += @"          \draw[bartobar] (" + horiz[j].ToString(SForm) + ",-" + (0.5 * bar_ht_cm).ToString(SForm) + ")--(" + horiz[j].ToString(SForm) + "," + (barcenterY[i + 1] - barcenterY[i] + 0.5 * bar_ht_cm).ToString(SForm) + ");" + Environment.NewLine;
                //draw inner verticals
                for (int j = 0; j < bardelim.Count; j++)
                    tikzrslt += @"          \draw[barinner] (" + bardelim[j].ToString(SForm) + "," + (0.5 * bar_ht_cm).ToString(SForm) + ") -- (" + bardelim[j].ToString(SForm) + ",-" + (0.5 * bar_ht_cm).ToString(SForm) + ");" + Environment.NewLine;
                //draw outer rectangle
                tikzrslt += @"          \draw[barrectangle] (0," + (0.5 * bar_ht_cm).ToString(SForm) + ") rectangle (" + scale_mult_cm.ToString(SForm) + ",-" + (0.5 * bar_ht_cm).ToString(SForm) + ");" + Environment.NewLine;//draw outer rectangle
                //taxa level label
                tikzrslt += @"          \node[taxabarlbl,anchor=mid east] at (0,0) {" + TaxaNames[i] + "};" + Environment.NewLine;
                //minor amount label
                tikzrslt += @"          \node[minorbarlbl,anchor=mid east] at (" + (scale_mult_cm + label_min_sep_cm + minor_noid_lbl_wid_cm).ToString(SForm) + ",0) " +
                    "{$" + (100d * minor).ToString("F2") + @"\,\%$};" + Environment.NewLine;
                //unidentified amount label
                tikzrslt += @"          \node[noidbarlbl,anchor=mid east] at (" + (scale_mult_cm + label_min_sep_cm + minor_noid_lbl_wid_cm + label_min_sep_cm + minor_noid_lbl_wid_cm).ToString(SForm) + ",0) " +
                    "{$" + (100d * unid).ToString("F2") + @"\,\%$};" + Environment.NewLine;
                //taxa labels
                if (positionedLabels.Count > 0)
                    if (i < positionedLabels.Count)//
                        for (int j = 0; j < positionedLabels[i].Length; j++)
                            tikzrslt += positionedLabels[i][j].tikz();
                    else//label unpositioned
                        tikzrslt += "%WARNING!!!!!: unpositioned labels" + Environment.NewLine;
                tikzrslt += @"      \end{scope}" + Environment.NewLine + Environment.NewLine;
            }
            tikzrslt += @"  \end{tikzpicture}" + Environment.NewLine;
            tikzrslt += @"\end{document}";//cap it off

            Console.WriteLine("Saving diagram to `" + Path.GetFileName(filepath_output) + "'");
            using (StreamWriter sw = new StreamWriter(filepath_output))//this is the output with all taxonomic levels for each sample
            {
                sw.WriteLine(tikzrslt);
            }
            pdflatex pdflatex = new pdflatex();
            pdflatex.RunSync(filepath_output);

            LaTeX_Figure.figure_inclgrphx(
                filepath_output_caption: (filepath_output.Replace(".tex","_float.tex")),
                filepath_output_captionof: (filepath_output.Replace(".tex", "_nofloat.tex")),
                figure_pdf_relative_path: figure_pdf_relative_path + Path.GetFileNameWithoutExtension(filepath_output),
                caption_mandatory: caption_mandatory,
                figure_label: figure_label,
                caption_optional: caption_optional);
        }

        /// <summary> Method to return the labels at a taxa level </summary>
        /// <param name="life">The classified taxa at the passed level</param>
        /// <param name="taxaLevel">The taxa level</param>
        /// <param name="scalemult">The scale of the bar (i.e., its total length in centimeters)</param>
        /// <returns></returns>
        private static TikzLabel[] GetLabelsAtTaxaLevel(TaxaClassLevel life, int taxaLevel, double scalemult, string unknown, double min_relabund_to_show)
        {
            //now, step through each level, grouping entries according to shared classification
            List<TikzLabel> labels = new List<TikzLabel>();
            GroupedTaxa[] rslt = life.GetAtLevel(taxaLevel, true, unknown);
            double top = scalemult;
            for (int i = 0; i < rslt.Length; i++)
            {
                top = top - rslt[i].TotalProportion * scalemult;
                if (rslt[i].DisplayedName != "Minor Taxa" && rslt[i].DisplayedName != "Unidentified" && rslt[i].DisplayedName!= unknown)
                {
                    if (rslt[i].TotalProportion < min_relabund_to_show) continue;
                    string disp = rslt[i].DisplayedName;
                    if (altvisngs_relabundtbl.IsIncertaeSedis(disp))
                        disp = altvisngs_relabundtbl.AbbrIncertaeSedis(disp, string.Empty);
                    labels.Add(new TikzLabel(disp, (100d * rslt[i].TotalProportion).ToString("F1"), taxaLevel, i, top + rslt[i].TotalProportion * scalemult * 0.5, rslt[i].TotalProportion * scalemult));
                }
            }
            return labels.ToArray();
        }

        /// <summary> Method to measure the labels using latex </summary>
        /// <remarks>Method to measure the labels in the output format.
        /// Basic approach:
        /// 1. Write all of the labels to a tex file, passing the contents to a custom command \EvalNode
        /// 2. Compile the tex file with the passed tex variant
        /// 3. Read the resulting txt file with the height, width, and depth measurements and assign them to the appropriate labels</remarks>
        /// <param name="lbls"></param>
        /// <param name="output_directory"></param>
        /// <param name="fileName"></param>
        /// <param name="tikzmeaspre">The preamble for the measurement tex file</param>
        /// <param name="tikzmeaspost">The footer for the measurement tex file</param>
        /// <param name="midtobaseline">The distance from the center of the bar to the baseline of a label centered in the bar (should be negative, see page 229 of the pgfmanual) </param>
        private static void MeasureLabels(TikzLabel[] lbls, string filepath_output, string tikzmeaspre, string tikzmeaspost, bool refresh_tex_measures, out double midtobaseline, out double all_labels_depth_cm, out double all_labels_height_cm)
        {
            string measrlstfile = filepath_output.Replace(".tex", "rdprslt.txt");
            if (refresh_tex_measures || !File.Exists(measrlstfile))
            {
                Console.WriteLine("Saving tex file for node measuring to `" + Path.GetFileName(filepath_output) + "'");
                using (StreamWriter sw = new StreamWriter(filepath_output))
                {
                    sw.WriteLine(tikzmeaspre);
                    sw.WriteLine(@"\begin{document}");
                    sw.WriteLine(@"    \begin{tikzpicture}");
                    sw.WriteLine(@"        \MeasureNode{tikz(midtobaseline)}{taxalbl}{tikzx}");//custom measure to determine the distance between mid and the baseline; used for positioning the labels.
                    for (int j = 0; j < lbls.Length; j++)//measure each label
                    {
                        sw.WriteLine(@"        \MeasureNode{" + lbls[j].LevelIndex + lbls[j].Taxon_Name + lbls[j].Proportion + "}{taxalbl" + lbls[j].Level.ToString() + "}{" + lbls[j].NodeContent_Full + "}");
                        sw.WriteLine(@"        \MeasureNode{" + lbls[j].LevelIndex + lbls[j].Taxon_Name + lbls[j].Proportion + "(prcnt)}{taxalbl" + lbls[j].Level.ToString() + "}{" + lbls[j].NodeContent_PercentOnly + "}");
                        sw.WriteLine(@"        \MeasureNode{" + lbls[j].LevelIndex + lbls[j].Taxon_Name + lbls[j].Proportion + "(taxa)}{taxalbl" + lbls[j].Level.ToString() + "}{" + lbls[j].NodeContent_TaxaOnly + "}");
                    }
                    sw.WriteLine(@"    \end{tikzpicture}");
                    sw.WriteLine(tikzmeaspost);
                    sw.WriteLine(@"\end{document}");
                }

                //run latex or tex or pdflatex, etc. on it.
                latex latex = new latex();
                latex.RunSync(filepath_output);
            }


            Console.WriteLine("Reading tex measurement results from `" + Path.GetFileName(measrlstfile) + "'");
            midtobaseline = 0d;
            all_labels_depth_cm = 0d;
            all_labels_height_cm = 0d;
            using (StreamReader sr = new StreamReader(measrlstfile))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (!line.Contains('\t')) throw new ArgumentException("Expecting tab delimited.");
                    string[] parsed = line.Split(new char[] { '\t' }, StringSplitOptions.None);
                    //expecting taxa,proportion,levelindex,width,height,depth
                    TikzLabelBox box = new TikzLabelBox(double.Parse(parsed[1]), double.Parse(parsed[2]), double.Parse(parsed[3]));
                    bool found = false;
                    string levidx = parsed[0];
                    if (parsed[0] == "tikz(midtobaseline)") 
                    { 
                        found = true; 
                        midtobaseline = -0.5 * box.Heightcm;
                        all_labels_depth_cm = box.Depthcm;
                        all_labels_height_cm = box.Heightcm;
                        continue; 
                    } //see page 229 of the pgfmanual
                    if (parsed[0].Contains("(prcnt)")) levidx = levidx.Substring(0, levidx.IndexOf("(prcnt)"));
                    if (parsed[0].Contains("(taxa)")) levidx = levidx.Substring(0, levidx.IndexOf("(taxa)"));
                    for (int i = 0; i < lbls.Length; i++)
                    {
                        if (levidx == lbls[i].LevelIndex + lbls[i].Taxon_Name + lbls[i].Proportion)//match
                        {
                            if (parsed[0].Contains("(prcnt)")) lbls[i].Percent_Box = box;
                            else if (parsed[0].Contains("(taxa)")) lbls[i].Label_Box = box;
                            else lbls[i].Label_And_Percent_Box = box;
                            found = true;
                            break;
                        }
                    }
                    if (!found) throw new ArgumentOutOfRangeException("A node from the TEX meas does not match with the collection");
                }
            }
            //verify that all boxes were measured
            for (int i = 0; i < lbls.Length; i++)
                if (lbls[i].Label_And_Percent_Box.IsEmpty)
                    throw new ArgumentOutOfRangeException("A box was not evaluated by TEX meas!");
        }

        /// <summary> Method to return an array of PositionedTikzLabels for the taxa at the passed level</summary>
        /// <remarks> Labels are placed for each taxa level separately for simplicity. While there could conceivably be overlap between levels (i.e., labels from different taxa in the same row), this is omitted for now. 
        /// Placement is determined with the following goals:
        /// 1. NO OVERLAP of boxes
        /// 2. NO OVERLAP of boxes and leaders
        /// 3. NO OVERLAP of leaders
        /// 4. Try to minimize the number of rows (expand upward by 2 then permit expansion downward as required)
        /// 5. Try to minimize the horizontal distance between the label and the center of the bar to which it refers.
        /// 6. NO VERTICAL leaders (avoid confusion with lines between bars)
        /// </remarks>
        /// <param name="lbls">The collection of all labels (at all taxa levels)</param>
        /// <param name="taxaLevel">The taxa level of interest</param>
        /// <param name="minimumX">The left limit for the labels (samplelabelx)</param>
        /// <param name="maximumX">The right limit for the labels (scalemult + permittedrightoverhang)</param>
        /// <param name="rowoffset">Baseline to baseline separation between the rows</param>
        /// <param name="minDelX">The minimum horizontal difference between the center of the bar and the end of a leader (to prevent vertical leaders)</param>
        /// <param name="labelminsep">The minimum horizontal separation between labels on the same row</param>
        /// <param name="midtobaseline">The distance between the center of the bar and baseline of the labels on the bar (half of the height of the x character; see pgfmanual 229)</param>
        /// <param name="rowPenalty">The penalty assigned for each additional row. Used in scoring a configuration.</param>
        /// <param name="rowfluff">An additional fluff factor used to inflate the estimate for the number of rows.</param>
        /// <returns>An array of PositionedTikzLabels around a taxa bar centered at Y=0</returns>
        private static PositionedTikzLabel[] PlaceLabelsAtLevel(
            TikzLabel[] lbls,
            int taxaLevel,
            double minimumX,
            double maximumX,
            double rowoffset,
            double minDelX,
            double labelminsep,
            double leader_buffer,
            double midtobaseline,
            double rowPenalty,
            int extra_rows_fluff,
            double maxleaderlength,
            double all_labels_depth_cm,
            double all_labels_height_cm,
            int max_permited_obstructed_label,
            out int nrowsabove, out int nrowsbelow, out bool all_positioned)
        {
            all_positioned = true;
            nrowsabove = 0;
            nrowsbelow = 0;
            //double barcenterY = 0d;//center each of the bars at zero initially for simplicity
            double barbaselineY = midtobaseline;
            double availwid = maximumX - minimumX;
            
            //1. Get the labels at this taxa level and determine the required width (the width if all the labels are placed side by side)
            //Also, evaluate if the label will fit within the bar.
            List<PositionedTikzLabel> OnBar = new List<PositionedTikzLabel>();
            List<PositionedTikzLabelBox> boxesOnBar = new List<PositionedTikzLabelBox>();
            double reqwid = 0d;
            Stack<TikzLabel> lblsAtLevel = new Stack<TikzLabel>();
            List<TikzLabel> lbls_for_cover = new List<TikzLabel>();
            for (int j = 0; j < lbls.Length; j++)
                if (lbls[j].Level == taxaLevel)// && !lbls[j].IsDrawn)
                {
                    if (lbls[j].Label_And_Percent_Box.Widthcm < lbls[j].widthofBar)//whole label will fit.
                    {
                        lbls[j].AllFitsInBar = true;
                        OnBar.Add(new PositionedTikzLabel(lbls[j], lbls[j].xCenterofBar - 0.5 * lbls[j].Label_And_Percent_Box.Widthcm, barbaselineY, double.NaN, -1));
                        boxesOnBar.Add(new PositionedTikzLabelBox(lbls[j].Label_And_Percent_Box, lbls[j].xCenterofBar - 0.5 * lbls[j].Label_And_Percent_Box.Widthcm, midtobaseline - lbls[j].Label_And_Percent_Box.Depthcm, lbls[j]));
                        continue;//don't add this
                    }
                    else if (lbls[j].Percent_Box.Widthcm < lbls[j].widthofBar)//percent label will fit.
                    {
                        lbls[j].PercentFitsInBar = true;
                        boxesOnBar.Add(new PositionedTikzLabelBox(lbls[j].Percent_Box, lbls[j].xCenterofBar - 0.5 * lbls[j].Percent_Box.Widthcm, midtobaseline - lbls[j].Percent_Box.Depthcm, lbls[j]));
                    }

                    lblsAtLevel.Push(lbls[j]);//note that these are in lbls in reverse, so adding to stack means they are evaluated left to right
                    lbls_for_cover.Insert(0, lbls[j]);
                    reqwid += lbls[j].LabelBox.Widthcm;
                }
            if (lblsAtLevel.Count == 0)
            {
                if (OnBar.Count != 0) return OnBar.ToArray();
                return new PositionedTikzLabel[] { };//taxa level already done! Go on to the next one.
            }
            reqwid += labelminsep * (double)(lblsAtLevel.Count - 1);
            //reqwid *= rowfluff;//add the fluff factor (subjective)

            //2. Estimate the maximum number of rows that would be required to achive this
            int nrowspersidemax = Math.Max((int)Math.Ceiling(0.5 * reqwid / availwid), 2);//at least two rows per side.
            nrowspersidemax += extra_rows_fluff;

            //3. Define arrays to hold the minimum X value for the rows above and below. The labels are added left to right, so the maximum will remain the same.
            //These arrays will be modified below as configurations are added.
            //Maintained in arrays to speed up assessment
            double[] aboveminX = new double[nrowspersidemax];
            double[] belowminX = new double[nrowspersidemax];
            for (int j = 0; j < nrowspersidemax; j++)
            {
                aboveminX[j] = minimumX;
                belowminX[j] = minimumX;
            }

            //4. Now, we have the expected number of rows, the constraints, and the labels which need to fit in them.
            //basic approach:
            //Break into arrangments (sequential collections of labels); alternating above and below the bar to fill.
            //Arrangements are constructed as follows:
            // 1. Try to add a label, assess if it will fit and work in the current arrangement.
            // 2. If a viable arrangement is found, keep it. If not, choose the arrangement with the best score and add it to the final arrangements. Then start a new arrangement collection.
            // 3. Continue adding labels until it either a viable arrangment isn't found or some max (say 6 for 3 rows, 9 for two rows) is met.
            List<LabelBoxArrangement> finalarrang = new List<LabelBoxArrangement>();//collection of the final (best fit) arrangements
            List<LabelBoxArrangement> currarrang = new List<LabelBoxArrangement>();//collection of the current arrangements
            currarrang.Add(LabelBoxArrangement.Empty);//used for the first one.
            PositionedTikzLabelBox[] BoxesOnBar = boxesOnBar.ToArray();
            bool Above = true;
            TikzLabel lastAbove = null;
            TikzLabel lastBelow = null;
            bool consolidated = false;
            while (lblsAtLevel.Count > 0)//while there are labels to evaluate
            {
                TikzLabel lbl = lblsAtLevel.Pop();
                double limiting_cover_X = double.MaxValue;
                if(lblsAtLevel.Count >= max_permited_obstructed_label)//labels might cover too many
                    limiting_cover_X = lbls_for_cover[lbls_for_cover.Count - lblsAtLevel.Count + max_permited_obstructed_label - 1].xCenterofBar;
                if ((Above && lastAbove == lbl) || (!Above && lastBelow == lbl))
                {
                    lblsAtLevel.Push(lbl);
                    break;// throw new Exception("Label placement failure");
                }

                List<LabelBoxArrangement> viableofcurrent = new List<LabelBoxArrangement>();

                for (int j = 0; j < currarrang.Count; j++)
                    for (int k = 0; k < nrowspersidemax; k++)
                    {
                        PositionedTikzLabel[] ptls = EvaluateArrangement(
                            lbl,
                            BoxesOnBar,
                            currarrang[j],
                            k,
                            midtobaseline,
                            (Above) ? (aboveminX) : (belowminX),
                            maximumX,
                            ((Above) ? (1d) : (-1d)) * rowoffset,
                            all_labels_depth_cm,
                            all_labels_height_cm,
                            labelminsep, 
                            leader_buffer,
                            minDelX,
                            maxleaderlength,
                            0.25,
                            consolidated);
                        if (ptls == null) continue;//not viable
                        if (ptls.Length > 1 && ptls[ptls.Length - 1].RightX >= limiting_cover_X) continue;//check if this will cover too many of the remaining labels
                        viableofcurrent.Add(LabelBoxArrangement.Evaluate(ptls, barbaselineY, 0d, rowoffset, rowPenalty, (Above) ? (aboveminX) : (belowminX)));
                    }

                if (viableofcurrent.Count > 0)//at least one is viable...replace currarrange
                {
                    currarrang.Clear();
                    currarrang = viableofcurrent;
                }
                else//none viable...add the best of the current arrange to the final and restart currarrang
                {
                    lblsAtLevel.Push(lbl);//return to the stack
                    if (Above) lastAbove = lbl;
                    else lastBelow = lbl;
                    if (currarrang.Count != 0)
                    {
                        if (!(currarrang.Count == 1 && currarrang[0].IsEmpty))//"initial collection"
                        {
                            LabelBoxArrangement best = currarrang[0];
                            for (int j = 1; j < currarrang.Count; j++)
                                if (best.Score > currarrang[j].Score)
                                    best = currarrang[j];
                            finalarrang.Add(best);

                            //update nrows
                            if (best.Boxes.Length > 0)
                                if (Above)
                                    for (int j = 0; j < best.Boxes.Length; j++)
                                        nrowsabove = Math.Max(nrowsabove, best.Boxes[j].RowIdx + 1);
                                else
                                    for (int j = 0; j < best.Boxes.Length; j++)
                                        nrowsbelow = Math.Max(nrowsbelow, best.Boxes[j].RowIdx + 1);

                            //redefine the minLeft values to accomodate this configuration (prevent the next configuation on this side from lapping this one).
                            for (int j = 0; j < nrowspersidemax; j++)
                            {
                                double yatlev = barbaselineY + ((Above) ? (1d) : (-1d)) * ((double)(j + 1)) * rowoffset;
                                double yminatlev = yatlev - all_labels_depth_cm;
                                double ymaxatlev = yatlev + all_labels_height_cm;
                                if (Above) aboveminX[j] = best.DetermineMaxRightX(j, yminatlev, ymaxatlev, aboveminX[j], labelminsep, leader_buffer);
                                else belowminX[j] = best.DetermineMaxRightX(j, yminatlev, ymaxatlev, belowminX[j], labelminsep, leader_buffer);
                            }
                            currarrang = new List<LabelBoxArrangement>();
                            currarrang.Add(LabelBoxArrangement.Empty);
                        }
                    }
                    Above = !Above;//alternate now
                }
            }//end of while(lblsAtLevel.Count>0)...all of the labels added
            //add the final currang
            if (currarrang.Count != 0)//something to add
            {
                if (!(currarrang.Count == 1 && currarrang[0].IsEmpty))//"initial collection"
                {
                    LabelBoxArrangement best = currarrang[0];
                    for (int j = 1; j < currarrang.Count; j++)
                        if (best.Score > currarrang[j].Score)
                            best = currarrang[j];

                    finalarrang.Add(best);
                    //update nrows
                    if (best.Boxes.Length > 0)
                        if (Above)
                            for (int j = 0; j < best.Boxes.Length; j++)
                                nrowsabove = Math.Max(nrowsabove, best.Boxes[j].RowIdx + 1);
                        else
                            for (int j = 0; j < best.Boxes.Length; j++)
                                nrowsbelow = Math.Max(nrowsbelow, best.Boxes[j].RowIdx + 1);
                }
            }

            List<PositionedTikzLabel> rslt = new List<PositionedTikzLabel>(lblsAtLevel.Count);
            if (OnBar.Count != 0) rslt.AddRange(OnBar.ToArray());
            for (int i = 0; i < finalarrang.Count; i++)
                rslt.AddRange(finalarrang[i].Boxes);
            if (lblsAtLevel.Count != 0)//labels remain...positioning failure!
            {
                all_positioned = false;
                while (lblsAtLevel.Count != 0)
                    rslt.Add(new PositionedTikzLabel(lblsAtLevel.Pop(), maximumX, 0d, maximumX, 0));
            }
            return rslt.ToArray();
        }

        /// <summary> Method to evaluate the arrangment built from the passed label and boxes </summary>
        /// <param name="lbl"></param>
        /// <param name="BoxesOnBar"></param>
        /// <param name="currentArr"></param>
        /// <param name="rowIdx"></param>
        /// <param name="minX"></param>
        /// <param name="maxX"></param>
        /// <param name="yatlev"></param>
        /// <param name="labelminsep"></param>
        /// <param name="minDelX"></param>
        /// <param name="maxleaderlength"></param>
        /// <param name="acuteleaderfrac"></param>
        /// <returns></returns>
        private static PositionedTikzLabel[] EvaluateArrangement(TikzLabel lbl,
            PositionedTikzLabelBox[] BoxesOnBar,
            LabelBoxArrangement currentArr,
            int rowIdx,
            double mid_to_baseline,//0 is assumed to be at the middle of the bar.
            double[] minX_for_config,
            double maxX,
            double row_offset,
            double all_label_depth_cm,
            double all_label_height_cm,
            double labelminsep,
            double leader_buffer,
            double minDelX,
            double maxleaderlength,
            double acute_leader_offset_cm,
            bool consolidated)
        {
            if (acute_leader_offset_cm > lbl.LabelBox.Widthcm) acute_leader_offset_cm = 0d;//keep things from getting crazy
            //Task: See if the label can be added in this configuration and still work
            //0. Initialize parameters for this label 
            //0.1. Initialize the y locations for this row
            double yatlev = ((double)(rowIdx + 1)) * row_offset + mid_to_baseline;
            double yminatlev = yatlev - all_label_depth_cm;// lbl.LabelBox.Depthcm;
            double ymaxatlev = yatlev + all_label_height_cm;// yminatlev + all_label_height_cm;// lbl.LabelBox.Heightcm;
            //0.2. Initialize minimum X locations for each of the rows
            double[] minX = new double[rowIdx + 1];//only need up to the current row
            for (int i = 0; i < rowIdx + 1; i++) minX[i] = minX_for_config[i];//initialize with the minimum values for the configuration as a whole
            for (int i = 0; i < currentArr.Boxes.Length; i++)
                if (currentArr.Boxes[i].RowIdx > rowIdx)//raw positions for rowIdx from the leaders
                    continue;
                else//raw position from the boxes themselves
                    minX[currentArr.Boxes[i].RowIdx] = Math.Max(minX[currentArr.Boxes[i].RowIdx], currentArr.Boxes[i].LeftX + currentArr.Boxes[i].Label.LabelBox.Widthcm);
            minX[rowIdx] = currentArr.DetermineMaxRightX(rowIdx, yminatlev, ymaxatlev, minX[rowIdx], labelminsep, leader_buffer);
            //0.3. Initialize the limit on the maximum value of leftX (the west edge of the box) for this row.
            double maxleftX = maxX - lbl.LabelBox.Widthcm;//the maximum value for the leftX

            //0.4. Special: Consolidate current arrangement.
            //If the current arrangement consists of a stepped stack (high row followed by lower row), consolidate 

            //1. Determine the minimum for the box itself:
            double minleft = minX[rowIdx];
            if (!currentArr.IsEmpty) minleft = currentArr.DetermineMaxRightX(rowIdx, yminatlev, ymaxatlev, minleft, labelminsep, leader_buffer);//the minimum value for the leftX in this row.
            if (minleft > maxleftX) 
                return null;//<============== *FAIL*: adding this label makes the configuration is too wide

            //2. Determine the minimum dictated by the leader (if the leader is drawn from the center of the bar to the left of the box, skirting the edge of the maximum X of rows closer to the bar):
            double leaderX = minleft;
            double limitingY;
            for (int i = 0; i < rowIdx; i++)//iterate through each row, projecting from the center of the bar through the maximum of the row
            {
                limitingY = ((double)(i + 1)) * row_offset + mid_to_baseline;//baseline of row
                limitingY += ((yatlev > 0d && lbl.xCenterofBar > minX[i]) || (yatlev < 0d && lbl.xCenterofBar < minX[i])) ? (all_label_height_cm) : (-all_label_depth_cm);//top/bottom of the box limits.
                leaderX = Math.Max(leaderX, ((yatlev > 0) ? (yminatlev) : (yminatlev)) / limitingY * (minX[i] - lbl.xCenterofBar) + lbl.xCenterofBar + leader_buffer);
            }
            //2.1. Address any crossings to the left (if the leader needs to move to the right to avoid crossing a box on the bar)
            for (int i = 0; i < BoxesOnBar.Length; i++)
                if (BoxesOnBar[i].Label == lbl || BoxesOnBar[i].LeftX > lbl.xCenterofBar) continue;//ignore this box and anything after it
                else
                {
                    limitingY = mid_to_baseline;//baseline of the box
                    limitingY += (yatlev > 0d)? (all_label_height_cm) : (-all_label_depth_cm);//top/bottom of the box limits.
                    leaderX = Math.Max(leaderX, lbl.xCenterofBar - (((yatlev>0)?(yminatlev):(yminatlev)) / limitingY) * (lbl.xCenterofBar - (BoxesOnBar[i].RightX + leader_buffer)));
                }

            //3. Assign the absolute minimum left X for the box
            double leftX = Math.Max(minleft, leaderX - lbl.LabelBox.Widthcm + acute_leader_offset_cm);
            //3.1. Check if it is too far to the right
            if (leftX > maxleftX) 
                return null;//<============== *FAIL*: adding this label makes the configuration is too wide

            //4. Box can fit. Fine-tune box and leader positions.
            //4.1. Minimize leader length if it can move to the right and this is not the first entry on the first row of the arrangement.
            if (leaderX < lbl.xCenterofBar - minDelX)
            {
                if (consolidated)
                    leaderX = Math.Min(Math.Max(leaderX, leftX + acute_leader_offset_cm), lbl.xCenterofBar - minDelX);
                else
                    leaderX = lbl.xCenterofBar - minDelX;
            }
            //4.2. Adjust leaderX to ensure it is not too vertical; adjusting leftX if needed. Both move to the right.
            if (leaderX > lbl.xCenterofBar - minDelX && leaderX < lbl.xCenterofBar + minDelX)//too vertical
            {
                leaderX = lbl.xCenterofBar + minDelX;
            }
            if (leaderX > leftX + lbl.LabelBox.Widthcm - acute_leader_offset_cm)//that moved the leader out of the box...compensate
                leftX = leaderX - lbl.LabelBox.Widthcm + acute_leader_offset_cm;
            if (leftX > maxleftX) 
                return null;//<============== *FAIL*: adding this label makes the configuration is too wide
            //4.3. Check the leader length
            PositionedTikzLabel ptl = new PositionedTikzLabel(lbl, leftX, yatlev, leaderX, rowIdx);
            if (ptl.LeaderLength > maxleaderlength)// * ((consolidated) ? (2d) : (1d)))// && !consolidated) 
                return null;//<============== *FAIL*: adding this label requires a leader which is too long
            //4.4. Check if the leader crosses any boxes (a bit redundant)
            for (int i = 0; i < BoxesOnBar.Length; i++)
                if (BoxesOnBar[i].Label == lbl) continue;//ignore this box
                else
                {
                    double testX = ptl.GetLeaderXAtY(mid_to_baseline + ((yatlev > 0d) ? (all_label_height_cm) : (-all_label_depth_cm)));
                    if (testX >= BoxesOnBar[i].LeftX && testX <= BoxesOnBar[i].RightX)
                        return null;//<============== *FAIL*: adding this label requires a leader which crosses a label on the bar
                    testX = ptl.GetLeaderXAtY(BoxesOnBar[i].UpperX);
                    if (testX >= BoxesOnBar[i].LeftX && testX <= BoxesOnBar[i].RightX)
                        return null;//<============== *FAIL*: adding this label requires a leader which crosses a label on the bar
                }

            //5. Box and leader have passed checks; return
            List<PositionedTikzLabel> Boxes = currentArr.Boxes.ToList();
            Boxes.Add(ptl);
            return Boxes.ToArray();

            ////if (leaderX < lbl.xCenterofBar) leaderX += acuteleaderfrac * lbl.LabelBox.Widthcm;
            //if (currentArr.IsEmpty || rowIdx == 0)//leader can't limit if the first row.(currentArr.IsEmpty || rowIdx == 0)//nothing before this or this is the first row => leader can't limit! Box fits!
            //{
            //place box at minleft. find the shortest leader
            //if (lbl.xCenterofBar - minDelX >= leftX && lbl.xCenterofBar - minDelX <= leftX + lbl.LabelBox.Widthcm)//good to left
            //    leaderX = lbl.xCenterofBar - minDelX;
            //else if (lbl.xCenterofBar + minDelX >= leftX && lbl.xCenterofBar - minDelX >= leftX + lbl.LabelBox.Widthcm)//good to right
            //    leaderX = lbl.xCenterofBar - minDelX;
            //else//outside of the box...go to one of the corners
            //    if (lbl.xCenterofBar < leftX) leaderX = leftX;//box to the right
            //    else leaderX = leftX + lbl.LabelBox.Widthcm;//box to the left

            //if (leaderX > leftX + lbl.LabelBox.Widthcm)//need to move the box to the right
            //    leftX = leaderX - (1d - acuteleaderfrac) * lbl.LabelBox.Widthcm;

            //if (leftX > maxleftX) return null;//<============== *FAIL*: adding this label makes the configuration is too wide

            ////see if the current leader/box is 
            //PositionedTikzLabel ptl = new PositionedTikzLabel(lbl, leftX, yatlev, leaderX, rowIdx);
            //if (ptl.LeaderLength > maxleaderlength) return null;//<============== *FAIL*: adding this label requires a leader which is too long

            ////see if the leader crosses any boxes
            //for (int i = 0; i < BoxesOnBar.Length; i++)
            //    if (BoxesOnBar[i].Label == lbl) continue;
            //    else
            //    {
            //        double testX = ptl.GetLeaderXAtY(BoxesOnBar[i].LowerY);
            //        if (testX >= BoxesOnBar[i].LeftX && testX <= BoxesOnBar[i].RightX) return null;//<============== *FAIL*: adding this label requires a leader which crosses a label on the bar
            //        testX = ptl.GetLeaderXAtY(BoxesOnBar[i].UpperX);
            //        if (testX >= BoxesOnBar[i].LeftX && testX <= BoxesOnBar[i].RightX) return null;//<============== *FAIL*: adding this label requires a leader which crosses a label on the bar
            //    }

            //List<PositionedTikzLabel> boxes = new List<PositionedTikzLabel>();
            //if (!currentArr.IsEmpty) boxes.AddRange(currentArr.Boxes);
            //boxes.Add(ptl);
            //return boxes.ToArray();
            //}
            //need to check the lower rows leader X can ONLY move to the right!
            //if (Math.Abs(leaderX - lbl.xCenterofBar) < minDelX)//make sure it isn't too vertical
            //{
            //    leaderX = lbl.xCenterofBar + minDelX;
            //    if (leaderX > leftX + lbl.LabelBox.Widthcm) leftX = leaderX - lbl.LabelBox.Widthcm;
            //}
            //for (int l = 0; l < rowIdx; l++)
            //{
            //    PositionedTikzLabel mostright = PositionedTikzLabel.Empty;
            //    for (int m = 0; m < currentArr.Boxes.Length; m++)
            //        if (currentArr.Boxes[m].RowIdx == l)//valid row
            //            if (mostright.IsEmpty) mostright = currentArr.Boxes[m];
            //            else if (mostright.RightX < currentArr.Boxes[m].RightX) mostright = currentArr.Boxes[m];
            //    if (mostright.IsEmpty) continue;//no boxes at this level to limit!
            //    //most right may limit the leader location...
            //    if (leaderX > lbl.xCenterofBar && mostright.RightX < lbl.xCenterofBar) continue;//leader goes to the right and the box is before the center (can't limit)
            //    else if (leaderX < lbl.xCenterofBar && mostright.RightX < leaderX) continue;//leader goes to the left and the box is before the leader (can't limit)
            //    else//might limit
            //    {
            //        double delx = mostright.RightX - lbl.xCenterofBar;
            //        double delytop, delybottom;// = mostright.BaseY - barcenterY;
            //        delytop = mostright.BaseY + mostright.Label.LabelBox.Heightcm - mostright.Label.LabelBox.Depthcm;
            //        delybottom = mostright.BaseY - mostright.Label.LabelBox.Depthcm;

            //        double minleaderX = Math.Max(delx / delytop * (yatlev) + lbl.xCenterofBar, delx / delybottom * (yatlev) + lbl.xCenterofBar) + 0.1;
            //        if (minleaderX < leaderX) continue;//doesn't limit...try the next row

            //        leaderX = minleaderX;//does limit!
            //        if (Math.Abs(leaderX - lbl.xCenterofBar) < minDelX) leaderX = lbl.xCenterofBar + minDelX;//make sure it isn't too vertical (should never apply)

            //        if (leaderX > leftX + lbl.LabelBox.Widthcm)//need to move the box to the right
            //            leftX = leaderX - (1d - acuteleaderfrac) * lbl.LabelBox.Widthcm;//shift the label to the right to accomodate...

            //        if (leftX > maxleftX) return null;//<============== *FAIL*: adding this label makes the configuration is too wide
            //    }
            //}

            ////At this point the label fits and minleft and leaderX are viable values...<=add to the list of viable options
            //PositionedTikzLabel ptltest = new PositionedTikzLabel(lbl, leftX, yatlev, leaderX, rowIdx);
            //if (ptltest.LeaderLength > maxleaderlength) return null;//<============== *FAIL*: adding this label requires a leader which is too long
            ////see if the leader crosses any boxes
            //for (int i = 0; i < BoxesOnBar.Length; i++)
            //    if (BoxesOnBar[i].Label == lbl) continue;
            //    else
            //    {
            //        double testX = ptltest.GetLeaderXAtY(BoxesOnBar[i].LowerY);
            //        if (testX >= BoxesOnBar[i].LeftX && testX <= BoxesOnBar[i].RightX) return null;//<============== *FAIL*: adding this label requires a leader which crosses a label on the bar
            //        testX = ptltest.GetLeaderXAtY(BoxesOnBar[i].UpperX);
            //        if (testX >= BoxesOnBar[i].LeftX && testX <= BoxesOnBar[i].RightX) return null;//<============== *FAIL*: adding this label requires a leader which crosses a label on the bar
            //    }

            //List<PositionedTikzLabel> Boxes = currentArr.Boxes.ToList();
            //Boxes.Add(ptltest);
            //return Boxes.ToArray();
        }
    }
}
