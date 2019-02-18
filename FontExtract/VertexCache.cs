using System;
using System.Linq;
using System.Collections.Generic;
using Tesselate;

using Point2 = System.Numerics.Vector2;
using Point3 = System.Numerics.Vector3;

using Util;

namespace FontExtract {
    using VCacheList = List<(Point3 Point, int Index)>;
    using VCacheDict = Dictionary<Point3, int>;

    public static class PointExt {
        public static float Epsilon = 0.001f;

        public static bool EqualsEps(this Point3 p1, Point3 p2) {
            return p1.X > p2.X - Epsilon && p1.X < p2.X + Epsilon &&
                p1.Y > p2.Y - Epsilon && p1.Y < p2.Y + Epsilon &&
                p1.Z > p2.Z - Epsilon && p1.Z < p2.Z + Epsilon;
        }

        public static Point2 ToPoint2(this Point3 p) {
            return new Point2(p.X, p.Y);
        }

        public static Point3 ToPoint3(this Point2 p, float z = 0) {
            return new Point3(p.X, p.Y, z);
        }
    }

    public class VertexNotFoundException : KeyNotFoundException {
        public VertexNotFoundException(Point3 point) :
            base($"vertex {point} not found") { }
    }

    public class VertexCache {
        private Type _containType;
        private VCacheDict _vert_d;
        private VCacheList _vert_l;
        private RawGlyph _glyph;
        private int _idx;
        private List<Triangle3> _tris;

        public enum Type { List, Dict }

        private void _Migrate(VCacheList src, VCacheDict dst) {
            foreach (var entry in src)
                dst[entry.Point] = entry.Index;
            src.Clear();
        }

        private void _Migrate(VCacheDict src, VCacheList dst) {
            foreach (var entry in src.OrderBy(e => e.Value))
                dst.Add((entry.Key, entry.Value));
            src.Clear();
        }

        private void _GatherMainOutline() {
            foreach (var p in _glyph.Vertices())
                Add(new Point3(p.X, p.Y, 0));
        }

        private void _GatherFrontTessOutline() {
            TessTool tessa = new TessTool();
            if (!tessa.TessPolygon(_glyph.GlyphPts, _glyph.ContourEnds))
                return;

            List<ushort> indices = tessa.TessIndexList;
            List<TessVertex2d> extras = tessa.TempVertexList;
            Point2[] oldVerts = _glyph.Vertices();
            int oldVertLen = oldVerts.Length;
            var vertices = new List<Point2>();

            foreach (var i in indices) {
                if (i >= oldVertLen) {
                    TessVertex2d extra = extras[i - oldVertLen];
                    var newPoint = new Point2( (float)extra.x, (float)extra.y);
                    vertices.Add(newPoint);
                    Add(newPoint.ToPoint3());
                }
                else
                    vertices.Add(oldVerts[i]);
            }

            for (int i = 0; i < vertices.Count; i += 3) {
                var v1 = vertices[i];
                var v2 = vertices[i + 1];
                var v3 = vertices[i + 2];
                var frontTri = new Triangle2 {
                    P1 = v1,
                    P2 = v2,
                    P3 = v3
                }.Front();

                _tris.Add(frontTri.ToTriangle3());
            }
        }

        public VertexCache(RawGlyph glyph, Type containerType = Type.List) {
            _containType = containerType;
            _vert_d = new VCacheDict();
            _vert_l = new VCacheList();
            _glyph = glyph;
            _idx = 0;
            _tris = new List<Triangle3>();

            _GatherMainOutline();
            _GatherFrontTessOutline();
        }

        public Triangle3[] Triangles {
            get => _tris.ToArray();
        }

        public Type ContainerType {
            get => _containType;
            set {
                _containType = value;
                if (_containType == Type.Dict)
                    _Migrate(_vert_l, _vert_d);
                else
                    _Migrate(_vert_d, _vert_l);
            }
        }

        public void Add(Point3 vertex) {
            if (_containType == Type.Dict)
                _vert_d[vertex] = ++_idx;
            else
                _vert_l.Add((vertex, ++_idx));
        }

        public bool Contains(Point3 vertex) {
            if (_containType == Type.Dict)
                return _vert_d.ContainsKey(vertex);
            try { return IndexOf(vertex) >= 0; }
            catch (VertexNotFoundException) { return false; }
        }

        public int IndexOf(Point3 vertex) {
            if (_containType == Type.Dict) {
                try { return _vert_d[vertex]; }
                catch (KeyNotFoundException) {
                    throw new VertexNotFoundException(vertex);
                }
            }

            foreach (var e in _vert_l)
                if (vertex.EqualsEps(e.Point))
                    return e.Index;
            throw new VertexNotFoundException(vertex);
        }

        public IEnumerable<Point3> Vertices {
            get {
                if (_containType == Type.Dict)
                    foreach (var entry in _vert_d.OrderBy(e => e.Value))
                        yield return entry.Key;
                else
                    foreach (var entry in _vert_l)
                        yield return entry.Point;
            }
        }
    }
}
