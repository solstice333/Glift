using System;
using System.Numerics;
using Point2 = System.Numerics.Vector2;

namespace FontExtract {
    public class Triangle {
        public Point2 P1 { get; set; }
        public Point2 P2 { get; set; }
        public Point2 P3 { get; set; }

        private Triangle _Face(bool front) {
            var p1 = new Vector3(P1.X, P1.Y, 0);
            var p2 = new Vector3(P2.X, P2.Y, 0);
            var p3 = new Vector3(P3.X, P3.Y, 0);
            float z = Vector3.Cross(p2 - p1, p3 - p2).Z;

            return (front ? z > 0 : z <= 0) ? this : new Triangle {
                P1 = new Point2(p3.X, p3.Y),
                P2 = new Point2(p2.X, p2.Y),
                P3 = new Point2(p1.X, p1.Y)
            };
        }

        public Triangle() { }

        public Triangle(Point2 P1, Point2 P2, Point2 P3) {
            this.P1 = P1;
            this.P2 = P2;
            this.P3 = P3;
        }

        public Triangle Front() => _Face(true);
        public Triangle Back() => _Face(false);
    }
}
