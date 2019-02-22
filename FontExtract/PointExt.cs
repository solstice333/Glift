using System;
using Point2 = System.Numerics.Vector2;
using Point3 = System.Numerics.Vector3;

namespace FontExtract {
    public static class PointExt {
        public static bool EqualsEps(this Point3 p1, Point3 p2) {
            return p1.X.EqualsEps(p2.X) &&
                p1.Y.EqualsEps(p2.Y) &&
                p1.Z.EqualsEps(p2.Z);
        }

        public static bool EqualsEps(this Point2 p1, Point2 p2) {
            return p1.X.EqualsEps(p2.X) && p1.Y.EqualsEps(p2.Y);
        }

        public static Point2 ToPoint2XY(this Point3 p) {
            return new Point2(p.X, p.Y);
        }

        public static Point3 ToPoint3(this Point2 p, float z = 0) {
            return new Point3(p.X, p.Y, z);
        }
    }
}
