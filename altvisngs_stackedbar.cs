using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace altvisngs
{
    abstract class altvisngs_stackedbar
    {
        #region StackedBarpgfplots

        public static void DefaultStackedBar(string stackfilePath, string[] legendentries, string[] xticklabels, string xlabel, int[] xpositions, string[] addplots)
        {
            string rslt =
@"\documentclass[tikz]{standalone}
\usepackage[scaled]{helvet}
\renewcommand\familydefault{\sfdefault} 
\usepackage[T1]{fontenc}
\usepackage{sansmath}
\sansmath

\usepackage{pgfplots}
\usepgfplotslibrary{colorbrewer}
\pgfplotsset{width=100mm,height=162mm,compat=newest}

%Patch to allow _ to be underscore in text mode. Requires font encoding to be T1.
%Ref: egreg soln: http://tex.stackexchange.com/a/38720/89497
\catcode`_=12
\begingroup\lccode`~=`_\lowercase{\endgroup\let~\sb}
\mathcode`_=""8000

\begin{document}
	\begin{tikzpicture}
		\begin{axis}[%
			ybar stacked,%
			ymin=0,
			ymax=100,
			ylabel={Relative abundance ($\%$)},
			legend entries={";
            rslt += string.Join(",", legendentries) + "}," + Environment.NewLine;
            rslt += @"			xtick={" + string.Join(",", xpositions) + "}," + Environment.NewLine;
            rslt += @"          xticklabels={" + string.Join(",", xticklabels) + "}," + Environment.NewLine;
            rslt += @"          xlabel={" + xlabel + "}," + Environment.NewLine;
            rslt +=
@"			x tick label style={rotate=90, anchor=east},
			xmin=-0.5,
			xmax=";
            rslt += xpositions[xpositions.Length - 1].ToString() + ".5," + Environment.NewLine;
            rslt +=
@"			bar width=0.5,
			axis on top,
			legend style={draw=none,at={(1.01,1)},anchor=north west},
			reverse legend,
			legend cell align=left,
			cycle list name=Paired-10,
			every axis plot/.append style={fill,draw=none,no markers}]
";
            rslt += string.Join(Environment.NewLine, addplots);
            rslt +=
@"   \end{axis}
\end{tikzpicture}
\end{document}";

            Console.WriteLine("Writing stacked bar");
            using (StreamWriter sw = new StreamWriter(stackfilePath))
            {
                sw.Write(rslt);
            }
            pdflatex pdflatex = new pdflatex();
            try
            {
                pdflatex.RunSync(stackfilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("pdflatex exited with error: " + ex.Message);
            }

        }

        #endregion
    }
}
