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

        private Matrix4x4 _transl;

        private delegate void _AddTri(Triangle3 t);
        private delegate void _AddVertex(Point3 p);

        public enum Face {
            Front, Side, All
        }

        private IEnumerable<Point3> _NonTranslatedVertices(
            Face face = Face.All) {
            if (face == Face.All)
                return _vertexStore.Vertices;
            else if (face == Face.Front)
                return _frontVertexStore.Vertices;
            else
                return _sideVertexStore.Vertices;
        }

        private void _PopulateSideFaces() {
            Point3Pair[] pps = _NonTranslatedVertices(Face.Front).Zip(
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

        private void _BindArms(Arm[] arms) {
            foreach (Arm arm1 in arms) {
                var arm1Squares = new Square[] {
                    arm1.UpperPrismoid.Square1,
                    arm1.UpperPrismoid.Square2,
                    arm1.LowerPrismoid.Square1,
                    arm1.LowerPrismoid.Square2
                };

                var arm1Points = new Point3[] {
                    arm1.UpperSegment.P1,
                    arm1.UpperSegment.P2,
                    arm1.LowerSegment.P1,
                    arm1.LowerSegment.P2
                };

                foreach (Arm arm2 in arms) {
                    var arm2Squares = new Square[] {
                        arm2.UpperPrismoid.Square1,
                        arm2.UpperPrismoid.Square2,
                        arm2.LowerPrismoid.Square1,
                        arm2.LowerPrismoid.Square2
                    };

                    var arm2Points = new Point3[] {
                        arm2.UpperSegment.P1,
                        arm2.UpperSegment.P2,
                        arm2.LowerSegment.P1,
                        arm2.LowerSegment.P2
                    };

                    for (int i = 0; i < arm1Points.Length; ++i) {
                        for (int j = 0; j < arm2Points.Length; ++j) {
                            if (arm1Points[i].EqualsEps(arm2Points[j]))
                                arm1Squares[i].BindTo(arm2Squares[j]);
                        }
                    }
                }
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
            if (_zdepth == 0)
                return;

            var transl = Matrix4x4.CreateTranslation(0, 0, _zdepth);
            var extPoints = 
                _NonTranslatedVertices(Face.Front).Select(
                    p => Vector3.Transform(p, transl)).ToArray();
            _extrudedPoints = extPoints;
            foreach (Point3 p in extPoints) {
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

        private void _InitArms() {
            _frontArms = _FoldFrontIntoArms();
            //_BindArms(_frontArms);
        }

        private void _GatherPrismoidMainOutline() {
            foreach (Arm arm in _frontArms) {
                Prismoid upperPrismoid = arm.UpperPrismoid;
                Prismoid lowerPrismoid = arm.LowerPrismoid;

                //Vector2 upperVec =
                //    upperPrismoid.CenterLine.P2.ToPoint2XY() -
                //    upperPrismoid.CenterLine.P1.ToPoint2XY();

                //Vector2 lowerVec =
                //    lowerPrismoid.CenterLine.P2.ToPoint2XY() -
                //    lowerPrismoid.CenterLine.P1.ToPoint2XY();

                //Vector2 tangentUnit = Vector2.Normalize(upperVec + lowerVec);
                //Vector2 miterUnit = tangentUnit.Rotate90CW();
                //Vector2 normalUnit = Vector2.Normalize(lowerVec.Rotate90CW());
                //float tHalf = (float)_thickness / 2;
                //float miterDist = tHalf / Vector2.Dot(miterUnit, normalUnit);

                //Point3 miterLocTop = (miterUnit * miterDist).ToPoint3();
                //Point3 miterLocBot = (-miterUnit * miterDist).ToPoint3();

                //Point3 miterLocTopIn = miterLocTop;
                //Point3 miterLocTopOut = miterLocTop;
                //Point3 miterLocBotIn = miterLocBot;
                //Point3 miterLocBotOut = miterLocBot;

                //miterLocTopIn.Z = tHalf;
                //miterLocTopOut.Z = -tHalf;
                //miterLocBotIn.Z = tHalf;
                //miterLocBotOut.Z = -tHalf;

                //upperPrismoid.Square2UpLeft = miterLocTopIn;
                //upperPrismoid.Square2UpRight = miterLocTopOut;
                //upperPrismoid.Square2DownRight = miterLocBotOut;
                //upperPrismoid.Square2DownLeft = miterLocBotIn;

                //lowerPrismoid.Square1UpLeft = miterLocTopIn;
                //lowerPrismoid.Square1UpRight = miterLocTopOut;
                //lowerPrismoid.Square1DownRight = miterLocBotOut;
                //lowerPrismoid.Square1DownLeft = miterLocBotIn;

                _AddVertex addVertex = _vertexStore.Add;
                addVertex += _frontVertexStore.Add;

                foreach (Point3 p in upperPrismoid.PointsCWStartUpperLeft)
                    addVertex(p);

                //addVertex(miterLocTopIn);
                //addVertex(miterLocTopOut);
                //addVertex(miterLocBotOut);
                //addVertex(miterLocBotIn);
            }
        }

        private void _GatherPrismoidMainOutlineTess() {
            foreach (Arm arm in _frontArms) {
                Prismoid upperPrismoid = arm.UpperPrismoid;

                var tessaTop1 = new Triangle3(
                    upperPrismoid.Square1UpLeft,
                    upperPrismoid.Square1UpRight,
                    upperPrismoid.Square2UpRight);

                var tessaTop2 = new Triangle3(
                    upperPrismoid.Square1UpLeft,
                    upperPrismoid.Square2UpRight,
                    upperPrismoid.Square2UpLeft);

                var tessaRight1 = new Triangle3(
                    upperPrismoid.Square1UpRight,
                    upperPrismoid.Square2DownRight,
                    upperPrismoid.Square2UpRight);

                var tessaRight2 = new Triangle3(
                    upperPrismoid.Square1UpRight,
                    upperPrismoid.Square1DownRight,
                    upperPrismoid.Square2DownRight);

                var tessaDown1 = new Triangle3(
                    upperPrismoid.Square1DownLeft,
                    upperPrismoid.Square2DownLeft,
                    upperPrismoid.Square2DownRight);

                var tessaDown2 = new Triangle3(
                    upperPrismoid.Square1DownLeft,
                    upperPrismoid.Square2DownRight,
                    upperPrismoid.Square1DownRight);

                var tessaLeft1 = new Triangle3(
                    upperPrismoid.Square1UpLeft,
                    upperPrismoid.Square2UpLeft,
                    upperPrismoid.Square2DownLeft);

                var tessaLeft2 = new Triangle3(
                    upperPrismoid.Square1UpLeft,
                    upperPrismoid.Square2DownLeft,
                    upperPrismoid.Square1DownLeft);

                _AddTri addTri = _tris.Add;
                addTri += _frontTris.Add;

                addTri(tessaTop1);
                addTri(tessaTop2);
                addTri(tessaRight1);
                addTri(tessaRight2);
                addTri(tessaDown1);
                addTri(tessaDown2);
                addTri(tessaLeft1);
                addTri(tessaLeft2);
            }
        }

        public VertexCache(
            RawGlyph glyph, int zdepth = 0, int thickness = 0,
            float xoffset = 0, float yoffset = 0,
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

            _transl = Matrix4x4.CreateTranslation(xoffset, yoffset, 0);

            _GatherMainOutline();
            _GatherFrontTessOutline();
            _GatherExtrudePoints();
            _GatherSideTess();
            _InitArms();
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
                return _NonTranslatedVertices(Face.All).Select(
                    p => Vector3.Transform(p, _transl));
            else if (face == Face.Front)
                return _NonTranslatedVertices(Face.Front).Select(
                    p => Vector3.Transform(p, _transl));
            else
                return _NonTranslatedVertices(Face.Side).Select(
                    p => Vector3.Transform(p, _transl));
        }

        public IEnumerable<Triangle3> Triangles(Face face = Face.All) {
            if (face == Face.All)
                return _tris;
            else if (face == Face.Front)
                return _frontTris;
            else
                return _sideTris;
        }
    }
}
