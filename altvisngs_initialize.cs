using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace altvisngs
{
    /// <summary> Abstract class containg methods called when the "initialize" option is passed to altvisngs </summary>
    /// <remarks> This is the first step in the workflow, which will generate individual _ngsdata.txt files containing the requisite taxonomic data </remarks>
    abstract class altvisngs_initialize
    {
        #region dbcAmplicons
        /// <summary> Method to create the sample_ngsdata.txt files for each sample in the passed files </summary>
        /// <param name="output_directory"></param>
        /// <param name="filepath_key"></param>
        /// <param name="filepath_abundance"></param>
        /// <param name="filepath_proportions"></param>
        /// <param name="filepath_taxainfo"></param>
        public static Sample[] Initialize_dbcAmplicons(string output_directory, string filepath_key, string filepath_abundance, string filepath_proportions, string filepath_taxainfo, string keysep = ",", string keyIDcol = "ID", string unknown="unknown")
        {
            //first, validate the passed values
            if (string.IsNullOrEmpty(filepath_abundance) || string.IsNullOrEmpty(filepath_proportions) || string.IsNullOrEmpty(filepath_taxainfo) || string.IsNullOrEmpty(filepath_key))
                throw new ArgumentNullException("abundance, proportions, taxa_info, and key file paths must not be null.");
            if (!File.Exists(filepath_abundance) || !File.Exists(filepath_proportions) || !File.Exists(filepath_taxainfo) || !File.Exists(filepath_key))
                throw new ArgumentException("abundance, proportions, taxa_info, and key files must exist.");
            if ((Path.GetExtension(filepath_abundance) != ".txt" && Path.GetExtension(filepath_abundance) != ".TXT") || (Path.GetExtension(filepath_proportions) != ".txt" && Path.GetExtension(filepath_proportions) != ".TXT") || (Path.GetExtension(filepath_taxainfo) != ".txt" && Path.GetExtension(filepath_taxainfo) != ".TXT"))
                throw new ArgumentException("abundance, proportions, and taxa_info files must be text (.txt) files");
            if (Path.GetExtension(filepath_key) != ".csv" && Path.GetExtension(filepath_key) != ".CSV")
                throw new ArgumentException("key file must be a comma separated values (.csv) file");
            if (string.IsNullOrEmpty(output_directory))
                throw new ArgumentNullException("output_directory must not be null.");
            if(!Directory.Exists(output_directory))
                try { Directory.CreateDirectory(output_directory); }
                catch(Exception ex) { throw new ArgumentException("Invalid output_directory", ex); }

            //Get the attributes from the key file
            Dictionary<string, NamedAttribute[]> attributes = altvisngs_initialize.ReadAttributesFromKey(filepath_key, keysep, keyIDcol);

            //Note, these three files are read separately and the intermediate results are compared, rather than reading all three simultaneously line by line, so that if the dbcAmplicons output changes, the requisite modifications could be made here easily (hopefully).\
            //get the full taxonomy from filepath_taxainfo
            string[][] taxaInfo_Full = altvisngs_initialize.ReadTaxaInfo_dbcAmplicions(filepath_taxainfo);
            //define the samples from the abundance
            sampleproportions_dbcAmplicions[] partialsamples = altvisngs_initialize.ReadProportions_dbcAmplicions(filepath_proportions, taxaInfo_Full);
            //add the abundance to the samples
            Sample[] samples = altvisngs_initialize.ReadAbundance_dbcAmplicions(filepath_abundance, partialsamples, taxaInfo_Full, attributes, unknown, string.Empty, null, unknown);

            altvisngs_initialize.WriteSamples(output_directory, altvisngs_initialize.ngsdata_heading_dbcAmplicons, samples,keyIDcol);
            return samples;
        }

        /// <summary> Method to read the key file path and pull the attributes for each of the samples </summary>
        /// <param name="filepath_key">The file path to the key</param>
        /// <param name="sep">The separator in the key file (default = ",")</param>
        /// <param name="ID">The heading for the column containing the ID by which the sample is denoted in the sequencing results (default = "ID")</param>
        /// <returns></returns>
        private static Dictionary<string, NamedAttribute[]> ReadAttributesFromKey(string filepath_key, string sep = ",", string ID = "ID")
        {
            Dictionary<string, NamedAttribute[]> rslt = new Dictionary<string, NamedAttribute[]>();
            using (StreamReader sr = new StreamReader(filepath_key))
            {
                int cols =-1;
                int ididx=-1;
                string[] names = null;
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (!line.Contains(sep)) throw new FormatException("Expected attribute delimiter (`" + sep + "') not found.");
                    string[] parsed = line.Split(new string[] {sep},StringSplitOptions.None);
                    if(cols == -1)
                    {
                        cols = parsed.Length;
                        names = parsed;
                        for(int i=0;i<names.Length;i++)
                            if(names[i] == ID)
                            {
                                ididx = i;
                                break;
                            }
                        if(ididx == -1) throw new FormatException("No Sample ID column found (expected heading: `" + ID + "').");
                    }
                    else
                    {
                        if(cols!=parsed.Length) throw new FormatException("Uneven number of columns in key file");
                        NamedAttribute[] rs = new NamedAttribute[cols];
                        for(int i=0;i<cols;i++)
                            rs[i] = new NamedAttribute(names[i],parsed[i]);
                        rslt.Add(parsed[ididx], rs);
                    }
                }
            }
            return rslt;
        }
        /// <summary> Method to read the table.taxa_info.txt file and return an array of classifications </summary>
        /// <param name="filepath_taxainfo"></param>
        /// <returns></returns>
        private static string[][] ReadTaxaInfo_dbcAmplicions(string filepath_taxainfo)
        {
            Console.WriteLine("Reading taxainfo from '" + filepath_taxainfo + "'");
            //Read ther taxainfo
            //Expecting the first column to be of the format:
            //d__Bacteria;p__Acidobacteria;c__Acidobacteria_Gp1;o__Candidatus Koribacter;f__Candidatus Koribacter;g__Candidatus Koribacter
            //the following prefixes are permitted d__: domain; p__: phylum; c__: class; o__: order; f__: family; g__: genus; s__: species; i__: isolate
            List<string[]> fulltaxa = new List<string[]>();
            int lineNo = 0;
            int lineLength = 0;
            int idxoftaxon = -1;
            int maxtaxalevels = 8;//maximum number permitted by dbcAmplicons
            using (StreamReader sr = new StreamReader(filepath_taxainfo))
            {
                while (!sr.EndOfStream)
                {
                    lineNo++;
                    try
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line)) continue;
                        if (!line.Contains('\t')) throw new ArgumentException("Unexpected format in `" + Path.GetFileName(filepath_taxainfo) + "': Non-empty line is not tab-delimited.");
                        string[] parsed = line.Split(new char[] { '\t' }, StringSplitOptions.None);//assume tab delimited
                        if (lineNo == 1)
                        {
                            lineLength = parsed.Length;
                            for (int i = 0; i < lineLength; i++)
                                if (parsed[i] == "Taxon_Name")
                                {
                                    idxoftaxon = i;
                                    break;
                                }
                            if (idxoftaxon == -1) throw new ArgumentException("Unexpected format in `" + Path.GetFileName(filepath_taxainfo) + "': `Taxon_Name' expected as a heading on the first line.");
                            continue;//skip the heading
                        }
                        if (parsed.Length != lineLength) throw new ArgumentOutOfRangeException("Unexpected format in `" + Path.GetFileName(filepath_taxainfo) + "': Inconsistent table width. Expecting " + lineLength.ToString() + " columns. This has " + parsed.Length.ToString() + " columns.");

                        parsed = parsed[idxoftaxon].Split(new char[] { ';' }, StringSplitOptions.None);//assume delimited by ;
                        string[] fulltaxon = new string[maxtaxalevels];
                        for (int i = 0; i < parsed.Length; i++)
                        {
                            if (string.IsNullOrEmpty(parsed[i])) throw new ArgumentNullException("Null taxon");
                            fulltaxon[altvisngs_initialize.TaxonLevelFromPrefix_dbcAmplicons(parsed[i].Substring(0, 3))] = parsed[i].Substring(3);
                        }
                        fulltaxa.Add(fulltaxon);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("An error was encountered in the processing of line: " + lineNo.ToString() + " of `" + filepath_taxainfo + "'.", ex);
                    }
                }
            }
            return fulltaxa.ToArray();
        }
        /// <summary> Method to read the table.proportions.txt file and return a collection of samples with taxon_names, levels and proportions </summary>
        /// <param name="filepath_proportions"></param>
        /// <param name="TaxaInfo_Full"></param>
        private static sampleproportions_dbcAmplicions[] ReadProportions_dbcAmplicions(string filepath_proportions, string[][] taxaInfo_Full)
        {
            Console.WriteLine("Reading proportions from '" + filepath_proportions + "'");
            //Read the proportions table
            //Expected format: Taxon_Name \t Level \t Sample \t Sample \t Sample \t...
            //expected to end with Sample Counts and the sum of the total counts for each

            int lineNo = 0;
            int lineLength = 0;
            int taxonNameCol = 0;
            int levelCol = 1;
            int sampleStartCol = 2;
            List<sampleproportions_dbcAmplicions> rslt = new List<sampleproportions_dbcAmplicions>();
            using (StreamReader sr = new StreamReader(filepath_proportions))
            {
                while (!sr.EndOfStream)
                {
                    lineNo++;
                    try
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line)) continue;
                        if (!line.Contains('\t')) throw new FormatException("Unexpected format in `" + Path.GetFileName(filepath_proportions) + "': Expecting a tab-delimited table");
                        string[] parsed = line.Split(new char[] { '\t' }, StringSplitOptions.None);//assume tab delimited
                        if (lineNo == 1)
                        {
                            lineLength = parsed.Length;
                            if (parsed[taxonNameCol] != "Taxon_Name" || parsed[1] != "Level")
                                throw new FormatException("Unexpected format in `" + Path.GetFileName(filepath_proportions) + "': `Taxon_Name' and `Level' are expected as the first two columns on the first line.");
                            for (int i = sampleStartCol; i < lineLength; i++)
                                rslt.Add(new sampleproportions_dbcAmplicions(parsed[i], i));
                            continue;//go to the next line
                        }
                        if (parsed.Length != lineLength) 
                            throw new FormatException("Unexpected format in `" + Path.GetFileName(filepath_proportions) + "': Inconsistent table width. Expecting " + lineLength.ToString() + " columns. This has " + parsed.Length.ToString() + " columns.");
                        if (lineNo == taxaInfo_Full.Length + 2)//sample counts?
                        {
                            if (parsed[taxonNameCol] != "Sample Counts") 
                                throw new FormatException("Unexpected format in `" + Path.GetFileName(filepath_proportions) + "': Expected the last line to start with `Sample Counts'");
                            break;//done
                        }

                        //verify that the taxa_info is the same.
                        if (taxaInfo_Full[lineNo - 2][altvisngs_initialize.TaxonLevelFromName_dbcAmplicons(parsed[levelCol])] != parsed[taxonNameCol])//not equal
                            throw new Exception("taxa_info/proportions disagreement. Expecting `" +
                                taxaInfo_Full[lineNo - 2][altvisngs_initialize.TaxonLevelFromName_dbcAmplicons(parsed[levelCol])] + "' at '" +
                                parsed[levelCol] + "' level. Line " + lineNo.ToString() + " has '" + parsed[taxonNameCol] + "'");
                        for (int i = sampleStartCol; i < lineLength; i++)
                        {
                            double proportion;
                            if (!double.TryParse(parsed[i], out proportion))
                                throw new FormatException("Invalid proportion of `" + parsed[i].ToString() + "' (cannot cast to double).");
                            rslt[i - sampleStartCol].proportions.Add(new proportion_dbcAmplicons(parsed[taxonNameCol], parsed[levelCol], proportion));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("An error was encountered in the processing of line: " + lineNo.ToString() + " of `" + filepath_proportions + "'.", ex);
                    }
                }
                if (lineNo != taxaInfo_Full.Length + 2)
                    throw new Exception("taxa_info/proportions disagreement. Inconsistent number of taxa levels. taxa_info:" + taxaInfo_Full.Length.ToString() + "; `" + Path.GetFileName(filepath_proportions) + "':" + (lineNo - 2).ToString());
            }
            return rslt.ToArray();
        }
        /// <summary> Method to read the table.abundance.txt file and return the revised sample_dbcAmplicons array to include the abundance </summary>
        /// <param name="filepath_abundance"></param>
        /// <param name="samples_proportions"></param>
        /// <returns></returns>
        private static Sample[] ReadAbundance_dbcAmplicions(string filepath_abundance, sampleproportions_dbcAmplicions[] samples_proportions, string[][] taxaInfo_Full, Dictionary<string, NamedAttribute[]> attrs, string unknown="unknown", params string[] considerunknown)
        {
            Console.WriteLine("Reading abundance from '" + filepath_abundance + "'");
            //Read the abundance table
            //Expected format: Taxon_Name \t Level \t Sample \t Sample \t Sample \t...
            //Expected to end with the last taxa
            int lineNo = 0;
            int lineLength = 0;
            int taxonNameCol = 0;
            int levelCol = 1;
            int sampleStartCol = 2;
            List<Sample> rslt = new List<Sample>();
            Dictionary<string, TaxonObservation[]> taxons = new Dictionary<string, TaxonObservation[]>();
            foreach (KeyValuePair<string, NamedAttribute[]> kvp in attrs)
                taxons.Add(kvp.Key, new TaxonObservation[samples_proportions[0].proportions.Count]);

            string[] ids = null;
            using (StreamReader sr = new StreamReader(filepath_abundance))
            {
                while (!sr.EndOfStream)
                {
                    lineNo++;
                    try
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line)) continue;
                        if (!line.Contains('\t')) throw new FormatException("Unexpected format in `" + Path.GetFileName(filepath_abundance) + "': Expecting a tab-delimited table");
                        string[] parsed = line.Split(new char[] { '\t' }, StringSplitOptions.None);//assume tab delimited
                        if (lineNo == 1)
                        {
                            lineLength = parsed.Length;
                            if (parsed[taxonNameCol] != "Taxon_Name" || parsed[1] != "Level")
                                throw new FormatException("Unexpected format in `" + Path.GetFileName(filepath_abundance) + "': `Taxon_Name' and `Level' are expected as the first two columns on the first line.");
                            if (samples_proportions.Length != lineLength - sampleStartCol)
                                throw new Exception("proportions/abundance disagreement. Expecting " + samples_proportions.Length.ToString() + " samples. " +
                                    Path.GetFileName(filepath_abundance) + " has " + sampleStartCol.ToString());
                            for (int i = sampleStartCol; i < lineLength; i++)
                            {
                                if (samples_proportions[i - sampleStartCol].ID != parsed[i])
                                    throw new Exception("proportions/abundance disagreement. Expecting `" + samples_proportions.Length.ToString() + "' sample ID at colum " + i.ToString() + ". " +
                                        Path.GetFileName(filepath_abundance) + " has `" + parsed[i] + "'");
                                //rslt.Add(new Sample(new TaxonObservations[]{}, attr));
                            }
                            ids = parsed;
                            continue;//go to the next line
                        }
                        if (parsed.Length != lineLength)
                            throw new FormatException("Unexpected format in `" + Path.GetFileName(filepath_abundance) + "': Inconsistent table width. Expecting " + lineLength.ToString() + " columns. This has " + parsed.Length.ToString() + " columns.");
                        for (int i = sampleStartCol; i < lineLength; i++)
                        {
                            if (i == sampleStartCol)//verify that the taxa_info is the same. Only need to check for one.
                            {
                                string shouldbetaxa = samples_proportions[i - sampleStartCol].proportions[lineNo - 2].Taxon_Name;
                                string shouldbelevel = samples_proportions[i - sampleStartCol].proportions[lineNo - 2].Level;
                                if (shouldbetaxa != parsed[taxonNameCol] || shouldbelevel != parsed[levelCol])
                                    throw new Exception("proportions/abundance disagreement. Expecting `" + shouldbetaxa + "' at level `" + shouldbelevel + "'. " +  
                                        "Line " + lineNo.ToString() + " has `" + parsed[taxonNameCol] + "' at level `" + parsed[levelCol] + "'");
                            }
                            int abundance;
                            if (!int.TryParse(parsed[i], out abundance))
                                throw new FormatException("Invalid proportion of `" + parsed[i].ToString() + "' (cannot cast to int).");
                            if (taxons.ContainsKey(ids[i]))
                            {
                                string[] corrected = taxaInfo_Full[lineNo - 2];
                                if (considerunknown != null)
                                    for (int j = 0; j < corrected.Length; j++)
                                        if (considerunknown.Contains(corrected[j]))
                                            corrected[j] = unknown;
                                taxons[ids[i]][lineNo - 2] = new TaxonObservation(
                                    new Taxon(corrected, unknown),
                                    new Observation(samples_proportions[i - sampleStartCol].proportions[lineNo - 2].Proportion, abundance));
                            }
                            else
                                throw new ArgumentException("Should never be here");//rslt[i - sampleStartCol].TaxaObs.Add(new TaxaObs(parsed[taxonNameCol], abundance, samples_proportions[i - sampleStartCol].proportions[lineNo - 2].Proportion, false, taxaInfo_Full[lineNo-2]));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("An error was encountered in the processing of line: " + lineNo.ToString() + " of `" + filepath_abundance + "'.", ex);
                    }
                }
                if (lineNo != taxaInfo_Full.Length + 1)
                    throw new Exception("proportions/abundance disagreement. Inconsistent number of taxa levels. proportions:" + samples_proportions.Length.ToString() + "; `" + Path.GetFileName(filepath_abundance) + "':" + (lineNo - 2).ToString());
            }

            //build the samples 
            foreach (KeyValuePair<string, NamedAttribute[]> kvp in attrs)
                rslt.Add(new Sample(kvp.Value, taxons[kvp.Key]));
            return rslt.ToArray();
        }

        #region TaxonName/Prefix <=> Level
        /// <summary> Get the appropriate array index corresponding to the taxa level prefix in table.taxa_info.txt </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private static int TaxonLevelFromPrefix_dbcAmplicons(string prefix)
        {
            switch (prefix)
            {
                case ("d__")://domain
                    return 0;
                case ("p__")://phylum
                    return 1;
                case ("c__")://class
                    return 2;
                case ("o__")://order
                    return 3;
                case ("f__")://family
                    return 4;
                case ("g__")://genus
                    return 5;
                case ("s__")://species
                    return 6;
                case ("i__")://isolate
                    return 7;
                default:
                    throw new ArgumentOutOfRangeException("Unrecognized taxon prefix: '" + prefix + "'");
            }
        }
        /// <summary> Get the appropriate array index corresponding to the taxa level name in table.proportions.txt or table.abundance.txt </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static int TaxonLevelFromName_dbcAmplicons(string name)
        {
            switch (name)
            {
                case ("domain")://d__
                    return 0;
                case ("phylum")://p__
                    return 1;
                case ("class")://c__
                    return 2;
                case ("order")://o__
                    return 3;
                case ("family")://f__
                    return 4;
                case ("genus")://g__
                    return 5;
                case ("species")://s__
                    return 6;
                case ("isolate")://i__
                    return 7;
                default:
                    throw new ArgumentOutOfRangeException("Unrecognized taxon name: '" + name + "'");
            }
        }

        public static string TaxonNameFromLevel_dbcAmplicons(int level)
        {
            switch (level)
            {
                case(0):
                    return "domain";//d__
                case (1):
                    return "phylum";//p__
                case (2):
                    return "class";//c__
                case (3):
                    return "order";//o__
                case (4):
                    return "family";//f__
                case (5):
                    return "genus";//g__
                case (6):
                    return "species";//s__
                case (7):
                    return "isolate";//i__
                default:
                    throw new ArgumentOutOfRangeException("Unrecognized taxon level: '" + level.ToString() + "'");
            }

        }

        #endregion

        /// <summary> Get the header for the ngsdata.txt files when using dbcAmplicons </summary>
        public static string ngsdata_heading_dbcAmplicons
        {
            get
            {
                return 
                    "Best_Taxon" + '\t' +
                    "Best_Level" + '\t' +
                    "0" + '\t' +
                    "1" + '\t' +
                    "2" + '\t' +
                    "3" + '\t' +
                    "4" + '\t' +
                    "5" + '\t' +
                    "6" + '\t' +
                    "7" + '\t' +
                    "Abundance" + '\t' +
                    "Proportion";
            }
        }
        
        private class sampleproportions_dbcAmplicions
        {
            public string ID;
            public int Column;
            public List<proportion_dbcAmplicons> proportions;

            public sampleproportions_dbcAmplicions(string id, int column)
            {
                ID = id;
                Column = column;
                proportions = new List<proportion_dbcAmplicons>();
            }
        }
        private struct proportion_dbcAmplicons
        {
            public string Taxon_Name;
            public string Level;
            public double Proportion;
            public proportion_dbcAmplicons(string taxon_Name, string level, double proportion)
            {
                Taxon_Name = taxon_Name;
                Level = level;
                Proportion = proportion;
            }
        }

        #endregion

        /// <summary> Method to write the individual sample_ngsdata.txt files </summary>
        /// <param name="output_directory">The directory to which all of the sample_ngsdata.txt files are written</param>
        /// <param name="ngsdata_heading">The heading for each sample file. May vary depending on the available taxonomic levels</param>
        /// <param name="samples">Array of Samples</param>
        private static void WriteSamples(string output_directory, string ngsdata_heading, Sample[] samples, string ID = "ID")
        {
            if(samples == null) return;
            for (int i = 0; i < samples.Length; i++)
                samples[i].WriteToFile(output_directory + "\\" + samples[i].GetAttr(ID) + "_ngsdata.xml");
        }
    }
}
