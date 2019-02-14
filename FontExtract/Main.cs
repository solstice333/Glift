using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Typography.OpenFont;
using Typography.Contours;

using DrawingGL;
using DrawingGL.Text;

using ArgParse;
using Util;

namespace FontExtract {
    static class Globals {
        public static bool allGlyphs = false;
    }

    class RawGlyph {
        public string Name { get; set; }
        public float[] GlyphPts { get; set; }
        public int[] ContourEnds { get; set; }

        public RawGlyph() { }

        public RawGlyph(string name, float[] glyphPts, int[] contourEnds) {
            Name = name;
            GlyphPts = glyphPts;
            ContourEnds = contourEnds;
        }
    }

    class MainClass {
        public static RawGlyph[] GetRawGlyphs(float sizeInPoints) {
            using (var fstrm = File.OpenRead(Args.ttfPath)) {
                var reader = new OpenFontReader();
                Typeface typeface = reader.Read(fstrm);

                GlyphNameMap[] nameIdxs = Globals.allGlyphs ?
                    typeface.GetGlyphNameIter().ToArray() :
                    Args.chars.Select(
                        c => new GlyphNameMap(
                            typeface.LookupIndex(c), c.ToString())).ToArray();

                return nameIdxs.Select(nameId => {
                    var builder = new GlyphPathBuilder(typeface);
                    builder.BuildFromGlyphIndex(
                        typeface.LookupIndex(nameId.glyphIndex), sizeInPoints);

                    var transl = new GlyphTranslatorToPath();
                    var wrPath = new WritablePath();
                    transl.SetOutput(wrPath);
                    builder.ReadShapes(transl);
                    var curveFlattener = new SimpleCurveFlattener();

                    int[] contourEnds;
                    float[] flattenedPoints = curveFlattener.Flatten(
                        wrPath._points, out contourEnds);

                    return new RawGlyph {
                        Name = nameId.glyphName,
                        GlyphPts = flattenedPoints,
                        ContourEnds = contourEnds
                    };
                }).ToArray();
            }
        }

        public static void Main(string[] args) {
            Args.Parse(args);
            Globals.allGlyphs = Args.chars.Count == 0;
            RawGlyph[] glyphs = GetRawGlyphs(Args.sizePt);
        }
    }
}
