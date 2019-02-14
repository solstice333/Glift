using System;
using System.IO;
using System.Collections.Generic;

using Typography.OpenFont;
using Typography.Contours;

using DrawingGL;
using DrawingGL.Text;

using NDesk.Options;

using Util;

namespace FontTest {
    static class Args {
        public static bool help = false;
        public static bool allGlyphs = true;
        public static List<char> chars = new List<char>();
        public static int xoffset = 0;
        public static int yoffset = 0;
        public static string ttfPath = "";
        public static int exst = 0;

        private static OptionSet _parser = new OptionSet {
            { 
                "c|char=", "specify a glyph to convert to .obj. " +
                "Exit 1 if VALUE is not a single character. " +
                "This can stack",
                v => {
                    if (v != null) {
                        if (v.Length > 1) {
                            help = true;
                            exst = 1;
                        }
                        allGlyphs = false;
                        chars.Add(v[0]);
                    }
                }
            },
            {
                "x|xoffset=", "translate the model VALUE units across " +
                "the x axis. Exit 1 if VALUE is a non-integer",
                v => {
                    help = !Int32.TryParse(v, out xoffset);
                    exst = help ? 1 : 0;
                }
            },
            {
                "y|yoffset=", "translate the model VALUE units across " +
                "the y axis. Exit 1 if VALUE is a non-integer",
                v => {
                    help = !Int32.TryParse(v, out yoffset);
                    exst = help ? 1 : 0;
                }
            },
            {
                "h|help", "show this message and exit",
                v => {
                    help = v != null;
                    exst = 0;
                }
            }
        };

        private static void _ShowHelp(OptionSet parser) {
            Console.WriteLine("usage: FontExtract [OPTIONS]+ TTF");
            Console.WriteLine();
            Console.WriteLine("convert .ttf glyphs to .obj");
            Console.WriteLine();
            Console.WriteLine("positional arguments:");
            Console.WriteLine(
                "  TTF                        path to .ttf file");
            Console.WriteLine();
            Console.WriteLine("optional arguments:");
            parser.WriteOptionDescriptions(Console.Out);
        }

        private static void _ConsumePositionalArgs(List<string> args) {
            if (args.Count != 1) {
                help = true;
                exst = 1;
            }
            else
                ttfPath = args[0];
        }

        public static void Parse(string[] args) {
            List<string> pos = _parser.Parse(args);
            _ConsumePositionalArgs(pos);

            if (help) {
                _ShowHelp(_parser);
                Environment.Exit(exst);
            }
        }
    }

    class MainClass {
        public static void Main(string[] args) {
            Args.Parse(args);
            Console.WriteLine($"want all glyphs?: {Args.allGlyphs}");
            Console.WriteLine($"chars: {Args.chars.ToString<char>()}");
            Console.WriteLine($"x offset: {Args.xoffset}");
            Console.WriteLine($"y offset: {Args.yoffset}");
            Console.WriteLine($"path to ttf: {Args.ttfPath}");
        }
    }
}
