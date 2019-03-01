using System;
using System.Linq;
using System.Collections.Generic;

using Point3 = System.Numerics.Vector3;

namespace Glift {
    public class Square {
        private LinkedList<Square> _squares;
        private Point3 _upLeft;
        private Point3 _upRight;
        private Point3 _downRight;
        private Point3 _downLeft;

        private void _InitSquares() {
            _squares = new LinkedList<Square>();
            _squares.AddLast(this);
        }

        public Square() {
            _upLeft = new Point3(0, 0, 0);
            _upRight = new Point3(0, 0, 0);
            _downLeft = new Point3(0, 0, 0);
            _downRight = new Point3(0, 0, 0);
            _InitSquares();
        }

        public Square(IEnumerable<Point3> points) {
            Point3[] pointsArr = points.ToArray(); ;
            _upLeft = pointsArr[0];
            _upRight = pointsArr[1];
            _downRight = pointsArr[2];
            _downLeft = pointsArr[3];
            _InitSquares();
        }

        public Point3 UpLeft {
            get => _upLeft;
            set {
                foreach (Square s in _squares)
                    s._upLeft = value;
            }
        }

        public Point3 UpRight {
            get => _upRight;
            set {
                foreach (Square s in _squares)
                    s._upRight = value;
            }
        }

        public Point3 DownRight {
            get => _downRight;
            set {
                foreach (Square s in _squares)
                    s._downRight = value;
            }
        }

        public Point3 DownLeft {
            get => _downLeft;
            set {
                foreach (Square s in _squares)
                    s._downLeft = value;
            }
        }

        public void BindTo(Square s) {
            _squares.AddLast(s);
        }

        public IEnumerable<Point3> PointsCWStartUpperLeft {
            get {
                Point3[] pts = { UpLeft, UpRight, DownRight, DownLeft };
                foreach (Point3 p in pts)
                    yield return p;
            }
        }

        public override string ToString() {
            return $"({UpLeft},{UpRight},{DownRight},{DownLeft})";
        }
    }
}
