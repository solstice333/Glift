using System;
using System.IO;
using System.Collections.Generic;

using Typography.OpenFont;
using Typography.Contours;

using DrawingGL;
using DrawingGL.Text;

using ArgParse;
using Util;

namespace FontExtract {
    class MainClass {
        public static void Main(string[] args) {
            Args.Parse(args);
            bool allGlyphs = Args.chars.Count == 0;
        }
    }
}
