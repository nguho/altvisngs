using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace altvisngs
{
    public enum MajorMinorCutoff { AnyGroup, AllGroups, AnyAverage, AllAverages, None_AllMajor }
    abstract class altvisngs_sidebar
    {
        public static void SideBarMeanRelativeAbundance(
            string filepath_output,
            Sample[][] grouped_samples,
            string[] legend_entries,
            int taxa_level,
            MajorMinorCutoff cutoff_criteria,
            double minimum_relabund_major,
            string caption_mandatory,
            string figure_label,
            string relative_filepath_prefix =@"Figures/",
            string caption_optional="",
            string minor_phylotypes = @"Minor phylotypes",
            string unknown="unknown",
            params string[] unknowns)
        {
            Console.WriteLine("Building relative abundance bar plot `" + figure_label + "'...");
            int n_samples = grouped_samples.Length;
            if (n_samples != legend_entries.Length) throw new ArgumentOutOfRangeException("legend-samples dimension mismatch");

            //Get the list of all available nonzero taxons (regardless of level)
            Dictionary<Taxon, _meanphylotypes> all_non_zero_taxons_at_lvl = new Dictionary<Taxon, _meanphylotypes>();
            for (int i = 0; i < n_samples; i++)
            {
                if (grouped_samples[i] == null) continue;
                for (int j = 0; j < grouped_samples[i].Length; j++)
                {
                    Taxon[] taxons = altvisngs_data.GetTaxonsAtLevel_Unsorted(altvisngs_data.GetAllTaxons_NonZero(grouped_samples[i][j]), taxa_level, unknown);
                    for (int k = 0; k < taxons.Length; k++)
                        if (!all_non_zero_taxons_at_lvl.ContainsKey(taxons[k]))
                            all_non_zero_taxons_at_lvl.Add(taxons[k], new _meanphylotypes(taxons[k], n_samples));
                }
            }

            //Go through each of the phylotypes and get all of the relative abundances
            foreach (KeyValuePair<Taxon, _meanphylotypes> kvp in all_non_zero_taxons_at_lvl)
                for (int i = 0; i < n_samples; i++)
                    if (grouped_samples[i] != null)
                    {
                        kvp.Value.Relative_Abundances[i] = new double[grouped_samples[i].Length];
                        for (int j = 0; j < grouped_samples[i].Length; j++)
                            kvp.Value.Relative_Abundances[i][j] = grouped_samples[i][j].SumAllMembersOf(kvp.Key, unknown, true).RelativeAbundance;
                    }
                    else
                        kvp.Value.Relative_Abundances[i] = new double[0];

            //Determine the averages and standard deviations and aggregate
            List<_meanphylotypes> minors = new List<_meanphylotypes>();
            List<_meanphylotypes> majors = new List<_meanphylotypes>();
            foreach (KeyValuePair<Taxon, _meanphylotypes> kvp in all_non_zero_taxons_at_lvl)
            {
                kvp.Value.DetermineStatistics();
                bool ismajor = false;
                switch (cutoff_criteria)
                {
                    case (MajorMinorCutoff.AnyGroup):
                        ismajor = kvp.Value.AnyReactor_geq(minimum_relabund_major);
                        break;
                    case (MajorMinorCutoff.AllGroups):
                        ismajor = kvp.Value.AllReactors_geq(minimum_relabund_major);
                        break;
                    case (MajorMinorCutoff.AnyAverage):
                        ismajor = kvp.Value.AnyMean_geq(minimum_relabund_major);
                        break;
                    case (MajorMinorCutoff.AllAverages):
                        ismajor = kvp.Value.AllMean_geq(minimum_relabund_major);
                        break;
                    case (MajorMinorCutoff.None_AllMajor):
                        ismajor = true;
                        break;
                    default:
                        throw new NotImplementedException("The MajorMinorCutoff `" + cutoff_criteria.ToString() + "' is not recognized.");
                }
                if (ismajor)
                    majors.Add(kvp.Value);
                else
                    minors.Add(kvp.Value);
            }

            //now, build the plot
            string[] x_tick_labels = new string[majors.Count + ((minors.Count == 0) ? (0) : (1))];
            int[] x_positions = new int[majors.Count + ((minors.Count == 0) ? (0) : (1))];
            string[] add_plots = new string[n_samples];

            for (int i = 0; i < majors.Count; i++)
                majors[i].DisplayedName = altvisngs_relabundtbl.TaxonName_StackedBar(
                    majors[i].Taxon, taxa_level,
                    altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons, minor_phylotypes, unknown, unknown, unknowns);
            majors.Sort((a, b) => string.Compare(a.DisplayedName, b.DisplayedName));//altvisngs_data.SortTaxon(a.Taxon,b.Taxon, unknowns));//<===need to impliment!
            for (int i = 0; i < majors.Count; i++)
            {
                x_tick_labels[i] = majors[i].DisplayedName;
                x_positions[i] = i;
            }
            for (int i = 0; i < n_samples; i++)
            {
                add_plots[i] = "%" + legend_entries[i] + Environment.NewLine;
                add_plots[i] += @"\addplot+[legend entry = {" + legend_entries[i] + @"}, error bars/.cd, y dir=both, y explicit, error bar style={mark size=\errbarwid}] coordinates {%" + Environment.NewLine;

                //bug in pgfplots skips empty addplots in legend entries.
                //To overcome, hack by adding a point outside the visible region of the plot (ridiculous)
                add_plots[i] += "(-1,0)+-(0,0)%hack to overcome bug in pgfplots which ignores otherwise empty plots and for the legend" + Environment.NewLine;
                for (int j = 0; j < majors.Count; j++)
                    if (majors[j].Mean[i] > 0d)//don't show zero
                        add_plots[i] += "(" + j.ToString() + "," + (100d * majors[j].Mean[i]).ToString("0.000000000000000") + ")+-(" +
                            (100d * (majors[j].Mean[i] - majors[j].Min[i])).ToString("0.000000000000000") + "," + (100d * (majors[j].Max[i] - majors[j].Mean[i])).ToString("0.000000000000000") + ")" + "%" + majors[j].DisplayedName + Environment.NewLine;

                double[] minorsforreactor = new double[0];//
                if (minors.Count != 0)
                {
                    int samplingdays = minors[0].Relative_Abundances[i].Length;
                    minorsforreactor = new double[samplingdays];
                    for (int j = 0; j < minors.Count; j++)
                        if (minors[j].Relative_Abundances[i].Length != samplingdays) throw new ArgumentOutOfRangeException("unexpected.");
                        else
                            for (int k = 0; k < samplingdays; k++)
                                minorsforreactor[k] += minors[j].Relative_Abundances[i][k];
                    double minormean = 100d * altvisngs_relabundtbl._Mean(minorsforreactor);//minors.Select((m) => (m.Relative_Abundances[i].Sum())).ToArray());
                    double minorstddev = 100d * altvisngs_relabundtbl._StdDev(minorsforreactor);//minors.Select((m) => (m.Relative_Abundances[i].Sum())).ToArray());
                    double minmin = 100d * altvisngs_relabundtbl._Min(minorsforreactor);
                    double minmax = 100d * altvisngs_relabundtbl._Max(minorsforreactor);

                    if (minormean > 0d)
                        add_plots[i] += "(" + majors.Count.ToString() + "," + minormean.ToString("0.000000000000000") + ")+-(" +
                            (minormean - minmin).ToString("0.000000000000000") + "," + (minmax - minormean).ToString("0.000000000000000") + ")" + "%" + minor_phylotypes + " (" + minors.Count.ToString() + ")" + Environment.NewLine; ;
                }
                add_plots[i] += "};" + Environment.NewLine;
            }
            if (minors.Count > 0)
            {
                x_tick_labels[majors.Count] = minor_phylotypes + " (" + minors.Count.ToString() + ")";
                x_positions[majors.Count] = majors.Count;
            }

            string taxonn = altvisngs_initialize.TaxonNameFromLevel_dbcAmplicons(taxa_level);
            taxonn = char.ToUpper(taxonn[0]) + taxonn.Substring(1) + @"-level phylotypes";// with mean relative abundance $>1\,\%$ in all samples"
            SideBarMeanRelativeAbundance(
                filepath_output: filepath_output,
                legend_entries: legend_entries,
                x_tick_labels: x_tick_labels,
                x_label: taxonn,
                x_positions: x_positions,
                add_plots: add_plots,
                caption_mandatory: caption_mandatory,
                figure_label: figure_label,
                relative_filepath_prefix: relative_filepath_prefix,
                caption_optional: caption_optional);
        }

        private class _meanphylotypes
        {
            public string DisplayedName;
            public Taxon Taxon;
            public double[][] Relative_Abundances;

            public double[] Mean;
            public double[] StdDev;
            public double[] Min;
            public double[] Max;

            public _meanphylotypes(Taxon taxon, int samples)
            {
                Taxon = taxon;
                Relative_Abundances = new double[samples][];
            }

            public void DetermineStatistics()
            {
                this.Mean = Relative_Abundances.Select((d) => altvisngs_relabundtbl._Mean(d)).ToArray(); 
                this.StdDev = Relative_Abundances.Select((d) => altvisngs_relabundtbl._StdDev(d)).ToArray();
                this.Min = Relative_Abundances.Select((d) => altvisngs_relabundtbl._Min(d)).ToArray();
                this.Max = Relative_Abundances.Select((d) => altvisngs_relabundtbl._Max(d)).ToArray();
            }

            public bool AnyMean_geq(double cutoff)
            {
                for (int i = 0; i < Mean.Length; i++)
                    if (Mean[i] >= cutoff) return true;
                return false;
            }
            public bool AllMean_geq(double cutoff)
            {
                for (int i = 0; i < Mean.Length; i++)
                    if (Mean[i] < cutoff) return false;
                return true;
            }
            public bool AnyReactor_geq(double cutoff)
            {
                for (int i = 0; i < Relative_Abundances.Length; i++)
                    for (int j = 0; j < Relative_Abundances[i].Length; j++)
                        if(Relative_Abundances[i][j] >= cutoff) return true;
                return false;
            }
            public bool AllReactors_geq(double cutoff)
            {
                for (int i = 0; i < Relative_Abundances.Length; i++)
                    for (int j = 0; j < Relative_Abundances[i].Length; j++)
                        if (Relative_Abundances[i][j] < cutoff) return false;
                return true;
            }
        }

        /// <summary> </summary>
        /// <param name="filepath_output"></param>
        /// <param name="samples"></param>
        private static void SideBarMeanRelativeAbundance(
            string filepath_output,
            string[] legend_entries,
            string[] x_tick_labels,
            string x_label,
            int[] x_positions,
            string[] add_plots,
            string caption_mandatory,
            string figure_label,
            string relative_filepath_prefix =@"Figures/",
            string caption_optional="")
        {
            if (x_positions.Length == 0) return;//nothing to draw

            double barwid = 4.2;//default width in points
            double width = Math.Max(100,(barwid * (double)(legend_entries.Length + 1) + (double)(legend_entries.Length + 1)) * (double)(x_tick_labels.Length + 1));
            if (width > 155540d)
            {
                width = 155540d;
                barwid = (width / ((double)(x_tick_labels.Length + 1)) - ((double)(legend_entries.Length + 1))) / ((double)(legend_entries.Length + 1));
            }
            

            //if (legend_entries.Length > 5) throw new ArgumentOutOfRangeException("Number of legend entries exceeds the capacity of the color cycle");
            //double width = Math.Min(Math.Max(100d,100d/8d*(double)(x_tick_labels.Length)), 5487d);//scale based on size with 8; max size of 19'...will be messed up, but will at least produce an output.
            double height = width / 1.618;

            string rslt =
@"%%%altvisngs output
\documentclass[tikz]{standalone}
\usepackage[scaled]{helvet}
\renewcommand\familydefault{\sfdefault} 
\usepackage[T1]{fontenc}
\usepackage{sansmath}
\sansmath

\usepackage{pgfplots}
\pgfplotsset{width=" + width.ToString("0.0") + "pt,height=" + height.ToString("0.0") + @"pt,compat=newest}

%Patch to allow _ to be underscore in text mode. Requires font encoding to be T1.
%Ref: egreg soln: http://tex.stackexchange.com/a/38720/89497
\catcode`_=12
\begingroup\lccode`~=`_\lowercase{\endgroup\let~\sb}
\mathcode`_=""8000

%custom color cycle
\definecolor{webgreen}{rgb}{0,0.5,0}
\definecolor{webdarkorange}{RGB}{255,140,0}
\definecolor{webcyan}{RGB}{128,128,0}
\definecolor{webdslate}{RGB}{47,79,79}
\pgfplotscreateplotcyclelist{custom5}{%
	{color=red, fill=red!35!white, draw=red},
	{color=blue, fill=blue!35!white, draw=blue},
	{color=webgreen, fill=webgreen!35!white, draw=webgreen},
	{color=violet, fill=violet!35!white, draw=violet},
	{color=webdarkorange, fill=webdarkorange!35!white, draw=webdarkorange},
    {color=webcyan, fill=webcyan!35!white, draw=webcyan},
    {color=webdslate, fill=webdslate!35!white, draw=webdslate}}

%pair legend entry with addplot to allow empty addplots and retain correct legend
%http://tex.stackexchange.com/a/219519/89497
\pgfplotsset{
    legend entry/.initial=,
    every axis plot post/.code={%
        \pgfkeysgetvalue{/pgfplots/legend entry}\tempValue
        \ifx\tempValue\empty
            \pgfkeysalso{/pgfplots/forget plot}%
        \else
            \expandafter\addlegendentry\expandafter{\tempValue}%
        \fi
    },
}
\newcommand\errbarwid{" + (barwid/3d).ToString("0.000") + @"pt}%width of the error bar marks

\begin{document}
	\begin{tikzpicture}
		\begin{axis}[%
			ybar=1pt,%
			ymin=0,
			ylabel={Mean relative abundance ($\%$)}," + Environment.NewLine;
			//legend entries={";
            //rslt += string.Join(",", legend_entries) + "}," + Environment.NewLine;
            rslt += @"			xtick={" + string.Join(",", x_positions) + "}," + Environment.NewLine;
            rslt += @"          xticklabels={" + string.Join(",", x_tick_labels) + "}," + Environment.NewLine;
            rslt += @"          xlabel={" + x_label + "}," + Environment.NewLine;
            rslt += @"			x tick label style={yshift=-5pt, rotate=45, anchor=north east},
            x axis line style = {yshift=-5pt},
	        xtick style={very thin,yshift=-5pt},
			xmin=-0.5,
			xmax=";
            rslt += x_positions[x_positions.Length - 1].ToString() + ".5," + Environment.NewLine;
            rslt += @"			bar width=" + barwid + @"pt,
			axis on top,
			legend style={draw=none, at={(1.01, 1)},anchor=north west},
			legend cell align=left,
			cycle list name=custom5],
";
            rslt += string.Join(Environment.NewLine, add_plots);
            rslt +=
@"   \end{axis}
\end{tikzpicture}
\end{document}";

            Console.WriteLine("Writing to `" + Path.GetFileName(filepath_output) + "'");
            using (StreamWriter sw = new StreamWriter(filepath_output))
            {
                sw.Write(rslt);
            }
            pdflatex pdflatex = new pdflatex();
            try
            {
                pdflatex.RunSync(filepath_output);
            }
            catch (Exception ex)
            {
                Console.WriteLine("pdflatex exited with error: " + ex.Message);
            }

            LaTeX_Figure.figure_inclgrphx(
                filepath_output_caption: filepath_output.Replace(".tex", "_float.tex"),
                filepath_output_captionof: filepath_output.Replace(".tex", "_nofloat.tex"),
                figure_pdf_relative_path: relative_filepath_prefix + Path.GetFileNameWithoutExtension(filepath_output),
                caption_mandatory: caption_mandatory,
                figure_label: figure_label,
                caption_optional: caption_optional);
        }
    }
}
