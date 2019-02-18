using System;
using System.Numerics;
using Point2 = System.Numerics.Vector2;
using Point3 = System.Numerics.Vector3;

namespace FontExtract {
    public class Triangle2 {
        public Point2 P1 { get; set; }
        public Point2 P2 { get; set; }
        public Point2 P3 { get; set; }

        private Triangle2 _Face(bool front) {
            var p1 = new Vector3(P1.X, P1.Y, 0);
            var p2 = new Vector3(P2.X, P2.Y, 0);
            var p3 = new Vector3(P3.X, P3.Y, 0);
            float z = Vector3.Cross(p2 - p1, p3 - p2).Z;

            return (front ? z > 0 : z <= 0) ? this : new Triangle2 {
                P1 = new Point2(p3.X, p3.Y),
                P2 = new Point2(p2.X, p2.Y),
                P3 = new Point2(p1.X, p1.Y)
            };
        }

        public Triangle2() { }

        public Triangle2(Point2 P1, Point2 P2, Point2 P3) {
            this.P1 = P1;
            this.P2 = P2;
            this.P3 = P3;
        }

        public Triangle2 Front() => _Face(true);
        public Triangle2 Back() => _Face(false);

        public Triangle3 ToTriangle3(float z = 0) {
            return new Triangle3 {
                P1 = new Point3(P1.X, P1.Y, z),
                P2 = new Point3(P2.X, P2.Y, z),
                P3 = new Point3(P3.X, P3.Y, z),
            };
        }

        public Triangle3 ToTriangle3(float z1, float z2, float z3) {
            return new Triangle3 {
                P1 = new Point3(P1.X, P1.Y, z1),
                P2 = new Point3(P2.X, P2.Y, z2),
                P3 = new Point3(P3.X, P3.Y, z3),
            };
        }

        public override string ToString() {
            return $"({P1}, {P2}, {P3})";
        }
    }
}
