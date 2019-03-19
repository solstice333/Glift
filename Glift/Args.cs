using System;
using System.Collections.Generic;
using NDesk.Options;

namespace Glift {
    static class Args {
        public static bool help = false;
        public static List<char> chars = new List<char>();
        public static bool frontOnly = false;
        public static bool sideOnly = false;
        public static bool outlineOnly = false;
        public static float xoffset = 0f;
        public static float yoffset = 0f;
        public static int zdepth = -15;
        public static float thickness = 2.5f;
        public static string ttfPath = "";
        public static float sizeMult = 1f;
        public static bool listNames = false;
        public static bool print = false;
        public static bool dryRun = false;
        public static int exst = 0;
        public static double angle = 135;
        public static bool experimental = false;

        private static OptionSet _parser = new OptionSet {
            {
                "c|char=", "specify a glyph by codepoint to convert " +
                "to .obj. Exit 1 if VALUE is not a single character. " +
                "If not specified, defaults to all glyphs in the ttf. " +
                "This can stack",
                v => {
                    if (v != null) {
                        if (v.Length > 1) {
                            help = true;
                            exst = 1;
                        }
                        chars.Add(v[0]);
                    }
                }
            },
            {
                "front-only", "generate a .obj for the front face only",
                v => frontOnly = true
            },
            {
                "side-only", "generate a .obj for the side face only",
                v => sideOnly = true
            },
            {
                "outline-only", "generate a .obj for the outline face only",
                v => outlineOnly = true
            },
            {
                "a|angle=", "angle (in degrees) restriction for generating " +
                "side outlines where anything less than VALUE will have " +
                "side outlines (prismoids) generated for that joint. In " +
                "other words, if VALUE is 135, any joint along the front " +
                "outline, whose angle is less than 135 degrees will have a " +
                "side outline/prismoid generated at that joint. VALUE " +
                "defaults to 135. Exit 1 if VALUE is not a valid double " +
                "precision format",
                v => {
                    help = !double.TryParse(v, out angle);
                    exst = help ? 1 : 0;
                }
            },
            {
                "l|list-names", "list glyph names",
                v => listNames = true
            },
            {
                "p|print", "print .obj to console",
                v => print = true
            },
            {
                "d|dry-run", "do not write to .obj. Useful with -p if " +
                "printing to console is the only requirement",
                v => dryRun = true
            },
            {
                "s|size=", "size multiplier. The multiplicand is " +
                "72 points. The multiplier defaults to 1. Exit 1 if " +
                "VALUE is not a valid floating point",
                v => {
                    help = !float.TryParse(v, out sizeMult);
                    exst = help ? 1 : 0;
                }
            },
            {
                "x|xoffset=", "translate the model VALUE units across " +
                "the x axis. Exit 1 if VALUE is not a valid floating point",
                v => {
                    help = !float.TryParse(v, out xoffset);
                    exst = help ? 1 : 0;
                }
            },
            {
                "y|yoffset=", "translate the model VALUE units across " +
                "the y axis. Exit 1 if VALUE is not a valid floating point",
                v => {
                    help = !float.TryParse(v, out yoffset);
                    exst = help ? 1 : 0;
                }
            },
            {
                "z|zdepth=", "depth of the extrusion VALUE units across " +
                "the z axis. Defaults to 15. Exit 1 if VALUE is a non-integer",
                v => {
                    help = !int.TryParse(v, out zdepth);
                    zdepth = -zdepth;
                    exst = help ? 1 : 0;
                }
            },
            {
                "t|thickness-outline=", "thickness of outline in VALUE " +
                "units. Defaults to 10. Exit 1 if VALUE is not a valid " +
                "floating point",
                v => {
                    help = !float.TryParse(v, out thickness);
                    exst = help ? 1 : 0;
                }
            },
            {
                "experimental", "enable experimental features",
                v => experimental = true
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
            Console.WriteLine("usage: " +
                $"{System.AppDomain.CurrentDomain.FriendlyName} " +
                "[OPTIONS]+ TTF");
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
}
