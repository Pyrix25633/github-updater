.RECIPEPREFIX=>
VERSION=1.0.0
COMMAND=install

default:
> clear
> make build-image
> make run-container
> make run-dotnet-debug

local:
> dotnet publish -c debug -p:Version=$(VERSION)
> make run-dotnet-local

release:
> clear
> make build-image
> make run-container
> make run-dotnet-release

build-image:
> docker build -t github-updater:$(VERSION) .

run-container:
> docker run -v $(shell pwd)/transfer:/transfer github-updater:$(VERSION)
> chown -R pyrix25633:pyrix25633 ./transfer/docker/*

run-dotnet-debug:
> dotnet ./transfer/docker/debug/github-updater.dll -- $(COMMAND)

run-dotnet-local:
> cp ./bin/debug/net6.0/github-updater.dll ./
> cp ./bin/debug/net6.0/github-updater.runtimeconfig.json ./
> cp ./bin/debug/net6.0/github-updater.pdb ./
> cp ./bin/debug/net6.0/github-updater.deps.json ./
> cp ./bin/debug/net6.0/Newtonsoft.Json.dll ./
> cp ./bin/debug/net6.0/ICSharpCode.SharpZipLib.dll ./
> cp ./bin/debug/net6.0/runtimes/linux-x64/lib/netstandard2.0/Mono.Posix.NETStandard.dll ./
> cp ./bin/debug/net6.0/runtimes/linux-x64/native/libMonoPosixHelper.so ./
> dotnet ./github-updater.dll -- $(COMMAND)

run-dotnet-release:
> ./transfer/docker/release/linux-x64/github-updater $(COMMAND)