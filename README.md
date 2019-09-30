# GitAutoCommit

Are you like me and do you often screw yourself over by undoing just a bit too much, or forgetting to commit that one crucial part of the code you're writing? :pensive:

No more! I got your back! :muscle::triumph:

GitAutoCommit can automatically commit changes to git on a set interval to a separate dev branch, ending with a squash merge with a custom commit msg to the original head! Of course, the temporary dev branch will be cleaned up as well! Don't worry, it's also smart enough not to auto delete any GitAutoCommit branches should something horrible ever happen...

Finished monitoring your git repo? Just press CTRL+C !

## Getting started

:raising_hand: Help!

If you want to know how to configure GitAutoCommit, just scream!

Run `dotnet GitAutocommit.dll --help` for all options currently available

```console
$ dotnet GitAutocommit.dll --help
GitAutocommit 1.0.0
Copyright (C) 2019 GitAutocommit

  -v, --verbose      (Default: false) Set output to verbose messages.

  -i, --interval     (Default: 60) Commit interval in seconds.

  -d, --directory    Set the git repo directory. Default is your current directory.

  --help             Display this help screen.

  --version          Display version information.
  ```
 
 ## Jumping right in
 :rocket: Running the watcher in a git directory:
 
 ```console
 $ dotnet GitAutocommit.dll
Monitoring D:\Git\GitAutoCommit for changes
Auto committed 1 change on 09/30/2019 23:40:14
Auto committed 3 changes on 09/30/2019 23:41:14
Auto committed 2 changes on 09/30/2019 23:42:14
Commit message: I built new features
Checking out development
Squashing and merging auto commits
Squashed changes have been committed
Removing auto commit branch
Finished auto committing
 ```
