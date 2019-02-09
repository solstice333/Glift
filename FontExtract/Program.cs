using System;
using System.IO;
using Typography.OpenFont;

namespace FontTest {
    class MainClass {
        public static void Main(string[] args) {
            var ttfPath = Path.GetFullPath(
               Path.Combine(Directory.GetCurrentDirectory(),
                  "..", "..", "Resources", "Typeface.ttf"));

            using (var ttf = File.OpenRead(ttfPath)) {
                var fr = new OpenFontReader();
                Typeface tf = fr.Read(ttf);
            }
        }
    }
}
