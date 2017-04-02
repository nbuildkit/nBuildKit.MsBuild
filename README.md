# nBuildKit

nBuildKit provides a collection of tools and scripts that provide an easy way to set-up a complete build and deployment
for a project.


## Status

nBuildKit uses [AppVeyor](http://www.appveyor.com) for the continuous integration and deployment processes.

* __Build:__ [![Build status](https://ci.appveyor.com/api/projects/status/6ue5o5odc8y4480y?svg=true)](https://ci.appveyor.com/project/pvandervelde/nbuildkit-msbuild)


## Set up

Currently the only version available provides MsBuild scripts that run the build. In order to use this version take
the following steps.

* Download the sample ZIP archive from the [Github releases page](https://github.com/nbuildkit/nBuildKit.MsBuild/releases)
  or grab the [NuGet package](https://www.nuget.org/packages/nBuildKit.MsBuild.Samples/)
* Extract the following files:
  * `entrypoint.msbuild`
  * `artefacts.settings.props`
  * `build.settings.props`
  * `deploy.settings.props`
  * `settings.props`
  * `test.settings.props`
* Also extract the `nuget.config` file if you don't already have one in the root of your repository.
* [Update the `*.props` files]() so that they matches your repository and environment.
* Execute a build by calling the following command line:

        msbuild entrypoint.msbuild /t:build

During the build process the correct version of the `nBuildKit.MsBuild` packages will be downloaded from [nuget.org](https://www.nuget.org).


## How to contribute
There are many ways to contribute to the project:

* By opening an [issue](https://github.com/nbuildkit/nBuildKit.MsBuild/issues/new) and describing the issue
  that occurred or the feature that would make things better.
* By providing a [pull-request](https://github.com/nbuildkit/nBuildKit.MsBuild/pulls) for a new feature or
  a bug.

Any suggestions or improvements you may have are more than welcome.
