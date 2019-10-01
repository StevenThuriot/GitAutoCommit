dotnet tool uninstall -g dotnet-autocommit

dotnet pack './GitAutocommit/GitAutocommit.csproj' --output ./

dotnet tool install -g dotnet-autocommit --add-source './'