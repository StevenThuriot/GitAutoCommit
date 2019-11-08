using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitAutocommit
{
    class CommitService
    {
        private readonly ILogger _logger;
        private readonly Options _options;

        public CommitService(ILogger<CommitService> logger, Options options)
        {
            _logger = logger;
            _options = options;
        }

        public async Task Execute()
        {
            var directory = _options.Directory;

            if (string.IsNullOrWhiteSpace(directory))
            {
                _logger.LogDebug("Setting directory to current");
                directory = Directory.GetCurrentDirectory();
            }

            _logger.LogDebug("Checking for git repo");

            if (!Repository.IsValid(directory))
            {
                _logger.LogInformation($"{directory} is not a valid git repository");
                return;
            }

            if (_options.Interval < 10)
            {
                _logger.LogInformation("The defined interval can't be smaller than 10 seconds");
                return;
            }

            using var cts = new CancellationTokenSource();
            
            void cancelToken(object sender, ConsoleCancelEventArgs e)
            {
                if (cts.IsCancellationRequested)
                    return;

                _logger.LogDebug("Cancelled directory watcher");

                e.Cancel = true;
                Console.CancelKeyPress -= cancelToken;

                cts.Cancel();
            }

            Console.CancelKeyPress += cancelToken;

            using var repo = new Repository(directory);
            CheckOut(repo);

            var startBranch = repo.Head;

            switch (startBranch.FriendlyName)
            {
                case nameof(GitAutocommit):
                    {
                        _logger.LogInformation($"You're still on a {nameof(GitAutocommit)} branch.");
                        _logger.LogInformation("This is most likely because a previous crash or unusual program termination. Please fix your repository first.");
                        return;
                    }

                case "master":
                    {
                        _logger.LogInformation("Warning: You're currently working on master branch!");
                        break;
                    }
            }

            var startCommit = startBranch.Tip;

            var branch = repo.Branches[nameof(GitAutocommit)];

            if (branch != null)
            {
                CreateBranch(repo, branch);
            }

            _logger.LogDebug("Creating auto commit branch");
            repo.CreateBranch(nameof(GitAutocommit));
            branch = repo.Branches[nameof(GitAutocommit)];

            _logger.LogDebug("Checking out auto commit branch");
            Commands.Checkout(repo, branch);

            var interval = TimeSpan.FromSeconds(_options.Interval);
            var autoCommitAuthor = new Signature(nameof(GitAutocommit), "@" + nameof(GitAutocommit), DateTime.Now);

            _logger.LogDebug($"Checking repo changes with an interval of {_options.Interval} seconds");
            _logger.LogInformation($"Monitoring {directory} for changes");

            await CommitChanges(interval, repo, autoCommitAuthor, cts);

            var currentHead = repo.Head.Tip;
            var hasChanges = currentHead != startBranch.Tip;

            string msg = ResolveCommitMessage(hasChanges);

            _logger.LogInformation("Checking out " + startBranch.FriendlyName);
            Commands.Checkout(repo, startBranch);

            if (hasChanges)
            {
                Commit(repo, startCommit, currentHead, msg);
            }

            _logger.LogInformation("Removing auto commit branch");
            repo.Branches.Remove(branch);

            if (_options.AutoPush != null)
            {
                AutoPush(repo, startBranch);
            }


            _logger.LogInformation("Finished auto committing");
        }

        private void CheckOut(Repository repo)
        {
            CheckOutSourceBranch(repo);

            CheckoutTargetBranch(repo);
        }

        private void CheckOutSourceBranch(Repository repo)
        {
            if (!string.IsNullOrWhiteSpace(_options.BranchFrom))
            {
                _logger.LogInformation($"Starting on branch {_options.BranchFrom}");

                var startpoint = repo.Branches[_options.BranchFrom];
                Commands.Checkout(repo, startpoint);

                var author = repo.Config.BuildSignature(DateTimeOffset.Now);
                Commands.Pull(repo, author, null);
            }
        }

        private void CheckoutTargetBranch(Repository repo)
        {
            if (!string.IsNullOrWhiteSpace(_options.Branch))
            {
                _logger.LogInformation($"Creating {_options.Branch} branch");
                var newBranch = repo.CreateBranch(_options.Branch);
                Commands.Checkout(repo, newBranch);
            }
        }

        private void CreateBranch(Repository repo, Branch branch)
        {
            _logger.LogInformation($"A {nameof(GitAutocommit)} branch still exists. This is most likely because a previous crash or unusual program termination.");

            _logger.LogDebug("Renaming old branch");

            var renamed = "autoCommits/" + DateTime.Now.ToString("yyyy-MM-dd/HH-mm-ss.f");

            repo.CreateBranch(renamed, branch.Tip);
            repo.Branches.Remove(nameof(GitAutocommit));

            _logger.LogInformation($"We have automatically renamed it to {renamed}.");
            _logger.LogInformation($"If you don't need it anymore, you can delete it by running `git branch -D {renamed}`");
            _logger.LogInformation("You can remove all of them at once using `git branch -D $(git branch | grep autoCommits/* )` ");
        }

        private async Task CommitChanges(TimeSpan interval, Repository repo, Signature autoCommitAuthor, CancellationTokenSource cts)
        {
            do
            {
                CommitChanges(repo, autoCommitAuthor);

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

            CommitChanges(repo, autoCommitAuthor);
        }

        private void CommitChanges(Repository repo, Signature autoCommitAuthor)
        {
            _logger.LogDebug("Checking for changes");
            var repoState = repo.RetrieveStatus();
            if (repoState.IsDirty)
            {
                _logger.LogDebug("Staging changes");

                Commands.Stage(repo, "*");

                _logger.LogDebug("Auto committing changes");

                repo.Commit("Git Auto Commit", autoCommitAuthor, autoCommitAuthor);

                var count = repoState.Count(entry => entry.State != FileStatus.Ignored && entry.State != FileStatus.Unaltered);
                _logger.LogInformation($"Auto committed {count} change{(count == 1 ? "" : "s")} on {DateTime.Now.ToString(CultureInfo.InvariantCulture)}");
            }
            else
            {
                _logger.LogDebug("No changes found");
            }
        }

        private string ResolveCommitMessage(bool hasChanges)
        {
            if (hasChanges)
            {
                string msg;

                do
                {
                    Console.Write("Commit message: ");
                    msg = Console.ReadLine();
                } while (string.IsNullOrWhiteSpace(msg));

                return msg;
            }

            _logger.LogInformation("Git Auto Commit didn't commit any changes while it was running");

            return null;
        }

        private void Commit(Repository repo, Commit startCommit, Commit currentHead, string msg)
        {
            _logger.LogDebug("Retrieving default author");
            var author = repo.Config.BuildSignature(DateTimeOffset.Now);

            _logger.LogInformation("Squashing and merging auto commits");
            repo.Reset(ResetMode.Hard, currentHead);
            repo.Reset(ResetMode.Mixed, startCommit);

            _logger.LogDebug("Staging changes");
            Commands.Stage(repo, "*");

            _logger.LogDebug("Committing changes with " + author);
            repo.Commit(msg, author, author);

            _logger.LogInformation("Squashed changes have been committed");
        }

        private void AutoPush(Repository repo, Branch startBranch)
        {
            var remotename = _options.AutoPush;
            if (string.IsNullOrWhiteSpace(remotename))
            {
                remotename = "origin";
            }

            try
            {
                _logger.LogDebug("Pushing to origin");

                var remote = repo.Network.Remotes[remotename];

                repo.Branches.Update(startBranch,
                    b => b.Remote = remote.Name,
                    b => b.UpstreamBranch = startBranch.CanonicalName);

                repo.Network.Push(startBranch);
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Failed to push to {remotename}: {e.Message}");
            }
        }
    }
}
