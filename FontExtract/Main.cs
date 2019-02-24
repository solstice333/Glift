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

namespace FontExtract {
    static class Globals {
        public static bool allGlyphs = false;
    }

    static class VertexCacheExt {
        public static IEnumerable<Point3> VerticesOfFace(
            this VertexCache vertCache, VertexCache.Face face) {
            if (face == VertexCache.Face.Front)
                return vertCache.Vertices(VertexCache.Face.Front);
            else if (face == VertexCache.Face.Side)
                return vertCache.Vertices(VertexCache.Face.Side);
            else
                return vertCache.Vertices(VertexCache.Face.All);
        }

        public static IEnumerable<Triangle3> TrianglesOfFace(
            this VertexCache vertCache, VertexCache.Face face) {
            if (face == VertexCache.Face.Front)
                return vertCache.Triangles(VertexCache.Face.Front);
            else if (face == VertexCache.Face.Side)
                return vertCache.Triangles(VertexCache.Face.Side);
            else
                return vertCache.Triangles(VertexCache.Face.All);
        }

        public static int IndexOfFace(this VertexCache vertCache, 
            Point3 p, VertexCache.Face face) {
            if (face == VertexCache.Face.Front)
                return vertCache.IndexOf(p, VertexCache.Face.Front);
            else if (face == VertexCache.Face.Side)
                return vertCache.IndexOf(p, VertexCache.Face.Side);
            else
                return vertCache.IndexOf(p, VertexCache.Face.All);
        }
    }

    class FaceTask {
        public string NameExt { get; set; }
        public VertexCache.Face Face { get; set; }

        public FaceTask() {
            NameExt = "";
            Face = VertexCache.Face.All;
        }

        public FaceTask(string nameExt, VertexCache.Face face) {
            NameExt = nameExt;
            Face = face;
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

        public static List<FaceTask> CreateFaceTasks() {
            var faceTasks = new List<FaceTask>();

            if (Args.sideOnly && Args.frontOnly) {
                faceTasks.Add(
                    new FaceTask("SideOnly", VertexCache.Face.Side));
                faceTasks.Add(
                    new FaceTask("FrontOnly", VertexCache.Face.Front));
            }
            else {
                if (Args.sideOnly)
                    faceTasks.Add(new FaceTask("", VertexCache.Face.Side));
                else if (Args.frontOnly)
                    faceTasks.Add(new FaceTask("", VertexCache.Face.Front));
                else
                    faceTasks.Add(new FaceTask("", VertexCache.Face.All));
            }

            return faceTasks;
        }

        public static void Main(string[] args) {
            Args.Parse(args);
            Globals.allGlyphs = Args.chars.Count == 0;
            RawGlyph[] glyphs = GetRawGlyphs(Args.sizePt);
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

                foreach (FaceTask task in faceTasks) {
                    if (!Args.dryRun) {
                        objWriter = File.CreateText(
                            $"{g.Filename}{task.NameExt}.obj");
                        tee += objWriter.WriteLine;
                    }

                    tee?.Invoke($"# {g.Name}");
                    var vtxCache = new VertexCache(g,
                        Args.zdepth, Args.thickness, 
                        Args.xoffset, Args.yoffset);
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
