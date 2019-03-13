using System;
using System.Collections.Generic;
using System.Numerics;

using Point2 = System.Numerics.Vector2;
using Point3 = System.Numerics.Vector3;

namespace Glift {
    public class Prismoid {
        private Point3Pair _centerline;
        private float _thickness;
        private Square _square1;
        private Square _square2;

        private enum AnchorPoint { P1, P2 }

        private Point3[] _SquarePoints2DXY(AnchorPoint point) {
            Point2 p1 = _centerline.P1.ToPoint2XY();
            Point2 p2 = _centerline.P2.ToPoint2XY();

            Point2 anchorPt = p1;
            if (point == AnchorPoint.P2)
                anchorPt = p2;

            Vector2 vec = p2 - p1;
            Vector2 unitUp = Vector2.Normalize(vec.Rotate90CW());
            Vector2 unitDown = Vector2.Normalize(vec.Rotate90CCW());

            Vector2 halfDistUp = unitUp * HalfThickness;
            Vector2 halfDistDown = unitDown * HalfThickness;

            Point3 upPt = (anchorPt + halfDistUp).ToPoint3();
            Point3 downPt = (anchorPt + halfDistDown).ToPoint3();

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

        private Point3[] _SquarePoints3D(AnchorPoint point) {
            Point3 p1 = _centerline.P1;
            Point3 p2 = _centerline.P2;

            Point3 anchorPt = p1;
            if (point == AnchorPoint.P2)
                anchorPt = p2;

            Matrix4x4 translUpLeft = 
                Matrix4x4.CreateTranslation(-HalfThickness, HalfThickness, 0);
            Matrix4x4 translUpRight =  
                Matrix4x4.CreateTranslation(HalfThickness, HalfThickness, 0);
            Matrix4x4 translDownRight = 
                Matrix4x4.CreateTranslation(HalfThickness, -HalfThickness, 0);
            Matrix4x4 translDownLeft = 
                Matrix4x4.CreateTranslation(-HalfThickness, -HalfThickness, 0);

            return new Point3[] {
                Vector3.Transform(anchorPt, translUpLeft),
                Vector3.Transform(anchorPt, translUpRight),
                Vector3.Transform(anchorPt, translDownRight),
                Vector3.Transform(anchorPt, translDownLeft)
            };
        }

        private Point3[] _SquarePoints(AnchorPoint point) {
            if (_centerline.P1.IsPoint2DXY() && _centerline.P2.IsPoint2DXY())
                return _SquarePoints2DXY(point);
            return _SquarePoints3D(point);
        }

        private void _InitSquare1() {
            _square1 = new Square(_SquarePoints(AnchorPoint.P1));
        }

        private void _InitSquare2() {
            _square2 = new Square(_SquarePoints(AnchorPoint.P2));
        }

        private void _InitVerticesIfNull() {
            if (_square1 == null)
                _InitSquare1();
            if (_square2 == null)
                _InitSquare2();
        }

        public Prismoid(Point3Pair segment, float thickness) {
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

        public float HalfThickness {
            get => _thickness / 2;
            set {
                _thickness = value * 2;
                Reset();
            }
        }

        public float Thickness {
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
