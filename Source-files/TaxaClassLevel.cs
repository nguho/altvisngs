using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace altvisngs
{
    class TaxaClassLevel
    {
        public TaxonObservation[] Unidentified;
        public TaxonObservation[] Minor;
        public TaxonObservation[] Major;

        public TaxaClassLevel[] UnidentifiedTCLs;
        public TaxaClassLevel[] MinorTCLs;
        public TaxaClassLevel[] MajorTCLs;

        int thislevel;

        //The presumption is that ALL of the content at thislevel has the same taxonomy; nextlevel is the next level in depth
        public TaxaClassLevel(TaxonObservation[] content, int nextlevel, int maxlevel, double minorcutoff, string unknown)
        {
            thislevel = nextlevel - 1;
            
            //Define classification based on passed content
            //assume it is already sorted
            //List<TaxonObservation> unid = new List<TaxonObservation>();
            //List<TaxonObservation> minor = new List<TaxonObservation>();
            List<TaxonObservation> major = new List<TaxonObservation>();
            //List<TaxaClassLevel> unidtcl = new List<TaxaClassLevel>();
            //List<TaxaClassLevel> minortcl = new List<TaxaClassLevel>();
            //List<TaxaClassLevel> majortcl = new List<TaxaClassLevel>();
            Unidentified = new TaxonObservation[] { };
            UnidentifiedTCLs = new TaxaClassLevel[] { };
            Minor = new TaxonObservation[] { };
            MinorTCLs = new TaxaClassLevel[] { };
            MajorTCLs = new TaxaClassLevel[] { };

            if (thislevel > maxlevel)
            {
                Major = content;
                return;
            }
            //Build a list of all of the unique taxon names at this level
            Dictionary<string, List<TaxonObservation>> taxons_at_this_level = new Dictionary<string, List<TaxonObservation>>();
            for (int i = 0; i < content.Length; i++)
            {
                if (!taxons_at_this_level.ContainsKey(content[i].Taxon.Hierarchy[nextlevel]))
                    taxons_at_this_level.Add(content[i].Taxon.Hierarchy[nextlevel], new List<TaxonObservation>());
                taxons_at_this_level[content[i].Taxon.Hierarchy[nextlevel]].Add(content[i]);
            }

            //agglomerate the content into the major, minor, and unidentified groups
            //first, the unkowns
            if (taxons_at_this_level.ContainsKey(unknown))
            {
                Unidentified = taxons_at_this_level[unknown].ToArray();
                UnidentifiedTCLs = new TaxaClassLevel[] { new TaxaClassLevel(Unidentified, nextlevel + 1, maxlevel, minorcutoff, unknown) };
                taxons_at_this_level.Remove(unknown);
            }

            //agglomerate by major/minor
            Dictionary<string,TaxonObservation[]> major_tax = new Dictionary<string,TaxonObservation[]>();
            List<TaxonObservation> minor_tax = new List<TaxonObservation>();
            foreach(KeyValuePair<string, List<TaxonObservation>> kvp in taxons_at_this_level)
            {
                if (kvp.Value.Sum((o) => o.Observation.RelativeAbundance) < minorcutoff) minor_tax.AddRange(kvp.Value.ToArray());
                else
                {
                    major_tax.Add(kvp.Key,kvp.Value.ToArray());
                    major.AddRange(kvp.Value.ToArray());
                }
            }
            if (minor_tax.Count != 0)
            {
                Minor = minor_tax.ToArray();
                MinorTCLs = new TaxaClassLevel[] { new TaxaClassLevel(Minor, nextlevel + 1, maxlevel, minorcutoff, unknown) };
            }
            Major = major.ToArray();
            if (major_tax.Count != 0)
            {
                List<string> taxon_names = new List<string>(major_tax.Keys);
                taxon_names.Sort();//sort the list of keys alphabetically
                MajorTCLs = new TaxaClassLevel[taxon_names.Count];
                for (int i = 0; i < taxon_names.Count; i++)
                    MajorTCLs[i] = new TaxaClassLevel(major_tax[taxon_names[i]], nextlevel + 1, maxlevel, minorcutoff, unknown);
            }

            //List<TaxonObservation> currlist = new List<TaxonObservation>();
            //for (int i = 0; i < content.Length; i++)
            //{
            //    if (nextlevel >= content[i].Taxon.Hierarchy.Length)//too long...add to the major
            //        major.Add(content[i]);
            //    else
            //    {
            //        if (currlist.Count == 0)
            //            currlist.Add(content[i]);
            //        else//check if the same as last
            //        {
            //            if (content[i].Taxon.Hierarchy[nextlevel] == currlist.Last().Taxon.Hierarchy[nextlevel])//same clss
            //                currlist.Add(content[i]);
            //            else//end of current list
            //            {
            //                if (string.IsNullOrEmpty(currlist.Last().Taxon.Hierarchy[nextlevel]) || currlist.Last().Taxon.Hierarchy[nextlevel] == unknown)//unid
            //                {
            //                    unid.AddRange(currlist.ToArray());
            //                    if (nextlevel < maxlevel)
            //                        unidtcl.Add(new TaxaClassLevel(currlist.ToArray(), nextlevel + 1, maxlevel, minorcutoff, unknown));
            //                }
            //                else
            //                {
            //                    double ttlprop = currlist.Sum((o) => o.Observation.RelativeAbundance);
            //                    if (IsMinor(ttlprop, minorcutoff))//MINOR CUTOFF!!!
            //                    {
            //                        minor.AddRange(currlist.ToArray());
            //                        if (nextlevel + 1 < maxlevel)
            //                            minortcl.Add(new TaxaClassLevel(currlist.ToArray(), nextlevel + 1, maxlevel, minorcutoff, unknown));
            //                    }
            //                    else
            //                    {
            //                        major.AddRange(currlist.ToArray());
            //                        if (nextlevel < maxlevel)
            //                            majortcl.Add(new TaxaClassLevel(currlist.ToArray(), nextlevel + 1, maxlevel, minorcutoff, unknown));
            //                    }
            //                }
            //                currlist = new List<TaxonObservation>();
            //                currlist.Add(content[i]);
            //            }
            //        }
            //    }
            //}

            ////process last list
            //if (currlist.Count != 0)
            //{
            //    if (string.IsNullOrEmpty(currlist.Last().Taxon.Hierarchy[nextlevel]) || currlist.Last().Taxon.Hierarchy[nextlevel] == unknown)//unid
            //    {
            //        unid.AddRange(currlist.ToArray());
            //        if (nextlevel < maxlevel)
            //            unidtcl.Add(new TaxaClassLevel(currlist.ToArray(), nextlevel + 1, maxlevel, minorcutoff, unknown));
            //    }
            //    else
            //    {
            //        double ttlprop = currlist.Sum((o) => o.Observation.RelativeAbundance);
            //        if (IsMinor(ttlprop, minorcutoff))//MINOR CUTOFF!!!
            //        {
            //            minor.AddRange(currlist.ToArray());
            //            if (nextlevel < maxlevel)
            //                minortcl.Add(new TaxaClassLevel(currlist.ToArray(), nextlevel + 1, maxlevel, minorcutoff, unknown));
            //        }
            //        else
            //        {
            //            major.AddRange(currlist.ToArray());
            //            if (nextlevel < maxlevel)
            //                majortcl.Add(new TaxaClassLevel(currlist.ToArray(), nextlevel + 1, maxlevel, minorcutoff, unknown));
            //        }
            //    }
            //    currlist = new List<TaxonObservation>();
            //}
            //this.Unidentified = unid.ToArray();
            //this.Minor = minor.ToArray();
            //this.Major = major.ToArray();
            //this.UnidentifiedTCLs = unidtcl.ToArray();
            //this.MinorTCLs = minortcl.ToArray();
            //this.MajorTCLs = majortcl.ToArray();
        }

        public GroupedTaxa[] GetAtLevel(int level, bool reverse, string unknown)
        {
            if (level == thislevel)
            {
                List<TaxonObservation> all = new List<TaxonObservation>();
                if (reverse)
                {
                    all.AddRange(this.Unidentified.Reverse());
                    all.AddRange(this.Minor.Reverse());
                    all.AddRange(this.Major.Reverse());
                }
                else
                {
                    all.AddRange(this.Unidentified);
                    all.AddRange(this.Minor);
                    all.AddRange(this.Major);
                }
                double ttlab = all.Sum((o) => o.Observation.RelativeAbundance);//Proportion);
                return new GroupedTaxa[] { new GroupedTaxa((level == -1) ? ("life") : (all[0].Taxon.Hierarchy[level]), ttlab, all.ToArray(), this) };
            }
            //else, higher level
            List<GroupedTaxa> rslt = new List<GroupedTaxa>();
            List<TaxonObservation> unid = new List<TaxonObservation>();
            List<TaxonObservation> mino = new List<TaxonObservation>();
            if (this.Unidentified.Length != 0)
            {
                if (reverse) unid.AddRange(this.Unidentified.ToArray().Reverse());
                else unid.AddRange(this.Unidentified.ToArray());
            }
            if (this.Minor.Length != 0)
            {
                List<TaxonObservation> nowunid = new List<TaxonObservation>();
                for (int i = 0; i < this.Minor.Length; i++)
                {
                    if (!string.IsNullOrEmpty(this.Minor[i].Taxon.Hierarchy[level]) && this.Minor[i].Taxon.Hierarchy[level] != unknown)
                        mino.Add(this.Minor[i]);
                    else
                    {
                        bool notid = true;
                        for (int j = level + 1; j < this.Minor[i].Taxon.Hierarchy.Length; j++)
                            if (!string.IsNullOrEmpty(this.Minor[i].Taxon.Hierarchy[j]) && this.Minor[i].Taxon.Hierarchy[j] != unknown)
                            {
                                notid = false;
                                break;
                            }
                        if (notid) nowunid.Add(this.Minor[i]);
                        else mino.Add(this.Minor[i]);
                    }
                }
                if (nowunid.Count != 0)
                {
                    if (reverse) { unid.Reverse(); nowunid.Reverse(); }
                    unid.AddRange(nowunid.ToArray());
                }
                if (reverse) mino.Reverse();
            }
            if (unid.Count != 0) rslt.Add(new GroupedTaxa("Unidentified", unid.Sum((o) => o.Observation.RelativeAbundance), unid.ToArray()));
            if (mino.Count != 0) rslt.Add(new GroupedTaxa("Minor Taxa", mino.Sum((o) => o.Observation.RelativeAbundance), mino.ToArray()));
            if (reverse)
                for (int i = this.MajorTCLs.Length - 1; i >= 0; i--)
                    rslt.AddRange(this.MajorTCLs[i].GetAtLevel(level, reverse, unknown));
            else
                for (int i = 0; i < this.MajorTCLs.Length; i++)
                    rslt.AddRange(this.MajorTCLs[i].GetAtLevel(level, reverse, unknown));
            return rslt.ToArray();
        }

        public GroupedTaxa[] GetBestClassification(bool reverse, double minorcutoff)
        {
            if (thislevel != -1) throw new ArgumentException("Can only run GetBestClassification on the basal taxaclasslevel");
            List<GroupedTaxa> rslt = new List<GroupedTaxa>();

            if (Unidentified.Length != 0)
                if (reverse)
                    rslt.Add(new GroupedTaxa("Unidentified", Unidentified.Sum((o) => o.Observation.RelativeAbundance), Unidentified.Reverse().ToArray()));
                else
                    rslt.Add(new GroupedTaxa("Unidentified", Unidentified.Sum((o) => o.Observation.RelativeAbundance), Unidentified));

            List<TaxonObservation> min = new List<TaxonObservation>();
            List<TaxonObservation> maj = new List<TaxonObservation>();
            min.AddRange(this.Minor);
            for (int i = 0; i < this.Major.Length; i++)
                if (IsMinor(this.Major[i].Observation.RelativeAbundance, minorcutoff))//MINOR CUTOFF
                    min.Add(this.Major[i]);
                else
                    maj.Add(this.Major[i]);

            if (reverse) { min.Reverse(); maj.Reverse(); }

            rslt.Add(new GroupedTaxa("Minor Taxa", min.Sum((o) => o.Observation.RelativeAbundance), min.ToArray()));
            for (int i = 0; i < maj.Count; i++)
                rslt.Add(new GroupedTaxa(maj[i].Taxon.BestClassification, maj[i].Observation.RelativeAbundance, new TaxonObservation[] { maj[i] }));

            return rslt.ToArray();
        }
        public double TotalProportion { get { return Unidentified.Sum((o) => o.Observation.RelativeAbundance) + Minor.Sum((o) => o.Observation.RelativeAbundance) + Major.Sum((o) => o.Observation.RelativeAbundance); } }
        private string GetBackgrounds(double rhs, double scalemult, double barht, double[] barcenterYs, int maxlevel, string unknown)
        {
            string rslt = string.Empty;
            if (thislevel + 1 == maxlevel || (this.Unidentified.Length == 0 && this.Minor.Length == 0)) return rslt;
            double[] unidents = new double[maxlevel - thislevel];
            double[] minors = new double[maxlevel - thislevel];
            for (int i = 0; i < maxlevel - thislevel; i++)
            {
                unidents[i] = this.GetUnidentifiedAtLevel(thislevel + i, unknown);
                minors[i] = this.GetMinorAtLevel(thislevel + i, unknown);
            }
            if (this.Unidentified.Length != 0)
            {
                //clockwise starting at the upper right corner
                rslt += @"      \fill[noidbackground]";
                rslt += "(" + rhs.ToString(Program.SForm) + "," + BarTop(thislevel + 1, barcenterYs, barht) + ")";// ",-" + (((double)(thislevel + 1)) * baroffset).ToString() + ")";//
                rslt += "--(" + rhs.ToString(Program.SForm) + "," + BarBottom(maxlevel - 1, barcenterYs, barht) + ")";// ",-" + (((double)maxlevel - 1d) * baroffset + barht).ToString() + ")";
                for (int i = maxlevel - thislevel - 1; i > 0; i--)//go back up on the left hand side; iterates through each bar, starting with the bottom one.
                {
                    rslt += "--(" + (rhs - unidents[i] * scalemult).ToString(Program.SForm) + "," + BarBottom(i + thislevel, barcenterYs, barht) + ")";// ",-" + (((double)(i + thislevel)) * baroffset + barht).ToString() + ")";
                    rslt += "--(" + (rhs - unidents[i] * scalemult).ToString(Program.SForm) + "," + BarTop(i + thislevel, barcenterYs, barht) + ")";//",-" + ((double)(i + thislevel) * baroffset).ToString() + ")";
                }
                rslt += "--cycle;" + Environment.NewLine;
            }
            if (this.Minor.Length != 0)
            {
                rslt += @"      \fill[minorbackground]";
                rslt += "(" + (rhs - (minors[0] + unidents[0]) * scalemult).ToString(Program.SForm) + "," + BarBottom(maxlevel - 1, barcenterYs, barht) + ")";//",-" + (((double)maxlevel - 1d) * baroffset + barht).ToString() + ")";
                rslt += "--(" + (rhs - (minors[0] + unidents[0]) * scalemult).ToString(Program.SForm) + "," + BarTop(thislevel + 1, barcenterYs, barht) + ")";// ",-" + (((double)(thislevel + 1)) * baroffset).ToString() + ")";
                for (int i = 1; i < maxlevel - thislevel; i++)//go back up on the right hand side
                {
                    rslt += "--(" + (rhs - unidents[i] * scalemult).ToString(Program.SForm) + "," + BarTop(i + thislevel, barcenterYs, barht) + ")";//",-" + (((double)(i + thislevel)) * baroffset).ToString() + ")";
                    rslt += "--(" + (rhs - unidents[i] * scalemult).ToString(Program.SForm) + "," + BarBottom(i + thislevel, barcenterYs, barht) + ")";//",-" + ((double)(i + thislevel) * baroffset + barht).ToString() + ")";
                }
                rslt += "--cycle;" + Environment.NewLine;
            }
            return rslt;
        }

        private string BarTop(int taxaLevel, double[] barcenterYs, double barht) { return (barcenterYs[taxaLevel] + 0.5 * barht).ToString(Program.SForm); }
        private string BarBottom(int taxaLevel, double[] barcenterYs, double barht) { return (barcenterYs[taxaLevel] - 0.5 * barht).ToString(Program.SForm); }

        public string GetNestedBackgrounds(double rhs, double scalemult, double barht, double[] barcenterYs, int maxlevel, bool reverse, string unknown)
        {
            string rslt = this.GetBackgrounds(rhs, scalemult, barht, barcenterYs, maxlevel, unknown);
            if (thislevel < maxlevel)
            {
                if (this.Unidentified.Length != 0) rhs -= this.Unidentified.Sum((o) => o.Observation.RelativeAbundance) * scalemult;
                if (this.Minor.Length != 0) rhs -= this.Minor.Sum((o) => o.Observation.RelativeAbundance) * scalemult;
                List<TaxaClassLevel> maj = new List<TaxaClassLevel>();
                maj.AddRange(this.MajorTCLs);
                if (reverse) maj.Reverse();
                for (int i = 0; i < maj.Count; i++)
                {
                    rslt += maj[i].GetNestedBackgrounds(rhs, scalemult, barht, barcenterYs, maxlevel, reverse, unknown);
                    rhs -= maj[i].TotalProportion * scalemult;
                }
            }
            return rslt;
        }

        public double GetUnidentifiedAtLevel(int level, string unknown)
        {
            if (level == thislevel)
                if (this.Unidentified.Length > 0) return this.Unidentified.Sum((o) => o.Observation.RelativeAbundance);
                else return 0d;
            List<TaxonObservation> unid = new List<TaxonObservation>();
            if (this.Unidentified.Length != 0) unid.AddRange(this.Unidentified.ToArray());
            if (this.Minor.Length != 0)
            {
                for (int i = 0; i < this.Minor.Length; i++)
                {
                    if (string.IsNullOrEmpty(this.Minor[i].Taxon.Hierarchy[level]) || this.Minor[i].Taxon.Hierarchy[level] == unknown)
                    {
                        bool notid = true;
                        for (int j = level + 1; j < this.Minor[i].Taxon.Hierarchy.Length; j++)
                            if (!string.IsNullOrEmpty(this.Minor[i].Taxon.Hierarchy[j]) && this.Minor[i].Taxon.Hierarchy[j] !=unknown)
                            {
                                notid = false;
                                break;
                            }
                        if (notid) unid.Add(this.Minor[i]);//unid.Add(this.Minor[i]);
                    }
                }
            }
            return unid.Sum((o) => o.Observation.RelativeAbundance);
        }
        public double GetMinorAtLevel(int level, string unknown)
        {
            if (level == thislevel)
                if (this.Minor.Length > 0) return this.Minor.Sum((o) => o.Observation.RelativeAbundance);
                else return 0d;
            List<TaxonObservation> mino = new List<TaxonObservation>();
            if (this.Minor.Length != 0)
            {
                for (int i = 0; i < this.Minor.Length; i++)
                {
                    if (!string.IsNullOrEmpty(this.Minor[i].Taxon.Hierarchy[level]) && this.Minor[i].Taxon.Hierarchy[level] != unknown)
                        mino.Add(this.Minor[i]);
                    else
                    {
                        bool notid = true;
                        for (int j = level + 1; j < this.Minor[i].Taxon.Hierarchy.Length; j++)
                            if (!string.IsNullOrEmpty(this.Minor[i].Taxon.Hierarchy[j]) && this.Minor[i].Taxon.Hierarchy[j] != unknown)
                            {
                                notid = false;
                                break;
                            }
                        if (!notid) mino.Add(this.Minor[i]);
                    }
                }
            }
            return mino.Sum((o) => o.Observation.RelativeAbundance);
        }
        /// <summary> Get if the proportion is minor </summary>
        /// <param name="proportion"></param>
        /// <param name="minorcutoff"></param>
        /// <returns></returns>
        private static bool IsMinor(double proportion, double minorcutoff) { return proportion < minorcutoff; }
    }
}
