using NUnit.Framework;
using System;
using System.IO;
using System.Diagnostics;

namespace FontExtractTest {
    static class Globals {
        public static string binDir = Path.GetFullPath(
            TestContext.CurrentContext.TestDirectory);
        public static string resources = Path.GetFullPath(Path.Combine(
            binDir, "..", "..", "Resources"));
        public static string fontExtract = Path.GetFullPath(Path.Combine(
            binDir, "..", "..", "..", 
            "FontExtract", "bin", "Debug", "FontExtract.exe"));
    }

    static class FontExtract {
        public static Process Run(string args) {
            var startInfo = new ProcessStartInfo {
                UseShellExecute = false,
                FileName = Globals.fontExtract,
                Arguments = args,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            return new Process { StartInfo = startInfo };
        }
    }

    [TestFixture()]
    public class Test {
        [Test()]
        public void TestCase() {
            var actual = Path.Combine(
                Globals.binDir, "fontExtractHelp.txt");
            var expected = Path.Combine(
                Globals.resources, "fontExtractHelp.exp");

            using (var outputFile = File.CreateText(actual))
            using (var proc = FontExtract.Run("-h")) {
                proc.Start();
                outputFile.Write(proc.StandardOutput.ReadToEnd());
            }

            FileAssert.AreEqual(actual, expected);
        }
    }
}
