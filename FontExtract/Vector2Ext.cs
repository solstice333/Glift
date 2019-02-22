using System;
using System.Numerics;

namespace FontExtract {
    public static class Vec2Ext {
        public static Vector2 Rotate90CW(this Vector2 v) {
            return new Vector2(v.Y, -v.X);
        }

        public static Vector2 Rotate90CCW(this Vector2 v) {
            return new Vector2(-v.Y, v.X);
        }
    }
}
