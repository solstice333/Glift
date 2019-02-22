using System;
using System.Numerics;
using Point3 = System.Numerics.Vector3;

namespace FontExtract {
    public struct Arm {
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

        public Point3Pair UpperSegment { get; set; }
        public Point3Pair LowerSegment { get; set; }

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

        public Arm(Point3Pair upperSegment, Point3Pair lowerSegment) {
            UpperSegment = upperSegment;
            LowerSegment = lowerSegment;
        }

        public override string ToString() {
            return $"({UpperSegment} | {LowerSegment})";
        }
    }
}
