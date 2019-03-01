using System;
using Point3 = System.Numerics.Vector3;

namespace Glift {
    public struct Point3Pair {
        public Point3 P1 { get; set; }
        public Point3 P2 { get; set; }

        public Point3Pair(Point3 p1, Point3 p2) {
            P1 = p1;
            P2 = p2;
        }

        public override string ToString() {
            return $"({P1}, {P2})";
        }

        public override bool Equals(object obj) {
            return this == (Point3Pair)obj;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public static bool operator ==(Point3Pair pp1, Point3Pair pp2) {
            return pp1.P1.EqualsEps(pp2.P1) && pp1.P2.EqualsEps(pp2.P2);
        }

        public static bool operator !=(Point3Pair pp1, Point3Pair pp2) {
            return !(pp1 == pp2);
        }
    }
}
