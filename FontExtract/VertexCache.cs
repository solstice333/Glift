using System.Linq;
using System.Collections.Generic;
using Tesselate;

using Point2 = System.Numerics.Vector2;

namespace FontExtract {
    using VCacheList = List<(Point2 Point, int Index)>;
    using VCacheDict = Dictionary<Point2, int>;

    public static class Point2Ext {
        public static float Epsilon = 0.001f;

        public static bool EqualsEps(this Point2 p1, Point2 p2) {
            return p1.X > p2.X - Epsilon && p1.X < p2.X + Epsilon &&
                p1.Y > p2.Y - Epsilon && p1.Y < p2.Y + Epsilon; 
        }
    }

    public class VertexNotFoundException : KeyNotFoundException {
        public VertexNotFoundException(Point2 point) :
            base($"vertex {point} not found") { }
    }

    public class VertexCache {
        private Type _containType;
        private Dictionary<Point2, int> _vert_d;
        private List<(Point2 Point, int Index)> _vert_l;
        private RawGlyph _glyph;
        private int _idx;
        private List<Triangle> _tris;

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
                Add(p);
        }

        private void _GatherTessOutline() {
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
                    var newPoint = new Point2((float)extra.x, (float)extra.y);
                    vertices.Add(newPoint);
                    Add(newPoint);
                }
                else
                    vertices.Add(oldVerts[i]);
            }

            for (int i = 0; i < vertices.Count; i += 3) {
                _tris.Add(new Triangle {
                    P1 = vertices[i],
                    P2 = vertices[i + 1],
                    P3 = vertices[i + 2]
                });
            }
        }

        public VertexCache(RawGlyph glyph, Type containerType = Type.List) {
            _containType = containerType;
            _vert_d = new VCacheDict();
            _vert_l = new VCacheList();
            _glyph = glyph;
            _idx = 0;
            _tris = new List<Triangle>();

            _GatherMainOutline();
            _GatherTessOutline();
        }

        public Triangle[] FrontTriangles {
            get => _tris.Select(tri => tri.Front()).ToArray();
        }

        public Triangle[] BackTriangles {
            get => _tris.Select(tri => tri.Back()).ToArray();
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

        public void Add(Point2 vertex) {
            if (_containType == Type.Dict)
                _vert_d[vertex] = ++_idx;
            else
                _vert_l.Add((vertex, ++_idx));
        }

        public bool Contains(Point2 vertex) {
            if (_containType == Type.Dict)
                return _vert_d.ContainsKey(vertex);
            try { return IndexOf(vertex) >= 0; }
            catch (VertexNotFoundException) { return false; }
        }

        public int IndexOf(Point2 vertex) {
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

        public IEnumerable<Point2> Vertices {
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
