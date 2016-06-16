using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace altvisngs
{
    /// <summary> Struct to contain a positioned box...used for determining if leaders cross boxes drawn within the bar </summary>
    struct PositionedTikzLabelBox
    {
        public TikzLabelBox Box;
        public double LeftX;
        public double LowerY;
        public TikzLabel Label;

        public PositionedTikzLabelBox(TikzLabelBox box, double leftX, double lowerY, TikzLabel label)
        {
            Box = box;
            LeftX = leftX;
            LowerY = lowerY;
            Label = label;
        }

        public double RightX { get { return LeftX + Box.Widthcm; } }
        public double UpperX { get { return LowerY + Box.Heightcm; } }
    }
}
