using System;
using Point3 = System.Numerics.Vector3;

namespace FontExtract {
    public class Triangle3 {
        public Point3 P1 { get; set; }
        public Point3 P2 { get; set; }
        public Point3 P3 { get; set; }

        public Triangle3() { }
        public Triangle3(Point3 p1, Point3 p2, Point3 p3) {
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        public override string ToString() {
            return $"({P1}, {P2}, {P3})";
        }
    }
}
