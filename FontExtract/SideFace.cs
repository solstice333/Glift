using System;
using System.Numerics;

namespace FontExtract {
    public class SideFace {
        public Point3Pair PP1 { get; set; }
        public Point3Pair PP2 { get; set; }

        public SideFace() { }

        public SideFace(Point3Pair pp1, Point3Pair pp2) {
            PP1 = pp1;
            PP2 = pp2;
        }

        public override string ToString() {
            return $"({PP1}, {PP2})";
        }
    }
}
