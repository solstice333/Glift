using NUnit.Framework;
using System;
using System.IO;
using System.Diagnostics;

namespace FontExtractTest {
    static class Globals {
        public static string thisBinDir = Path.GetFullPath(
            TestContext.CurrentContext.TestDirectory);
        public static string resources = Path.GetFullPath(Path.Combine(
            thisBinDir, "..", "..", "Resources"));
        public static string fontExtractBinDir = Path.GetFullPath(Path.Combine(
            thisBinDir, "..", "..", "..",
            "FontExtract", "bin", "Debug"));
        public static string fontExtract = Path.GetFullPath(Path.Combine(
            fontExtractBinDir, "FontExtract.exe"));
    }

    static class FontExtract {
        public static Process Run(string args) {
            var startInfo = new ProcessStartInfo {
                UseShellExecute = false,
                FileName="mono",
                Arguments = $"--debug {Globals.fontExtract} {args}",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            return new Process { StartInfo = startInfo };
        }
    }

    [TestFixture()]
    public class Test {
        public static string FileInThisBinDir(string filename) {
            return Path.Combine(Globals.thisBinDir, filename);
        }

        public static string FileInResources(string filename) {
            return Path.Combine(Globals.resources, filename);
        }

        public static string FileInFontExtractBin(string filename) {
            return Path.Combine(Globals.fontExtractBinDir, filename);
        }

        public static string TtfFile(string filename) {
            return Path.Combine(Globals.resources, filename);
        }

        public static void FontExtractRun(string args) {
            using (var proc = FontExtract.Run(args)) {
                proc.Start();
                Console.WriteLine(proc.StandardOutput.ReadToEnd());
                Console.WriteLine(proc.StandardError.ReadToEnd());
            }
        }

        public static void FontExtractRun(string args, string redirectTo) {
            using (var outputFile = File.CreateText(redirectTo))
            using (var proc = FontExtract.Run(args)) {
                proc.Start();
                outputFile.Write(proc.StandardOutput.ReadToEnd());
            }
        }

        [OneTimeSetUp]
        public void Init() {
            Directory.SetCurrentDirectory(Globals.thisBinDir);
        }

        [Test]
        public void HelpTest() {
            var actual = FileInThisBinDir("fontExtractHelp.txt");
            var expected = FileInResources("fontExtractHelp.exp");
            FontExtractRun("-h", actual);
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void AObjTest() {
            var actual = FileInThisBinDir("A.obj");
            var expected = FileInResources("A.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            FontExtractRun($"-c A {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void SObjTest() {
            var actual = FileInThisBinDir("S.obj");
            var expected = FileInResources("S.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            FontExtractRun($"-c S {ttf}");
            FileAssert.AreEqual(actual, expected);
        }
    }
}
