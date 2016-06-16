using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace altvisngs
{
    struct TikzLabelBox
    {
        public double HeightPt;
        public double WidthPt;
        public double DepthPt;
        public bool IsEmpty;
        private TikzLabelBox(double width, double height, double depth, bool isEmpty)
        {
            WidthPt = width;
            HeightPt = height;
            DepthPt = depth;
            IsEmpty = isEmpty;
        }
        public TikzLabelBox(double width, double height, double depth) : this(width, height, depth, false) { }
        public static TikzLabelBox Empty { get { return new TikzLabelBox(0d, 0d, 0d, true); } }
        public double Widthcm { get { return this.WidthPt / 72.27 * 2.54; } }
        public double Heightcm { get { return this.HeightPt / 72.27 * 2.54; } }
        public double Depthcm { get { return this.DepthPt / 72.27 * 2.54; } }
    }
}
