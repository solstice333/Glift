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

        private Arm[] _frontArms;

        private delegate void _AddTri(Triangle3 t);
        private delegate void _AddVertex(Point3 p);

        public enum Face {
            Front, Side, All
        }

        private void _PopulateSideFaces() {
            Point3Pair[] pps = Vertices(Face.Front).Zip(
                _extrudedPoints,
                (p1, p2) => new Point3Pair(p1, p2)).ToArray();

            pps.Select(pp => pp.P1.ToPoint2XY()).ForEachVertex(_glyph,
                (i, start, end) => {
                    if (pps[i] != pps[start])
                        _sideFaces.Add(new SideFace(pps[i], pps[start]));
                },
                (i, start, end) => {
                    if (pps[i] != pps[i + 1])
                        _sideFaces.Add(new SideFace(pps[i], pps[i + 1]));
                }
            );
        }

        private void _WrapPointsAroundAtContourEnd(
            Point3[] verts, int currIdx, int startIdx, int contourEndIdx,
            out Point3 point1, out Point3 point2) {

            try {
                if (currIdx + 1 > contourEndIdx)
                    throw new IndexOutOfRangeException();
                point1 = verts[currIdx + 1];
            }
            catch (IndexOutOfRangeException) { point1 = verts[startIdx]; }

            try {
                if (currIdx + 2 > contourEndIdx)
                    throw new IndexOutOfRangeException();
                point2 = verts[currIdx + 2];
            }
            catch (IndexOutOfRangeException) { point2 = verts[startIdx + 1]; }
        }

        private Arm[] _FoldFrontIntoArms() {
            var frontArms = new List<Arm>();
            Point3[] frontVerts = _frontVertexStore.Vertices.ToArray();

            frontVerts.Select(v => v.ToPoint2XY()).ForEachVertex(_glyph,
                (i, start, end) => {
                    Point3 p0 = frontVerts[i];
                    Point3 p1, p2;
                    _WrapPointsAroundAtContourEnd(
                        frontVerts, i, start, end, out p1, out p2);

                    if (!p0.EqualsEps(p1) && !p1.EqualsEps(p2))
                        frontArms.Add(new Arm(
                            new Point3Pair(p0, p1),
                            new Point3Pair(p1, p2),
                            _thickness
                        ));
                });

            return frontArms.ToArray();
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
            if (_zdepth == 0)
                return;

            var transl = Matrix4x4.CreateTranslation(0, 0, _zdepth);
            var extPoints = 
                Vertices(Face.Front).Select(
                    p => Vector3.Transform(p, transl)).ToArray();
            _extrudedPoints = extPoints;
            foreach (var p in extPoints) {
                _vertexStore.Add(p);
                _sideVertexStore.Add(p);
            }
        }

        private void _GatherSideTess() {
            if (_zdepth == 0)
                return;

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

        private void _GatherPrismoidMainOutline() {
            _frontArms = _FoldFrontIntoArms();

            foreach (var arm in _frontArms) {
                Prismoid upperPrismoid = arm.UpperPrismoid;
                Prismoid lowerPrismoid = arm.LowerPrismoid;
                foreach (Point3 p in upperPrismoid.PointsCWStartUpperLeft) {
                    _vertexStore.Add(p);
                    _frontVertexStore.Add(p);
                }
                foreach (Point3 p in lowerPrismoid.PointsCWStartUpperLeft) {
                    _vertexStore.Add(p);
                    _frontVertexStore.Add(p);
                }
            }
        }

        private void _GatherPrismoidMainOutlineTess() {
            foreach (var arm in _frontArms) {
                Prismoid upperPrismoid = arm.UpperPrismoid;
                Prismoid lowerPrismoid = arm.LowerPrismoid;

                var sqUpper1 = 
                    new Square(upperPrismoid.PointsCWStartUpperLeft);
                var sqUpper2 = 
                    new Square(upperPrismoid.PointsCWStartUpperLeft.Skip(4));
                var sqLower1 = 
                    new Square(lowerPrismoid.PointsCWStartUpperLeft);
                var sqLower2 = 
                    new Square(lowerPrismoid.PointsCWStartUpperLeft.Skip(4));

                var tessaTop1 = new Triangle3(sqUpper1.UpLeft, 
                    sqUpper1.UpRight, sqUpper2.UpRight);
                var tessaTop2 = new Triangle3(sqUpper1.UpLeft,
                    sqUpper2.UpRight, sqUpper2.UpLeft);

                var tessaRight1 = new Triangle3(sqUpper1.UpRight,
                    sqUpper2.DownRight, sqUpper2.UpRight);
                var tessaRight2 = new Triangle3(sqUpper1.UpRight,
                    sqUpper1.DownRight, sqUpper2.DownRight);

                var tessaBottom1 = new Triangle3(sqUpper1.DownLeft,
                    sqUpper2.DownLeft, sqUpper2.DownRight);
                var tessaBottom2 = new Triangle3(sqUpper1.DownLeft,
                    sqUpper2.DownRight, sqUpper1.DownRight);

                var tessaLeft1 = new Triangle3(sqUpper1.UpLeft,
                    sqUpper2.UpLeft, sqUpper2.DownLeft);
                var tessaLeft2 = new Triangle3(sqUpper1.UpLeft,
                    sqUpper2.DownLeft, sqUpper1.DownLeft);

                _AddTri addTri = _tris.Add;
                addTri += _frontTris.Add;

                addTri(tessaTop1);
                addTri(tessaTop2);
                addTri(tessaRight1);
                addTri(tessaRight2);
                addTri(tessaBottom1);
                addTri(tessaBottom2);
                addTri(tessaLeft1);
                addTri(tessaLeft2);
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

            _frontArms = null;

            _GatherMainOutline();
            _GatherFrontTessOutline();
            _GatherExtrudePoints();
            _GatherSideTess();
            _GatherPrismoidMainOutline();
            _GatherPrismoidMainOutlineTess();
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
