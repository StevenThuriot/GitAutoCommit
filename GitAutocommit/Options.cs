using CommandLine;

namespace GitAutocommit
{
    class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.", Default = false)]
        public bool Verbose { get; set; }

        [Option('i', "interval", Required = false, HelpText = "Commit interval in seconds.", Default = 60)]
        public int Interval { get; set; }

        [Option('d', "directory", Required = false, HelpText = "Set the git repo directory. Default is your current directory.")]
        public string Directory { get; set; }
    }
}
