<#
    This file contains the 'verification tests' for the 'C# build' section of the nbuildkit verification test suite.
    These tests are executed using Pester (https://github.com/pester/Pester).
#>

param(
    # The minimum version of nBuildKit that should be used for the test
    [string] $nbuildkitminimumversion,

    # The maximum version of nBuildKit that should be used for the test
    [string] $nbuildkitmaximumversion,

    # The path to the directory that contains the nBuildKit NuGet packages that need to be tested
    [string] $localNuGetFeed,

    # The URL of the remote repository that contains the test code. This repository will be mirror cloned into
    # a local repository so that the tests can push to the repository without destroying the original
    [string] $remoteRepositoryUrl,

    # The active branch from which the code should be taken. This branch will be merged into develop / master
    # according to the gitversion rules in the cloned remote repository. From there the test can make changes
    # to the repository
    [string] $activeBranch,

    # The local location where the cloned repository can be placed
    [string] $repositoryLocation,

    # The local location where the workspace for the current test can be placed
    [string] $workspaceLocation,

    # The path to where the nuget packages can be deployed at the end of the test
    [string] $nugetPath,

    # The path to where the nuget symbol packages can be deployed at the end of the test
    [string] $symbolsPath,

    # The path to where the artefacts can be deployed at the end of the test
    [string] $artefactsPath,

    # The location where the log files should be placed
    [string] $logLocation,

    # A temporary directory that can be used for the current test
    [string] $tempLocation
)

. (Join-Path $PSScriptRoot '..\src\tests\TestFunctions.Git.ps1')
. (Join-Path $PSScriptRoot '..\src\tests\TestFunctions.MsBuild.ps1')
. (Join-Path $PSScriptRoot '..\src\tests\TestFunctions.PrepareWorkspace.ps1')

Add-Type -AssemblyName System.IO.Compression.FileSystem

# prepare the environment
if (-not (Test-Path $nugetPath))
{
    New-Item -Path $nugetPath -ItemType Directory | Out-Null
}

if (-not (Test-Path $symbolsPath))
{
    New-Item -Path $symbolsPath -ItemType Directory | Out-Null
}

if (-not (Test-Path $artefactsPath))
{
    New-Item -Path $artefactsPath -ItemType Directory | Out-Null
}

Describe 'For the VB.NET test' {

    Context 'the preparation of the workspace' {
        New-Workspace `
            -remoteRepositoryUrl $remoteRepositoryUrl `
            -activeBranch $activeBranch `
            -repositoryLocation $repositoryLocation `
            -workspaceLocation $workspaceLocation `
            -tempLocation $tempLocation `
            -Verbose

        It 'has created the local repository' {
            $repositoryLocation | Should Exist
            "$repositoryLocation\HEAD" | Should Exist
        }

        It 'has created the workspace' {
            $workspaceLocation | Should Exist
            "$workspaceLocation\.git" | Should Exist
            "$workspaceLocation\entrypoint.msbuild" | Should Exist
        }

        It 'has set the workspace origin to the local repository' {
            $origin = Get-Origin -workspace $workspaceLocation
            $origin | Should Be $repositoryLocation
        }

        It 'has created a feature branch' {
            $currentBranch = Get-CurrentBranch -workspace $workspaceLocation
            $currentBranch | Should Not BeNullOrEmpty
            $currentBranch.StartsWith('feature/') | Should Be $true
        }
    }

    Context 'the build executes successfully' {
        $msBuildProperties = @{
            "FileEnvironment" = (Join-Path $workspaceLocation 'environment.props')
            'NBuildKitMinimumVersion' = $nbuildkitminimumversion
            'NBuildKitMaximumVersion' = $nbuildkitmaximumversion
            'LocalNuGetRepository' = $localNuGetFeed
        }

        $exitCode = Invoke-MsBuildFromCommandLine `
            -scriptToExecute (Join-Path $workspaceLocation 'entrypoint.msbuild') `
            -target 'build' `
            -properties $msBuildProperties `
            -logPath (Join-Path $logLocation 'test.latest.vbnet.build.log') `
            -Verbose

        $hasBuild = ($exitCode -eq 0)
        It 'and completes with a zero exit code' {
            $exitCode | Should Be 0
        }
    }

    Context 'the build produces a NuGet package' {
        $nugetPackage = Join-Path $workspaceLocation 'build\deploy\nBuildKit.Test.VbNet.Library.1.2.3.nupkg'

        It 'in the expected location' {
            $nugetpackage | Should Exist
        }

        if (Test-Path $nugetPackage)
        {
            # extract the package
            $packageUnzipLocation = Join-Path $workspaceLocation 'build\temp\unzip\nuget'
            if (-not (Test-Path $packageUnziplocation))
            {
                New-Item -Path $packageUnzipLocation -ItemType Directory | Out-Null
            }
            [System.IO.Compression.ZipFile]::ExtractToDirectory($nugetPackage, $packageUnzipLocation)

            It 'with the expected metadata' {
                $nuspec = Join-Path $packageUnzipLocation 'nBuildKit.Test.VbNet.library.nuspec'
                $nuspec | Should Exist

                $xmlDoc = [xml](Get-Content $nuspec)
                $xmlDoc.package.metadata.version | Should Be '1.2.3'
                $xmlDoc.package.metadata.releaseNotes | Should BeNullOrEmpty

                $dependencies = $xmlDoc.package.metadata.dependencies
                $dependencies.ChildNodes.Count | Should Be 0
            }

            $assemblyFile = Join-Path $packageUnzipLocation 'lib\net45\NBuildKit.Test.VbNet.Library.dll'
            It 'with the expected files' {
                $assemblyFile | Should Exist
            }

            It 'has files with the right metadata' {
                [Reflection.AssemblyName]::GetAssemblyName($assemblyFile).Version | Should Be '1.2.0.0'

                $file = [System.IO.FileInfo]$assemblyFile
                $file.VersionInfo.FileVersion | Should Be '1.2.3.0'
                $file.VersionInfo.ProductVersion | Should Be '1.2.3+0'

                $file.VersionInfo.ProductName | Should Be 'nBuildKit.Test.VbNet.Library'
                $file.VersionInfo.LegalCopyright | Should Be "Copyright (c) - My Company 2015 - $((Get-Date).Year). All rights reserved."
            }
        }
    }

    Context 'the build produces a symbol package' {
        $symbolPackage = Join-Path $workspaceLocation 'build\deploy\nBuildKit.Test.VbNet.Library.1.2.3.symbols.nupkg'

        It 'in the expected location' {
            $symbolPackage | Should Exist
        }

        if (Test-Path $symbolPackage)
        {
            # extract the package
            $packageUnzipLocation = Join-Path $workspaceLocation 'build\temp\unzip\symbols'
            if (-not (Test-Path $packageUnziplocation))
            {
                New-Item -Path $packageUnzipLocation -ItemType Directory | Out-Null
            }
            [System.IO.Compression.ZipFile]::ExtractToDirectory($symbolPackage, $packageUnzipLocation)

            It 'with the expected metadata' {
                $nuspec = Join-Path $packageUnzipLocation 'nBuildKit.Test.VbNet.library.nuspec'
                $nuspec | Should Exist

                $xmlDoc = [xml](Get-Content $nuspec)
                $xmlDoc.package.metadata.version | Should Be '1.2.3'
                $xmlDoc.package.metadata.releaseNotes | Should BeNullOrEmpty

                $dependencies = $xmlDoc.package.metadata.dependencies
                $dependencies.ChildNodes.Count | Should Be 0
            }

            $assemblyFile = Join-Path $packageUnzipLocation 'lib\net45\NBuildKit.Test.VbNet.Library.dll'
            It 'with the expected files' {
                $assemblyFile | Should Exist

                (Join-Path $packageUnzipLocation 'lib\net45\NBuildKit.Test.VbNet.Library.pdb') | Should Exist

                (Join-Path $packageUnzipLocation 'src\NBuildKit.Test.VbNet.Library\My%20Project\AssemblyInfo.vb') | Should Exist
                (Join-Path $packageUnzipLocation 'src\NBuildKit.Test.VbNet.Library\My%20Project\Application.Designer.vb') | Should Exist
                (Join-Path $packageUnzipLocation 'src\NBuildKit.Test.VbNet.Library\My%20Project\Resources.Designer.vb') | Should Exist
                (Join-Path $packageUnzipLocation 'src\NBuildKit.Test.VbNet.Library\My%20Project\Settings.Designer.vb') | Should Exist
                (Join-Path $packageUnzipLocation 'src\NBuildKit.Test.VbNet.Library\HelloWorld.vb') | Should Exist
            }

            It 'has files with the right metadata' {
                [Reflection.AssemblyName]::GetAssemblyName($assemblyFile).Version | Should Be '1.2.0.0'

                $file = [System.IO.FileInfo]$assemblyFile
                $file.VersionInfo.FileVersion | Should Be '1.2.3.0'
                $file.VersionInfo.ProductVersion | Should Be '1.2.3+0'

                $file.VersionInfo.ProductName | Should Be 'nBuildKit.Test.VbNet.Library'
                $file.VersionInfo.LegalCopyright | Should Be "Copyright (c) - My Company 2015 - $((Get-Date).Year). All rights reserved."
            }
        }
    }

    Context 'the build produces an archive package' {
        $archive = Join-Path $workspaceLocation 'build\deploy\nBuildKit.Test.VbNet-1.2.3.zip'

        It 'in the expected location' {
            $archive | Should Exist
        }

        if (Test-Path $archive)
        {
            # extract the package
            $packageUnzipLocation = Join-Path $workspaceLocation 'build\temp\unzip\archive'
            if (-not (Test-Path $packageUnziplocation))
            {
                New-Item -Path $packageUnzipLocation -ItemType Directory | Out-Null
            }
            [System.IO.Compression.ZipFile]::ExtractToDirectory($archive, $packageUnzipLocation)

            It 'with the expected files' {
                $consoleLocation = Join-Path $packageUnzipLocation 'console'
                (Join-Path $consoleLocation 'autofac.dll') | Should Exist
                (Join-Path $consoleLocation 'log4net.dll') | Should Exist
                (Join-Path $consoleLocation 'Lokad.ActionPolicy.dll') | Should Exist
                (Join-Path $consoleLocation 'Lokad.Logging.dll') | Should Exist
                (Join-Path $consoleLocation 'Lokad.Quality.dll') | Should Exist
                (Join-Path $consoleLocation 'Lokad.Shared.dll') | Should Exist
                (Join-Path $consoleLocation 'Lokad.Stack.dll') | Should Exist
                (Join-Path $consoleLocation 'Lokad.Testing.dll') | Should Exist
                (Join-Path $consoleLocation 'Mono.Cecil.dll') | Should Exist
                (Join-Path $consoleLocation 'Mono.Cecil.Mdb.dll') | Should Exist
                (Join-Path $consoleLocation 'Mono.Cecil.Pdb.dll') | Should Exist
                (Join-Path $consoleLocation 'Mono.Cecil.Rocks.dll') | Should Exist
                (Join-Path $consoleLocation 'NBuildKit.Test.VbNet.Console.exe') | Should Exist
                (Join-Path $consoleLocation 'NBuildKit.Test.VbNet.Console.pdb') | Should Exist
                (Join-Path $consoleLocation 'NBuildKit.Test.VbNet.Library.dll') | Should Exist
                (Join-Path $consoleLocation 'NBuildKit.Test.VbNet.Library.pdb') | Should Exist

                $wpfLocation = Join-Path $packageUnzipLocation 'wpf'
                (Join-Path $wpfLocation 'autofac.dll') | Should Exist
                (Join-Path $wpfLocation 'log4net.dll') | Should Exist
                (Join-Path $wpfLocation 'Lokad.ActionPolicy.dll') | Should Exist
                (Join-Path $wpfLocation 'Lokad.Logging.dll') | Should Exist
                (Join-Path $wpfLocation 'Lokad.Quality.dll') | Should Exist
                (Join-Path $wpfLocation 'Lokad.Shared.dll') | Should Exist
                (Join-Path $wpfLocation 'Lokad.Stack.dll') | Should Exist
                (Join-Path $wpfLocation 'Lokad.Testing.dll') | Should Exist
                (Join-Path $wpfLocation 'Mono.Cecil.dll') | Should Exist
                (Join-Path $wpfLocation 'Mono.Cecil.Mdb.dll') | Should Exist
                (Join-Path $wpfLocation 'Mono.Cecil.Pdb.dll') | Should Exist
                (Join-Path $wpfLocation 'Mono.Cecil.Rocks.dll') | Should Exist
                (Join-Path $wpfLocation 'NBuildKit.Test.VbNet.Wpf.exe') | Should Exist
                (Join-Path $wpfLocation 'NBuildKit.Test.VbNet.Wpf.pdb') | Should Exist
                (Join-Path $wpfLocation 'NBuildKit.Test.VbNet.Library.dll') | Should Exist
                (Join-Path $wpfLocation 'NBuildKit.Test.VbNet.Library.pdb') | Should Exist
            }
        }
    }

    Context 'the deploy executes successfully' {
        $msBuildProperties = @{
            "FileEnvironment" = (Join-Path $workspaceLocation 'environment.props')
            'NBuildKitMinimumVersion' = $nbuildkitminimumversion
            'NBuildKitMaximumVersion' = $nbuildkitmaximumversion
            'LocalNuGetRepository' = $localNuGetFeed
            'ArtifactsServerPath' = $artefactsPath
            'NugetFeedPath' = $nugetPath
            'SymbolServerPath' = $symbolsPath
        }

        $exitCode = Invoke-MsBuildFromCommandLine `
            -scriptToExecute (Join-Path $workspaceLocation 'entrypoint.msbuild') `
            -target 'deploy' `
            -properties $msBuildProperties `
            -logPath (Join-Path $logLocation 'test.latest.vbnet.deploy.log') `
            -Verbose

        $hasBuild = ($exitCode -eq 0)
        It 'and completes with a zero exit code' {
            $exitCode | Should Be 0
        }
    }

    Context 'the deploy pushed to the nuget feed' {
        It 'pushed the nuget package' {
            (Join-Path $nugetPath 'nBuildKit.Test.VbNet.Library.1.2.3.nupkg') | Should Exist
        }
    }

    Context 'the deploy pushed to the symbol store' {
        It 'pushed the symbol package' {
            (Join-Path $symbolsPath 'nBuildKit.Test.VbNet.Library.1.2.3.symbols.nupkg') | Should Exist
        }
    }

    Context 'the deploy pushed to the file system' {
        It 'pushed the archive' {
            (Join-Path $artefactsPath 'nBuildKit.Test.VbNet\1.2.3\nBuildKit.Test.VbNet-1.2.3.zip') | Should Exist
        }
    }
}