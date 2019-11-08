using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GitAutocommit
{
    static class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed(options => Run(options).GetAwaiter().GetResult());
        }

        private static Task Run(Options options)
        {
            var serviceCollection = new ServiceCollection()
                                            .AddSingleton(options)
                                            .AddTransient<CommitService>();

            serviceCollection.AddLogging(logging => logging.ClearProviders().AddConsole(x => x.Format = Microsoft.Extensions.Logging.Console.ConsoleLoggerFormat.Systemd))
                             .Configure<LoggerFilterOptions>(x => x.MinLevel = options.Verbose ? LogLevel.Debug : LogLevel.Information);


            using var services = serviceCollection.BuildServiceProvider();

            var service = services.GetService<CommitService>();

            return service.Execute();
        }
    }
}
