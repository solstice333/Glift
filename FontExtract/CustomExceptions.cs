using System;
using System.Collections.Generic;

using Point3 = System.Numerics.Vector3;

namespace FontExtract {
    public class VertexNotFoundException : KeyNotFoundException {
        public VertexNotFoundException(Point3 point, Exception e = null) :
            base($"vertex {point} not found", e) { }
    }

    public class Non2DXYException : FormatException {
        public Non2DXYException(Arm arm, Exception e = null) :
            base($"arm {arm} leaks into 3d space", e) { }
    }
}
