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
        public static string ActualFile(string filename) {
            return Path.Combine(Globals.binDir, filename);
        }

        public static string ExpectedFile(string filename) {
            return Path.Combine(Globals.resources, filename);
        }

        public static void FontExtractRun(string args, string redirectTo) {
            using (var outputFile = File.CreateText(redirectTo))
            using (var proc = FontExtract.Run(args)) {
                proc.Start();
                outputFile.Write(proc.StandardOutput.ReadToEnd());
            }
        }

        [Test()]
        public void HelpTest() {
            var actual = ActualFile("fontExtractHelp.txt");
            var expected = ExpectedFile("fontExtractHelp.exp");
            FontExtractRun("-h", actual);
            FileAssert.AreEqual(actual, expected);
        }

        public void AObjTest() { }
        public void SObjTest() { }
    }
}
