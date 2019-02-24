using System;
using System.Collections.Generic;
using NDesk.Options;

namespace FontExtract {
    static class Args {
        public static bool help = false;
        public static List<char> chars = new List<char>();
        public static bool frontOnly = false;
        public static bool sideOnly = false;
        public static int xoffset = 0;
        public static int yoffset = 0;
        public static int zdepth = -50;
        public static int thickness = 20;
        public static string ttfPath = "";
        public static float sizePt = 300f;
        public static bool listNames = false;
        public static bool print = false;
        public static bool dryRun = false;
        public static int exst = 0;

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
                v => {
                    frontOnly = true;
                    sideOnly = false;
                }
            },
            {
                "side-only", "generate a .obj for the side face only",
                v => {
                    sideOnly = true;
                    frontOnly = false;
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
                "s|size=", "size in points (1/72 of 1 inch). " +
                "Defaults to 300. Exit 1 if VALUE is not a valid floating " +
                "point",
                v => {
                    help = !float.TryParse(v, out sizePt);
                    exst = help ? 1 : 0;
                }
            },
            {
                "x|xoffset=", "translate the model VALUE units across " +
                "the x axis. Exit 1 if VALUE is a non-integer",
                v => {
                    help = !int.TryParse(v, out xoffset);
                    exst = help ? 1 : 0;
                }
            },
            {
                "y|yoffset=", "translate the model VALUE units across " +
                "the y axis. Exit 1 if VALUE is a non-integer",
                v => {
                    help = !int.TryParse(v, out yoffset);
                    exst = help ? 1 : 0;
                }
            },
            {
                "z|zdepth=", "depth of the extrusion VALUE units across " +
                "the z axis. Defaults to 50. Exit 1 if VALUE is a non-integer",
                v => {
                    help = !int.TryParse(v, out zdepth);
                    zdepth = -zdepth;
                    exst = help ? 1 : 0;
                }
            },
            {
                "t|thickness-outline", "thickness of outline in VALUE " +
                "units. Defaults to 20",
                v => {
                    help = !int.TryParse(v, out thickness);
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
}
