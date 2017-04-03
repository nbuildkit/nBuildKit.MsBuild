Description: Library usage instructions.
---

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
