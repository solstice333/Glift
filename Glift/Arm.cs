using System;
using System.Numerics;
using Point3 = System.Numerics.Vector3;

namespace Glift {
    public class Arm {
        private Point3Pair _upperSegment;
        private Point3Pair _lowerSegment;
        private Prismoid _upperPrismoid;
        private Prismoid _lowerPrismoid;
        private int _thickness;

        private void _AssertUpperVecIsXY2D() {
            if (!UpperSegment.P1.Z.EqualsEps(0) ||
                !UpperSegment.P2.Z.EqualsEps(0))
                throw new Non2DXYException(this);
        }

        private void _AssertLowerVecIsXY2D() {
            if (!LowerSegment.P1.Z.EqualsEps(0) ||
                !LowerSegment.P2.Z.EqualsEps(0)) {
                throw new Non2DXYException(this);
            }
        }

        private Vector2 _LowerVec2XY(bool zMustBe0 = false) {
            if (zMustBe0)
                _AssertLowerVecIsXY2D();
            return LowerSegment.P2.ToPoint2XY() - LowerSegment.P1.ToPoint2XY();
        }

        private Vector2 _UpperVec2XY(bool zMustBe0 = false) {
            if (zMustBe0)
                _AssertUpperVecIsXY2D();
            return UpperSegment.P2.ToPoint2XY() - UpperSegment.P1.ToPoint2XY();
        }

        public int Thickness {
            get => _thickness;
            set {
                _thickness = value;
                _upperPrismoid = null;
                _lowerPrismoid = null;
            }
        }

        public Point3Pair UpperSegment {
            get => _upperSegment;
            set {
                _upperSegment = value;
                _upperPrismoid = null; 
            }
        }

        public Point3Pair LowerSegment {
            get => _lowerSegment;
            set {
                _lowerSegment = value;
                _lowerPrismoid = null;
            }
        }

        public Prismoid UpperPrismoid { 
            get {
                if (_upperPrismoid == null)
                    _upperPrismoid = new Prismoid(UpperSegment, Thickness);
                return _upperPrismoid;
            }
        }

        public Prismoid LowerPrismoid {
            get {
                if (_lowerPrismoid == null)
                    _lowerPrismoid = new Prismoid(LowerSegment, Thickness);
                return _lowerPrismoid;
            }
        }

        public Vector2 LowerVec2XY {
            get => _LowerVec2XY();
        }

        public Vector2 UpperVec2XY {
            get => _UpperVec2XY();
        }

        public Vector2 LowerVec2XYSafe {
            get => _LowerVec2XY(true);
        }

        public Vector2 UpperVec2XYSafe {
            get => _UpperVec2XY(true);
        }

        public Arm(
            Point3Pair upperSegment, Point3Pair lowerSegment, int thickness) {
            _upperSegment = upperSegment;
            _lowerSegment = lowerSegment;
            _thickness = thickness;
            _upperPrismoid = null;
            _lowerPrismoid = null;
        }

        public override string ToString() {
            return $"({UpperSegment} | {LowerSegment})";
        }
    }
}
