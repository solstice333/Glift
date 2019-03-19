using System;
namespace Glift {
    public static class DoubleExt {
        public static double ToDegrees(this double rad) {
            return rad * 180 / Math.PI;
        }

        public static double ToRadians(this double deg) {
            return deg * Math.PI / 180;
        }
    }
}
