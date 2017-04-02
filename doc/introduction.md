# Introduction

nBuildKit is a collection of tools and scripts that provide an easy way to set-up a complete build and deployment for a (.NET) project.

The build scripts provide the following default abilities:

* Clear previously created files from the workspace.
* Restore any missing NuGet packages for the project.
* Calculate the version based on the selected method of determining version numbers. Currently available methods are:
    * Getting the version through a custom calculation method.
    * Getting the version from a XML file.
    * Using the [GitVersion](https://github.com/ParticularLabs/GitVersion) application to get the version.
    * Using the [GitHubFlowVersion](https://github.com/JakeGinnivan/GitHubFlowVersion) application to get the version.
* Gather the release notes via the [GitReleaseNotes](https://github.com/GitTools/GitReleaseNotes) application.
* Gather information about the current revision number and the working branch. Currently only GIT is supported.
* Generate one or more files from custom templates.
* Analyse sources with the [CCM](http://www.blunck.se/ccm.html) application.
* Analyse sources with the [SourceMonitor](http://www.campwoodsw.com/sourcemonitor.html) application.
* Build one or more Visual Studio solutions each with their own build configuration and platform.
* Execute unit tests and calculate code coverage. Code coverage reports can optionally be processed by [ReportGenerator](https://github.com/danielpalme/ReportGenerator). Currently supported test frameworks are
    * MsTest - Code coverage is always calculated with the [OpenCover](https://github.com/sawilde/opencover) library
    * VsTest - Code coverage is calculated with VsTest itself
    * nUnit - Code coverage is always calculated with the [OpenCover](https://github.com/sawilde/opencover) library
* Analyse the created binaries with [Moma](http://www.mono-project.com/docs/tools+libraries/tools/moma/).
* Analyse the created binaries with FxCop.
* Generate API documentation with [Sandcastle](https://github.com/EWSoftware/SHFB).
* Package the created binaries with:
    * NuGet
    * Zip archives

Build steps can be executed in any order providing the prerequisites for a build step are provided. Additionally custom build steps can be added at any point in the sequence.

## Available scripts
nBuildKit currently has build scripts that use:
* [[MsBuild|MsBuild]] as the script 'language'.

Build scripts for other languages may be added at a later stage.

## Building and contributing
The following pages provide the information necessary to build a local copy of the library and to contribute to the library.

* [[How to build|Building]]
* [[How to contribute|Contributing]]
