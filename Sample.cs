using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace altvisngs
{
    public struct SampleKeyFilter
    {
        public string PropertyName;
        public string PropertyValue;
        public bool IsEmpty;

        public SampleKeyFilter(string propertyName, string propertyValue)
        {
            PropertyName = propertyName;
            PropertyValue = propertyValue;
            IsEmpty = false;
        }
        private SampleKeyFilter(string propertyName, string propertyValue, bool isEmpty)
        {
            PropertyName = propertyName;
            PropertyValue = propertyValue;
            IsEmpty = isEmpty;
        }
        public static SampleKeyFilter Empty { get { return new SampleKeyFilter(string.Empty, string.Empty, true); } }
        public static SampleKeyFilter[] MultipleNames(string propertyName, params string[] propertyValues)
        {
            if (propertyValues == null) return new SampleKeyFilter[] { };
            SampleKeyFilter[] rslt = new SampleKeyFilter[propertyValues.Length];
            for (int i = 0; i < propertyValues.Length; i++)
                rslt[i] = new SampleKeyFilter(propertyName, propertyValues[i]);
            return rslt;
        }
    }
    [Serializable]
    public class Sample
    {
        #region Fields
        private NamedAttribute[] _attributes;//attributes initially defined by the _key.txt file, then saved in the sample file
        private TaxonObservation[] _taxonObservations;//the observations for this sample

        #endregion

        #region Constructors
        public Sample()
        {
            _attributes = new NamedAttribute[] {};
            _taxonObservations = new TaxonObservation[] { };
        }
        /// <summary> </summary>
        /// <param name="taxonObservations"></param>
        /// <param name="attributes">Dictionary containing the attributes (meta data; i.e., sample ID, date, etc.)</param>
        public Sample(NamedAttribute[] attributes, TaxonObservation[] taxonObservations)
        {
            _attributes = attributes;
            _taxonObservations = taxonObservations;
        }

        #endregion

        #region Properties
        /// <summary> Get the number of attributes </summary>
        public int AttributesCount { get { return _attributes.Length; } }
        /// <summary> Get the number of taxon observations </summary>
        public int TaxonObservationsCount { get { return _taxonObservations.Length; } }
        /// <summary> Get or set the TaxonObservations of the sample </summary>
        public TaxonObservation[] TaxonObservations { get { return _taxonObservations; } set { _taxonObservations = value; } }
        /// <summary> Get or set the attributes of the Sample </summary>
        public NamedAttribute[] Attributes { get { return _attributes; } set { _attributes = value; } }
        #endregion

        #region Methods - Attributes
        /// <summary> Determines whether the sample has the passed attribute </summary>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public bool HasAttr(string attributeName) 
        {
            for (int i = 0; i < _attributes.Length; i++)
                if (_attributes[i].Name == attributeName)
                    return true;
            return false;
        }
        /// <summary> Returns the value of the passed attribute</summary>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public string GetAttr(string attributeName) 
        {
            for (int i = 0; i < _attributes.Length; i++)
                if (_attributes[i].Name == attributeName)
                    return _attributes[i].Value;
            throw new ArgumentOutOfRangeException("The name `" + attributeName + "' was not found in the list of attributes");
        }
        
        #endregion

        #region Methods - Read/Write
        public void WriteToFile(string filePath)
        {
            Console.WriteLine("Saving sample to `" + Path.GetFileName(filePath) + "'");
            XmlSerializer x = new XmlSerializer(typeof(Sample));
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                x.Serialize(sw, this);
                //sw.WriteLine(@"<?xml version = ""1.0""?>");//added to make it a "legitimate" xml
                //sw.WriteLine(@"<sample>");
                //sw.WriteLine(@"   <attributes>");
                //foreach(KeyValuePair<string,string> kvp in _attributes)
                //    sw.WriteLine(@"      <attribute name=""" + kvp.Key + @""">" + kvp.Value + @"</attribute>");
                //sw.WriteLine(@"   </attributes>");
                //sw.WriteLine(@"   <taxaobservs>");
                //for (int i = 0; i < _taxonObservations.Length; i++)
                //{
                //    if (_taxonObservations[i].Observations.Length != 1) throw new ArgumentOutOfRangeException("Expecting observations to be unitary for a single sample.");
                //    sw.WriteLine(@"      <taxaobserv>");
                //    sw.WriteLine(@"         <taxonname>");
                //    for (int j = 0; j < _taxonObservations[i].Taxon.Hierarchy.Length; j++)
                //        sw.WriteLine(@"            <taxonlevel idx=""" + j.ToString() + @""" name=""" + _taxonObservations[i].Taxon.Hierarchy[j] + @"""/>");
                //    sw.WriteLine(@"         </taxonname>");
                //    sw.WriteLine(@"         <observ abundance=""" + _taxonObservations[i].Observations[0].Abundance.ToString() + @""" relabundance=""" + _taxonObservations[i].Observations[0].RelativeAbundance.ToString() + @"/>");
                //    sw.WriteLine(@"      </taxaobserv>");
                //}
                //sw.WriteLine(@"   </taxaobservs>");
                //sw.WriteLine(@"</sample>");
            }
        }

        public static Sample ReadFromFile(string filePath)
        {
            Console.WriteLine("Opening sample from `" + Path.GetFileName(filePath) + "'");
            XmlSerializer x = new XmlSerializer(typeof(Sample));
            Sample rslt;
            using (StreamReader sr = new StreamReader(filePath))
            {
                rslt = (Sample)(x.Deserialize(sr));
            }
            return rslt;
        }

        private static int ReadNextLine(StreamReader sr, int lastidx, out string line, params string[] expectedLeftAfterTrim)
        {
            if (sr.EndOfStream) throw new FormatException(@"Invalid sample file format. End of file reached prematurely; """ + expectedLeftAfterTrim + @""" expected.");
            line = sr.ReadLine().TrimStart( new char[] {'\t',' '} );
            int idx = lastidx + 1;
            if (expectedLeftAfterTrim == null) return idx;
            //else, check if the content is there
            for (int i = 0; i < expectedLeftAfterTrim.Length; i++)
            {
                if (line.Length < expectedLeftAfterTrim[i].Length) continue;
                if (line.Substring(0, expectedLeftAfterTrim[i].Length) != expectedLeftAfterTrim[i]) continue;
                return idx;//passed
            }
            throw new FormatException(@"Invalid file format (expecting """ + string.Join(@""" or """, expectedLeftAfterTrim) + @""" on line " + idx.ToString() + ")");
        }
        #endregion

        #region Methods - TaxonSum
        /// <summary> Method to sum all of the observations whose taxons are members of the passed taxon </summary>
        /// <param name="supertaxon"></param>
        /// <param name="unknown"></param>
        /// <param name="includeunknownsinwild"></param>
        /// <returns></returns>
        public Observation SumAllMembersOf(Taxon supertaxon, string unknown, bool includeunknownsinwild = false)
        {
            Observation rslt = new Observation(0d,0);
            for (int i = 0; i < TaxonObservationsCount; i++)
                if (TaxonObservations[i].Taxon.IsMemberOf(supertaxon, unknown, includeunknownsinwild))
                    rslt += TaxonObservations[i].Observation;
            return rslt;
        }

        #endregion

        #region Equality
        public override bool Equals(object obj)
        {
            if (!(obj is Sample)) return false;
            Sample sample = (Sample)obj;

            if(this.AttributesCount != sample.AttributesCount) return false;
            if(this.TaxonObservationsCount != sample.TaxonObservationsCount) return false;

            //compare the hierarchy
            foreach (NamedAttribute kvp in this._attributes)
            {
                if (!sample.HasAttr(kvp.Name)) return false;
                if (kvp.Value != sample.GetAttr(kvp.Name)) return false;
            }
            //compare the taxaObservations
            for (int i = 0; i < _taxonObservations.Length; i++)
                if (this.TaxonObservations[i] != sample.TaxonObservations[i]) return false;

            return true;
        }
        public static bool operator ==(Sample A, Sample B) { return A.Equals(B); }
        public static bool operator !=(Sample A, Sample B) { return !A.Equals(B); }
        
        /// <summary>Return the hashcode for this TaxonObservations</summary>
        /// <remarks>Implementation based on: http://stackoverflow.com/a/263416</remarks>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (NamedAttribute kvp in this._attributes)
                    hash = hash * 23 + kvp.Value.GetHashCode();
                for (int i = 0; i < this._taxonObservations.Length; i++)
                    hash = hash * 23 + this._taxonObservations[i].GetHashCode();
                return hash;
            }
        }

        #endregion
    }
    /// <summary> Custom keyvalue pair to permit serialization </summary>
    public struct NamedAttribute
    {
        public string Name;
        public string Value;
        public NamedAttribute(string key, string value)
        {
            Name = key;
            Value = value;
        }
    }
}
