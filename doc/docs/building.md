To build the project invoke MsBuild on the `nBuildKit.msbuild` script with the `build` target in the repository root directory, i.e.

    msbuild nbuildkit.msbuild /t:build 

This will package the scripts and create the NuGet packages and ZIP archives. Final artifacts will be placed in the `build\deploy` directory.

In order to do some basic verification you can run the verification tests by executing

    msbuild nbuildkit.msbuild /t:test

This will take the recently build nuget packages and try to build a solution with C# projects and one with VB.NET projects. 

The build script assumes that:

* The connection to the repository is available so that the version number can be obtained via [GitVersion](https://github.com/ParticularLabs/GitVersion).
* The NuGet command line executable is available from the PATH