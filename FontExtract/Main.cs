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

using ArgParse;
using Util;

namespace FontExtract {
    static class Globals {
        public static bool allGlyphs = false;
    }

    class RawGlyph {
        private static HashSet<string> _allNames = new HashSet<string>();

        private string _name;
        private string _filename;
        private float[] _glyphPts;
        private int[] _contourEnds;

        public string Name {
            get => _name; 
            set { 
                _name = value ?? "";
                var fname = _name;
                while (_allNames.Contains(fname.ToLower()))
                    fname += "_";
                _filename = fname;
                _allNames.Add(_filename.ToLower());
            }
        }

        public string Filename {
            get => _filename;
        }

        public float[] GlyphPts {
            get => _glyphPts;
            set => _glyphPts = value ?? new float[] { };
        }

        public int[] ContourEnds { 
            get => _contourEnds;
            set => _contourEnds = value ?? new int[] { };
        }

        public RawGlyph() {
            _name = "";
            _glyphPts = new float[] { };
            _contourEnds = new int[] { };
        }

        public RawGlyph(string name, float[] glyphPts, int[] contourEnds) {
            Name = name ?? "";
            GlyphPts = glyphPts ?? new float[] { };
            ContourEnds = contourEnds ?? new int[] { };
        }

        public Point2[] Vertices(out int[] contourEnds) {
            contourEnds = ContourEnds.Select(v => v / 2).ToArray();
            return Vertices();
        }

        public Point2[] Vertices() {
            var pts = new List<Point2>();
            for (int i = 0; i < GlyphPts.Length; i += 2)
                pts.Add(new Point2(GlyphPts[i], GlyphPts[i + 1]));
            return pts.ToArray();
        }
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
                foreach (var p in g.Vertices())
                    tee?.Invoke($"v {p.X} {p.Y} 0");
                tee?.Invoke();

                if (objWriter != null) {
                    tee -= objWriter.WriteLine;
                    objWriter?.Dispose();
                }
            }
        }
    }
}
