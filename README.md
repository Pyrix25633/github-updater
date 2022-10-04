# github-updater
A command line utility to updates programs that come from github releases

### Index
- How to use it
- How to join the project

### How to use it
- Download the release
- Extract it
- Open a terminal inside the extracted folder
- Run "./github-updater help" for help
- Always run "./github-updater list" after a fresh install

### How to join the project
It is very easy to make your repository compatible with github updater:
- You repository name should be unique among the ones that have joined the project
- Create a json file called "github-updater.REPOSITORY_NAME.json" in your repository root containing
  - "latest": "LATEST_VERSION" (e.g. 1.5.4)
  - "keep": list of folders or files that have to be kept between updates, wildcards can be used
  - "releases": list of releases
    - "tag": "RELEASE_TAG" (e.g. 1.5.4)
    - "linux": "LINUX_RELEASE_FILE_NAME" (optional)
    - "win": "WINDOWS_RELEASE_FILE_NAME" (optional)
    - "cross": "CROSS_PLATFORM_RELEASE_FILE_NAME" (optional)
  - You can find examples in my repositories