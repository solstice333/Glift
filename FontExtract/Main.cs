using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Point2 = System.Numerics.Vector2;
using Vec2 = System.Numerics.Vector2;

using Typography.OpenFont;
using Typography.Contours;

using DrawingGL;
using DrawingGL.Text;

using Util;

namespace FontExtract {
    static class Globals {
        public static bool allGlyphs = false;
    }

    class MainClass {
        public delegate void WriteLine(string msg = "");

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
                        nameId.glyphIndex, sizeInPoints);

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

            WriteLine tee = null;
            if (Args.print)
                tee = Console.WriteLine;

            foreach (var g in glyphs) {
                if (Args.listNames) {
                    Console.WriteLine(g.Name);
                    continue;
                }

                StreamWriter objWriter = null;
                if (!Args.dryRun) {
                    objWriter = File.CreateText($"{g.Filename}.obj");
                    tee += objWriter.WriteLine;
                }

                tee?.Invoke($"# {g.Name}");
                var vtxCache = new VertexCache(g);
                foreach (var pt in vtxCache.Vertices)
                    tee?.Invoke($"v {pt.X} {pt.Y} 0");
                foreach (var tri in vtxCache.FrontTriangles) {
                    int vtxIdx1 = vtxCache.IndexOf(tri.P1);
                    int vtxIdx2 = vtxCache.IndexOf(tri.P2);
                    int vtxIdx3 = vtxCache.IndexOf(tri.P3);
                    tee?.Invoke($"f {vtxIdx1} {vtxIdx2} {vtxIdx3}");
                }
                tee?.Invoke();

                if (objWriter != null) {
                    tee -= objWriter.WriteLine;
                    objWriter?.Dispose();
                }
            }
        }
    }
}
