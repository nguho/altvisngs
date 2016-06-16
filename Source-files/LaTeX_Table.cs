using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace altvisngs
{
    public abstract class LaTeX_Table
    {
        #region Table Building Methods (used for lambda expressions for BuildTexTable)
        /// <summary> Standard Table building delegate for a ThreePartTable using `longtable' as the inner tabular, `booktabs' for rules, and the custom `longtablesup' for miscellaneous caption/label formating. </summary>
        /// <remarks> 
        /// 1. Indents the various components to make the result somewhat readable.
        /// 2. Automates caption generation from the passed options.
        /// 3. Automates table footnote ordering and generation. Will comment out those that are not referenced in the table.
        ///    *Table footnote handling is a naiive check based on the presence of the label. If a non-unique value is used, table footnotes may be included erroneously.
        ///    *Note as well that if table footnotes are used in the subsequent caption and no where else, then they may appear prematurely in the table footnotes (there is no way to no where longtable will break.
        /// 
        /// Requires threeparttablex.sty, longtable.sty, booktabs.sty, longtablesup.sty.
        /// May use siunitx.sty and longbooktabs.sty </remarks>
        /// <param name="tabular_cols">String array of column types for the inner tabular environment (e.g., { "l", "C", "S[tight-spacing=true]" })</param>
        /// <param name="table_heading">String for the heading to appear on all pages</param>
        /// <param name="table_body">The full body of the table</param>
        /// <param name="table_notes">The body of the TableNotes environment</param>
        /// <param name="mandatory_caption"> The mandatory argument for \caption. If null or empty, \caption will be omitted.")</param>
        /// <param name="table_label">The label for the table (the argument for \label) (e.g., "tbl:class")</param>
        /// <param name="table_notes_options">Options for the TableNotes environment (default = "para, flushleft") </param>
        /// <param name="table_note_func">Delegate which builds a tablenote from the label and content strings (default = null, which yields "\tblfootnote{label}{content}\par"; note that \tblfootnote is a custom command)</param>
        /// <param name="caption_option">The optional parameter passed to \caption (default = "").</param>
        /// <param name="subsequent_caption">Addition to the top of each subsequent page. If null, then caption_option will be used and subsequent_caption_append will be appended to it. If that is null or empty, the mandatory will be used. If all are null or empty, then none are used. If empty, then non used regardless of caption args.</param>
        /// <param name="subsequent_caption_append">The text appended to subsequent_caption when determined from caption_option or mandatory_caption (default = " continued"). Trailing periods are handled automatically.</param>
        /// <param name="font">font declaration for the table as a whole for the table content. If null or empty, omitted. (default = "\small")</param>
        /// <param name="new_tab_col_sep_length">New length for \tabcolsep. If null or empty, the default value is used. (default = "")</param>
        /// <param name="table_note_font">font declaration for the table notes. If null or empty, omitted. (default = "\footnotesize")</param>
        /// <param name="si_setup">Content passed to \sisetup for the table. If null or empty, \sisetup is omitted. (default = "")</param>
        /// <param name="refine_si_columns">If true, the contents of any S column will be assessed and the narowest figure, decimal, and exponent options will be used. Note, the comparision is crude and permits only numbers to be evaluated currently (default=false) </param>
        /// <param name="expo_letter">The letter used to indicate exponent (default = "E")</param>
        /// <param name="pre_tabular_cols">Content before the first tabular column type (default = "@{}")</param>
        /// <param name="post_tabular_cols">Content after the last tabular column type (default = "@{}")</param>
        /// <param name="tabcolsep">The string used to indicate a tab stop (i.e., column) (default = "&")</param>
        /// <param name="tabular_new_line">The string used for a tabularnewline (default = "\tabularnewline")</param>
        /// <param name="indt0">Preceding space for indent level 0 (default = 0)</param>
        /// <param name="indt1">Preceding space for indent level 1 (default = 3)</param>
        /// <param name="indt2">Preceding space for indent level 2 (default = 6)</param>
        /// <param name="indt3">Preceding space for indent level 3 (default = 9)</param>
        /// <returns>The full string of the constructed ThreePartTable (\begin{ThreePartTable} ... \end{ThreePartTable})</returns>
        public static string ThreePartTable_longtable(
            string[] tabular_cols,
            string[] table_heading,
            string[] table_body,
            Dictionary<string, string> table_notes,
            string mandatory_caption,
            string table_label,
            string table_notes_options = "para,flushleft",
            Func<string, string, string> table_note_func = null,
            string caption_option = "",
            string subsequent_caption = null,
            string subsequent_caption_append = " continued",
            string font = @"\small",
            string new_tab_col_sep_length = "",
            string table_note_font = @"\footnotesize",
            string si_setup = "",
            bool refine_si_columns = true,
            string expo_letter = "E",
            string pre_tabular_cols = @"@{}",
            string post_tabular_cols = @"@{}",
            string tabcolsep = @"&",
            string tabular_new_line = @"\tabularnewline",
            string indt0 = @"",
            string indt1 = @"   ",
            string indt2 = @"      ",
            string indt3 = @"         ")
        {
            //first, refine tabular_cols to reflect the actual widest requirement of the data
            if (refine_si_columns && !string.IsNullOrEmpty(si_setup))//refine the siunitx columns to consolidate the widths so that they are only as wide as need be
            {
                siunitx_Sdefault defaultS = siunitx_Sdefault.Empty;
                try { defaultS = new siunitx_Sdefault(si_setup); }
                catch{ defaultS = siunitx_Sdefault.Empty; }
                if(!defaultS.IsEmpty)
                {
                    //build the dictionary of the current settings
                    Dictionary<int, siunitx_Sdefault> Scols_default = new Dictionary<int, siunitx_Sdefault>();
                    Dictionary<int, siunitx_Sdefault> Scols_actual = new Dictionary<int, siunitx_Sdefault>();
                    for (int i = 0; i < tabular_cols.Length; i++)
                    {
                        if (tabular_cols[i].Contains("S"))
                        {
                            Scols_actual.Add(i, siunitx_Sdefault.Empty);
                            string post = tabular_cols[i].Substring(tabular_cols[i].LastIndexOf("S") + 1);
                            if (string.IsNullOrEmpty(post))
                                Scols_default.Add(i, defaultS);
                            else//something there
                                if (post[0] != '[')
                                    Scols_default.Add(i, defaultS);
                                else//options
                                {
                                    siunitx_Sdefault temp = siunitx_Sdefault.Empty;
                                    try { temp = new siunitx_Sdefault(post.Substring(1, post.IndexOf("]"))); }
                                    catch { temp = siunitx_Sdefault.Empty; }
                                    if (!temp.IsEmpty) Scols_default.Add(i, temp);
                                    else Scols_default.Add(i, defaultS);
                                }
                        }
                    }
                    //now, go through and get the widest entry for each column from the heading
                    for (int i = 0; i < table_heading.Length; i++)
                    {
                        string[] rslt = Element_At_Column_Idx_siunitx(table_heading[i], tabcolsep, tabular_new_line);
                        if(rslt == null) 
                            continue;
                        foreach (KeyValuePair<int, siunitx_Sdefault> kvp in Scols_default)
                        {
                            if (rslt.Length <= kvp.Key) 
                                continue;
                            siunitx_Sdefault temp = siunitx_Sdefault.GetFromFormattedResult(rslt[kvp.Key], new string[] { expo_letter });
                            if (temp.IsEmpty) continue;
                            if (Scols_actual[kvp.Key].IsEmpty) Scols_actual[kvp.Key] = temp;
                            else Scols_actual[kvp.Key] = siunitx_Sdefault.GetWidest(Scols_actual[kvp.Key], temp);
                        }
                    }
                    //now, go through and get the widest entry for each column from the body
                    for (int i = 0; i < table_body.Length; i++)
                    {
                        string[] rslt = Element_At_Column_Idx_siunitx(table_body[i], tabcolsep, tabular_new_line);
                        if (rslt == null) continue;
                        foreach (KeyValuePair<int, siunitx_Sdefault> kvp in Scols_default)
                        {
                            if (rslt.Length <= kvp.Key) continue;
                            siunitx_Sdefault temp = siunitx_Sdefault.GetFromFormattedResult(rslt[kvp.Key], new string[] { expo_letter });
                            if (temp.IsEmpty) continue;
                            if (Scols_actual[kvp.Key].IsEmpty) Scols_actual[kvp.Key] = temp;
                            else Scols_actual[kvp.Key] = siunitx_Sdefault.GetWidest(Scols_actual[kvp.Key], temp);
                        }
                    }
                    //now, correct the tabular options to reflect the narrowest option
                    foreach (KeyValuePair<int, siunitx_Sdefault> kvp in Scols_actual)
                    {
                        string pre = tabular_cols[kvp.Key].Substring(0, tabular_cols[kvp.Key].IndexOf("S"));
                        string post = tabular_cols[kvp.Key].Substring(tabular_cols[kvp.Key].LastIndexOf("S") + 1);
                        string new_options = kvp.Value.Accomodate_Relative_To_Default(Scols_default[kvp.Key]);
                        if (string.IsNullOrEmpty(new_options)) continue;//no change.
                        if (string.IsNullOrEmpty(post))
                            tabular_cols[kvp.Key] = pre + "S" + "[" + new_options + "]";
                        else
                            if (post[0] != '[')
                                tabular_cols[kvp.Key] = pre + "S" + "[" + new_options + "]" + post;
                            else//options already exist
                            {
                                string postpre = post.Substring(0, post.IndexOf("]"));
                                string postpost = post.Substring(post.LastIndexOf("]"));
                                tabular_cols[kvp.Key] = pre + "S" + postpre + "," + new_options + "," + postpost;
                            }
                    }
                }
            }
            
            if (subsequent_caption == null)
            {
                subsequent_caption = ((string.IsNullOrEmpty(caption_option)) ? (string.IsNullOrEmpty(mandatory_caption) ? (null) : (mandatory_caption.Trim())) : (caption_option.Trim()));
                if (!string.IsNullOrEmpty(subsequent_caption))
                    if (subsequent_caption[subsequent_caption.Length - 1] == '.')
                        subsequent_caption = subsequent_caption.Substring(0, subsequent_caption.Length - 1) + subsequent_caption_append + ".";
                    else
                        subsequent_caption += subsequent_caption_append;
            }

            //will sort the table notes based on the order in the header and body of the table.
            List<TableNote> orderedTblNotes = new List<TableNote>();
            List<string> Labels;//
            if (table_notes != null)
                Labels = new List<string>(table_notes.Keys);
            else
                Labels = new List<string>();
            if (table_note_func == null) table_note_func = ((l, c) => @"\tblfootnote{" + l + "}{" + c + "}" + @"\par");//"default" builder

            //captions...note that there is no way to no what occurs before the subsequent caption, so if a tblfootnote is ref there and no where else the order will be off...
            if (!string.IsNullOrEmpty(mandatory_caption))
            {
                List<NoteLabelIdxInLine> found = new List<NoteLabelIdxInLine>();
                for (int j = 0; j < Labels.Count; j++)
                    if (mandatory_caption.Contains(Labels[j]))
                        found.Add(new NoteLabelIdxInLine(mandatory_caption.IndexOf(Labels[j]), Labels[j]));
                found = found.OrderBy((n) => n.Index).ToList();
                for (int j = 0; j < found.Count; j++)
                {
                    Labels.Remove(found[j].Label);
                    orderedTblNotes.Add(new TableNote(found[j].Label, table_notes[found[j].Label]));
                }
            }
            if (!string.IsNullOrEmpty(subsequent_caption))
            {
                List<NoteLabelIdxInLine> found = new List<NoteLabelIdxInLine>();
                for (int j = 0; j < Labels.Count; j++)
                    if (subsequent_caption.Contains(Labels[j]))
                        found.Add(new NoteLabelIdxInLine(subsequent_caption.IndexOf(Labels[j]), Labels[j]));
                found = found.OrderBy((n) => n.Index).ToList();
                for (int j = 0; j < found.Count; j++)
                {
                    Labels.Remove(found[j].Label);
                    orderedTblNotes.Add(new TableNote(found[j].Label, table_notes[found[j].Label]));
                }
            }

            //head
            string head = string.Empty;
            if (table_heading != null)
                for (int i = 0; i < table_heading.Length; i++)
                    if (!string.IsNullOrEmpty(table_heading[i]))
                    {
                        head += indt2 + table_heading[i] + Environment.NewLine;
                        //mind the footnotes
                        if (Labels.Count == 0) continue;
                        List<NoteLabelIdxInLine> found = new List<NoteLabelIdxInLine>();
                        for (int j = 0; j < Labels.Count; j++)
                            if (table_heading[i].Contains(Labels[j]))
                                found.Add(new NoteLabelIdxInLine(table_heading[i].IndexOf(Labels[j]), Labels[j]));
                        found = found.OrderBy((n) => n.Index).ToList();
                        for (int j = 0; j < found.Count; j++)
                        {
                            Labels.Remove(found[j].Label);
                            orderedTblNotes.Add(new TableNote(found[j].Label, table_notes[found[j].Label]));
                        }
                    }
            if (string.IsNullOrEmpty(head)) head = tabular_new_line + Environment.NewLine;//must be at least a tabularnewline (flanked by rules)

            //body
            string body = string.Empty;
            if (table_body != null)
                for (int i = 0; i < table_body.Length; i++)
                    if (!string.IsNullOrEmpty(table_body[i]))
                    {
                        body += indt2 + table_body[i] + Environment.NewLine;
                        //mind the footnotes
                        if (Labels.Count == 0) continue;
                        List<NoteLabelIdxInLine> found = new List<NoteLabelIdxInLine>();
                        for (int j = 0; j < Labels.Count; j++)
                            if (table_body[i].Contains(Labels[j]))
                                found.Add(new NoteLabelIdxInLine(table_body[i].IndexOf(Labels[j]), Labels[j]));
                        found = found.OrderBy((n) => n.Index).ToList();
                        for (int j = 0; j < found.Count; j++)
                        {
                            Labels.Remove(found[j].Label);
                            orderedTblNotes.Add(new TableNote(found[j].Label, table_notes[found[j].Label]));
                        }
                    }
            if (string.IsNullOrEmpty(body)) body = tabular_new_line + Environment.NewLine;//must be at least a tabularnewline (flanked by rules)

            //now, check the notes for notes
            //to accomodate nesting, loop
            while (true)
            {
                if (Labels.Count == 0) break;
                List<NoteLabelIdxInLine> found = new List<NoteLabelIdxInLine>();
                for (int i = 0; i < orderedTblNotes.Count; i++)
                {
                    for (int j = 0; j < Labels.Count; j++)
                        if (orderedTblNotes[i].Content.Contains(Labels[j]))
                            found.Add(new NoteLabelIdxInLine(orderedTblNotes[i].Content.IndexOf(Labels[j]), Labels[j]));
                    if (found.Count != 0) break;
                }
                if (found.Count == 0) break;//no change
                else
                    found = found.OrderBy((n) => n.Index).ToList();
                for (int j = 0; j < found.Count; j++)
                {
                    Labels.Remove(found[j].Label);
                    orderedTblNotes.Add(new TableNote(found[j].Label, table_notes[found[j].Label]));
                }
            }

            //finally, build the notes
            string notes = string.Empty;
            for (int i = 0; i < orderedTblNotes.Count; i++)
                notes += indt2 + table_note_func(orderedTblNotes[i].Label, orderedTblNotes[i].Content) + Environment.NewLine;
            //Add any remaining notes, commented out
            if (Labels.Count != 0)
            {
                notes += indt2 + @"%*** Unused tablenotes:" + Environment.NewLine;
                for (int i = 0; i < Labels.Count; i++)
                    notes += indt2 + @"%" + table_note_func(Labels[i], table_notes[Labels[i]]) + Environment.NewLine;
            }

            return
            indt0 + @"%%% Created: " + DateTime.Now.ToShortDateString() +" "+ DateTime.Now.ToLongTimeString() + Environment.NewLine +
            indt0 + @"\begin{ThreePartTable}" + Environment.NewLine +
                //***Table font
                ((string.IsNullOrEmpty(font)) ? ("") : (indt1 + font + Environment.NewLine)) +
                //***tabcolsep
                ((string.IsNullOrEmpty(new_tab_col_sep_length)) ? ("") : (indt1 + @"\setlength{\tabcolsep}{" + new_tab_col_sep_length + "}" + Environment.NewLine)) +
                //***Table Notes
                indt1 + @"\begin{TableNotes}" + ((string.IsNullOrEmpty(table_notes_options)) ? ("") : ("[" + table_notes_options + "]")) + Environment.NewLine +
                    ((string.IsNullOrEmpty(table_note_font)) ? ("") : (indt2 + table_note_font + Environment.NewLine)) +
                    notes +
                indt1 + @"\end{TableNotes}" + Environment.NewLine +
                //***Table siunitx setup
                ((string.IsNullOrEmpty(si_setup)) ? ("") : (indt1 + @"\sisetup{" + si_setup + "}" + Environment.NewLine)) +
                //***longtable
                indt1 + @"\begin{longtable}{" + pre_tabular_cols + string.Join("", tabular_cols) + post_tabular_cols + "}" + Environment.NewLine +
                //***Heading for first page
                    indt2 + @"%%% Heading for first page %%%" + Environment.NewLine +
                    ((!string.IsNullOrEmpty(mandatory_caption)) ? (indt2 + @"\caption" + ((string.IsNullOrEmpty(caption_option)) ? ("") : ("[" + caption_option + "]")) + "{" + mandatory_caption + "}" + Environment.NewLine +
                        indt3 + @"\label{" + table_label + "}" + @"\locktableref" + tabular_new_line + Environment.NewLine) : ("")) +//\locktableref is a "Hack" to allow the table to be bookmarked; requires the custom package `longtablesup'
                    indt2 + @"\toprule" + Environment.NewLine +
                    head +
                    indt2 + @"\midrule" + Environment.NewLine +
                    indt2 + @"\endfirsthead" + Environment.NewLine +
                //***Heading for subsequent pages
                    indt2 + @"%%% Heading for subsequent pages %%%" + Environment.NewLine +
                    ((string.IsNullOrEmpty(subsequent_caption)) ? ("") : (indt2 + @"\multicolumn{" + tabular_cols.Length + @"}{c}{\captioncont{" + subsequent_caption + "}}" + tabular_new_line + Environment.NewLine)) +//\captioncont is a "Hack" to prevent the caption on subsequent pages from controlling the table width; requires the custom package `longtablesup'
                    indt2 + @"\toprule" + Environment.NewLine +
                    head +
                    indt2 + @"\midrule" + Environment.NewLine +
                    indt2 + @"\endhead" + Environment.NewLine +
                //***Body
                    indt2 + @"%%% Body of table %%%" + Environment.NewLine +
                    body +
                    indt2 + @"\bottomrule" + Environment.NewLine +
                //***inserttablenotes
                    ((string.IsNullOrEmpty(notes)) ? ("") : (indt2 + @"\insertTableNotes" + Environment.NewLine)) +
                indt1 + @"\end{longtable}" + Environment.NewLine +
            indt0 + @"\end{ThreePartTable}";
        }

        /// <summary> Standard Table building delegate for a ThreePartTable using `longtable' as the inner tabular, `booktabs' for rules, and the custom `longtablesup' for miscellaneous caption/label formating. </summary>
        /// <remarks> 
        /// 1. Indents the various components to make the result somewhat readable.
        /// 2. Automates caption generation from the passed options.
        /// 3. Automates table footnote ordering and generation. Will comment out those that are not referenced in the table.
        ///    *Table footnote handling is a naiive check based on the presence of the label. If a non-unique value is used, table footnotes may be included erroneously.
        ///    *Note as well that if table footnotes are used in the subsequent caption and no where else, then they may appear prematurely in the table footnotes (there is no way to no where longtable will break.
        /// 
        /// Requires threeparttablex.sty, longtable.sty, booktabs.sty, longtablesup.sty.
        /// May use siunitx.sty and longbooktabs.sty </remarks>
        /// <param name="file_path">The destination for the table</param>
        /// <param name="tabular_cols">String array of column types for the inner tabular environment (e.g., { "l", "C", "S[tight-spacing=true]" })</param>
        /// <param name="table_heading">String for the heading to appear on all pages</param>
        /// <param name="table_body">The full body of the table</param>
        /// <param name="table_notes">The body of the TableNotes environment</param>
        /// <param name="mandatory_caption"> The mandatory argument for \caption. If null or empty, \caption will be omitted.")</param>
        /// <param name="table_label">The label for the table (the argument for \label) (e.g., "tbl:class")</param>
        /// <param name="table_notes_options">Options for the TableNotes environment (default = "para, flushleft") </param>
        /// <param name="table_note_func">Delegate which builds a tablenote from the label and content strings (default = null, which yields "\tblfootnote{label}{content}\par"; note that \tblfootnote is a custom command)</param>
        /// <param name="caption_option">The optional parameter passed to \caption (default = "").</param>
        /// <param name="subsequent_caption">Addition to the top of each subsequent page. If null, then caption_option will be used and subsequent_caption_append will be appended to it. If that is null or empty, the mandatory will be used. If all are null or empty, then none are used. If empty, then non used regardless of caption args.</param>
        /// <param name="subsequent_caption_append">The text appended to subsequent_caption when determined from caption_option or mandatory_caption (default = " continued"). Trailing periods are handled automatically.</param>
        /// <param name="font">font declaration for the table as a whole for the table content. If null or empty, omitted. (default = "\small")</param>
        /// <param name="new_tab_col_sep_length">New length for \tabcolsep. If null or empty, the default is used. (default = "")</param>
        /// <param name="table_note_font">font declaration for the table notes. If null or empty, omitted. (default = "\footnotesize")</param>
        /// <param name="si_setup">Content passed to \sisetup for the table. If null or empty, \sisetup is omitted. (default = "")</param>
        /// <param name="refine_si_columns">If true, the contents of any S column will be assessed and the narowest figure, decimal, and exponent options will be used. Note, the comparision is crude and permits only numbers to be evaluated currently (default=false) </param>
        /// <param name="expo_letter">The letter used to indicate exponent (default = "E")</param>
        /// <param name="pre_tabular_cols">Content before the first tabular column type (default = "@{}")</param>
        /// <param name="post_tabular_cols">Content after the last tabular column type (default = "@{}")</param>
        /// <param name="tabcolsep">The string used to indicate a tab stop (i.e., column) (default = "&")</param>
        /// <param name="tabular_new_line">The string used for a tabularnewline (default = "\tabularnewline")</param>
        /// <param name="indt0">Preceding space for indent level 0 (default = 0)</param>
        /// <param name="indt1">Preceding space for indent level 1 (default = 3)</param>
        /// <param name="indt2">Preceding space for indent level 2 (default = 6)</param>
        /// <param name="indt3">Preceding space for indent level 3 (default = 9)</param>
        public static void ThreePartTable_longtable(
            string file_path,
            string[] tabular_cols,
            string[] table_heading,
            string[] table_body,
            Dictionary<string, string> table_notes,
            string mandatory_caption,
            string table_label,
            string table_notes_options = "para,flushleft",
            Func<string, string, string> table_note_func = null,
            string caption_option = "",
            string subsequent_caption = null,
            string subsequent_caption_append = " continued",
            string font = @"\small",
            string new_tab_col_sep_length = "",
            string table_note_font = @"\footnotesize",
            string si_setup = "",
            bool refine_si_columns = true,
            string expo_letter = "E",
            string pre_tabular_cols = @"@{}",
            string post_tabular_cols = @"@{}",
            string tabcolsep = @"&",
            string tabular_new_line = @"\tabularnewline",
            string indt0 = @"",
            string indt1 = @"   ",
            string indt2 = @"      ",
            string indt3 = @"         ")
        {
            string rslt = ThreePartTable_longtable(
                tabular_cols: tabular_cols,
                table_heading: table_heading,
                table_body: table_body,
                table_notes: table_notes,
                mandatory_caption: mandatory_caption,
                table_label: table_label,
                table_notes_options: table_notes_options,
                table_note_func: table_note_func,
                caption_option: caption_option,
                subsequent_caption: subsequent_caption,
                subsequent_caption_append: subsequent_caption_append,
                font: font,
                new_tab_col_sep_length:new_tab_col_sep_length,
                table_note_font: table_note_font,
                si_setup: si_setup,
                refine_si_columns:refine_si_columns,
                expo_letter: expo_letter,
                pre_tabular_cols: pre_tabular_cols,
                post_tabular_cols: post_tabular_cols,
                tabcolsep: tabcolsep,
                tabular_new_line: tabular_new_line,
                indt0: indt0,
                indt1: indt1,
                indt2: indt2,
                indt3: indt3);
            Console.WriteLine("Saving `" + Path.GetFileName(file_path) + "'");
            using (StreamWriter sw = new StreamWriter(file_path))
            {
                sw.Write(rslt);
            }
        }

        #endregion

        /// <summary> Method to get the element at the passed column index. Used to pull numbers from rows to refine the siunitx S column options. </summary>
        /// <param name="row"></param>
        /// <param name="colidx"></param>
        /// <param name="tabcolsep"></param>
        /// <param name="tabularnewline"></param>
        /// <returns></returns>
        private static string[] Element_At_Column_Idx_siunitx(string row, string tabcolsep = "&", string tabularnewline = @"\tabularnewline")
        {
            if (string.IsNullOrEmpty(row)) return new string[] { };
            string[] parsedatctrltabstop;
            if (row.Contains(@"\" + tabcolsep))
                parsedatctrltabstop = row.Split(new string[] { @"\" + tabcolsep }, StringSplitOptions.None);
            else
                parsedatctrltabstop = new string[] { row };

            List<string> exp_tbl_col_sep = new List<string>();
            for (int i = 0; i < parsedatctrltabstop.Length; i++)
            {
                string[] parsed;
                if(parsedatctrltabstop[i].Contains(tabcolsep))
                    parsed = parsedatctrltabstop[i].Split(new string[] { tabcolsep }, StringSplitOptions.None);
                else
                    parsed = new string[] { parsedatctrltabstop[i] };
                for (int j = 0; j < parsed.Length; j++)
                    if (j == 0 && exp_tbl_col_sep.Count != 0)//append to the last entry
                        exp_tbl_col_sep[exp_tbl_col_sep.Count - 1] += parsed[j];
                    else//otherwise, new entry
                        exp_tbl_col_sep.Add(parsed[j]);
            }
            List<string> siunitx_strings = new List<string>();
            for (int i = 0; i < exp_tbl_col_sep.Count; i++)
            {
                string val = exp_tbl_col_sep[i].Trim();
                //pull off the \tabularnewline.
                if (val.Contains(tabularnewline))
                    if (i != exp_tbl_col_sep.Count - 1) return siunitx_strings.ToArray();//can't do anything more with this row (tabularnewline somewhere within line.
                    else
                        if (val.LastIndexOf(tabularnewline) + tabularnewline.Length == val.Length)//its the last thing in the string
                            val = val.Substring(0, val.LastIndexOf(tabularnewline));

                if (val.Contains(@"\multicolumn"))
                {
                    if (val.IndexOf(@"\multicolumn") != 0) return siunitx_strings.ToArray();//can't do anything more with this row.
                    int multicols = 0;
                    try { multicols = int.Parse(val.Substring((@"\multicolumn{").Length, val.IndexOf("}") - (@"\multicolumn{").Length)); }
                    catch { return siunitx_strings.ToArray(); }//can't do anything more with this row.
                    for (int j = 0; j < multicols; j++)
                        siunitx_strings.Add(string.Empty);
                }
                else siunitx_strings.Add(val);
            }
            return siunitx_strings.ToArray();
        }

        #region Classes and Structs
        private struct TableNote
        {
            public string Label;
            public string Content;
            public TableNote(string label, string content)
            {
                Label = label;
                Content = content;
            }
        }
        private struct NoteLabelIdxInLine
        {
            public int Index;
            public string Label;
            public NoteLabelIdxInLine(int idx, string label)
            {
                Index = idx;
                Label = label;
            }
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
            /// <summary> Option corresponding to the "table-sign-mantissa" flag (siunitx default = false)</summary>
            public bool SignMantissa;

            public bool IsEmpty;
            /// <summary> Initialize a new instance of the siunitx_Sdefault </summary>
            /// <param name="inte"></param>
            /// <param name="deci"></param>
            /// <param name="expo"></param>
            /// <param name="signExponent"></param>
            public siunitx_Sdefault(int inte = 3, int deci = 2, int expo = 0, bool signExponent = false, bool signMantissa = false)
            {
                Integers = inte;
                Decimal = deci;
                Exponent = expo;
                SignExponent = signExponent;
                SignMantissa = signMantissa;
                IsEmpty = false;
            }

            public siunitx_Sdefault(string options)
            {
                //assign default values
                Integers = 3;
                Decimal = 2;
                Exponent = 0;
                SignExponent = false;
                SignMantissa = false;
                IsEmpty = false;
                if (string.IsNullOrEmpty(options)) return;//then assume the defaults are in use

                string[] parsed;
                if (options.Contains(",")) parsed = options.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                else parsed = new string[] { options };

                for (int i = 0; i < parsed.Length; i++)
                {
                    if (!parsed[i].Contains("=")) continue;
                    string key = parsed[i].Substring(0, parsed[i].IndexOf("=")).Trim();
                    string val = parsed[i].Substring(parsed[i].IndexOf("=") + 1).Trim();
                    switch (key)
                    {
                        case("table-figures-integer"):
                            Integers = int.Parse(val);
                            break;
                        case("table-figures-decimal"):
                            Decimal = int.Parse(val);
                            break;
                        case("table-figures-exponent"):
                            Exponent = int.Parse(val);
                            break;
                        case("table-sign-exponent"):
                            SignExponent = bool.Parse(val);
                            break;
                        case ("table-sign-mantissa"):
                            SignMantissa = bool.Parse(val);
                            break;
                        case("table-format"):
                            throw new NotImplementedException("`table-format' not implemented.");
                        default:
                            break;
                    }
                }
            }

            private siunitx_Sdefault(bool empty)
            {
                Integers = 0;
                Decimal = 0;
                Exponent = 0;
                SignExponent = false;
                SignMantissa = false;
                IsEmpty = true;
            }

            public static siunitx_Sdefault Empty { get { return new siunitx_Sdefault(true); } }

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
                bool sign_mant = false;
                if (string.IsNullOrEmpty(formattedrslt)) return siunitx_Sdefault.Empty;//invalid parse
                if (formattedrslt.Length == 0) return siunitx_Sdefault.Empty;//invalid parse
                if (formattedrslt[0] != '{' && formattedrslt[exponentind.Length - 1] != '}')
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
                        int right = 0;
                        try { right = int.Parse(exponent); }
                        catch { return siunitx_Sdefault.Empty; }
                        if (right < 0) sign = true;
                        expo = right.ToString().Length;
                    }
                    if (!string.IsNullOrEmpty(mantissa))
                    {
                        if (mantissa[0] == '-')
                        {
                            sign_mant = true;
                            mantissa = mantissa.Substring(1);
                        }
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
                if (inte == 0 && deci == 0 && expo == 0 && !sign) return siunitx_Sdefault.Empty;//invalid parse
                return new siunitx_Sdefault(inte, deci, expo, sign);
            }

            /// <summary> Return the widest siunitX_S options of the two </summary>
            /// <param name="A"></param>
            /// <param name="B"></param>
            /// <returns></returns>
            public static siunitx_Sdefault GetWidest(siunitx_Sdefault A, siunitx_Sdefault B)
            {
                return new siunitx_Sdefault(Math.Max(A.Integers, B.Integers), Math.Max(A.Decimal, B.Decimal), Math.Max(A.Exponent, B.Exponent), A.SignExponent || B.SignExponent,A.SignMantissa || B.SignMantissa);
            }
            /// <summary> Return the string containing the options which will permit the display of the data in the column, narrowing from the passed default if possible. </summary>
            /// <param name="defaultS"></param>
            /// <returns></returns>
            public string Accomodate_Relative_To_Default(siunitx_Sdefault defaultS)
            {
                if (this.IsEmpty && defaultS.IsEmpty) return string.Empty;
                if (this.IsEmpty) return defaultS.ToString();
                if (defaultS.IsEmpty) return this.ToString();

                string rslt = string.Empty;
                if(this.Integers != defaultS.Integers)
                    rslt += "table-figures-integer=" + this.Integers.ToString();
                if (this.Decimal != defaultS.Decimal)
                    rslt += ((string.IsNullOrEmpty(rslt)) ? ("") : (",")) + "table-figures-decimal=" + this.Decimal.ToString();
                if (this.Exponent != defaultS.Exponent)
                    rslt += ((string.IsNullOrEmpty(rslt)) ? ("") : (",")) + "table-figures-exponent=" + this.Exponent.ToString();
                if (this.SignExponent != defaultS.SignExponent)
                    rslt += ((string.IsNullOrEmpty(rslt)) ? ("") : (",")) + "table-sign-exponent=" + ((this.SignExponent)?("true"):("false"));
                if(this.SignMantissa != defaultS.SignMantissa)
                    rslt += ((string.IsNullOrEmpty(rslt)) ? ("") : (",")) + "table-sign-mantissa=" + ((this.SignMantissa) ? ("true") : ("false"));
                return rslt;
            }

            public override string ToString()
            {
                if (this.IsEmpty) return string.Empty;
                return "table-figures-integer=" + this.Integers.ToString() + "," +
                    "table-figures-decimal=" + this.Decimal.ToString() + "," +
                    "table-figures-exponent=" + this.Exponent.ToString() + "," +
                    "table-sign-exponent=" + ((this.SignExponent) ? ("true") : ("false")) + "," +
                    "table-sign-mantissa=" + ((this.SignMantissa) ? ("true") : ("false"));
            }
        }

        #endregion
    }
}
