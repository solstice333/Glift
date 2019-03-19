using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Tesselate;

using Point2 = System.Numerics.Vector2;
using Point3 = System.Numerics.Vector3;

using Util;

namespace Glift {
    public class VertexCache {
        private RawGlyph _glyph;
        private int _zdepth;
        private Point3[] _extrudedPoints;
        private List<SideFace> _sideFaces;
        private float _thickness;

        private VertexStore _vertexStore;
        private VertexStore _frontVertexStore;
        private VertexStore _frontSkeletonVertexStore;
        private VertexStore _sideVertexStore;
        private VertexStore _outlineVertexStore;

        private List<Triangle3> _tris;
        private List<Triangle3> _frontTris;
        private List<Triangle3> _sideTris;
        private List<Triangle3> _outlineTris;

        private Arm[] _frontArms;

        private Prismoid[] _sideOutline;

        private Matrix4x4 _transl;
        private Matrix4x4 _translZ;
        private Matrix4x4 _xyzScale;

        private delegate void _TriAdder(Triangle3 t);
        private delegate void _VertexAdder(Point3 p);
        private delegate IEnumerable<Point3> _VertGetter(Face f);
        private delegate bool _ContainmentChecker(Point3 p);
        private delegate int _IndexOfGetter(Point3 p);

        public enum Face {
            Front, Side, Outline, All
        }

        private IEnumerable<VertexStore> _VertexStores {
            get {
                return new VertexStore[] { 
                    _vertexStore, 
                    _frontVertexStore, 
                    _frontSkeletonVertexStore, 
                    _sideVertexStore, 
                    _outlineVertexStore 
                };
            }
        }

        private Dictionary<Face, VertexStore> _FaceToVertexStore;
        private Dictionary<Face, _ContainmentChecker> _FaceToContainmentChecker;
        private Dictionary<Face, _IndexOfGetter> _FaceToIndexOfGetter;
        private Dictionary<Face, _VertGetter> _FaceToVertGetter;
        private Dictionary<Face, List<Triangle3>> _FaceToTriangle3List;

        private IEnumerable<Point3> _NonTranslatedVertices(
            Face face = Face.All) {
            return _FaceToVertexStore[face].Vertices;
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
            Point3 point0, out Point3 point1, out Point3 point2) {
            int offset = 0;
            int offsetToCurrIdx = 1;

            try {
                do {
                    if (currIdx + offsetToCurrIdx > contourEndIdx)
                        throw new IndexOutOfRangeException();
                    point1 = verts[currIdx + offsetToCurrIdx++];
                } while (point0.EqualsEps(point1));
            }
            catch (IndexOutOfRangeException) {
                do {
                    point1 = verts[startIdx + offset++];
                } while (point0.EqualsEps(point1));

                do {
                    point2 = verts[startIdx + offset++];
                } while (point1.EqualsEps(point2));

                return;
            }

            try {
                do {
                    if (currIdx + offsetToCurrIdx > contourEndIdx)
                        throw new IndexOutOfRangeException();
                    point2 = verts[currIdx + offsetToCurrIdx++];
                } while (point1.EqualsEps(point2));
            }
            catch (IndexOutOfRangeException) {
                do {
                    point2 = verts[startIdx + offset++];
                } while (point1.EqualsEps(point2));
            }
        }

