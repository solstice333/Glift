using System;
using System.Collections.Generic;
using System.Numerics;

using Point2 = System.Numerics.Vector2;
using Point3 = System.Numerics.Vector3;

namespace FontExtract {
    public class Prismoid {
        private Point3Pair _centerline;
        private int _thickness;
        private Point3[] _vertices;

        private enum RelativePoint { P1, P2 }

        private static void _AssertSegmentIs2DXY(Point3Pair segment) {
            if (!segment.P1.IsPoint2DXY() || !segment.P2.IsPoint2DXY())
                throw new Non2DXYException(segment);
        }

        public Point3Pair CenterLine {
            get => _centerline;
            set {
                _centerline = value;
                _vertices = null;
            }
        }

        public int HalfThickness {
            get => _thickness / 2;
            set {
                _thickness = value * 2;
                _vertices = null;
            }
        }

        public int Thickness {
            get => _thickness;
            set {
                _thickness = value;
                _vertices = null;
            }
        }

        private Point3[] _SquarePoints(RelativePoint point) {
            Point2 p1 = _centerline.P1.ToPoint2XY();
            Point2 p2 = _centerline.P2.ToPoint2XY();

            Point2 relativePoint = p1;
            if (point == RelativePoint.P2)
                relativePoint = p2;

            Vector2 vec = p2 - p1;
            Vector2 unitUp = Vector2.Normalize(vec.Rotate90CW());
            Vector2 unitDown = Vector2.Normalize(vec.Rotate90CCW());

            Vector2 halfDistUp = unitUp * HalfThickness;
            Vector2 halfDistDown = unitDown * HalfThickness;

            Point3 upPt = (relativePoint + halfDistUp).ToPoint3();
            Point3 downPt = (relativePoint + halfDistDown).ToPoint3();

            Point3 upPtTowards = upPt;
            Point3 upPtAway = upPt;
            Point3 downPtTowards = downPt;
            Point3 downPtAway = downPt;

            upPtTowards.Z = HalfThickness;
            upPtAway.Z = -HalfThickness;
            downPtAway.Z = -HalfThickness;
            downPtTowards.Z = HalfThickness;

            return new Point3[] { 
                upPtTowards, upPtAway, downPtAway, downPtTowards 
            };
        }

        public IEnumerable<Point3> PointsCWStartUpperLeft {
            get {
                if (_vertices == null) {
                    List<Point3> verts = new List<Point3>();
                    foreach (Point3 p in _SquarePoints(RelativePoint.P1))
                        verts.Add(p);
                    foreach (Point3 p in _SquarePoints(RelativePoint.P2))
                        verts.Add(p);
                    _vertices = verts.ToArray();
                }
                foreach (Point3 vert in _vertices)
                    yield return vert;
            }
        }

        public Prismoid(Point3Pair segment, int thickness) {
            _AssertSegmentIs2DXY(segment);
            _thickness = thickness;
            _vertices = null; 
            _centerline = segment;
        }
    }
}
