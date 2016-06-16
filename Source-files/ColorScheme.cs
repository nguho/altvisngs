using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace altvisngs
{
    /// <summary> Basic struct for RGBA color </summary>
    public struct _color
    {
        #region Static Fields
        public static _color Red { get { return new _color(255, 0, 0); } }
        public static _color Orange { get { return new _color(255, 127, 0); } }
        public static _color Yellow { get { return new _color(255, 255, 0); } }
        public static _color Green { get { return new _color(0, 255, 0); } }
        public static _color Blue { get { return new _color(0, 0, 255); } }
        public static _color Indigo { get { return new _color(75, 0, 130); } }
        public static _color Violet { get { return new _color(127, 0, 255); } }

        public static _color Black { get { return new _color(0, 0, 0); } }
        public static _color White { get { return new _color(255, 255, 255); } }

        public static _color Empty { get { return new _color(255, 255, 255, 0, true); } }

        #endregion

        #region Fields
        /// <summary> The red component of the color (0-255) </summary>
        public byte R;
        /// <summary> The green component of the color (0-255) </summary>
        public byte G;
        /// <summary> The blue component of the color (0-255) </summary>
        public byte B;
        /// <summary> The alpha (transparency) component of the color (0-255) </summary>
        public byte A;

        /// <summary> If the color is empty (overrides RGBA values if true) </summary>
        public bool IsEmpty;

        #endregion

        #region Constructors
        /// <summary> Initialize a new instance of the color </summary>
        /// <param name="r">Red component</param>
        /// <param name="g">Green component</param>
        /// <param name="b">Blue component</param>
        /// <param name="a">Alpha component</param>
        /// <param name="isEmpty">Empty color</param>
        private _color(byte r, byte g, byte b, byte a, bool isEmpty)
        {
            R = r;
            G = g;
            B = b;
            A = a;
            IsEmpty = isEmpty;
        }
        /// <summary> Initialize a new instance of a color from RGB </summary>
        /// <param name="r">Red component</param>
        /// <param name="g">Green component</param>
        /// <param name="b">Blue component</param>
        public _color(byte r, byte g, byte b) : this(r, g, b, byte.MaxValue, false) { }
        /// <summary> Initialize a new instance of a color from RGBA</summary>
        /// <param name="r">Red component</param>
        /// <param name="g">Green component</param>
        /// <param name="b">Blue component</param>
        /// <param name="a">Alpha component</param>
        public _color(byte r, byte g, byte b, byte a) : this(r, g, b, a, false) { }

        #endregion

        #region Properties
        /// <summary> Get the TikzString representation of the color </summary>
        /// <remarks> Requires on "\tikzset{RGB color/.code={\pgfutil@definecolor{.}{RGB}{#1}\tikzset{color=.}}}"</remarks>
        public string TikZString { get { return "RGB color={" + this.R.ToString() + ", " + this.G.ToString() + ", " + this.B.ToString() + "}"; } }
        
        #endregion
    }

    /// <summary> Abstract base class for color schemes </summary>
    public abstract class ColorScheme { public virtual _color GetColor(double fraction) { throw new NotImplementedException("The Color Scheme method 'GetColor' is not implemented."); } }

    /// <summary> Class describing the cubehelix color scheme </summary>
    /// <remarks> 
    /// Provides a constantly increasing intensity for each level with color changes, making the changes in intensity level appropriate AND allowing grayscale renderings to adequately convey information as well (i.e., printed black and white)
    /// Reference: Green, D.A., 2011, A color scheme for the display of astronomical intensity images, Bulletin of the Astronomical Society of India, 39, 289. 
    /// (https://www.mrao.cam.ac.uk/~dag/CUBEHELIX/) 
    /// </remarks>
    public class CubeHelix
        : ColorScheme
    {
        #region Fields
        private double _hue;
        private double _gamma;
        private double _start;
        private double _rotations;
        private double _startFraction;
        private double _endFraction;

        private bool _clippedColor;
        private bool _reversed;

        #endregion

        #region Constructors
        /// <summary> Instanciate the 'default' cube helix color scheme </summary>
        public CubeHelix() : this(1d, 1d, 0.5d, -1.5d, 0d, 0d, false) { }
        /// <summary> Instanciate the cube helix color scheme </summary>
        /// <param name="pHue"></param>
        /// <param name="pGamma"></param>
        /// <param name="pStart"></param>
        /// <param name="pRotations"></param>
        /// <param name="pStartFraction"></param>
        /// <param name="pEndFraction"></param>
        public CubeHelix(double pHue, double pGamma, double pStart, double pRotations, double pStartFraction, double pEndFraction, bool isReversed)
            : base()
        {
            this.Hue = pHue;
            this.Gamma = pGamma;
            this.Start = pStart;
            this.Rotations = pRotations;
            this.StartFraction = pStartFraction;
            this.EndFraction = pEndFraction;
            _reversed = isReversed;

            _clippedColor = false;
        }

        #endregion

        #region Properties
        /// <summary> Get or set the Hue parameter, which controls how saturated the colors appear. Hue = 0 is pure gray scale. Hue greater than 1 may result in clipped colors (see the 'ClippedColor' property). </summary>
        public double Hue { get { return _hue; } set { _hue = value; this.ClippedColor = false; } }
        /// <summary> Get or set the Gamma factor used to emphasize low (gamma greater than 1) or high (gamma less than 1) intensity values (deviation from a linear increase in intensity) </summary>
        public double Gamma { get { return _gamma; } set { _gamma = value; this.ClippedColor = false; } }
        /// <summary> Get or set the Start Color (i.e., the direction of the predominant color deviation from black; R=1, G=2, B=3) </summary>
        /// <remarks> Acceptable range is 0 to 3 </remarks>
        public double Start { get { return _start; } set { _start = value; this.ClippedColor = false; } }
        /// <summary> Get or set the number of R to G to B rotations that are made from the start (black) to the end (white) of the color scheme </summary>
        public double Rotations { get { return _rotations; } set { _rotations = value; this.ClippedColor = false; } }
        /// <summary> Get or set the fraction of the total color range that is excluded at the black end (to avoid having a black color that may conflict with plot boundaries (i.e., a black axis)) </summary>
        /// <remarks> Acceptable range is 0 to 1, with 'StartFraction' + 'EndFraction' being lessthan or equal to 1 </remarks>
        public double StartFraction { get { return _startFraction; } set { _startFraction = value; this.ClippedColor = false; } }
        /// <summary> Get or set the fraction of the total color range that is excluded at the white end (to avoid having a white color that may conflict with a white background (i.e., the page)) </summary>
        /// <remarks> Acceptable range is 0 to 1, with 'StartFraction' + 'EndFraction' being lessthan or equal to 1 </remarks>
        public double EndFraction { get { return _endFraction; } set { _endFraction = value; this.ClippedColor = false; } }
        /// <summary> Get or (protected) set if a color returned by the 'GetColor' method returned a color outside the possible range (i.e., below 0 or above 1) </summary>
        public bool ClippedColor { get { return _clippedColor; } protected set { _clippedColor = value; } }
        /// <summary> Get or set if the color is reversed (false = dark to light) </summary>
        public bool IsReversed { get { return _reversed; } set { _reversed = value; } }

        #endregion

        #region Methods
        /// <summary> Method to get the color at the fraction </summary>
        /// <param name="frac"></param>
        /// <returns></returns>
        public override _color GetColor(double frac)
        {
            if (frac > 1d) frac = 1d;
            if (frac < 0d) frac = 0d;
            if (this.IsReversed) frac = 1d - frac;

            frac = this.StartFraction + frac * (1d - this.StartFraction - this.EndFraction);
            double angle = 2d * Math.PI * (this.Start / 3d + 1d + this.Rotations * frac);
            frac = Math.Pow(frac, this.Gamma);
            double amp = this.Hue * frac * (1 - frac) / 2d;

            double r = frac + amp * (-0.14861 * Math.Cos(angle) + 1.78277 * Math.Sin(angle));
            if (r > 1d || r < 0d)
            {
                ClippedColor = true;
                r = Math.Max(Math.Min(r, 1d), 0d);
            }

            double g = frac + amp * (-0.29227 * Math.Cos(angle) - 0.90649 * Math.Sin(angle));
            if (g > 1d || g < 0d)
            {
                ClippedColor = true;
                g = Math.Max(Math.Min(g, 1d), 0d);
            }

            double b = frac + amp * (1.97294 * Math.Cos(angle));
            if (b > 1d || b < 0d)
            {
                ClippedColor = true;
                b = Math.Max(Math.Min(b, 1d), 0d);
            }
            return new _color((byte)(r * byte.MaxValue), (byte)(g * byte.MaxValue), (byte)(b * byte.MaxValue), byte.MaxValue);
        }

        #endregion
    }

    /// <summary> Color scheme that is linearly interpolated between the colors of ROYGBIV (a rainbow) </summary>
    public class ROYGBIV
        : ColorScheme
    {        
        /// <summary> Method to provide a rainbow linearly interpolated between ROYGBIV </summary>
        /// <param name="pLevel"></param>
        /// <param name="pNumberOfLevels"></param>
        /// <returns></returns>
        public override _color GetColor(double frac)
        {
            if (frac < 0d) return _color.Red;
            if (frac < 1d / 6d) return Interp_Colors(6d * frac - 0d, _color.Red, _color.Orange);
            if (frac < 2d / 6d) return Interp_Colors(6d * frac - 1d, _color.Orange, _color.Yellow);
            if (frac < 3d / 6d) return Interp_Colors(6d * frac - 2d, _color.Yellow, _color.Green);
            if (frac < 4d / 6d) return Interp_Colors(6d * frac - 3d, _color.Green, _color.Blue);
            if (frac < 5d / 6d) return Interp_Colors(6d * frac - 4d, _color.Blue, _color.Indigo);
            if (frac < 1d) return Interp_Colors(6d * frac - 5d, _color.Indigo, _color.Violet);
            return _color.Violet;
        }

        /// <summary> Method to interpolate linearly between two colors </summary>
        /// <param name="prcnt_1to2"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <returns></returns>
        private _color Interp_Colors(double prcnt_1to2, _color color1, _color color2) { return new _color(Interp_Bytes(prcnt_1to2, color1.R, color2.R), Interp_Bytes(prcnt_1to2, color1.G, color2.G), Interp_Bytes(prcnt_1to2, color1.B, color2.B)); }
        /// <summary> Method to interpolate the byte components of two colors </summary>
        /// <param name="percentAlong"></param>
        /// <param name="int1"></param>
        /// <param name="int2"></param>
        /// <returns></returns>
        private byte Interp_Bytes(double percentAlong, byte int1, byte int2) { return (byte)((percentAlong) * ((double)(int2 - int1)) + (double)int1); }
    }
}