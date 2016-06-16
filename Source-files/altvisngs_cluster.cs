using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace altvisngs
{
    /* Hierarchical cluster analysis
     * 24 APR 2016
     * Nick Guho 
     * Adapted from `Methods in Multivariate Analysis' by Alvin C. Rencher and William F. Christensen 
     * 
     */
    /// <summary> The method used in the evaluation of the distance between clusters to select the next for joining </summary>
    public enum LinkageMethod { SingleLinkage, CompleteLinkage, AverageLinkage, Centroid, Median, Wards, FlexibleBeta }
    /// <summary> The method used to determine the distance between clusters </summary>
    public enum DistanceMethod { Euclidean, SquaredEuclidean, BrayCurtis }

    /// <summary> Abstract class implementing hierarchical agglomerative clustering algorithms with output to latex/tikz </summary>
    abstract class altvisngs_cluster
    {
        
        /// <summary> Cluster analysis test data from Chapter 15 of Methods of Multivariate Analysis </summary>
        /// <returns></returns>
        private static SampleAsCluster[] TestData()
        {
            List<SampleAsCluster> s = new List<SampleAsCluster>();
            s.Add(new SampleAsCluster("Atlanta", new double[] { 16.5, 24.8, 106, 147, 1112, 905, 494 }, "blue"));
            s.Add(new SampleAsCluster("Boston", new double[] { 4.2, 13.3, 122, 90, 982, 669, 954 }));
            s.Add(new SampleAsCluster("Chicago", new double[] { 11.6, 24.7, 340, 242, 808, 609, 645 }));
            s.Add(new SampleAsCluster("Dallas", new double[] { 18.1, 34.2, 184, 293, 1668, 901, 602 }));
            s.Add(new SampleAsCluster("Denver", new double[] { 6.9, 41.5, 173, 191, 1534, 1368, 780 }, "red"));
            s.Add(new SampleAsCluster("Detroit", new double[] { 13.0, 35.7, 477, 220, 1566, 1183, 788 }));
            s.Add(new SampleAsCluster("Hartford", new double[] { 2.5, 8.8, 68, 103, 1017, 724, 468 }));
            s.Add(new SampleAsCluster("Honolulu", new double[] { 3.6, 12.7, 42, 28, 1457, 1102, 637 }, "red"));
            s.Add(new SampleAsCluster("Houston", new double[] { 16.8, 26.6, 289, 186, 1509, 787, 697 }));
            s.Add(new SampleAsCluster("Kansas City", new double[] { 10.8, 43.2, 255, 226, 1494, 955, 765 }));
            s.Add(new SampleAsCluster("Los Angeles", new double[] { 9.7, 51.8, 286, 355, 1902, 1386, 862 }));
            s.Add(new SampleAsCluster("New Orleans", new double[] { 10.3, 39.7, 266, 283, 1056, 1036, 776 }));
            s.Add(new SampleAsCluster("New York", new double[] { 9.4, 19.4, 522, 267, 1674, 1392, 848 }));
            s.Add(new SampleAsCluster("Portland", new double[] { 5.0, 23.0, 157, 144, 1530, 1281, 488 }, "red", "blue"));
            s.Add(new SampleAsCluster("Tucson", new double[] { 5.1, 22.9, 85, 148, 1206, 756, 482 }, "blue"));
            s.Add(new SampleAsCluster("Washington", new double[] { 12.5, 27.6, 524, 217, 1496, 1003, 739 }));
            return s.ToArray();
        }

        /// <summary>Method to perform hierarchical agglomerative cluster analysis on the samples according to the passed criteria using the selected distance and linkage methods and outputting the result to a tex file for visualization </summary>
        /// <param name="filepath_key"></param>
        /// <param name="filePath_output"></param>
        /// <param name="samplecriteria"></param>
        /// <param name="level"></param>
        /// <param name="labeltext"></param>
        /// <param name="distmeth"></param>
        /// <param name="linkmeth"></param>
        /// <param name="beta"></param>
        /// <param name="labelstyle"></param>
        /// <param name="linestyle"></param>
        /// <param name="ID"></param>
        /// <param name="unknown"></param>
        /// <param name="baselinestyle"></param>
        /// <param name="baselabelstyle"></param>
        public static void ClusterAnalysis(
            string filepath_key,
            string filePath_output,
            Sample[] samples,
            string[] groups,
            string color_on_attr,
            int level,
            Func<Sample,
            string> labeltext,
            string caption_mandatory,
            string figure_label,
            string relative_filepath_prefix = @"Figures/",
            string caption_optional = "",
            DistanceMethod distmeth = DistanceMethod.BrayCurtis,
            LinkageMethod linkmeth = LinkageMethod.AverageLinkage,
            double beta = double.NaN,
            string ID="ID",
            string unknown="unknown",
            string baselinestyle="thick",
            string baselabelstyle="")
        {
            //Sample[] samples = altvisngs_data.OpenSamples(filepath_key, ID, samplecriteria);
            string[] _colors = new string[] { "red", "blue", "webgreen", "violet", "webdarkorange", "webcyan", "webslate" };
                                    
            GroupedObservations[] data = altvisngs_data.BuildTaxonObservations(samples, level, unknown);
            //build the clusters (base on abundance)
            List<altvisngs_cluster.SampleAsCluster> clust = new List<altvisngs_cluster.SampleAsCluster>();
            for (int i = 0; i < samples.Length; i++)
            {
                double[] rslt = new double[data.Length];
                for (int k = 0; k < rslt.Length; k++)
                    rslt[k] = data[k].Observations[i].Abundance;
                clust.Add(new altvisngs_cluster.SampleAsCluster(labeltext(samples[i]), rslt, 
                    (string.IsNullOrEmpty(color_on_attr))?(DefaultColorFormat(samples[i].GetAttr(color_on_attr),groups,_colors)):(""),
                    (string.IsNullOrEmpty(color_on_attr))?(DefaultColorFormat(samples[i].GetAttr(color_on_attr),groups,_colors)):("")));
            }
            altvisngs_cluster.ClusterAnalysis(
                filePath_output:filePath_output,
                data: clust.ToArray(),
                distanceMethod: distmeth,
                linkageMethod: linkmeth,
                caption_mandatory:caption_mandatory,
                figure_label: figure_label,
                relative_filepath_prefix: relative_filepath_prefix,
                caption_optional: caption_optional,
                beta: beta,
                baselinestyle: baselinestyle,
                baselabelstyle: baselabelstyle);
        }
        
        /// <summary> Method to perform hierarchical agglomerative cluster analysis on the passed data using the selected distance and linkage methods and outputting the result to a tex file for visualization </summary>
        /// <param name="filePath_output"></param>
        /// <param name="data"></param>
        /// <param name="distanceMethod"></param>
        /// <param name="linkageMethod"></param>
        /// <param name="beta"></param>
        /// <param name="baselinestyle"></param>
        /// <param name="baselabelstyle"></param>
        private static void ClusterAnalysis(
            string filePath_output, 
            SampleAsCluster[] data, 
            DistanceMethod distanceMethod, 
            LinkageMethod linkageMethod,
            string caption_mandatory,
            string figure_label,
            string relative_filepath_prefix = @"Figures/",
            string caption_optional = "",
            double beta = double.NaN, 
            string baselinestyle = "thick", 
            string baselabelstyle = "")
        {
            if (data.Length == 0) return;
            Console.WriteLine("Building hierarchical cluster analysis `" + figure_label + "'...");
            bool takesqrtofdist = false;
            string xlabel = string.Empty;
            string xdistlabel = string.Empty;
            Func<int, int, int, double> alphaA = null;
            Func<int, int, int, double> alphaB = null;
            Func<int, int, int, double> beta_meth = null;
            double gamma = double.NaN;

            //establish the distance method
            Func<double[], double[], double> dist_meth = altvisngs_cluster.LinkageandDistanceMethods_FromValue(
                distanceMethod,
                linkageMethod,
                beta,
                out takesqrtofdist,
                out alphaA,
                out alphaB,
                out beta_meth,
                out gamma,
                out xlabel);

            List<Cluster> s = new List<Cluster>();
            s.AddRange(data);
            double mindiss;
            double[][] olddissmat = null;
            Cluster[] oldCluster = null;
            while (s.Count > 1)
            {
                double[][] dissmat;
                if (olddissmat == null)
                    dissmat = Dissimilarity_Intialize(s.ToArray(), dist_meth);//(dissmat = Dissimilarity(s.ToArray(), (A, B) => altvisngs_cluster.SingleLinkage(A, B, dist_meth));
                else
                    dissmat = Dissimilarity_FlexibleBeta(s.ToArray(), oldCluster, olddissmat, alphaA, alphaB, beta_meth, gamma);

                mindiss = double.MaxValue;
                for (int i = 0; i < dissmat.Length; i++)//find the minimum dissimilarity
                    for (int j = 0; j < i; j++)//don't assess diagonal
                        mindiss = Math.Min(mindiss, dissmat[i][j]);

                //As more than one might have the same minimum, multiple clustering may be necessary
                List<Cluster[]> newclusters = new List<Cluster[]>();
                for (int i = 0; i < dissmat.Length; i++)
                    for (int j = 0; j < i; j++)//don't make it one
                        if (dissmat[i][j] == mindiss)
                            newclusters.Add(new Cluster[] { s[i], s[j] });

                olddissmat = dissmat;
                oldCluster = s.ToArray();
                for (int i = 0; i < newclusters.Count; i++)
                {
                    s.Remove(newclusters[i][0]);
                    s.Remove(newclusters[i][1]);
                    s.Add(new Cluster(newclusters[i], mindiss));
                }
            }
            s[0].SortSubordinates(altvisngs_cluster.Alphanumericdate);//sort so that the labels are in the best order possible

            string tikz =
@"\documentclass[tikz]{standalone}
\usepackage[scaled]{helvet}
\renewcommand\familydefault{\sfdefault} 
\usepackage[T1]{fontenc}
\usepackage{sansmath}
\sansmath

\definecolor{webgreen}{rgb}{0,0.5,0}
\definecolor{webdarkorange}{RGB}{255,140,0}
\definecolor{webcyan}{RGB}{128,128,0}
\definecolor{webdslate}{RGB}{47,79,79}
\usetikzlibrary{calc}
\usepackage{pgfplots}
\pgfplotsset{compat=newest}
\edef\dendrolabelsep{0.5cm}
\begin{document}
   \begin{tikzpicture}
      \begin{axis}[%
         width=12cm,
         clip=false,
         hide y axis,
         axis x line*=middle,
         x dir=reverse,
         ymin=0,
         xmin=0,
         extra x ticks={0},
         xlabel={";

            tikz += xlabel + "}";
            tikz += @"]
	\addplot[draw=none,mark=none] coordinates {" + s[0].TikZpgfplotsCoords(takesqrtofdist) + "};" + Environment.NewLine;
            tikz += s[0].TikZNodes(takesqrtofdist, 0, @"\dendrolabelsep", baselabelstyle);
            tikz += s[0].TikZDendrogram(takesqrtofdist, baselinestyle);
            tikz += @"      \end{axis}
   \end{tikzpicture}
\end{document}";

            Console.WriteLine("Writing to `" + Path.GetFileName(filePath_output) + "'.");
            using (StreamWriter sw = new StreamWriter(filePath_output))
            {
                sw.Write(tikz);
            }
            pdflatex pdflatex = new pdflatex();
            pdflatex.RunSync(filePath_output);
            LaTeX_Figure.figure_inclgrphx(
                filepath_output_caption: filePath_output.Replace(".tex", "_float.tex"),
                filepath_output_captionof: filePath_output.Replace(".tex", "_nofloat.tex"),
                figure_pdf_relative_path: relative_filepath_prefix + Path.GetFileNameWithoutExtension(filePath_output),
                caption_mandatory: caption_mandatory,
                figure_label: figure_label,
                caption_optional: caption_optional);
        }

        private static string DefaultColorFormat(string value, string[] groups, string[] colors)
        {
            int coloridx = 0;
            if(colors.Length>0)
                for(int i=0;i<groups.Length;i++)
                {
                    if(groups[i] == value) return colors[coloridx];
                    coloridx++;
                    if(coloridx==colors.Length)
                        coloridx =0;
                }
            return "";
        }

        public static void DissimilarityMatrix(
            string filePath_output,
            Sample[][] grouped_samples,
            string[] attrvals_dim_0,
            string attrname_dim_1,
            DistanceMethod distanceMethod,
            LinkageMethod linkageMethod,
            string mandatory_caption,
            string optional_caption,
            string label,
            double beta = double.NaN,
            string tabular_col_sep = @"&",
            string tabular_new_line = @"\tabularnewline")
        {
            if (grouped_samples.Length == 0) return;
            string[] exponentind = new string[] { "E" };
            Console.WriteLine("Building dissimilarity table `" + label + "'...");
            bool takesqrtofdist = false;
            string xlabel = string.Empty;
            Func<int, int, int, double> alphaA = null;
            Func<int, int, int, double> alphaB = null;
            Func<int, int, int, double> beta_meth = null;
            double gamma = double.NaN;

            //establish the distance method
            Func<double[], double[], double> dist_meth = altvisngs_cluster.LinkageandDistanceMethods_FromValue(
                distanceMethod,
                linkageMethod,
                beta,
                out takesqrtofdist,
                out alphaA,
                out alphaB,
                out beta_meth,
                out gamma,
                out xlabel);

            List<Cluster> s = new List<Cluster>();
            for(int i=0;i<grouped_samples.Length;i++)//add them in the same order they will be presented in (ensure the matrix is correct)
                for(int j=0;j<grouped_samples[i].Length;j++)
                    s.Add(new SampleAsCluster("",grouped_samples[i][j].TaxonObservations.Select((t) => (double)(t.Observation.Abundance)).ToArray()));
            double[][] dissmat = Dissimilarity_Intialize(s.ToArray(), dist_meth);//(dissmat = Dissimilarity(s.ToArray(), (A, B) => altvisngs_cluster.SingleLinkage(A, B, dist_meth));
            string[] heading = new string[3];
            List<string> tabular_columns = new List<string>();
            tabular_columns.Add(@"c");
            tabular_columns.Add(@"c");
            for (int i = 0; i < grouped_samples.Length; i++)
                for (int j = 0; j < grouped_samples[i].Length; j++)
                    tabular_columns.Add("S" + ((j == grouped_samples[i].Length-1)?(((i==grouped_samples.Length-1 && j== grouped_samples[i].Length -1))?(""):(@"")):(@"@{\hskip 0.7\tabcolsep}")));

            heading[0] = "&&";//attrname_dim_0 + tabular_col_sep + tabular_col_sep;//
            int left = 3;
            int right = 0;
            for (int i = 0; i < grouped_samples.Length; i++)
            {
                heading[0] += @"\multicolumn{" + grouped_samples[i].Length.ToString() + "}{c}{" + attrvals_dim_0[i] + "}" + ((i != grouped_samples.Length - 1) ? (tabular_col_sep) : (tabular_new_line));
                right = left + grouped_samples[i].Length -1;
                heading[1] += @"\cmidrule(l" + ((i==grouped_samples.Length -1)?("l"):("r")) + "){" + left.ToString() + "-" + right.ToString() + "} ";//<=== the last cmidrule needs to be ll if it is to span the entire column
                left = right + 1;
            }            

            //the sub headings may be numerical. If so, align with siunitx. if not, brace all.
            bool subisnumb = true;
            List<string> vals = new List<string>();
            for (int i = 0; i < grouped_samples.Length; i++)
                if(subisnumb)
                    for (int j = 0; j < grouped_samples[i].Length; j++)
                    {
                        double dummy;
                        if (!double.TryParse(grouped_samples[i][j].GetAttr(attrname_dim_1), out dummy))
                        {
                            subisnumb = false;
                            break;
                        }
                        else vals.Add(grouped_samples[i][j].GetAttr(attrname_dim_1));
                    }
            heading[2] = "&&";//tabular_col_sep + attrname_dim_1 + tabular_col_sep;            
            for (int i = 0; i < grouped_samples.Length; i++)
                for(int j=0; j < grouped_samples[i].Length; j++)
                    heading[2] += ((subisnumb) ? ("") : ("{")) + grouped_samples[i][j].GetAttr(attrname_dim_1) + ((subisnumb) ? ("") : ("}")) + ((i == grouped_samples.Length - 1 && j == grouped_samples[i].Length - 1) ? (tabular_new_line) : (tabular_col_sep));
            LaTeX_Table.siunitx_Sdefault min = new LaTeX_Table.siunitx_Sdefault(0, 0, 0, false, false);//
            if (subisnumb)//find the maximum siunitx format
            {
                for (int i = 0; i < vals.Count; i++)
                    min = LaTeX_Table.siunitx_Sdefault.GetWidest(min, LaTeX_Table.siunitx_Sdefault.GetFromFormattedResult(vals[i], exponentind));
            }

            List<string> body = new List<string>();// string.Empty;
            int dissmatidx = 0;
            for (int i = 0; i < grouped_samples.Length; i++)
            {
                for (int j = 0; j < grouped_samples[i].Length; j++)
                {
                    string bod = string.Empty;
                    if (j == 0)//first one...add rotated row entry for sample group
                        bod += @"\parbox[t]{2mm}{\multirow{" + grouped_samples[i].Length.ToString() + @"}{*}{\rotatebox[origin=c]{90}{" +attrvals_dim_0[i] + @"}}}";
                    bod += tabular_col_sep + grouped_samples[i][j].GetAttr(attrname_dim_1) + tabular_col_sep;
                    for (int k = 0; k < dissmat[dissmatidx].Length; k++)
                    {
                        string val =(100d * dissmat[dissmatidx][k]).ToString("0") ;
                        min = LaTeX_Table.siunitx_Sdefault.GetWidest(min, LaTeX_Table.siunitx_Sdefault.GetFromFormattedResult(val, exponentind));
                        bod += val + ((k != dissmat[dissmatidx].Length - 1) ? (tabular_col_sep) : (tabular_new_line));
                    }
                    body.Add(bod);
                    dissmatidx++;
                }
                if (i != grouped_samples.Length - 1)
                    body.Add(@"\addlinespace");//\breakableaddlinespace
            }

            LaTeX_Table.ThreePartTable_longtable(
                file_path:filePath_output,
                tabular_cols: tabular_columns.ToArray(),
                table_heading: heading,
                table_body: body.ToArray(),
                table_notes: null,
                mandatory_caption: mandatory_caption,
                table_label: label,
                caption_option:optional_caption,
                si_setup:  "table-number-alignment=center,tight-spacing=true," + min.ToString(),//table-figures-integer=3,table-figures-decimal=0",
                refine_si_columns: false,//want the columns to be the same width.
                tabcolsep: tabular_col_sep,
                tabular_new_line: tabular_new_line,
                new_tab_col_sep_length: "5pt");
        }

        /// <summary> Get the distance method from the DistanceMethod </summary>
        /// <param name="distance_method"></param>
        /// <param name="distance_label"></param>
        /// <returns></returns>
        private static Func<double[], double[], double> DistanceMethod_FromValue(DistanceMethod distance_method, out string distance_label)
        {
            distance_label = string.Empty;
            //establish the distance method
            Func<double[], double[], double> dist_meth = null;
            switch (distance_method)
            {
                case(DistanceMethod.Euclidean):
                    dist_meth = altvisngs_cluster.Euclidean;
                    distance_label = "Euclidean distance";
                    break;
                case(DistanceMethod.SquaredEuclidean):
                    dist_meth = altvisngs_cluster.SquaredEuclidean;
                    distance_label = "squared Euclidean distance";
                    break;
                case(DistanceMethod.BrayCurtis):
                    dist_meth = altvisngs_cluster.BrayCurtis;
                    distance_label = "Bray-Curtis dissimilarity";
                    break;
                default:
                    throw new NotImplementedException("DistanceMethod `" + distance_method.ToString() + "' not implemented.");
            }
            return dist_meth;
        }

        /// <summary> Get the distance and linkage methods </summary>
        /// <param name="distance_method"></param>
        /// <param name="linkage_method"></param>
        /// <param name="passed_beta"></param>
        /// <param name="takes_sqrt_of_dist"></param>
        /// <param name="alphaA"></param>
        /// <param name="alphaB"></param>
        /// <param name="beta_meth"></param>
        /// <param name="gamma"></param>
        /// <param name="combined_label"></param>
        /// <returns></returns>
        private static Func<double[], double[], double> LinkageandDistanceMethods_FromValue(
            DistanceMethod distance_method,
            LinkageMethod linkage_method,
            double passed_beta,
            out bool takes_sqrt_of_dist,
            out Func<int, int, int, double> alphaA,
            out Func<int, int, int, double> alphaB,
            out Func<int, int, int, double> beta_meth,
            out double gamma,
            out string combined_label)
        {
            string distance_method_label = null;
            takes_sqrt_of_dist = false;
            Func<double[], double[], double> dist_meth_func = DistanceMethod_FromValue(distance_method, out distance_method_label);
            //establish the linkage method (using the flexible beta method as the base)
            alphaA = null;
            alphaB = null;
            beta_meth = null;
            gamma = double.NaN;

            switch (linkage_method)
            {
                case (LinkageMethod.SingleLinkage):
                    alphaA = (a, b, c) => (0.5d);
                    alphaB = (a, b, c) => (0.5d);
                    beta_meth = (a, b, c) => (0d);
                    gamma = -0.5;
                    combined_label = "Minimum " + distance_method_label + " between clusters";
                    break;
                case (LinkageMethod.CompleteLinkage):
                    alphaA = (a, b, c) => (0.5d);
                    alphaB = (a, b, c) => (0.5d);
                    beta_meth = (a, b, c) => (0d);
                    gamma = 0.5;
                    combined_label = "Maximum " + distance_method_label + " between clusters";
                    break;
                case (LinkageMethod.AverageLinkage):
                    alphaA = (a, b, c) => (((double)a) / ((double)(a + b)));
                    alphaB = (a, b, c) => (((double)b) / ((double)(a + b)));
                    beta_meth = (a, b, c) => (0d);
                    gamma = 0d;
                    combined_label = "Average " + distance_method_label + " between clusters";
                    break;
                case (LinkageMethod.Centroid):
                    if (distance_method == DistanceMethod.Euclidean)
                    {
                        dist_meth_func = altvisngs_cluster.SquaredEuclidean;
                        takes_sqrt_of_dist = true;//throw new ArgumentOutOfRangeException("For the centroid linkage method, squared euclidean distance should be used.");
                    }
                    alphaA = (a, b, c) => (((double)a) / ((double)(a + b)));
                    alphaB = (a, b, c) => (((double)b) / ((double)(a + b)));
                    beta_meth = (a, b, c) => (((double)(-a * b)) / (Math.Pow((double)(a + b), 2d)));
                    gamma = 0d;
                    combined_label = distance_method_label + " between cluster centroids";
                    break;
                case (LinkageMethod.Median):
                    if (distance_method == DistanceMethod.Euclidean)
                    {
                        dist_meth_func = altvisngs_cluster.SquaredEuclidean;
                        takes_sqrt_of_dist = true;// throw new ArgumentOutOfRangeException("For the median linkage method, squared euclidean distance should be used.");
                    }
                    alphaA = (a, b, c) => (0.5);
                    alphaB = (a, b, c) => (0.5);
                    beta_meth = (a, b, c) => (-0.25);
                    gamma = 0d;
                    combined_label = distance_method_label + " between cluster medians";
                    break;
                case (LinkageMethod.Wards):
                    if (distance_method != DistanceMethod.Euclidean && distance_method != DistanceMethod.SquaredEuclidean)
                        throw new ArgumentOutOfRangeException("For Ward's linkage method, Euclidean/squared Eculidean distance should be used.");
                    if (distance_method == DistanceMethod.Euclidean)
                    {
                        dist_meth_func = altvisngs_cluster.SquaredEuclidean;
                        takes_sqrt_of_dist = true;//throw new ArgumentOutOfRangeException("For Ward's linkage method, squared euclidean distance should be used.");
                    }
                    alphaA = (a, b, c) => (((double)(a + c)) / ((double)(a + b + c)));
                    alphaB = (a, b, c) => (((double)(b + c)) / ((double)(a + b + c)));
                    beta_meth = (a, b, c) => (((double)(-c)) / ((double)(a + b + c)));
                    gamma = 0d;
                    combined_label = "Cluster increase in SSE (" + distance_method_label + ")";
                    break;
                case (LinkageMethod.FlexibleBeta):
                    if (double.IsNaN(passed_beta) || passed_beta >= 1d) 
                        throw new ArgumentOutOfRangeException("The beta value `" + passed_beta.ToString() + "' is invalid; beta must be a number less than one.");
                    alphaA = (a, b, c) => ((1d - passed_beta) / 2d);
                    alphaB = (a, b, c) => ((1d - passed_beta) / 2d);
                    beta_meth = (a, b, c) => (passed_beta);
                    gamma = 0d;
                    combined_label = "Flexible-beta " + distance_method_label;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unrecognized LinkageMethod `" + linkage_method.ToString() + "'");
            }
            return dist_meth_func;
        }

        #region Label Sorting
        /// <summary> Comparer that breaks the strings down into an alpha and a numeric component. If the alphas match, then comparison is done on the numeric component. If not, alphabetical. </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        private static int Alphanumericdate(string A, string B)
        {
            string[] splitA = altvisngs_cluster.SplitAlphaNumeric(A);
            string[] splitB = altvisngs_cluster.SplitAlphaNumeric(B);
            if ((!string.IsNullOrEmpty(splitA[1]) && !string.IsNullOrEmpty(splitB[1])) && splitA[0] == splitB[0])//splits as alpha numeric
            {
                double dblA, dblB;

                if (!double.TryParse(splitA[1], out dblA) || !double.TryParse(splitB[1], out dblB))
                    return string.Compare(A, B);
                if (dblA < dblB) return -1;
                if (dblA == dblB) return 0;
                return 1;
            }
            else//try as alpha date
            {
                splitA = SplitAlphaDate(A);
                splitB = SplitAlphaDate(B);
                if ((!string.IsNullOrEmpty(splitA[1]) && !string.IsNullOrEmpty(splitB[1])) && splitA[0] == splitB[0])//splits as alpha date
                {
                    DateTime dateA, dateB;
                    if (!DateTime.TryParse(splitA[1], out dateA) || !DateTime.TryParse(splitB[1], out dateB))
                        return string.Compare(A, B);
                    return DateTime.Compare(dateA, dateB);
                }
            }
            return string.Compare(A, B);
        }

        /// <summary> Method to split the string into alpha and numeric components (if more than one of each, then all lumped into alpha) </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string[] SplitAlphaNumeric(string str)
        {
            string[] rslt = new string[] { string.Empty, string.Empty };//alpha in 0, num in 1.
            if (!string.IsNullOrEmpty(str))
            {
                bool numer = char.IsNumber(str[0]);
                for (int i = 0; i < str.Length; i++)
                    if (char.IsNumber(str[i]))
                    {
                        if (!numer && !string.IsNullOrEmpty(rslt[1]))//this is cycling back to numeral...multiple numerals
                            return new string[] { str, string.Empty };
                        rslt[1] += str[i];
                        numer = true;
                    }
                    else
                    {
                        if (numer && !string.IsNullOrEmpty(rslt[0]))//this is cycling back to alpha...multiple alphas
                            return new string[] { str, string.Empty };
                        rslt[0] += str[i];
                    }
            }
            return rslt;
        }

        private static string[] SplitAlphaDate(string str)
        {
            string[] rslt = new string[] { string.Empty, string.Empty };//alpha in 0, num in 1.
            if (string.IsNullOrEmpty(str)) return rslt;
            DateTime dtrslt;
            //try starting at 0 first
            for (int i = 1; i < str.Length; i++)
                if (DateTime.TryParse(str.Substring(0, i), out dtrslt))
                    return new string[] { str.Substring(i), str.Substring(0, i) };
            //try at the end.
            for (int i = 0; i < str.Length - 1; i++)
                if (DateTime.TryParse(str.Substring(i), out dtrslt))
                    return new string[] { str.Substring(0,i), str.Substring(i) };
            return new string[] { str, string.Empty };
        }

        #endregion

        #region Dissimilarity (distance) Matrix
        /// <summary> Initialize the dissimilarity matrix </summary>
        /// <param name="categories"></param>
        /// <param name="DistanceMethod"></param>
        /// <returns></returns>
        private static double[][] Dissimilarity_Intialize(Cluster[] categories, Func<double[], double[], double> DistanceMethod)
        {
            double[][] rslt = new double[categories.Length][];
            for (int i = 0; i < categories.Length; i++)
            {
                rslt[i] = new double[i + 1];
                rslt[i][i] = 0d;
                for (int j = 0; j < i; j++)
                    rslt[i][j] = DistanceMethod(categories[i].Reads[0], categories[j].Reads[0]);//, categories[i].SumsOfReads[0] + categories[j].SumsOfReads[0]);
            }
            return rslt;
        }
        /// <summary> Refine the dissimilarity matrix using the Flexible Beta (and therefore any of the other defined) linkage method </summary>
        /// <param name="newClusters"></param>
        /// <param name="oldClusters"></param>
        /// <param name="oldDissimilarity"></param>
        /// <param name="alphaA"></param>
        /// <param name="alphaB"></param>
        /// <param name="beta"></param>
        /// <param name="gamma"></param>
        /// <returns></returns>
        private static double[][] Dissimilarity_FlexibleBeta(Cluster[] newClusters, Cluster[] oldClusters, double[][] oldDissimilarity,
            Func<int, int, int, double> alphaA,
            Func<int, int, int, double> alphaB,
            Func<int, int, int, double> beta, double gamma)
        {
            int[][] oldidx = new int[newClusters.Length][];//array to translate from the new index to the old index.
            for(int i=0;i<newClusters.Length;i++)
                if(!oldClusters.Contains(newClusters[i]))//multi
                    oldidx[i] = new int[] { Array.FindIndex(oldClusters,(c)=>(c==newClusters[i].A)), Array.FindIndex(oldClusters, (c)=>(c==newClusters[i].B))};
                else
                    oldidx[i] = new int[] { Array.FindIndex(oldClusters,(c)=>(c==newClusters[i]))};

            double[][] rslt = new double[newClusters.Length][];
            for (int i = 0; i < newClusters.Length; i++)
            {
                rslt[i] = new double[i + 1];
                rslt[i][i] = 0d;
                for (int j = 0; j < i; j++)
                {
                    if (oldidx[i].Length == 1 && oldidx[j].Length == 1)//pull directly from oldD
                        rslt[i][j] = TriMatVal(oldDissimilarity,oldidx[i][0],oldidx[j][0]);
                    else//one of them is the last joined
                    {
                        double DAB, DCA, DCB;
                        int nA, nB, nC;
                        if (oldidx[i].Length == 1)//cluster at j is the last one joined
                        {
                            DAB = TriMatVal(oldDissimilarity, oldidx[j][0], oldidx[j][1]);
                            nA = newClusters[j].A.SubordinateCount;
                            nB = newClusters[j].B.SubordinateCount;

                            DCA = TriMatVal(oldDissimilarity, oldidx[j][0], oldidx[i][0]);
                            DCB = TriMatVal(oldDissimilarity, oldidx[j][1], oldidx[i][0]);
                            nC = newClusters[i].SubordinateCount;
                        }
                        else//cluster at i is the last one joined
                        {
                            DAB = TriMatVal(oldDissimilarity, oldidx[i][0], oldidx[i][1]);
                            nA = newClusters[i].A.SubordinateCount;
                            nB = newClusters[i].B.SubordinateCount;

                            DCA = TriMatVal(oldDissimilarity, oldidx[i][0], oldidx[j][0]);
                            DCB = TriMatVal(oldDissimilarity, oldidx[i][1], oldidx[j][0]);
                            nC = newClusters[j].SubordinateCount;
                        }
                        rslt[i][j] = FlexibleBeta(DCA, DCB, DAB, alphaA(nA, nB, nC), alphaB(nA, nB, nC), beta(nA, nB, nC), gamma);
                    }
                }
            }
            return rslt;
        }
        /// <summary> Method to get the element of a triangular matrix corresponding to i and j (used to ensure that the lower triangular portion is accessed for an arbitrarily ordered i and j) </summary>
        /// <param name="mat"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        private static double TriMatVal(double[][] mat, int i, int j) { return mat[Math.Max(i, j)][Math.Min(i, j)]; }

        #endregion

        #region Distance Measures
        /// <summary> Simple Euclidean distance between the two vectors </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="sumAB"></param>
        /// <returns></returns>
        public static double Euclidean(double[] A, double[] B)
        {
            double rslt = 0d;
            for (int i = 0; i < A.Length; i++)
                rslt += Math.Pow(A[i] - B[i], 2d);
            return Math.Sqrt(rslt);
        }
        public static double SquaredEuclidean(double[] A, double[] B)
        {
            double rslt = 0d;
            for (int i = 0; i < A.Length; i++)
                rslt += Math.Pow(A[i] - B[i], 2d);
            return rslt;// Math.Sqrt(rslt);
        }
        /// <summary> Determine the Bray-Curtis dissimilarity measure between two samples A and B</summary>
        /// <remarks>
        /// Result given from:
        /// $1 - 2\cdot\dfrac{\sum\limits_{i=0}^{n} {\min(A_i, B_i)}}{\sum\limits_{i=0}^{n}{A_i + B_i}}$
        /// where $A_i$ and $B_i$ are the number of sequences assigned to phylotype $i$ in samples $A$ and $B$, repsectively, and $n$ is the total number of phylotypes.</remarks>
        /// <param name="A">Sample A</param>
        /// <param name="B">Sample B</param>
        /// <returns></returns>
        public static double BrayCurtis(double[] A, double[] B)
        {
            if (A.Length != B.Length) throw new ArgumentOutOfRangeException("Unequal phylotypes between samples");
            double num = 0d;
            for (int i = 0; i < A.Length; i++)
                num += Math.Min(A[i], B[i]);
            return 1d - 2d * ((double)num) / ((double)(A.Sum() + B.Sum()));
        }

        #endregion
        
        #region Cluster Linkage Methods
        /// <summary> Flexible beta cluster linkage method. This is a general method for which others (simple, average, complete, Ward's, etc.) are special cases </summary>
        /// <remarks> Determines the distance between cluster C and a newly joined cluster AB formed by joining clusters A and B. Given by
        /// alphaA * DCA + alphaB * DCB + beta * DAB + gamma * Math.Abs(DCA - DCB)
        /// See Methods in Multivariate Analysis by Alvin C. Rencher and William F. Christensen</remarks>
        /// <param name="DCA">The original distance between cluster C and cluster A before joining AB</param>
        /// <param name="DCB">The original distance between cluster C and cluster B before joining AB</param>
        /// <param name="DAB">The original distance between cluster A and cluster B before joining AB</param>
        /// <param name="alphaA">Parameter in relationship; see eqn.</param>
        /// <param name="alphaB">Parameter in relationship; see eqn.</param>
        /// <param name="beta">Parameter in relationship; see eqn.</param>
        /// <param name="gamma">Parameter in relationship; see eqn.</param>
        /// <returns></returns>
        private static double FlexibleBeta(double DCA, double DCB, double DAB, double alphaA, double alphaB, double beta, double gamma)
        {
            return alphaA * DCA + alphaB * DCB + beta * DAB + gamma * Math.Abs(DCA - DCB);
        }
        #endregion

        #region Classes
        /// <summary> General cluster </summary>
        private class Cluster
        {
            #region Fields
            private Cluster _A;
            private Cluster _B;

            protected double[][] _reads;
            //protected double[] _sums;
            //protected double[] _centroid;
            //protected double[] _median;

            public double Distance;

            public string FirstNode { get; private set; }
            public string LastNode { get; private set; }
            public string SuperordinateConnectionCoord { get; private set; }

            public string TikZLabelStyle;
            public string TikZLineStyle;

            #endregion

            #region Constructors
            public Cluster(Cluster[] subordinates, double distance)
            {
                TikZLineStyle = string.Empty;
                TikZLabelStyle = string.Empty;
                if (subordinates == null) return;
                _A = subordinates[0];
                _B = subordinates[1];
                List<double[]> allReads = new List<double[]>();
                List<double> allSums = new List<double>();
                //_centroid = new double[A.Reads[0].Length];
                //_median = new double[_centroid.Length];

                allReads.AddRange(A.Reads);
                allReads.AddRange(B.Reads);
                //allSums.AddRange(A.SumsOfReads);
                //allSums.AddRange(B.SumsOfReads);
                double deno = (double)(A.SubordinateCount + B.SubordinateCount);
                
                //for (int j = 0; j < _centroid.Length; j++)
                //{
                //    _centroid[j] += (A.Centroid[j] * ((double)A.SubordinateCount) + B.Centroid[j]*((double)B.SubordinateCount)) / deno;//the numerator
                //    _median[j] += (A.Centroid[j] + B.Centroid[j])/2d;
                //}
                _reads = allReads.ToArray();
                //_sums = allSums.ToArray();

                Distance = distance;
                if(A.TikZLabelStyle == B.TikZLabelStyle) TikZLabelStyle = A.TikZLabelStyle;
                if(A.TikZLineStyle == B.TikZLineStyle) TikZLineStyle = A.TikZLineStyle;
            }

            #endregion

            #region Properties
            public double[][] Reads { get { return _reads; } }
            //public double[] SumsOfReads { get { return _sums; } }
            //public double[] Centroid { get { return _centroid; } }
            //public double[] Median { get { return _median; } }

            public virtual string FirstLabel { get { return this.A.FirstLabel; } }
            public virtual string LastLabel { get { return this.B.LastLabel; } }
            public int SubordinateCount 
            { 
                get 
                { 
                    if (this.A == null || this.B == null) return 1;
                    return this.A.SubordinateCount + this.B.SubordinateCount;
                } 
            }

            public Cluster A { get { return _A; } private set { _A = value; } }
            public Cluster B { get { return _B; } private set { _B = value; } }
            #endregion

            public void SortSubordinates(System.Comparison<string> Comparator)
            {
                if (this.A == null || this.B == null) return;
                List<Cluster> sorted = new List<Cluster>();
                this.A.SortSubordinates(Comparator);
                this.B.SortSubordinates(Comparator);
                if (Comparator(this.A.FirstLabel, this.B.FirstLabel) > 0)//reverse order
                {
                    Cluster temp = this.A;
                    this.A = this.B;
                    this.B = temp;
                }
            }

            #region Tikz
            public string TikZNodes(bool takesqrtofdist,int firstNodeIdx, string labelsep, string baselabelstyle)
            {
                string rslt = string.Empty;
                if (this.A != null && this.B !=null)
                {
                    FirstNode = "A" + firstNodeIdx.ToString();
                    rslt += A.TikZNodes(takesqrtofdist, firstNodeIdx, labelsep, baselabelstyle);
                    firstNodeIdx+=A.SubordinateCount;
                    rslt += B.TikZNodes(takesqrtofdist, firstNodeIdx, labelsep, baselabelstyle);
                    firstNodeIdx+=B.SubordinateCount;
                    LastNode = "A" + (firstNodeIdx-1).ToString();
                    rslt += @"      \path let \p1=(" + this.A.SuperordinateConnectionCoord + @"),\p2=(" + this.B.SuperordinateConnectionCoord +
                        ") in coordinate (" + this.A.SuperordinateConnectionCoord + this.B.SuperordinateConnectionCoord + ") at (" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.###############") + @",\y1/2+\y2/2);" + Environment.NewLine;
                    this.SuperordinateConnectionCoord = this.A.SuperordinateConnectionCoord + this.B.SuperordinateConnectionCoord;
                }
                else
                {
                    rslt += @"      \node[anchor=mid west," + ((string.IsNullOrEmpty(TikZLabelStyle))?(baselabelstyle):(TikZLabelStyle)) + "] (A" + firstNodeIdx.ToString() + ") at (0,";
                    for (int i = 0; i < firstNodeIdx + 1; i++)
                        rslt += labelsep + ((i != firstNodeIdx) ? ("+") : (""));
                    rslt += ") {" + FirstLabel + "};" + Environment.NewLine;

                    FirstNode = "A" + firstNodeIdx.ToString();
                    LastNode = "A" + firstNodeIdx.ToString();
                    SuperordinateConnectionCoord = "A" + firstNodeIdx.ToString();
                }
                return rslt;
            }
            public string TikZDendrogram(bool takesqrtofdist, string baselinestyle)
            {
                if (this.A == null || this.B == null) return string.Empty;
                string rslt = string.Empty;
                if (!string.IsNullOrEmpty(this.TikZLineStyle) || (string.IsNullOrEmpty(this.A.TikZLineStyle) && string.IsNullOrEmpty(this.B.TikZLineStyle)))
                {
                    string sty = (string.IsNullOrEmpty(this.TikZLineStyle))?(baselinestyle):(baselinestyle + "," + this.TikZLineStyle);
                    rslt = @"      \draw[" + sty + @"] let \p1=(" + this.A.SuperordinateConnectionCoord + @"),\p2=(" + this.B.SuperordinateConnectionCoord + @")" +
                        @"in (" + ((takesqrtofdist) ? (Math.Sqrt(this.A.Distance)) : (this.A.Distance)).ToString("0.###############") + @",\y1)--(" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.################") + @",\y1)--" +
                        @"(" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.################") + @",\y2)--(" + ((takesqrtofdist) ? (Math.Sqrt(this.B.Distance)) : (this.B.Distance)).ToString("0.###############") + @",\y2);" + Environment.NewLine;
                }
                else//some difference
                {
                    if (string.IsNullOrEmpty(A.TikZLineStyle))
                    {
                        rslt = @"      \draw[" + baselinestyle + @"] let \p1=(" + this.A.SuperordinateConnectionCoord + @"),\p2=(" + this.B.SuperordinateConnectionCoord + @")" +
                            @"in (" + ((takesqrtofdist) ? (Math.Sqrt(this.A.Distance)) : (this.A.Distance)).ToString("0.###############") + @",\y1)--(" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.################") + @",\y1)--" +
                            @"(" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.################") + @",\y2);" + Environment.NewLine;
                        rslt += @"      \draw[" + baselinestyle + "," + B.TikZLineStyle + @"] let \p1=(" + this.A.SuperordinateConnectionCoord + @"),\p2=(" + this.B.SuperordinateConnectionCoord + @")" +
                            @"in (" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.################") + @",\y2)--" +
                            "(" + ((takesqrtofdist) ? (Math.Sqrt(this.B.Distance)) : (this.B.Distance)).ToString("0.###############") + @",\y2);" + Environment.NewLine;
                    }
                    else if (string.IsNullOrEmpty(B.TikZLineStyle))
                    {
                        rslt = @"      \draw[" + baselinestyle + @"] let \p1=(" + this.A.SuperordinateConnectionCoord + @"),\p2=(" + this.B.SuperordinateConnectionCoord + @")" +
                            @"in (" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.################") + @",\y1)--" +
                            @"(" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.################") + @",\y2)--(" + ((takesqrtofdist) ? (Math.Sqrt(this.B.Distance)) : (this.B.Distance)).ToString("0.###############") + @",\y2);" + Environment.NewLine;
                        rslt += @"      \draw[" + baselinestyle + "," + A.TikZLineStyle + @"] let \p1=(" + this.A.SuperordinateConnectionCoord + @"),\p2=(" + this.B.SuperordinateConnectionCoord + @")" +
                            @"in (" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.################") + @",\y1)--" +
                            "(" + ((takesqrtofdist) ? (Math.Sqrt(this.A.Distance)) : (this.A.Distance)).ToString("0.###############") + @",\y1);" + Environment.NewLine;
                    }
                    else//all three separate
                    {
                        rslt = @"      \draw[" + baselinestyle + "," + A.TikZLineStyle + @"] let \p1=(" + this.A.SuperordinateConnectionCoord + @"),\p2=(" + this.B.SuperordinateConnectionCoord + @")" +
                            @"in (" + ((takesqrtofdist) ? (Math.Sqrt(this.A.Distance)) : (this.A.Distance)).ToString("0.###############") + @",\y1)--(" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.################") + @",\y1);" + Environment.NewLine;
                        rslt += @"      \draw[" + baselinestyle + "," + B.TikZLineStyle + @"] let \p1=(" + this.A.SuperordinateConnectionCoord + @"),\p2=(" + this.B.SuperordinateConnectionCoord + @")" +
                            @"in (" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.################") + @",\y2)--(" + ((takesqrtofdist) ? (Math.Sqrt(this.B.Distance)) : (this.B.Distance)).ToString("0.###############") + @",\y2);" + Environment.NewLine;
                        rslt += @"      \draw[" + baselinestyle +  @"] let \p1=(" + this.A.SuperordinateConnectionCoord + @"),\p2=(" + this.B.SuperordinateConnectionCoord + @")" +
                            @"in (" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.################") + @",\y1)--" +
                            @"(" + ((takesqrtofdist) ? (Math.Sqrt(this.Distance)) : (this.Distance)).ToString("0.################") + @",\y2);" + Environment.NewLine;
                    }
                }
                rslt += this.A.TikZDendrogram(takesqrtofdist,baselinestyle);
                rslt += this.B.TikZDendrogram(takesqrtofdist,baselinestyle);
                return rslt;
            }
            public string TikZpgfplotsCoords(bool takesqrtofdist)
            {
                string rslt = "(" + ((takesqrtofdist)?(Math.Sqrt(this.Distance)):(this.Distance)).ToString("0.###############") + ",0)";
                if (this.A == null || this.B == null) return rslt;
                return rslt + " " + this.A.TikZpgfplotsCoords(takesqrtofdist) + " " + this.B.TikZpgfplotsCoords(takesqrtofdist);
            }
            #endregion
        }

        /// <summary> Special case of a cluster which is just a sample (no subordinate clusters) </summary>
        private class SampleAsCluster 
            : Cluster
        {
            public string Label;

            public SampleAsCluster(string label, double[] reads, string lineStyle = "", string labelStyle = "")
                : base(null, 0d)
            {
                Label = label;
                TikZLabelStyle = labelStyle;
                TikZLineStyle = lineStyle;
                _reads = new double[][] { reads };
                //_sums = new double[] { reads.Sum() };
                //_centroid = reads;
                //_median = reads;
            }

            public override string FirstLabel { get { return Label; } }
            public override string LastLabel { get { return Label; } }
        }

        #endregion
    }
}