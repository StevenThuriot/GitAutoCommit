using CommandLine;

namespace GitAutocommit
{
    class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.", Default = false)]
        public bool Verbose { get; set; }

        [Option('i', "interval", Required = false, HelpText = "Commit interval in seconds.", Default = 60)]
        public int Interval { get; set; }

        [Option('d', "directory", Required = false, HelpText = "(Default: Current Directory) Set the git repo directory.")]
        public string Directory { get; set; }

        [Option('p', "push", Required = false, HelpText = "Automatically push to origin after squashing. If not supplied, the squashed commit will not be pushed.")]
        public string AutoPush { get; set; }

        [Option('b', "branch", Required = false, HelpText = "Create a new branch before starting your work. It's good practice not to work on master, after all!")]
        public string Branch { get; set; }
    }
}
