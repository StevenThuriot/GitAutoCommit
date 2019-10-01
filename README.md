# GitAutoCommit

Are you like me and do you often screw yourself over by undoing just a bit too much, or forgetting to commit that one crucial part of the code you're writing? :pensive:

No more! I got your back! :muscle::triumph:

GitAutoCommit can automatically commit changes to git on a set interval to a separate dev branch, ending with a squash merge with a custom commit msg to the original head! Of course, the temporary dev branch will be cleaned up as well! Don't worry, it's also smart enough not to auto delete any GitAutoCommit branches should something horrible ever happen...

Finished monitoring your git repo? Just press CTRL+C !

## Installation

As easy as running

```console
$ dotnet tool install -g dotnet-autocommit
```

## Getting started

:raising_hand: Help!

If you want to know how to configure GitAutoCommit, just scream!

Run `dotnet GitAutocommit.dll --help` for all options currently available

```console
$ autocommit --help
GitAutocommit 1.1.0
Copyright (C) 2019 Steven Thuriot

  -v, --verbose      (Default: false) Set output to verbose messages.

  -i, --interval     (Default: 60) Commit interval in seconds.

  -d, --directory    (Default: Current Directory) Set the git repo directory.

  -p, --push         Automatically push to origin after squashing. If no name
                     supplied, origin will be used. If not supplied at all, the

                     squash will not be pushed.

  -b, --branch       Create a new branch before starting your work. It's good
                     practice not to work on master, after all!

  --help             Display this help screen.

  --version          Display version information.
  ```
 
 ## Jumping right in
 :rocket: Running the watcher in a git directory:
 
 ```console
 $ autocommit
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
