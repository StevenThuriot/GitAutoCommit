using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitAutocommit
{
    static class Program
    {
        public static async Task Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            await result.MapResult(async options => await Run(options), MapErrors);
        }

        private static Task MapErrors(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                Console.WriteLine(error.ToString());
            }

            return Task.CompletedTask;
        }

        private static async Task Run(Options options)
        {
            var serviceCollection = new ServiceCollection()
                                            .AddSingleton(options)
                                            .AddTransient<CommitService>();

            serviceCollection.AddLogging(logging => logging.ClearProviders().AddConsole(x => x.Format = Microsoft.Extensions.Logging.Console.ConsoleLoggerFormat.Systemd))
                             .Configure<LoggerFilterOptions>(x => x.MinLevel = options.Verbose ? LogLevel.Debug : LogLevel.Information);


            using var services = serviceCollection.BuildServiceProvider();

            var service = services.GetService<CommitService>();

            try
            {
                await service.Execute();
            }
            catch (Exception e)
            {
                services.GetService<ILogger<CommitService>>().LogError(e.Message);
            }
        }
    }
}
