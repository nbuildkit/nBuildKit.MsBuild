# nBuildKit

nBuildKit is a collection of tools and scripts that provide an easy way to set-up a complete build and deployment for a (.NET) project.

## Build status

[![Build status](https://ci.appveyor.com/api/projects/status/yqpaush2xajbyn74)](https://ci.appveyor.com/project/pvandervelde/nbuildkit)

## Set up

Currently the only version available provides MsBuild scripts that run the build. In order to use this version take the following steps.

* Import the NuGet package as part of the project. Note that it does not need to be imported by a .NET project file as it does not add anything to the projects. Having a packages.config file at the root directory of the repository with the import line for `nBuildKit.MsBuild` will work fine.
* From the [releases page](https://github.com/pvandervelde/nBuildKit/releases) download the latest `nbuildkit.msbuild.samples.zip` archive.
* Extract the `settings.props`, `build.msbuild` and `deploy.msbuild` from the ZIP archive and place them in the root directory of the repository.
* Update the `settings.props` file so that it matches your repository and environment.
* Execute a build.

## Installation instructions

The MsBuild libraries are available on [NuGet.org](nuget.org). The ZIP archive containing the sample scripts can be found on the [releases page](https://github.com/pvandervelde/nBuildKit/releases).

## How to build

To build the project invoke MsBuild on the `build.msbuild` script in the repository root directory. This will package the scripts and create the NuGet packages and ZIP archives. Final artifacts will be placed in the `build\deploy` directory.

The build script assumes that:

* The connection to the repository is available so that the version number can be obtained via [GitHubFlowVersion](https://github.com/JakeGinnivan/GitHubFlowVersion).
* The NuGet command line executable is available from the PATH