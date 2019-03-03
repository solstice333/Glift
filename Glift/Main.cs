using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Point3 = System.Numerics.Vector3;

using Typography.OpenFont;
using Typography.Contours;

using DrawingGL;
using DrawingGL.Text;

using Util;

namespace Glift {
    using Face = VertexCache.Face;

    static class Globals {
        public static bool allGlyphs = false;
    }

    static class VertexCacheExt {
        private delegate IEnumerable<Point3> _VertGetter(Face f);
        private delegate IEnumerable<Triangle3> _TriGetter(Face f);
        private delegate int _IndexOfGetter(Point3 p, Face f);

        public static IEnumerable<Point3> VerticesOfFace(
            this VertexCache vertCache, Face face) {
            var d = new Dictionary<Face, _VertGetter> {
                { Face.Front, vertCache.Vertices },
                { Face.Side, vertCache.Vertices },
                { Face.Outline, vertCache.Vertices },
                { Face.All, vertCache.Vertices }
            };
            return d[face](face);
        }

        public static IEnumerable<Triangle3> TrianglesOfFace(
            this VertexCache vertCache, Face face) {
            var d = new Dictionary<Face, _TriGetter> {
                { Face.Front, vertCache.Triangles },
                { Face.Side, vertCache.Triangles },
                { Face.Outline, vertCache.Triangles },
                { Face.All, vertCache.Triangles }
            };
            return d[face](face);
        }

        public static int IndexOfFace(this VertexCache vertCache, 
            Point3 p, Face face) {
            var d = new Dictionary<Face, _IndexOfGetter> {
                { Face.Front, vertCache.IndexOf },
                { Face.Side, vertCache.IndexOf },
                { Face.Outline, vertCache.IndexOf },
                { Face.All, vertCache.IndexOf }
            };
            return d[face](p, face);
        }
    }

    class FaceTask {
        public string NameExt { get; set; }
        public Face Face { get; set; }

        public FaceTask() {
            NameExt = "";
            Face = Face.All;
        }

        public FaceTask(string nameExt, Face face) {
            NameExt = nameExt;
            Face = face;
        }
    }

    class MainClass {
        public delegate void WriteLine(string msg = "");

        public static RawGlyph[] GetRawGlyphs() {
            using (var fstrm = File.OpenRead(Args.ttfPath)) {
                var reader = new OpenFontReader();
                Typeface typeface = reader.Read(fstrm);
                float defaultSizeInPoints = 72f;

                GlyphNameMap[] nameIdxs = Globals.allGlyphs ?
                    typeface.GetGlyphNameIter().ToArray() :
                    Args.chars.Select(
                        c => new GlyphNameMap(
                            typeface.LookupIndex(c), c.ToString())).ToArray();

                return nameIdxs.Select(nameId => {
                    var builder = new GlyphPathBuilder(typeface);
                    builder.BuildFromGlyphIndex(
                        nameId.glyphIndex, defaultSizeInPoints);

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

        public static List<FaceTask> CreateFaceTasks() {
            var faceTasks = new List<FaceTask>();
            var faceOnlies = new List<Face>();

            if (Args.frontOnly)
                faceOnlies.Add(Face.Front);
            if (Args.sideOnly)
                faceOnlies.Add(Face.Side);
            if (Args.outlineOnly)
                faceOnlies.Add(Face.Outline);

            var d = new Dictionary<Face, string>();
            if (faceOnlies.Count > 1) {
                d[Face.Front] = "FrontOnly";
                d[Face.Side] = "SideOnly";
                d[Face.Outline] = "OutlineOnly";
            }
            else {
                d[Face.Front] = "";
                d[Face.Side] = "";
                d[Face.Outline] = "";
            }

            if (faceOnlies.Count == 0)
                faceTasks.Add(new FaceTask("", Face.All));
            else
                foreach (Face f in faceOnlies)
                    faceTasks.Add(new FaceTask(d[f], f));

            return faceTasks;
        }

        public static void Main(string[] args) {
            Args.Parse(args);
            Globals.allGlyphs = Args.chars.Count == 0;
            RawGlyph[] glyphs = GetRawGlyphs();
            List<FaceTask> faceTasks = CreateFaceTasks();

            WriteLine tee = null;
            if (Args.print)
                tee = Console.WriteLine;

            foreach (RawGlyph g in glyphs) {
                if (Args.listNames) {
                    Console.WriteLine(g.Name);
                    continue;
                }

                StreamWriter objWriter = null;

                var vtxCache = new VertexCache(g,
                    Args.zdepth, Args.thickness,
                    Args.xoffset, Args.yoffset,
                    Args.sizeMult);

                foreach (FaceTask task in faceTasks) {
                    if (!Args.dryRun) {
                        objWriter = File.CreateText(
                            $"{g.Filename}{task.NameExt}.obj");
                        tee += objWriter.WriteLine;
                    }

                    tee?.Invoke($"# {g.Name}");

                    foreach (Point3 pt in vtxCache.VerticesOfFace(task.Face))
                        tee?.Invoke($"v {pt.X} {pt.Y} {pt.Z}");
                    foreach (
                        Triangle3 tri in vtxCache.TrianglesOfFace(task.Face)) {
                        int vtxIdx1 = vtxCache.IndexOfFace(tri.P1, task.Face);
                        int vtxIdx2 = vtxCache.IndexOfFace(tri.P2, task.Face);
                        int vtxIdx3 = vtxCache.IndexOfFace(tri.P3, task.Face);
                        tee?.Invoke($"f {vtxIdx1} {vtxIdx2} {vtxIdx3}");
                    }
                    tee?.Invoke();

                    if (objWriter != null) {
                        tee -= objWriter.WriteLine;
                        objWriter?.Dispose();
                        objWriter = null;
                    }
                }
            }
        }
    }
}
