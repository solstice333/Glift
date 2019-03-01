using System.Linq;
using System.Collections.Generic;

using Point2 = System.Numerics.Vector2;

namespace Glift {
    public class RawGlyph {
        private static HashSet<string> _allNames = new HashSet<string>();

        private string _name;
        private string _filename;
        private float[] _glyphPts;
        private int[] _contourEnds;

        private List<Point2> _cachedVertices;
        private int[] _cachedVertexContourEnds;

        public string Name {
            get => _name;
            set {
                _name = value ?? "";
                var fname = _name;
                while (_allNames.Contains(fname.ToLower()))
                    fname += "_";
                _filename = fname;
                _allNames.Add(_filename.ToLower());
            }
        }

        public string Filename {
            get => _filename;
        }

        public float[] GlyphPts {
            get => _glyphPts;
            set => _glyphPts = value ?? new float[] { };
        }

        public int[] ContourEnds {
            get => _contourEnds;
            set => _contourEnds = value ?? new int[] { };
        }

        public Point2[] ContourEndsAsPoint2 {
            get {
                int[] contourEnds;
                Point2[] vertices = Vertices(out contourEnds);
                return contourEnds.Select(i => vertices[i]).ToArray();
            }
        }

        public RawGlyph() {
            _name = "";
            _glyphPts = new float[] { };
            _contourEnds = new int[] { };
        }

        public RawGlyph(string name, float[] glyphPts, int[] contourEnds) {
            Name = name ?? "";
            GlyphPts = glyphPts ?? new float[] { };
            ContourEnds = contourEnds ?? new int[] { };
            _cachedVertices = null;
            _cachedVertexContourEnds = null;
        }

        public Point2[] Vertices(out int[] contourEnds) {
            if (_cachedVertexContourEnds == null)
                _cachedVertexContourEnds = 
                    ContourEnds.Select(v => v / 2).ToArray();
            contourEnds = _cachedVertexContourEnds;
            return Vertices();
        }

        public Point2[] Vertices() {
            if (_cachedVertices == null) {
                _cachedVertices = new List<Point2>();
                for (int i = 0; i < GlyphPts.Length; i += 2)
                    _cachedVertices.Add(
                        new Point2(GlyphPts[i], GlyphPts[i + 1]));
            }
            return _cachedVertices.ToArray();
        }
    }
}
