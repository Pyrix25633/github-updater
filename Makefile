.RECIPEPREFIX=>
VERSION=0.1.0

default:
> clear
> make build-image
> make run-container
> make run-dotnet-debug

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
> dotnet ./transfer/docker/debug/github-updater.dll -- help

run-dotnet-release:
> ./transfer/docker/release/linux-x64/github-updater help