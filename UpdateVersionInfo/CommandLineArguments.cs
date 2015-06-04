using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDesk.Options;

namespace UpdateVersionInfo
{
    public class CommandLineArguments
    {
        private readonly OptionSet _options;
        public bool ShowHelp { get; private set; }
        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int? Build { get; private set; }
        public int? Revision { get; private set; }
        public String VersionCsPath { get; private set; }
        public String AndroidManifestPath { get; private set; }
        public String TouchPListPath { get; private set; }
        public bool IncBuild { get; private set; }
        public bool RevisionStamp { get; private set; }
        public bool DoNotExit { get; private set; }

        private OptionSet Initialize()
        {
            var options = new OptionSet {
                {
                    "?", "Shows help/usage information.", h => ShowHelp = true
                },
                {
                    "v|major=", "A numeric major version number greater than zero.", (int v) => Major = v
                },
                {
                    "m|minor=", "A numeric minor number greater than zero.", (int m) => Minor = m
                },
                {
                    "b|build=", "A numeric build number greater than zero.", (int b) => Build = b
                },
                {
                    "r|revision=", "A numeric revision number greater than zero.", (int r) => Revision = r
                },
                {
                    "p|path=", "The path to a C# file to update with version information.", p => VersionCsPath = p
                },
                {
                    "a|androidManifest=", "The path to an android manifest file to update with version information.", ap => AndroidManifestPath = ap
                },
                {
                    "t|touchPlist=", "The path to an iOS plist file to update with version information.", tp => TouchPListPath = tp
                },
                {
                    "i|incBuild=", "If true, increments build number by 1 (overrides b flag).", (bool i) => IncBuild = i
                },
                {
                    "s|revisionStamp=", "If true, stamps the revision number with the number of seconds today (overrides r flag).", (bool s) => RevisionStamp = s
                },
                {
                    "d|doNotExit=", "Prevent app exiting after execution (useful for setting up post-build use).", (bool d) => DoNotExit = d
                }
            };
            return options;
        }

        public CommandLineArguments(IEnumerable<String> args)
        {
            Major = 1;
            Minor = 0;
            _options = Initialize();
            _options.Parse(args);
        }

        public void WriteHelp(System.IO.TextWriter writer)
        {
            _options.WriteOptionDescriptions(writer);
        }
    }
}
