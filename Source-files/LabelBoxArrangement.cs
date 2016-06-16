using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace altvisngs
{
    /// <summary> Struct containing an arrangement of labels </summary>
    struct LabelBoxArrangement
    {
        public double Score;//the score for the arrangement
        public PositionedTikzLabel[] Boxes;
        public bool IsEmpty;

        public LabelBoxArrangement(double score, PositionedTikzLabel[] boxes)
        {
            throw new NotImplementedException("LabelBoxArrangement intended to be initialized through the static 'Evaluate' method.");
            //Score = score;
            //Boxes = boxes;
            //IsEmpty = false;
        }
        private LabelBoxArrangement(double score, PositionedTikzLabel[] boxes, bool isEmpty)
        {
            Score = score;
            Boxes = boxes;
            IsEmpty = isEmpty;
        }

        public static LabelBoxArrangement Empty { get { return new LabelBoxArrangement(double.PositiveInfinity, new PositionedTikzLabel[] { }, true); } }
        /// <summary> Evaluate a collection of positioned labels and assign a score </summary>
        /// <param name="boxes"></param>
        /// <returns></returns>
        public static LabelBoxArrangement Evaluate(PositionedTikzLabel[] boxes, double barBaseY, double barcenterY, double rowstep, double rowPenalty, double[] minX_for_config)
        {
            if (boxes.Length == 0) throw new ArgumentOutOfRangeException("Must not be empty array of boxes");
            //base score on average length
            double maxY = 0d;
            double ttllen = 0d;
            double max_excursion = 0d;
            for (int i = 0; i < boxes.Length; i++)
            {
                if (Math.Abs(boxes[i].BaseY) > maxY)
                    maxY = boxes[i].BaseY;
                ttllen += boxes[i].LeaderLength;
                //max_excursion = Math.Max(max_excursion, boxes[i].RightX - minX_for_config[boxes[i].RowIdx]);
            }
            double score = rowPenalty * (Math.Ceiling(Math.Abs(maxY - barBaseY) / rowstep)) + max_excursion + ttllen / ((double)boxes.Length);
            return new LabelBoxArrangement(score, boxes, false);
        }
        /// <summary> Method to evaluate all of the boxes in the configuration and determine the minimum X value at the requested row index for configurations added to the right of this one. </summary>
        /// <remarks> Either a box at this row or a leader from a row above can limit. Determine which</remarks>
        /// <param name="rowIdx">The index of the row</param>
        /// <param name="ymin">The Y value of the bottom of the row</param>
        /// <param name="ymax">The Y value of the top of the row</param>
        /// <param name="oldLimit">The limiting x value if nothing limits in this arrangement</param>
        /// <param name="labelminsep">The minimum separation between labels</param>
        /// <returns></returns>
        public double DetermineMaxRightX(int rowIdx, double ymin, double ymax, double oldLimit, double labelminsep, double leader_buffer)
        {
            double rslt = oldLimit;
            for (int i = 0; i < this.Boxes.Length; i++)
                if (this.Boxes[i].RowIdx == rowIdx)
                    rslt = Math.Max(rslt, this.Boxes[i].RightX + labelminsep);
                else if (this.Boxes[i].RowIdx > rowIdx)//above...may be leader limited.
                    rslt = Math.Max(rslt, Math.Max(this.Boxes[i].GetLeaderXAtY(ymin), this.Boxes[i].GetLeaderXAtY(ymax)) + leader_buffer);
            return rslt;
        }
    }
}
