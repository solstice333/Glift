using System;
using System.Linq;
using System.Collections.Generic;

using Point2 = System.Numerics.Vector2;

namespace FontExtract {
    public static class VertexIter {
        public delegate void VertexHandler(int currIdx, int contourStartIdx);

        public static int[] normalizeContourEnds(
            this IEnumerable<Point2> verts, RawGlyph glyph) {

            Point2[] vertsArr = verts.ToArray();
            Point2[] contourEnds = glyph.ContourEndsAsPoint2;

            return contourEnds.Select(end =>
                Array.FindLastIndex(
                    vertsArr, pt => pt.EqualsEps(end))).ToArray();
        }

        public static void ForEachVertex(
            this IEnumerable<Point2> verts, RawGlyph glyph,
            VertexHandler onContourEnd, VertexHandler onNonContourEnd) {

            int[] contourEnds = verts.normalizeContourEnds(glyph);
            int start = 0;
            int i = 0;

            foreach (int contourEnd in contourEnds) {
                for (i = start; i <= contourEnd; ++i) {
                    if (i == contourEnd)
                        onContourEnd?.Invoke(i, start);
                    else
                        onNonContourEnd?.Invoke(i, start);
                }
                start = i;
            }
        }

        public static void ForEachVertex(
            this IEnumerable<Point2> verts, RawGlyph glyph,
            VertexHandler onVertex) {
            verts.ForEachVertex(glyph, onVertex, onVertex);
        }
    }
}