        private Arm[] _FoldFrontIntoArms() {
            var frontArms = new List<Arm>();
            Point3[] frontVerts = _frontVertexStore.Vertices.ToArray();

            frontVerts.Select(v => v.ToPoint2XY()).ForEachVertex(_glyph,
                (i, start, end) => {
                    Point3 p0 = frontVerts[i];
                    Point3 p1, p2;
                    _WrapPointsAroundAtContourEnd(
                        frontVerts, i, start, end, p0, out p1, out p2);

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

            var extPoints = 
                _NonTranslatedVertices(Face.Front).Select(
                    p => Vector3.Transform(p, _translZ)).ToArray();
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
            _BindArms(_frontArms);
        }

        private void _GatherPrismoidMainOutline() {
            foreach (Arm arm in _frontArms) {
                Prismoid upperPrismoid = arm.UpperPrismoid;
                Prismoid lowerPrismoid = arm.LowerPrismoid;

                Vector2 upperUnit =
                    Vector2.Normalize(
                        upperPrismoid.CenterLine.P2.ToPoint2XY() -
                        upperPrismoid.CenterLine.P1.ToPoint2XY());

                Vector2 lowerUnit =
                    Vector2.Normalize(
                        lowerPrismoid.CenterLine.P2.ToPoint2XY() -
                        lowerPrismoid.CenterLine.P1.ToPoint2XY());

                Vector2 tangentUnit = Vector2.Normalize(upperUnit + lowerUnit);
                Vector2 miterUnit = Vector2.Normalize(tangentUnit.Rotate90CW());
                Vector2 normalUnit = Vector2.Normalize(upperUnit.Rotate90CW());
                float tHalf = (float)_thickness / 2;
                float miterDist = tHalf / Vector2.Dot(miterUnit, normalUnit);

                Point3 miterLocTop = (miterUnit * miterDist).ToPoint3();
                Point3 miterLocBot = (-miterUnit * miterDist).ToPoint3();

                Point3 miterLocTopIn = miterLocTop;
                Point3 miterLocTopOut = miterLocTop;
                Point3 miterLocBotIn = miterLocBot;
                Point3 miterLocBotOut = miterLocBot;

                miterLocTopIn.Z = tHalf;
                miterLocTopOut.Z = -tHalf;
                miterLocBotIn.Z = tHalf;
                miterLocBotOut.Z = -tHalf;

                upperPrismoid.Square2UpLeft = 
                    upperPrismoid.CenterLine.P2 + miterLocTopIn;
                upperPrismoid.Square2UpRight = 
                    upperPrismoid.CenterLine.P2 + miterLocTopOut;
                upperPrismoid.Square2DownRight = 
                    upperPrismoid.CenterLine.P2 + miterLocBotOut;
                upperPrismoid.Square2DownLeft = 
                    upperPrismoid.CenterLine.P2 + miterLocBotIn;

                _VertexAdder addVertex = _vertexStore.Add;
                addVertex += _outlineVertexStore.Add;

                addVertex(upperPrismoid.Square2UpLeft);
                addVertex(upperPrismoid.Square2UpRight);
                addVertex(upperPrismoid.Square2DownRight);
                addVertex(upperPrismoid.Square2DownLeft);
            }
        }

        private void _GatherPrismoidMainOutlineTess() {
            foreach (Arm arm in _frontArms) {
                Prismoid upperPrismoid = arm.UpperPrismoid;
                Prismoid lowerPrismoid = arm.LowerPrismoid;

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

                _TriAdder addTri = _tris.Add;
                addTri += _outlineTris.Add;

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

        private void _InitSideOutlinePrismoids() {
            if (_zdepth == 0)
                return;

            var prismoids = new List<Prismoid>();
            foreach (Arm a in _frontArms) {
                if (a.Angle.ToDegrees() >= Args.angle)
                    continue;
                Point3 p = a.UpperSegment.P2;
                var pp = new Point3Pair(p, Vector3.Transform(p, _translZ));
                var moid = new Prismoid(pp, _thickness);
                prismoids.Add(moid);
            }
            _sideOutline = prismoids.ToArray();
        }

        private void _GatherPrismoidSideOutline() {
            if (_zdepth == 0)
                return;

            foreach (Prismoid moid in _sideOutline) {
                _VertexAdder addVert = _vertexStore.Add;
                addVert += _outlineVertexStore.Add;
                foreach (Point3 p in moid.PointsCWStartUpperLeft)
                    addVert(p);
            }
        }

        private void _GatherPrismoidSideOutlineTess() {
            foreach (Prismoid moid in _sideOutline) {
                var tessaTop1 = new Triangle3(
                    moid.Square1UpLeft,
                    moid.Square1UpRight,
                    moid.Square2UpRight);

                var tessaTop2 = new Triangle3(
                    moid.Square1UpLeft,
                    moid.Square2UpRight,
                    moid.Square2UpLeft);

                var tessaRight1 = new Triangle3(
                    moid.Square1UpRight,
                    moid.Square2DownRight,
                    moid.Square2UpRight);

                var tessaRight2 = new Triangle3(
                    moid.Square1UpRight,
                    moid.Square1DownRight,
                    moid.Square2DownRight);

                var tessaDown1 = new Triangle3(
                    moid.Square1DownLeft,
                    moid.Square2DownLeft,
                    moid.Square2DownRight);

                var tessaDown2 = new Triangle3(
                    moid.Square1DownLeft,
                    moid.Square2DownRight,
                    moid.Square1DownRight);

                var tessaLeft1 = new Triangle3(
                    moid.Square1UpLeft,
                    moid.Square2UpLeft,
                    moid.Square2DownLeft);

                var tessaLeft2 = new Triangle3(
                    moid.Square1UpLeft,
                    moid.Square2DownLeft,
                    moid.Square1DownLeft);

                _TriAdder addTri = _tris.Add;
                addTri += _outlineTris.Add;

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
            RawGlyph glyph, int zdepth = 0, float thickness = 0,
            float xoffset = 0, float yoffset = 0, float sizeMult = 1,
            VertexStore.Type containerType = VertexStore.Type.List) {
            _glyph = glyph;
            _zdepth = zdepth;
            _extrudedPoints = null;
            _sideFaces = new List<SideFace>();
            _thickness = thickness;

            _vertexStore = new VertexStore(containerType);
            _frontVertexStore = new VertexStore(containerType);
            _sideVertexStore = new VertexStore(containerType);
            _outlineVertexStore = new VertexStore(containerType);
            _frontSkeletonVertexStore = new VertexStore(containerType);

            _tris = new List<Triangle3>();
            _frontTris = new List<Triangle3>();
            _sideTris = new List<Triangle3>();
            _outlineTris = new List<Triangle3>();

            _frontArms = null;
            _sideOutline = null;

            _transl = Matrix4x4.CreateTranslation(xoffset, yoffset, 0);
            _translZ = Matrix4x4.CreateTranslation(0, 0, _zdepth);
            _xyzScale = Matrix4x4.CreateScale(sizeMult, sizeMult, sizeMult);

            _FaceToVertexStore = new Dictionary<Face, VertexStore> {
                { Face.Front, _frontVertexStore },
                { Face.Side, _sideVertexStore },
                { Face.Outline, _outlineVertexStore },
                { Face.All, _vertexStore }
            };

            _FaceToContainmentChecker = 
                new Dictionary<Face, _ContainmentChecker> {
                { Face.Front, _frontVertexStore.Contains },
                { Face.Side, _sideVertexStore.Contains },
                { Face.Outline, _outlineVertexStore.Contains },
                { Face.All, _vertexStore.Contains }
            };

            _FaceToIndexOfGetter = new Dictionary<Face, _IndexOfGetter> {
                { Face.Front, _frontVertexStore.IndexOf },
                { Face.Side, _sideVertexStore.IndexOf },
                { Face.Outline, _outlineVertexStore.IndexOf },
                { Face.All, _vertexStore.IndexOf }
            };

            _FaceToVertGetter = new Dictionary<Face, _VertGetter> {
                { Face.Front, _NonTranslatedVertices },
                { Face.Side, _NonTranslatedVertices },
                { Face.Outline, _NonTranslatedVertices },
                { Face.All, _NonTranslatedVertices }
            };

            _FaceToTriangle3List = new Dictionary<Face, List<Triangle3>> {
                { Face.Front, _frontTris },
                { Face.Side, _sideTris },
                { Face.Outline, _outlineTris },
                { Face.All, _tris }
            };

            _GatherMainOutline();
            _GatherFrontTessOutline();
            _GatherExtrudePoints();
            _GatherSideTess();
            _InitArms();
            _GatherPrismoidMainOutline();
            _GatherPrismoidMainOutlineTess();
            _InitSideOutlinePrismoids();
            _GatherPrismoidSideOutline();
            _GatherPrismoidSideOutlineTess();
        }

        public VertexStore.Type ContainerType {
            get => _vertexStore.ContainerType;
            set {
                foreach (VertexStore store in _VertexStores)
                    store.ContainerType = value;
            }
        }

        public bool Contains(Point3 vertex, Face face = Face.All) {
            return _FaceToContainmentChecker[face](vertex);
        }

        public int IndexOf(Point3 vertex, Face face = Face.All) {
            return _FaceToIndexOfGetter[face](vertex);
        }

        public IEnumerable<Point3> Vertices(Face face = Face.All) {
            return _FaceToVertGetter[face](face).Select(
                p => Vector3.Transform(p, _transl)).Select(
                p => Vector3.Transform(p, _xyzScale));
        }

        public IEnumerable<Triangle3> Triangles(Face face = Face.All) {
            return _FaceToTriangle3List[face];
        }
    }
}
