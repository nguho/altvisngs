using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace altvisngs
{
    enum HorizontalAnchor { West, Mid, East };
    class TikzLabel
    {
        #region Field
        public string SpecialTaxaFormat;
        public string Taxon_Name;
        public string Proportion;
        public int Level;
        public int Index;
        public double xCenterofBar;//measured from the left
        public double widthofBar;

        public TikzLabelBox Label_And_Percent_Box;//box for label and percent
        public TikzLabelBox Label_Box;
        public TikzLabelBox Percent_Box;//box for only the percent
        public bool AllFitsInBar;
        public bool PercentFitsInBar;
        public double X;//location of the box anchor (y at baseline).
        public HorizontalAnchor Anchor;//box anchor...dictates where X is on the box
        public double xshift;//horizontal shift of the box relative to the anchor location.
        public double baselineY;//the box y location;

        #endregion

        #region Constructors
        /// <summary> Initialize an instance of a TikzLabel </summary>
        /// <param name="taxon_name"></param>
        /// <param name="proportion"></param>
        /// <param name="level"></param>
        /// <param name="index"></param>
        /// <param name="xcenter"></param>
        /// <param name="barWidth"></param>
        public TikzLabel(string taxon_name, string proportion, int level, int index, double xcenter, double barWidth)
        {
            this.Taxon_Name = taxon_name;
            this.Proportion = proportion;
            this.Level = level;
            this.Index = index;
            this.xCenterofBar = xcenter;
            this.widthofBar = barWidth;
            this.Label_And_Percent_Box = TikzLabelBox.Empty;
            this.Label_Box = TikzLabelBox.Empty;
            this.Percent_Box = TikzLabelBox.Empty;
            this.AllFitsInBar = false;
            this.PercentFitsInBar = false;
            Anchor = HorizontalAnchor.East;
        }

        #endregion

        #region Properties
        /// <summary> Get the unique identifier for the TikzLabel for measuring </summary>
        public string LevelIndex { get { return this.Level.ToString() + "b" + this.Index.ToString(); } }
        /// <summary> Get the TikzLabelBox to be positioned outside the bar </summary>
        public TikzLabelBox LabelBox
        {
            get
            {
                if (this.PercentFitsInBar) return Label_Box;//only the label
                else return Label_And_Percent_Box;//full box
            }
        }

        /// <summary> Get or set the x location of the left side of the box </summary>
        public double LabelBoxWest
        {
            get
            {
                switch (this.Anchor)
                {
                    case (HorizontalAnchor.Mid):
                        return this.X - 0.5 * this.LabelBox.Widthcm + this.xshift;
                    case (HorizontalAnchor.West):
                        return this.X + this.xshift;
                    case (HorizontalAnchor.East):
                        return this.X - this.LabelBox.Widthcm + this.xshift;
                    default: throw new ArgumentException("Unknown HorizontalAnchor");
                }
            }
            set
            {
                this.xshift = 0;
                if (value > this.xCenterofBar)//to the right of the center...
                {
                    this.Anchor = HorizontalAnchor.West;
                    this.X = value;
                    if (this.X - this.xCenterofBar < 0.125)//too vertical...make it angled a bit
                    {
                        this.X = this.xCenterofBar + 0.125;//shift the node back
                        this.xshift = value - this.X;//shift back
                    }
                    return;
                }
                if (value + this.LabelBox.Widthcm < this.xCenterofBar)//to the left of the center...
                {
                    this.Anchor = HorizontalAnchor.East;
                    this.X = value + this.LabelBox.Widthcm;
                    if (this.xCenterofBar - this.X < 0.125)//too vertical...make it angled a bit
                    {
                        this.X = this.xCenterofBar - 0.125;
                        this.xshift = this.X - (value + this.LabelBox.Widthcm);//positive to the right.
                    }
                    return;
                }
                //at this point the label is over the center.
                this.Anchor = HorizontalAnchor.Mid;
                if (value + 0.5 * this.LabelBox.Widthcm > this.xCenterofBar)//center of the label is to the right of the center of the box...angle right
                    this.X = this.xCenterofBar + 0.125;
                else
                    this.X = this.xCenterofBar - 0.125;

                this.xshift = (value + 0.5 * this.LabelBox.Widthcm) - this.X;
            }
        }
        /// <summary> Get the x location of the right side of the box </summary>
        public double LabelBoxEast { get { return this.LabelBoxWest + this.LabelBox.Widthcm; } }
        /// <summary> Get the horizontal distance between the anchor of the label and the center of the bar ("ideal" is -0.125 to 0.125) </summary>
        public double Displacement { get { return this.X - this.xCenterofBar; } }

        /// <summary> Get the node content for the full label </summary>
        /// <remarks> Used if the whole label fits in the bar or if nothing but the leader end fits in the bar</remarks>
        public string NodeContent_Full { get { return this.Taxon_Name + @" \normalfont(" + this.Proportion + @"\,\%)"; } }
        /// <summary> Get the node content for the taxa label only </summary>
        /// <remarks> Used if the percent only fits in the bar</remarks>
        public string NodeContent_TaxaOnly { get { return this.Taxon_Name; } }
        /// <summary> Get the node content for the percent label only </summary>
        /// <remarks> Used if the percent only fits in the bar</remarks>
        public string NodeContent_PercentOnly { get { return @"\normalfont" + this.Proportion + @"\,\%"; } }

        #endregion
    }
}
