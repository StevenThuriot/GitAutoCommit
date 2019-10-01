using CommandLine;
using LibGit2Sharp;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static System.Console;

namespace GitAutocommit
{
    static class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed(options => OnExecute(options).GetAwaiter().GetResult());
        }

        private static async Task OnExecute(Options options)
        {
            var verbose = options.Verbose
                ? new Action<string>(x => WriteLine("VERBOSE: " + x))
                : new Action<string>(_ => { });

            var directory = options.Directory;

            if (string.IsNullOrWhiteSpace(directory))
            {
                verbose("Setting directory to current");

                directory = Directory.GetCurrentDirectory();
            }

            verbose("Checking for git repo");

            if (!Repository.IsValid(directory))
            {
                WriteLine($"{directory} is not a valid git repository");
                return;
            }

            if (options.Interval < 10)
            {
                WriteLine("The defined interval can't be smaller than 10 seconds");
                return;
            }

            using (var cts = new CancellationTokenSource())
            {
                void cancelToken(object sender, ConsoleCancelEventArgs e)
                {
                    verbose("Cancelled directory watcher");

                    e.Cancel = true;
                    CancelKeyPress -= cancelToken;

                    cts.Cancel();
                }

                CancelKeyPress += cancelToken;

                using (var repo = new Repository(directory))
                {
                    if (!string.IsNullOrWhiteSpace(options.Branch))
                    {
                        WriteLine($"Creating {options.Branch} branch");
                        var newBranch = repo.CreateBranch(options.Branch);
                        Commands.Checkout(repo, newBranch);
                    }

                    var startBranch = repo.Head;

                    switch (startBranch.FriendlyName)
                    {
                        case nameof(GitAutocommit):
                        {
                            WriteLine($"You're still on a {nameof(GitAutocommit)} branch.");
                            WriteLine("This is most likely because a previous crash or unusual program termination. Please fix your repository first.");
                            return;
                        }

                        case "master":
                        {
                            WriteLine("Warning: You're currently working on master branch!");
                            break;
                        }
                    }

                    var startCommit = startBranch.Tip;

                    var branch = repo.Branches[nameof(GitAutocommit)];

                    if (branch != null)
                    {
                        WriteLine($"A {nameof(GitAutocommit)} branch still exists. This is most likely because a previous crash or unusual program termination.");

                        verbose("Renaming old branch");

                        var renamed = "autoCommits/" + DateTime.Now.ToString("yyyy-MM-dd/HH-mm-ss.f");

                        repo.CreateBranch(renamed, branch.Tip);
                        repo.Branches.Remove(nameof(GitAutocommit));

                        WriteLine($"We have automatically renamed it to {renamed}.");
                        WriteLine($"If you don't need it anymore, you can delete it by running `git branch -D {renamed}`");
                        WriteLine("You can remove all of them at once using `git branch -D $(git branch | grep autoCommits/* )` ");
                    }

                    verbose("Creating auto commit branch");
                    repo.CreateBranch(nameof(GitAutocommit));
                    branch = repo.Branches[nameof(GitAutocommit)];

                    verbose("Checking out auto commit branch");
                    Commands.Checkout(repo, branch);

                    var interval = TimeSpan.FromSeconds(options.Interval);
                    var autoCommitAuthor = new Signature(nameof(GitAutocommit), "@" + nameof(GitAutocommit), DateTime.Now);

                    verbose($"Checking repo changes with an interval of {options.Interval} seconds");

                    void CommitChanges()
                    {
                        verbose("Checking for changes");
                        var repoState = repo.RetrieveStatus();
                        if (repoState.IsDirty)
                        {
                            verbose("Staging changes");

                            Commands.Stage(repo, "*");

                            verbose("Auto committing changes");

                            repo.Commit("Git Auto Commit", autoCommitAuthor, autoCommitAuthor);

                            var count = repoState.Count(entry => entry.State != FileStatus.Ignored && entry.State != FileStatus.Unaltered);
                            WriteLine($"Auto committed {count} change{(count == 1 ? "" : "s")} on {DateTime.Now.ToString(CultureInfo.InvariantCulture)}");
                        }
                        else
                        {
                            verbose("No changes found");
                        }
                    }

                    WriteLine($"Monitoring {directory} for changes");

                    do
                    {
                        CommitChanges();

                        try
                        {
                            await Task.Delay(interval, cts.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            //Cancel requested
                        }
                    }
                    while (!cts.IsCancellationRequested);

                    CommitChanges();

                    var currentHead = repo.Head.Tip;
                    var noChanges = currentHead == startBranch.Tip;

                    string msg = null;
                    if (noChanges)
                    {
                        WriteLine("Git Auto Commit didn't commit any changes while it was running");
                    }
                    else
                    {
                        do
                        {
                            Write("Commit message: ");
                            msg = ReadLine();
                        } while (string.IsNullOrWhiteSpace(msg));
                    }

                    WriteLine("Checking out " + startBranch.FriendlyName);
                    Commands.Checkout(repo, startBranch);

                    if (!noChanges)
                    {
                        verbose("Retrieving default author");
                        var author = repo.Config.BuildSignature(DateTimeOffset.Now);

                        WriteLine("Squashing and merging auto commits");
                        repo.Reset(ResetMode.Hard, currentHead);
                        repo.Reset(ResetMode.Mixed, startCommit);

                        verbose("Staging changes");
                        Commands.Stage(repo, "*");

                        verbose("Committing changes with " + author);
                        repo.Commit(msg, author, author);

                        WriteLine("Squashed changes have been committed");
                    }

                    WriteLine("Removing auto commit branch");
                    repo.Branches.Remove(branch);

                    if (options.AutoPush != null)
                    {
                        var remotename = options.AutoPush;
                        if (string.IsNullOrWhiteSpace(remotename))
                        {
                            remotename = "origin";
                        }

                        try
                        {
                            verbose("Pushing to origin");

                            var remote = repo.Network.Remotes[remotename];

                            repo.Branches.Update(startBranch,
                                b => b.Remote = remote.Name,
                                b => b.UpstreamBranch = startBranch.CanonicalName);

                            repo.Network.Push(startBranch);
                        }
                        catch (Exception e)
                        {
                            WriteLine($"Failed to push to {remotename}: {e.Message}");
                        }
                    }


                    WriteLine("Finished auto committing");
                }
            }
        }
    }
}
