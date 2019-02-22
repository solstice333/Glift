using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Tesselate;

using Point2 = System.Numerics.Vector2;
using Point3 = System.Numerics.Vector3;

using Util;

namespace FontExtract {
    public class VertexCache {
        private RawGlyph _glyph;
        private int _zdepth;
        private Point3[] _extrudedPoints;
        private List<SideFace> _sideFaces;
        private int _thickness;

        private VertexStore _vertexStore;
        private VertexStore _frontVertexStore;
        private VertexStore _frontSkeletonVertexStore;
        private VertexStore _sideVertexStore;

        private List<Triangle3> _tris;
        private List<Triangle3> _frontTris;
        private List<Triangle3> _sideTris;

        private delegate void _VertexHandler(int currIdx, int contourStartIdx);

        public enum Face {
            Front, Side, All
        }

        private void _ForEachFrontVertex(
            _VertexHandler onContourEnd, _VertexHandler onNonContourEnd) {
            int[] contourEnds;
            _glyph.Vertices(out contourEnds);

            int start = 0;
            int i = 0;
            foreach (int contourEnd in contourEnds) {
                for (i = start; i <= contourEnd; ++i) {
                    if (i == contourEnd)
                        onContourEnd?.Invoke(i, start);
                    else
                        onNonContourEnd?.Invoke(i, start);
                }
                start = i;
            }
        }

        private void _GatherMainOutline() {
            foreach (var p in _glyph.Vertices()) {
                var vertex = new Point3(p.X, p.Y, 0);
                _vertexStore.Add(vertex);
                _frontVertexStore.Add(vertex);
                _sideVertexStore.Add(vertex);
                _frontSkeletonVertexStore.Add(vertex);
            }
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
                    _vertexStore.Add(newPoint.ToPoint3());
                    _frontVertexStore.Add(newPoint.ToPoint3());
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
                _frontTris.Add(frontTri.ToTriangle3());
            }
        }

        private void _GatherExtrudePoints() {
            var transl = Matrix4x4.CreateTranslation(0, 0, _zdepth);
            var extPoints = 
                Vertices(Face.Front).Select(p => Vector3.Transform(p, transl)).ToArray();
            _extrudedPoints = extPoints;
            foreach (var p in extPoints) {
                _vertexStore.Add(p);
                _sideVertexStore.Add(p);
            }
        }

        private void _PopulateSideFaces() {
            Point3Pair[] pps = Vertices(Face.Front).Zip(
                _extrudedPoints,
                (p1, p2) => new Point3Pair(p1, p2)).ToArray();

            _ForEachFrontVertex(
                (i, start) => {
                    if (pps[i] != pps[start])
                        _sideFaces.Add(new SideFace(pps[i], pps[start]));
                },
                (i, start) => {
                    if (pps[i] != pps[i + 1])
                        _sideFaces.Add(new SideFace(pps[i], pps[i + 1]));
                }
            );
        }

        private void _GatherSideTess() {
            _PopulateSideFaces();

            foreach (var sideFace in _sideFaces) {
                Point3 firstFrontCorner = sideFace.PP1.P1;
                Point3 oppositeBackCorner = sideFace.PP2.P2;
                Point3 firstBackCorner = sideFace.PP1.P2;
                var tri1 = new Triangle3(
                    firstFrontCorner, oppositeBackCorner, firstBackCorner);
                _tris.Add(tri1);
                _sideTris.Add(tri1);

                Point3 oppositeFrontCorner = sideFace.PP2.P1;
                var tri2 = new Triangle3(
                    firstFrontCorner, oppositeFrontCorner, oppositeBackCorner);
                _tris.Add(tri2);
                _sideTris.Add(tri2);
            }
        }

        private Arm[] _FoldFrontIntoArms() {
            var frontArms = new List<Arm>();
            Point3[] frontVerts = _frontVertexStore.Vertices.ToArray();

            int i;
            for (i = 0; i < frontVerts.Length; ++i) {
                try {
                    frontArms.Add(new Arm(
                        new Point3Pair(frontVerts[i], frontVerts[i + 1]),
                        new Point3Pair(frontVerts[i + 1], frontVerts[i + 2])
                    ));
                }
                catch (IndexOutOfRangeException ie) {
                    throw new NotImplementedException("", ie);
                    // TODO: should actually have some sort of contour iterator 
                }
            }
            return frontArms.ToArray();
        }

        private void _GatherPrismoidMainOutline() {
            Arm[] frontArms = _FoldFrontIntoArms();
            int halfDist = _thickness / 2;
            double diag = Math.Sqrt(halfDist * halfDist + halfDist * halfDist);
            foreach (var arm in frontArms) {
                Vector2 upper = arm.UpperVec2XYSafe;
                Vector2 lower = arm.LowerVec2XYSafe;
            }
        }

        public VertexCache(
            RawGlyph glyph, int zdepth = 0, int thickness = 0,
            VertexStore.Type containerType = VertexStore.Type.List) {
            _glyph = glyph;
            _zdepth = zdepth;
            _extrudedPoints = null;
            _sideFaces = new List<SideFace>();
            _thickness = thickness;

            _vertexStore = new VertexStore(containerType);
            _frontVertexStore = new VertexStore(containerType);
            _sideVertexStore = new VertexStore(containerType);
            _frontSkeletonVertexStore = new VertexStore(containerType);

            _tris = new List<Triangle3>();
            _frontTris = new List<Triangle3>();
            _sideTris = new List<Triangle3>();

            _GatherMainOutline();
            _GatherFrontTessOutline();
            _GatherExtrudePoints();
            _GatherSideTess();
            //_GatherPrismoidMainOutline();
        }

        public VertexStore.Type ContainerType {
            get => _vertexStore.ContainerType;
            set {
                _vertexStore.ContainerType = value;
                _frontVertexStore.ContainerType = value;
                _sideVertexStore.ContainerType = value;
                _frontSkeletonVertexStore.ContainerType = value;
            }
        }

        public bool Contains(Point3 vertex, Face face = Face.All) {
            if (face == Face.All)
                return _vertexStore.Contains(vertex);
            else if (face == Face.Front)
                return _frontVertexStore.Contains(vertex);
            else
                return _sideVertexStore.Contains(vertex);
        }

        public int IndexOf(Point3 vertex, Face face = Face.All) {
            if (face == Face.All)
                return _vertexStore.IndexOf(vertex);
            else if (face == Face.Front)
                return _frontVertexStore.IndexOf(vertex);
            else
                return _sideVertexStore.IndexOf(vertex);

        }

        public IEnumerable<Point3> Vertices(Face face = Face.All) {
            if (face == Face.All)
                return _vertexStore.Vertices;
            else if (face == Face.Front)
                return _frontVertexStore.Vertices;
            else
                return _sideVertexStore.Vertices;
        }

        public Triangle3[] Triangles(Face face = Face.All) {
            if (face == Face.All)
                return _tris.ToArray();
            else if (face == Face.Front)
                return _frontTris.ToArray();
            else
                return _sideTris.ToArray();
        }

    }
}
