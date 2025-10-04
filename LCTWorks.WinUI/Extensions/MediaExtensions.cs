using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace LCTWorks.WinUI.Extensions
{
    public static class MediaExtensions
    {
        public static readonly Color DefaultColor = Colors.White;

        /// <summary>
        /// Returns a representative color for the specified <see cref="Brush"/>.
        /// </summary>
        /// <param name="brush">
        /// The brush to sample. If null, the method returns <paramref name="fallbackColor"/> or <see cref="DefaultColor"/> if not provided.
        /// </param>
        /// <param name="fallbackColor">
        /// Optional color to use when the brush cannot provide a representative color (e.g., unsupported brush types),
        /// or when <paramref name="brush"/> is <c>null</c>. If omitted, <see cref="DefaultColor"/> is used.
        /// </param>
        /// <returns>
        /// A <see cref="Windows.UI.Color"/> derived from the brush. The brush's <see cref="Brush.Opacity"/> is respected.
        /// </returns>
        public static Color ToColor(this Brush brush, Color? fallbackColor = null)
        {
            if (brush is null)
                return fallbackColor ?? DefaultColor;

            var opacity = brush.Opacity;

            switch (brush)
            {
                case SolidColorBrush scb:
                    return ApplyOpacity(scb.Color, opacity);

                case GradientBrush gb:
                    return AverageGradient(gb, opacity);

                case AcrylicBrush ab:
                    {
                        var fallback = ab.FallbackColor;
                        if (fallback.A > 0)
                            return ApplyOpacity(fallback, opacity);

                        var tint = ApplyOpacity(ab.TintColor, ab.TintOpacity);
                        var approx = CompositeOver(tint, Colors.White);
                        return ApplyOpacity(approx, opacity);
                    }

                case XamlCompositionBrushBase xcb:
                    // Generic composition brushes: use FallbackColor if provided
                    return ApplyOpacity(xcb.FallbackColor, opacity);

                default:
                    // Unknown brush type (e.g., ImageBrush): no reliable color; return transparent with opacity
                    return ApplyOpacity(fallbackColor ?? DefaultColor, opacity);
            }
        }

        #region Internal

        private static Color ApplyOpacity(Color color, double opacity)
        {
            var a = ToByte((color.A / 255.0) * opacity);
            return Color.FromArgb(a, color.R, color.G, color.B);
        }

        private static Color AverageGradient(GradientBrush gb, double brushOpacity)
        {
            var stops = gb.GradientStops;
            if (stops is null || stops.Count == 0)
                return ApplyOpacity(Colors.Transparent, brushOpacity);

            // Copy and clamp offsets to [0,1]
            var list = new (double offset, Color color)[stops.Count];
            for (int i = 0; i < stops.Count; i++)
            {
                var off = stops[i].Offset;
                if (double.IsNaN(off)) off = 0;
                off = Math.Clamp(off, 0.0, 1.0);
                list[i] = (off, stops[i].Color);
            }

            // Sort by offset
            Array.Sort(list, (a, b) => a.offset.CompareTo(b.offset));

            // Ensure coverage of [0,1] by padding ends if necessary (pad uses edge colors)
            var needsHead = list[0].offset > 0.0;
            var needsTail = list[^1].offset < 1.0;

            int count = list.Length + (needsHead ? 1 : 0) + (needsTail ? 1 : 0);
            var stopsClamped = new (double offset, Color color)[count];
            int idx = 0;

            if (needsHead)
                stopsClamped[idx++] = (0.0, list[0].color);

            for (int i = 0; i < list.Length; i++)
                stopsClamped[idx++] = list[i];

            if (needsTail)
                stopsClamped[idx++] = (1.0, list[^1].color);

            // Integrate the linear gradient over [0,1] in premultiplied space:
            // For each segment [o0,o1], the average is (C0 + C1)/2 weighted by (o1 - o0).
            double accR = 0, accG = 0, accB = 0, accA = 0, totalLen = 0;

            for (int i = 0; i < stopsClamped.Length - 1; i++)
            {
                var (o0, c0) = stopsClamped[i];
                var (o1, c1) = stopsClamped[i + 1];
                var len = o1 - o0;
                if (len <= 0) continue;

                Premul pc0 = Premultiply(c0);
                Premul pc1 = Premultiply(c1);

                var segR = (pc0.R + pc1.R) * 0.5;
                var segG = (pc0.G + pc1.G) * 0.5;
                var segB = (pc0.B + pc1.B) * 0.5;
                var segA = (pc0.A + pc1.A) * 0.5;

                accR += segR * len;
                accG += segG * len;
                accB += segB * len;
                accA += segA * len;
                totalLen += len;
            }

            if (totalLen <= 0)
                return ApplyOpacity(stopsClamped[0].color, brushOpacity);

            // Normalize
            accR /= totalLen; accG /= totalLen; accB /= totalLen; accA /= totalLen;

            // Apply brush opacity in premultiplied space
            accR *= brushOpacity; accG *= brushOpacity; accB *= brushOpacity; accA *= brushOpacity;

            // Un-premultiply
            if (accA <= 0)
                return Colors.Transparent;

            var r = accR / accA;
            var g = accG / accA;
            var b = accB / accA;

            return FromDoubles(accA, r, g, b);
        }

        private static Color CompositeOver(Color fg, Color bg)
        {
            // Standard Porter-Duff "over"
            double fa = fg.A / 255.0, fr = fg.R / 255.0, fgG = fg.G / 255.0, fb = fg.B / 255.0;
            double ba = bg.A / 255.0, br = bg.R / 255.0, bgG = bg.G / 255.0, bb = bg.B / 255.0;

            double outA = fa + ba * (1 - fa);
            if (outA <= 0) return Colors.Transparent;

            double outR = (fr * fa + br * ba * (1 - fa)) / outA;
            double outG = (fgG * fa + bgG * ba * (1 - fa)) / outA;
            double outB = (fb * fa + bb * ba * (1 - fa)) / outA;

            return FromDoubles(outA, outR, outG, outB);
        }

        private static Color FromDoubles(double a, double r, double g, double b)
        {
            return Color.FromArgb(ToByte(a), ToByte(r), ToByte(g), ToByte(b));
        }

        private static Premul Premultiply(Color c)
        {
            double a = c.A / 255.0;
            return new Premul((c.R / 255.0) * a, (c.G / 255.0) * a, (c.B / 255.0) * a, a);
        }

        private static byte ToByte(double x)
        {
            var v = (int)Math.Round(x * 255.0);
            v = Math.Clamp(v, 0, 255);
            return (byte)v;
        }

        private readonly struct Premul(double r, double g, double b, double a)
        {
            public readonly double R = r, G = g, B = b, A = a;
        }

        #endregion Internal
    }
}