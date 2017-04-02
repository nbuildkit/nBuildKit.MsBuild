nBuildKit provides the following MsBuild packages:


## Requirements

The `nBuildKit` package makes the following assumptions:

* There is an installation of the command line version of NuGet available for use during the build process. This may be either be found via the PATH environment variable or may be found via the path value provided in the `settings.props` file.
* Certain tasks cannot be skipped
 * Workspace preparation - Cleans out the `build` directory
 * Gather version information - Collects information about the current version and stores it in a temporary file to be used by the rest of the build. Current options are to use a MsBuild version file or to use the GitHubFlowVersion application.
* Certain tasks require certain applications / packages to be available. These are:
 * Release notes are gathered via [GitReleaseNotes](https://github.com/GitTools/GitReleaseNotes)
 * Version numbers are either gathered via a `version.xml` msbuild file, via [GitVersion](https://github.com/ParticularLabs/GitVersion), or via [GitHubFlowVersion](https://github.com/JakeGinnivan/GitHubFlowVersion)
 * [CCM](http://www.blunck.se/ccm.html)
 * [SourceMonitor](http://www.campwoodsw.com/sourcemonitor.html)
 * Binaries --> should be able to build with MsBuild, hence all tasks need to be available
 * [Moma](http://www.mono-project.com/docs/tools+libraries/tools/moma/)
 * FxCop
 * [Sandcastle](https://github.com/EWSoftware/SHFB) for documentation
* All binaries are dropped in a specific directory (`workspace/build/bin`) at the end of the compilation process.


## Workspace layout

The `nBuildKit.MsBuild` package makes the following assumptions about the layout of the build environment:

* There is a `packages.config` file in the same directory as the `nbuildkit.msbuild` main build script. The `packages.config` file is used to find the installed version of nBuildKit.
* There is either a `packages` directory, which contains the installed NuGet packages for the project, or a `nuget.config` file in the same directory as the `nsbuildkit.msbuild`  main build script. If the `packages` directory is found then it will be assumed that the installed version of nBuildKit will be in that directory, otherwise the `nuget.config` file will be searched for a `repositorypath` which will be used to determine the location of the packages directory.

In other words `nBuildKit` expects at least either one of the following directory (`[D]`) and file structures:

    [D] workspace
        |
        |- myproject.msbuild
        |- nuget.config
        |- packages.config
        |- settings.props

where the `nuget.config` file points to a `repositorypath`, or the following situation

    [D] workspace
        |
        |- [D] packages
        |- myproject.msbuild
        |- packages.config
        |- settings.props

By default the `settings.props` file describes a workspace with the following layout:

    [D] workspace
        |
        |- [D] build
        |      |
        |      |- [D] bin
        |      |- [D] deploy
        |      |- [D] logs
        |      |- [D] temp
        |- [D] packages
        |- [D] src
        |- [D] templates
        |- nbuildkit.msbuild
        |- packages.config
        |- settings.props

Where:

* __build__ - The directory that will contain all files generated during the build process, including binaries, artifacts, log files etc. etc.
 * __bin__ - The directory that will contain the binary files generated during the build process.
 * __deploy__ - The directory that will contain the artifacts (e.g. NuGet packages, ZIP archives etc.) generated during the build process.
 * __logs__ - The directory that will contain all the log files that were generated during the build process including the reports from the unit tests and the static analysis.
 * __temp__ - The directory that will contain all the temporary files that were generated during the build process.
* __packages__ - The directory that contains the NuGet packages for the build process. This directory may also contain the NuGet packages for the Visual Studio solution but that is not required.
* __src__ - The directory that contains the source files and Visual Studio solution and projects.
* __templates__ - The directory that contains the template files which are used as a basis for generated files.

Note that all these locations, other than the location of the `settings.props` file, which has to be in the 'root' of the workspace, may be changed by specifying where they are located in the `settings.props` file.

## Updating the settings file
Once a basic version of the `settings.props` file is available in the workspace directory you may want to configure it so that it fits your specific case better. All sections, properties and collections are fully documented in the `settings.props` file. The documentation should provide enough information to determine the goal of each item. The `settings.props` file contains the following sections:

* __Global settings__ - Contains the properties and collections that apply to all parts of the build, test and deploy process. These are items such as product information, directory and file locations and the selected processes for getting version numbers and release notes.
* __Build settings__ - Contains the properties and collections that apply to the build part of the process. These are items such as source code analysis with tools like CCM, compilation of source code, execution of unit tests, execution of static analysis through tools like FxCop, generation of API documentation through tools like Sandcastle and finally creating NuGet packages or ZIP archives. Note that any type of tests done in this part of the process should be assumed to be running in the same workspace and process environment as the compilation part of the build.
* __Test settings__ - Contains the properties and collections that apply to the test part of the process. These are items such as automated regression or performance tests. Any tests executed during this stage of the process can be assumed to be executed in a different workspace and process space from the build steps, making this section more suitable for longer running tests.
* __Deploy settings__ - Contains the properties and collections that apply to the deploy part of the process. These are items such as tagging the current VCS revision, pushing NuGet packages to a NuGet feed and copying files to a local or remote location.

### Build settings specifics
There are several build specific settings defined in the `settings.props` file.

#### Available build steps
The build part of `nBuildKit` has defined the following default steps:

* __Build / workspace preparation__
 * __Workspace cleaning__ - Removes the `build` directory and all the files generated during the build.
 * __Restore NuGet packages__ - Restores the NuGet packages for all of the build.
 * __Gather version numbers__ - Gathers the version number information for the build.
 * __Gather release notes__ - Gathers the release notes for the build.
 * __Gather VCS information__ - Gathers information about the current revision number and branch. Currently only supported for GIT.
 * __Generate license file__ - Generates an XML file containing the license information for each of the NuGet packages used in the build.
 * __Generate files__ - Generate one or more files based on a set of templates.
* __Analysis of source code__
 * __CCM__ - Analyzes the source code with [CCM](http://www.blunck.se/ccm.html).
 * __SourceMonitor__ - Analyzes the source code with [SourceMonitor](http://www.campwoodsw.com/sourcemonitor.html).
* __Compilation__ - Invokes MsBuild on one or more Visual Studio solutions.
* __Unit testing__
 * __MsTest__ - Executes the unit tests with [MsTest]()
 * __NUnit__ - Executes the unit tests with [NUnit](http://nunit.org)
 * __VsTest__ - Executes the unit tests with [VsTest]()
* __Analysis of binaries__
 * __MoMA__ - Analyzes the binaries with the [Mono Migration Analyzer](http://www.mono-project.com/MoMA).
 * __FxCop__ - Analyzes the binaries with FxCop.
* __API documentation__ - Builds the API documentation with [Sandcastle](http://sandcastle.codeplex.com/) and [Sandcastle helpfile builder](http://shfb.codeplex.com/).
* __Packing__
 * __NuGet__ - Creates the [NuGet](http://nuget.org/) packages.
 * __ZIP archives__ - Creates one or more ZIP archives.

Note that the build steps described above may have its own set of properties that need to be configured. These properties can also be found in the `settings.props` file below the build steps `ItemGroup`.

### Test settings

#### Available test steps
The test part of `nBuildKit` can execute the following steps:

* __Build / workspace preparation__
 * __Workspace cleaning__ - Removes the `build` directory and all the files generated during the build.
 * __Restore NuGet packages__ - Restores the NuGet packages for all of the build.
 * __Gather version numbers__ - Gathers the version number information for the build.
 * __Gather release notes__ - Gathers the release notes for the build.
 * __Gather VCS information__ - Gathers information about the current branch. Currently only supported for GIT.
 * __Generate files__ - Generate one or more files based on a set of templates.


### Deploy settings

#### Available deploy steps
The deploy part of `nBuildKit` can execute the following steps:

* Tag current revision with the version number
 * GIT
 * TFS
* Push artifacts to local / remote directory
* Push NuGet packages to NuGet feed
* Push NuGet symbol packages to local symbol server (copy to directory) (nAnicitus approach)
* Create release on GitHub (based on earlier tag) and push artifacts to release

### Custom build steps

All sections allow custom build steps to be executed before and after the section. Custom build step must be a MsBuild script. Custom build steps can be inserted in the sequence by adding them in the desired location in the `BuildStepsToExecute`, `TestStepsToExecute` or `DeployStepsToExecute` item groups.

## Versions

* Build scripts require a version. Using the [semantic version](semver.org) at all times. Normally provide `major.minor.patch`. Note that binaries are versioned according to the way they are set up. The build scripts do not change that.
* Version can be gotten from a custom MsBuild script that defines the major, minor, patch and build numbers, e.g.

        <Project xmlns='http://schemas.microsoft.com/developer/msbuild/2003'
                 ToolsVersion="4.0">
            <UsingTask TaskName="CalculateCustomVersion"
                       TaskFactory="CodeTaskFactory"
                       AssemblyFile="$(MSBuildToolsPath)\Microsoft.Build.Tasks.v4.0.dll">
                <ParameterGroup>
                    <VersionMajor ParameterType="System.String" Output="true" />
                    <VersionMinor ParameterType="System.String" Output="true" />
                    <VersionPatch ParameterType="System.String" Output="true" />
                    <VersionBuild ParameterType="System.String" Output="true" />
                    <VersionPreRelease ParameterType="System.String" Output="true" />
                    <VersionSemantic ParameterType="System.String" Output="true" />
                    <VersionSemanticFull ParameterType="System.String" Output="true" />
                    <VersionSemanticNuGet ParameterType="System.String" Output="true" />
                </ParameterGroup>
                <Task>
                    <Code Type="Method" Language="cs">
                        <![CDATA[
                            public override bool Execute()
                            {
                                try
                                {
                                    VersionMajor = "4";
                                    VersionMinor = "3";
                                    VersionPatch = "2";
                                    VersionBuild = "1";
                                    VersionPreRelease = "MyPreRelease";
                                    VersionSemantic = string.Format(
                                        "{0}.{1}.{2}",
                                        VersionMajor,
                                        VersionMinor,
                                        VersionPatch);
                                    VersionSemanticFull = string.Format(
                                        "{0}.{1}.{2}-{3}+{4}",
                                        VersionMajor,
                                        VersionMinor,
                                        VersionPatch,
                                        VersionPreRelease,
                                        VersionBuild);
                                    VersionSemanticNuGet = string.Format(
                                        "{0}.{1}.{2}-{3}{4}",
                                        VersionMajor,
                                        VersionMinor,
                                        VersionPatch,
                                        VersionPreRelease,
                                        VersionBuild);
                                }
                                catch(Exception e)
                                {
                                    Log.LogError(e.ToString());
                                }

                                // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
                                // from a task's constructor or property setter. As long as this task is written to always log an error
                                // when it fails, we can reliably return HasLoggedErrors.
                                return !Log.HasLoggedErrors;
                            }
                        ]]>
                    </Code>
                </Task>
            </UsingTask>
        </Project>

* Getting the version from a XML file.

        <?xml version="1.0" encoding="utf-8"?>
        <Project ToolsVersion="3.5"
                 DefaultTargets="Build"
                 xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
            <!-- Define the values for the version number -->
            <PropertyGroup>
                <!-- Get the build number from a CI server (e.g. Jenkins) -->
                <BuildNumber>$(BUILD_NUMBER)</BuildNumber>

                <!-- If the build number doesn't exist then just set it to zero so that it has some kind of value -->
                <BuildNumber Condition=" '$(BuildNumber)' == '' ">0</BuildNumber>

                <!-- The version numbers -->
                <VersionMajor>1</VersionMajor>
                <VersionMinor>2</VersionMinor>
                <VersionPatch>3</VersionPatch>
                <VersionBuild>$(BuildNumber)</VersionBuild>
                <VersionPreRelease></VersionPreRelease>
            </PropertyGroup>
        </Project>

* Using the [GitVersion](https://github.com/ParticularLabs/GitVersion) application to get the version.
* Using the [GitHubFlowVersion](https://github.com/JakeGinnivan/GitHubFlowVersion) application to get the version.


## Commonly asked questions

* How to add a custom build step
* Minimal edits to `settings.props` for working build
* How to control what build steps are taken
* How to control what test steps are taken
* How to control what deploy steps are taken
* Multi stage build - test - deploy process
* Deploying through promoting
