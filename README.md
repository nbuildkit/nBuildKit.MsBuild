# nBuildKit

nBuildKit is a collection of tools and scripts that provide an easy way to set-up a complete build and deployment
for a (.NET) project.


## Continuous integration & deployment

nBuildKit uses [AppVeyor](http://www.appveyor.com) for the continuous integration and deployment processes.

* __Build:__ [![Build status](https://ci.appveyor.com/api/projects/status/yqpaush2xajbyn74)](https://ci.appveyor.com/project/pvandervelde/nbuildkit)

* __Deployment status:__ [![Build status](https://ci.appveyor.com/api/projects/status/jcp6k51ntktugenm)](https://ci.appveyor.com/project/pvandervelde/nbuildkit-244)


## Set up

Currently the only version available provides MsBuild scripts that run the build. In order to use this version take
the following steps.

* Import the NuGet package as part of the project. Note that it does not need to be imported by a .NET project file as
  it does not add anything to the projects. Having a packages.config file at the root directory of the repository with
  the import line for `nBuildKit.MsBuild` will work fine.
* From the [releases page](https://github.com/pvandervelde/nBuildKit/releases) download the latest
  `nbuildkit.msbuild.samples.zip` archive.
* Extract the `settings.props`, `build.msbuild` and `deploy.msbuild` from the ZIP archive and place them
  in the root directory of the repository.
* Update the `settings.props` file so that it matches your repository and environment.
* Execute a build.


## Installation instructions

The MsBuild libraries are available on [NuGet.org](http://www.nuget.org/packages/nBuildKit.MsBuild/). The ZIP archive containing the sample scripts
can be found on the [releases page](https://github.com/pvandervelde/nBuildKit/releases).


## How to build

To build the project invoke MsBuild on the `build.msbuild` script in the repository root directory. This will package
the scripts and create the NuGet packages and ZIP archives. Final artifacts will be placed in the `build\deploy` directory.

The build script assumes that:

* The connection to the repository is available so that the version number can be obtained via
  [GitVersion](https://github.com/GitTools/GitVersion) tool.
* The [NuGet command line executable](http://dist.nuget.org/win-x86-commandline/latest/nuget.exe) is available
  from the PATH so that the build can restore all the NuGet packages.
* The GIT executable is availabe from the PATH so that the build can gather information about the current branch and
  commit ID.


## How to contribute
There are many ways to contribute to the project:

* By opening an [issue](https://github.com/nbuildkit/nBuildKit.MsBuild/issues/new) and describing the issue
  that occurred or the feature that would make things better.
* By providing a [pull-request](https://github.com/nbuildkit/nBuildKit.MsBuild/pulls) for a new feature or
  a bug.

Any suggestions or improvements you may have are more than welcome.