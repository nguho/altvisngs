using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace altvisngs
{
    public abstract class LaTeX_Figure
    {
        #region general figure
        /// <summary>Return the string for the </summary>
        /// <param name="figure_contents"></param>
        /// <param name="caption_mandatory"></param>
        /// <param name="figure_label"></param>
        /// <param name="caption_optional"></param>
        /// <param name="figure_options"></param>
        /// <param name="figure_alignment">The alignment for the figure (default = "\centering")</param>
        /// <param name="indt0"></param>
        /// <param name="indt1"></param>
        /// <param name="indt2"></param>
        /// <returns>The string for the figure environment referencing the pdf</returns>
        private static string figure_float(
            string[] figure_contents,
            string caption_mandatory,
            string figure_label,
            string caption_optional = "",
            string figure_options = "htb",
            string figure_alignment = @"\centering",
            string indt0 = "",
            string indt1 = "   ",
            string indt2 = "      ")
        {
            string figcont = string.Empty;
            if (figure_contents != null)
                for (int i = 0; i < figure_contents.Length; i++)
                    figcont += indt1 + figure_contents[i] + Environment.NewLine;
            if (string.IsNullOrEmpty(figcont))
                figcont = Environment.NewLine;
            return
                indt0 + @"%%% " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + Environment.NewLine +
                indt0 + @"\begin{figure}" + ((!string.IsNullOrEmpty(figure_options)) ? ("[" + figure_options + "]") : ("")) + Environment.NewLine +
                ((!string.IsNullOrEmpty(figure_alignment)) ? (indt1 + figure_alignment + Environment.NewLine) : ("")) +
                figcont +
                ((!string.IsNullOrEmpty(caption_mandatory)) ? (indt1 + @"\caption" + ((!string.IsNullOrEmpty(caption_optional)) ? ("[" + caption_optional + "]") : ("")) + "{" + caption_mandatory + "}" + Environment.NewLine +
                ((!string.IsNullOrEmpty(figure_label)) ? (indt2 + @"\label{" + figure_label + "}" + Environment.NewLine) : (""))) : ("")) +
                indt0 + @"\end{figure}";
        }

        /// <summary>Return the string for the nonfloat figure (placed in a minipage with the desired width using the \captionof{figure} command inplace of \caption</summary>
        /// <param name="figure_contents"></param>
        /// <param name="caption_mandatory"></param>
        /// <param name="figure_label"></param>
        /// <param name="caption_optional"></param>
        /// <param name="figure_options"></param>
        /// <param name="figure_alignment">The alignment for the figure (default = "\centering")</param>
        /// <param name="indt0"></param>
        /// <param name="indt1"></param>
        /// <param name="indt2"></param>
        /// <returns>The string for the figure environment referencing the pdf</returns>
        private static string figure_nofloat(
            string[] figure_contents,
            string caption_mandatory,
            string figure_label,
            string caption_optional = "",
            string minipage_options = "",
            string minipage_width = @"\linewidth",
            string figure_alignment = @"\centering",
            string figure_bookmark = @"\bookmarkfigure",//custom command added after the label to book mark the figure. set to \relax or omit here to do nothing.
            string indt0 = "",
            string indt1 = "   ",
            string indt2 = "      ")
        {
            string figcont = string.Empty;
            if (figure_contents != null)
                for (int i = 0; i < figure_contents.Length; i++)
                    figcont += indt1 + figure_contents[i] + Environment.NewLine;
            if (string.IsNullOrEmpty(figcont))
                figcont = Environment.NewLine;
            return
                indt0 + @"%%% " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + Environment.NewLine +
                indt0 + @"\noindent\begin{minipage}" + ((!string.IsNullOrEmpty(minipage_options)) ? ("[" + minipage_options + "]") : ("")) + "{" + minipage_width +"}" + Environment.NewLine +
                @"\captionsetup{type=figure" + ((!string.IsNullOrEmpty(caption_optional)) ? ("") : (",list=false")) + "}" + Environment.NewLine + 
                ((!string.IsNullOrEmpty(figure_alignment)) ? (indt1 + figure_alignment + Environment.NewLine) : ("")) +
                figcont +
                ((!string.IsNullOrEmpty(caption_mandatory)) ? (indt1 + @"\caption" + ((!string.IsNullOrEmpty(caption_optional)) ? ("[" + caption_optional + "]") : ("")) + "{" + caption_mandatory + "}" + Environment.NewLine +
                ((!string.IsNullOrEmpty(figure_label)) ? (indt2 + @"\label{" + figure_label + "}" + Environment.NewLine) : ("")) +
                ((!string.IsNullOrEmpty(figure_bookmark)) ? (indt2 + figure_bookmark + "%" + Environment.NewLine) : (""))) : ("")) +
                indt0 + @"\end{minipage}";
        }
        
        #endregion

        #region figure w/ includegraphics
        /// <summary>Return the string for the </summary>
        /// <param name="figure_pdf_relative_path"></param>
        /// <param name="caption_mandatory"></param>
        /// <param name="figure_label"></param>
        /// <param name="caption_optional"></param>
        /// <param name="include_graphics_options">Options passed to \includegraphics. (default = "max width=\textwidth, max height=\dimexpr\textheight-1.5in\relax", which limits the figure to the width of the page and the height minus 1.5in (for the caption). This requires \usepackage[export]{adjustbox}.)</param>
        /// <param name="figure_options"></param>
        /// <param name="figure_alignment">The alignment for the figure (default = "\centering")</param>
        /// <param name="indt0"></param>
        /// <param name="indt1"></param>
        /// <param name="indt2"></param>
        /// <returns>The string for the figure environment referencing the pdf</returns>
        private static string figure_float_inclgrphx(
            string figure_pdf_relative_path,
            string caption_mandatory,
            string figure_label,
            string caption_optional = "",
            string include_graphics_options = @"max width=\textwidth, max height=\dimexpr\textheight-1.5in\relax",
            string figure_options = "htb",
            string figure_alignment = @"\centering",
            string indt0="",
            string indt1="   ",
            string indt2="      ")
        {
            return
                figure_float(
                    figure_contents: new string[] { @"\includegraphics" + ((!string.IsNullOrEmpty(include_graphics_options)) ? ("[" + include_graphics_options + "]") : ("")) + "{" + figure_pdf_relative_path + "}" },
                    caption_mandatory: caption_mandatory,
                    figure_label: figure_label,
                    caption_optional: caption_optional,
                    figure_options: figure_options,
                    figure_alignment: figure_alignment,
                    indt0: indt0,
                    indt1: indt1,
                    indt2: indt2);
        }

        /// <summary>Return the string for the </summary>
        /// <param name="figure_pdf_relative_path"></param>
        /// <param name="caption_mandatory"></param>
        /// <param name="figure_label"></param>
        /// <param name="caption_optional"></param>
        /// <param name="include_graphics_options">Options passed to \includegraphics. (default = "max width=\textwidth, max height=\dimexpr\textheight-1.5in\relax", which limits the figure to the width of the page and the height minus 1.5in (for the caption). This requires \usepackage[export]{adjustbox}.)</param>
        /// <param name="figure_options"></param>
        /// <param name="figure_alignment">The alignment for the figure (default = "\centering")</param>
        /// <param name="indt0"></param>
        /// <param name="indt1"></param>
        /// <param name="indt2"></param>
        /// <returns>The string for the figure environment referencing the pdf</returns>
        private static string figure_nofloat_inclgrphx(
            string figure_pdf_relative_path,
            string caption_mandatory,
            string figure_label,
            string caption_optional = "",
            string include_graphics_options = @"max width=\linewidth, max height=\dimexpr\textheight-1.5in\relax",
            string minipage_options = "",
            string minipage_width = @"\linewidth",
            string figure_alignment = @"\centering",
            string indt0 = "",
            string indt1 = "   ",
            string indt2 = "      ")
        {
            return
                figure_nofloat(
                    figure_contents: new string[] { @"\includegraphics" + ((!string.IsNullOrEmpty(include_graphics_options)) ? ("[" + include_graphics_options + "]") : ("")) + "{" + figure_pdf_relative_path + "}" },
                    caption_mandatory: caption_mandatory,
                    figure_label: figure_label,
                    caption_optional: caption_optional,
                    minipage_options: minipage_options,
                    minipage_width: minipage_width,
                    figure_alignment: figure_alignment,
                    indt0: indt0,
                    indt1: indt1,
                    indt2: indt2);
        }

        public static void figure_inclgrphx(
            string filepath_output_caption,
            string filepath_output_captionof,
            string figure_pdf_relative_path,
            string caption_mandatory,
            string figure_label,
            string caption_optional = "",
            string include_graphics_options = @"max width=\linewidth, max height=\dimexpr\textheight-1.5in\relax",
            string figure_options = "htb",
            string minipage_options = "",
            string minipage_width= @"\linewidth",
            string figure_alignment = @"\centering",
            string indt0 = "",
            string indt1 = "   ",
            string indt2 = "      ")
        {
            string rslt = figure_float_inclgrphx(
                figure_pdf_relative_path:figure_pdf_relative_path,
                caption_mandatory: caption_mandatory,
                figure_label: figure_label,
                caption_optional: caption_optional,
                include_graphics_options: include_graphics_options,
                figure_options: figure_options,
                figure_alignment: figure_alignment,
                indt0: indt0,
                indt1: indt1,
                indt2: indt2);
            Console.WriteLine("Saving `" + Path.GetFileName(filepath_output_caption) + "'");
            using (StreamWriter sw = new StreamWriter(filepath_output_caption))
            {
                sw.Write(rslt);
            }
            rslt = figure_nofloat_inclgrphx(
                figure_pdf_relative_path: figure_pdf_relative_path,
                caption_mandatory: caption_mandatory,
                figure_label: figure_label,
                caption_optional: caption_optional,
                include_graphics_options: include_graphics_options,
                minipage_options: minipage_options,
                minipage_width:minipage_width,
                figure_alignment: figure_alignment,
                indt0: indt0,
                indt1: indt1,
                indt2: indt2);
            Console.WriteLine("Saving `" + Path.GetFileName(filepath_output_captionof) + "'");
            using (StreamWriter sw = new StreamWriter(filepath_output_captionof))
            {
                sw.Write(rslt);
            }
        }

        #endregion
    }
}
