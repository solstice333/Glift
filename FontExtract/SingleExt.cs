using System;

namespace FontExtract {
    public static class SingleExt {
        public static float Epsilon = 0.001f;

        public static bool EqualsEps(this float a, float b) {
            return a > b - Epsilon && a < b + Epsilon;
        }
    }
}
