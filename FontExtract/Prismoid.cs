using System;
using System.Collections.Generic;
using System.Numerics;

using Point2 = System.Numerics.Vector2;
using Point3 = System.Numerics.Vector3;

namespace FontExtract {
    public class Prismoid {
        private Point3Pair _centerline;
        private int _thickness;
        private Square _square1;
        private Square _square2;

        private enum RelativePoint { P1, P2 }

        private static void _AssertSegmentIs2DXY(Point3Pair segment) {
            if (!segment.P1.IsPoint2DXY() || !segment.P2.IsPoint2DXY())
                throw new Non2DXYException(segment);
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

        private void _InitSquare1() {
            _square1 = new Square(_SquarePoints(RelativePoint.P1));
        }

        private void _InitSquare2() {
            _square2 = new Square(_SquarePoints(RelativePoint.P2));
        }

        private void _InitVerticesIfNull() {
            if (_square1 == null)
                _InitSquare1();
            if (_square2 == null)
                _InitSquare2();
        }

        public Prismoid(Point3Pair segment, int thickness) {
            _AssertSegmentIs2DXY(segment);
            _thickness = thickness;
            _square1 = null;
            _square2 = null;
            _centerline = segment;
        }

        public Point3Pair CenterLine {
            get => _centerline;
            set {
                _centerline = value;
                Reset();
            }
        }

        public int HalfThickness {
            get => _thickness / 2;
            set {
                _thickness = value * 2;
                Reset();
            }
        }

        public int Thickness {
            get => _thickness;
            set {
                _thickness = value;
                Reset();
            }
        }

        public IEnumerable<Point3> PointsCWStartUpperLeft {
            get {
                _InitVerticesIfNull();
                foreach (Point3 vert in _square1.PointsCWStartUpperLeft)
                    yield return vert;
                foreach (Point3 vert in _square2.PointsCWStartUpperLeft)
                    yield return vert;
            }
        }

        public Square Square1 {
            get {
                _InitVerticesIfNull();
                return _square1;
            }
            set {
                _InitVerticesIfNull();
                _square1 = value;
            }
        }

        public Square Square2 {
            get {
                _InitVerticesIfNull();
                return _square2;
            }
            set {
                _InitVerticesIfNull();
                _square2 = value;
            }
        }

        public Point3 Square1UpLeft {
            get {
                _InitVerticesIfNull();
                return _square1.UpLeft;
            }
            set {
                _InitVerticesIfNull();
                _square1.UpLeft = value;
            }
        }

        public Point3 Square1UpRight {
            get {
                _InitVerticesIfNull();
                return _square1.UpRight;
            }
            set {
                _InitVerticesIfNull();
                _square1.UpRight = value;
            }
        }

        public Point3 Square1DownRight {
            get {
                _InitVerticesIfNull();
                return _square1.DownRight;
            }
            set {
                _InitVerticesIfNull();
                _square1.DownRight = value;
            }
        }

        public Point3 Square1DownLeft {
            get {
                _InitVerticesIfNull();
                return _square1.DownLeft;
            }
            set {
                _InitVerticesIfNull();
                _square1.DownLeft = value;
            }
        }

        public Point3 Square2UpLeft {
            get {
                _InitVerticesIfNull();
                return _square2.UpLeft;
            }
            set {
                _InitVerticesIfNull();
                _square2.UpLeft = value;
            }
        }

        public Point3 Square2UpRight {
            get {
                _InitVerticesIfNull();
                return _square2.UpRight;
            }
            set {
                _InitVerticesIfNull();
                _square2.UpRight = value;
            }
        }

        public Point3 Square2DownRight {
            get {
                _InitVerticesIfNull();
                return _square2.DownRight;
            }
            set {
                _InitVerticesIfNull();
                _square2.DownRight = value;
            }
        }

        public Point3 Square2DownLeft {
            get {
                _InitVerticesIfNull();
                return _square2.DownLeft;
            }
            set {
                _InitVerticesIfNull();
                _square2.DownLeft = value;
            }
        }

        public void Reset() {
            _square1 = null;
            _square2 = null;
        }
    }
}
