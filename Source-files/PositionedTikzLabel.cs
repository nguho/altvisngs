using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace altvisngs
{
    /// <summary> Struct containing a positioned label </summary>
    struct PositionedTikzLabel
    {
        public TikzLabel Label;
        public double LeftX;
        public double BaseY;
        public int RowIdx;
        public double LeaderX;
        public bool IsEmpty;

        public PositionedTikzLabel(TikzLabel label, double leftX, double baseY, double leaderX, int rowIdx)
        {
            Label = label;
            LeftX = leftX;
            BaseY = baseY;
            LeaderX = leaderX;
            RowIdx = rowIdx;
            IsEmpty = false;
        }
        private PositionedTikzLabel(TikzLabel label, double leftX, double baseY, double leaderX, int rowIdx, bool isEmpty)
        {
            Label = label;
            LeftX = leftX;
            BaseY = baseY;
            LeaderX = leaderX;
            RowIdx = rowIdx;
            IsEmpty = isEmpty;
        }

        /// <summary> Get the horizontal distance between the leader and the center of the box </summary>
        public double HorizontalDistance { get { return this.LeaderX - this.Label.xCenterofBar; } }
        /// <summary> Return the Y value of the leader at the label </summary>
        /// <param name="barCenterY"> Center of the bar </param>
        /// <returns></returns>
        public double LeaderY
        {
            get
            {
                if (this.BaseY > 0) return BaseY - Label.LabelBox.Depthcm;//label above row
                else return BaseY + Label.LabelBox.Heightcm;// Depthcm;//label below row
            }
        }
        /// <summary> Return the length of the leader </summary>
        /// <param name="barCenterY">Center of the bar</param>
        /// <returns></returns>
        public double LeaderLength { get { return Math.Sqrt(Math.Pow(this.LeaderY, 2d) + Math.Pow(this.HorizontalDistance, 2d)); } }
        /// <summary> Get the right edge of the label (as drawn) </summary>
        public double RightX { get { return this.LeftX + this.Label.LabelBox.Widthcm; } }
        /// <summary> Get an empty positioned tikzlabel </summary>
        public static PositionedTikzLabel Empty { get { return new PositionedTikzLabel(null, 0d, 0d, 0d, -1, true); } }

        public string tikz()
        {
            if (this.Label.AllFitsInBar)
                return @"          \node[taxalbl" + this.Label.Level.ToString() + ",anchor=mid] at (" + this.Label.xCenterofBar.ToString(Program.SForm) + ",0) {" + this.Label.NodeContent_Full + "};" + Environment.NewLine;

            string tikzrslt = this.tikzleader;
            if (this.Label.PercentFitsInBar)
            {
                tikzrslt += @"          \node[taxalbl" + this.Label.Level.ToString() + ", anchor=mid] at (" + this.Label.xCenterofBar.ToString(Program.SForm) + ",0) {" + this.Label.NodeContent_PercentOnly + "};" + Environment.NewLine;
                tikzrslt += @"          \node[taxalbl" + this.Label.Level.ToString() + ", anchor=base west] at (" + this.LeftX.ToString(Program.SForm) + "," + this.BaseY.ToString(Program.SForm) + ") {" + this.Label.NodeContent_TaxaOnly + "};" + Environment.NewLine;
            }
            else
                tikzrslt +=@"          \node[taxalbl" + this.Label.Level.ToString() + ",anchor=base west] at (" + this.LeftX.ToString(Program.SForm) + "," + this.BaseY.ToString(Program.SForm) + ") {" + this.Label.NodeContent_Full + "};" + Environment.NewLine;

            return tikzrslt;
        }
        private string tikzleader
        {
            get
            {
                if (this.Label.AllFitsInBar) return string.Empty;
                else
                {
                    double yatinter = (this.BaseY > 0) ? (BaseY - Math.Max(Label.LabelBox.Depthcm, 0.035)) : (BaseY + Label.LabelBox.Heightcm - Label.LabelBox.Depthcm + 0.035);
                    //offset the leader from the inersection with the outline of the label by 1pt
                    double xatinter = this.GetLeaderXAtY(yatinter);
                    return @"          \draw[leaderstyle] (" + xatinter.ToString(Program.SForm) + "," + yatinter.ToString(Program.SForm) + ") -- (" + this.Label.xCenterofBar.ToString(Program.SForm) + ",0);" + Environment.NewLine;
                }
            }
        }

        public double GetLeaderXAtY(double Y)
        {
            if (this.Label.AllFitsInBar) return this.Label.xCenterofBar;
            return (this.LeaderX - this.Label.xCenterofBar) / (this.LeaderY) * Y + this.Label.xCenterofBar;
        }
    }
}
