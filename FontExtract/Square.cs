using System;
using System.Linq;
using System.Collections.Generic;

using Point3 = System.Numerics.Vector3;

namespace FontExtract {
    public class Square {
        public Point3 UpLeft { get; set; }

        public Point3 UpRight { get; set; }

        public Point3 DownRight { get; set; }

        public Point3 DownLeft { get; set; }

        public Square() {
            UpLeft = new Point3(0, 0, 0);
            UpRight = new Point3(0, 0, 0);
            DownLeft = new Point3(0, 0, 0);
            DownRight = new Point3(0, 0, 0);
        }

        public Square(IEnumerable<Point3> points) {
            Point3[] pointsArr = points.ToArray();;
            UpLeft = pointsArr[0];
            UpRight = pointsArr[1];
            DownRight = pointsArr[2];
            DownLeft = pointsArr[3];
        }
    }
}
