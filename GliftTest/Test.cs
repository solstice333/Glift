﻿using NUnit.Framework;
using System;
using System.IO;
using System.Diagnostics;

namespace GliftTest {
    static class Globals {
        public static string thisBinDir = Path.GetFullPath(
            TestContext.CurrentContext.TestDirectory);
        public static string resources = Path.GetFullPath(Path.Combine(
            thisBinDir, "..", "..", "Resources"));
        public static string gliftBinDir = Path.GetFullPath(Path.Combine(
            thisBinDir, "..", "..", "..",
            "Glift", "bin", "Debug"));
        public static string glift = Path.GetFullPath(Path.Combine(
            gliftBinDir, "Glift.exe"));
    }

    static class Glift {
        public static Process Run(string args) {
            var startInfo = new ProcessStartInfo {
                UseShellExecute = false,
                FileName="mono",
                Arguments = $"--debug {Globals.glift} {args}",
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

        public static string FileInGliftBin(string filename) {
            return Path.Combine(Globals.gliftBinDir, filename);
        }

        public static string TtfFile(string filename) {
            return Path.Combine(Globals.resources, filename);
        }

        public static void GliftRun(string args) {
            using (var proc = Glift.Run(args)) {
                proc.Start();
                Console.WriteLine(proc.StandardOutput.ReadToEnd());
                Console.WriteLine(proc.StandardError.ReadToEnd());
            }
        }

        public static void GliftRun(string args, string redirectTo) {
            using (var outputFile = File.CreateText(redirectTo))
            using (var proc = Glift.Run(args)) {
                proc.Start();
                outputFile.Write(proc.StandardOutput.ReadToEnd());
            }
        }

        [OneTimeSetUp]
        public void SetUpOnce() {
            Directory.SetCurrentDirectory(Globals.thisBinDir);
        }

        [Test]
        public void HelpTest() {
            var actual = FileInThisBinDir("help.txt");
            var expected = FileInResources("help.exp");
            GliftRun("-h", actual);
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void AObjTest() {
            var actual = FileInThisBinDir("A.obj");
            var expected = FileInResources("A.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c A {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void SObjTest() {
            var actual = FileInThisBinDir("S.obj");
            var expected = FileInResources("S.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c S {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void BObjTest() {
            var actual = FileInThisBinDir("B.obj");
            var expected = FileInResources("B.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c B {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void AObjFrontTest() {
            var actual = FileInThisBinDir("A.obj");
            var expected = FileInResources("AFront.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c A --front-only {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void SObjFrontTest() {
            var actual = FileInThisBinDir("S.obj");
            var expected = FileInResources("SFront.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c S --front-only {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void BObjFrontTest() {
            var actual = FileInThisBinDir("B.obj");
            var expected = FileInResources("BFront.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c B --front-only {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void AObjSideTest() {
            var actual = FileInThisBinDir("A.obj");
            var expected = FileInResources("ASide.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c A --side-only {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void SObjSideTest() {
            var actual = FileInThisBinDir("S.obj");
            var expected = FileInResources("SSide.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c S --side-only {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void BObjSideTest() {
            var actual = FileInThisBinDir("B.obj");
            var expected = FileInResources("BSide.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c B --side-only {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void AObjOutlineTest() {
            var actual = FileInThisBinDir("A.obj");
            var expected = FileInResources("AOutline.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c A --outline-only {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void SObjOutlineTest() {
            var actual = FileInThisBinDir("S.obj");
            var expected = FileInResources("SOutline.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c S --outline-only {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void BObjOutlineTest() {
            var actual = FileInThisBinDir("B.obj");
            var expected = FileInResources("BOutline.exp");
            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c B --outline-only {ttf}");
            FileAssert.AreEqual(actual, expected);
        }

        [Test]
        public void ABSObjXYOffsetFrontSideOutlineTest() {
            var actualASide = FileInThisBinDir("ASideOnly.obj");
            var expectedASide = FileInResources("ASideOnly.exp");
            var actualBSide = FileInThisBinDir("BSideOnly.obj");
            var expectedBSide = FileInResources("BSideOnly.exp");
            var actualSSide = FileInThisBinDir("SSideOnly.obj");
            var expectedSSide = FileInResources("SSideOnly.exp");

            var actualAFront = FileInThisBinDir("AFrontOnly.obj");
            var expectedAFront = FileInResources("AFrontOnly.exp");
            var actualBFront = FileInThisBinDir("BFrontOnly.obj");
            var expectedBFront = FileInResources("BFrontOnly.exp");
            var actualSFront = FileInThisBinDir("SFrontOnly.obj");
            var expectedSFront = FileInResources("SFrontOnly.exp");

            var actualAOutline = FileInThisBinDir("AOutlineOnly.obj");
            var expectedAOutline = FileInResources("AOutlineOnly.exp");
            var actualBOutline = FileInThisBinDir("BOutlineOnly.obj");
            var expectedBOutline = FileInResources("BOutlineOnly.exp");
            var actualSOutline = FileInThisBinDir("SOutlineOnly.obj");
            var expectedSOutline = FileInResources("SOutlineOnly.exp");

            var ttf = TtfFile("Alef-Bold.ttf");
            GliftRun($"-c B -c A -c S " +
                $"--size 1.25 " +
                $"--xoffset -34.99 --yoffset -34.99 --zdepth 10 " +
                $"--front-only --side-only --outline-only {ttf}");

            FileAssert.AreEqual(actualASide, expectedASide);
            FileAssert.AreEqual(actualBSide, expectedBSide);
            FileAssert.AreEqual(actualSSide, expectedSSide);
            FileAssert.AreEqual(actualAFront, expectedAFront);
            FileAssert.AreEqual(actualBFront, expectedBFront);
            FileAssert.AreEqual(actualSFront, expectedSFront);
            FileAssert.AreEqual(actualAOutline, expectedAOutline);
            FileAssert.AreEqual(actualBOutline, expectedBOutline);
            FileAssert.AreEqual(actualSOutline, expectedSOutline);
        }

        [OneTimeTearDown]
        public void TearDownOnce() {
            File.Delete(FileInThisBinDir("A.obj"));
            File.Delete(FileInThisBinDir("B.obj"));
            File.Delete(FileInThisBinDir("S.obj"));

            File.Delete(FileInThisBinDir("AFrontOnly.obj"));
            File.Delete(FileInThisBinDir("BFrontOnly.obj"));
            File.Delete(FileInThisBinDir("SFrontOnly.obj"));

            File.Delete(FileInThisBinDir("ASideOnly.obj"));
            File.Delete(FileInThisBinDir("BSideOnly.obj"));
            File.Delete(FileInThisBinDir("SSideOnly.obj"));

            File.Delete(FileInThisBinDir("AOutlineOnly.obj"));
            File.Delete(FileInThisBinDir("BOutlineOnly.obj"));
            File.Delete(FileInThisBinDir("SOutlineOnly.obj"));
        }
    }
}
