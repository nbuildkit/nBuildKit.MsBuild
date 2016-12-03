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

. (Join-Path $PSScriptRoot 'TestFunctions.Git.ps1')
. (Join-Path $PSScriptRoot 'TestFunctions.MsBuild.ps1')
. (Join-Path $PSScriptRoot 'TestFunctions.PrepareWorkspace.ps1')

Add-Type -AssemblyName System.IO.Compression.FileSystem

Describe 'For the C# test' {

    Context 'the preparation of the workspace' {
        New-Workspace `
            -Verbose

        It 'has created the local repository' {
            $repositoryLocation | Should Exist
            "$repositoryLocation\.git" | Should Exist
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
            'NBuildKitMinimumVersion' = $nbuildkitminimumversion
            'NBuildKitMaximumVersion' = $nbuildkitmaximumversion
            'DirUserSettings' = (Join-Path $workspaceLocation 'tools')
            'LocalNuGetRepository' = $localNuGetFeed
        }

        $exitCode = Invoke-MsBuildFromCommandLine `
            -scriptToExecute (Join-Path $workspaceLocation 'entrypoint.msbuild') `
            -target 'build' `
            -properties $msBuildProperties `
            -logPath (Join-Path $logLocation 'test.latest.csharp.build.log') `
            -Verbose

        $hasBuild = ($exitCode -eq 0)
        It 'and completes with a zero exit code' {
            $exitCode | Should Be 0
        }
    }

    Context 'the build produces a NuGet package' {
        $nugetPackage = Join-Path $workspaceLocation 'build\deploy\nBuildKit.Test.CSharp.Library.4.3.2-MyPreRelease1.nupkg'

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
                $nuspec = Join-Path $packageUnzipLocation 'nBuildKit.Test.CSharp.library.nuspec'
                $nuspec | Should Exist

                $xmlDoc = [xml](Get-Content $nuspec)
                $xmlDoc.package.metadata.version | Should Be '4.3.2-MyPreRelease1'
                $xmlDoc.package.metadata.releaseNotes | Should Not BeNullOrEmpty

                $dependencies = $xmlDoc.package.metadata.dependencies
                $dependencies.ChildNodes.Count | Should Be 5
                $dependencies.ChildNodes | Where-Object { $_.id -eq 'Autofac' } | Select-Object -ExpandProperty version -First 1 | Should Be '[2.2.4.900, 2.3.0)'
                $dependencies.ChildNodes | Where-Object { $_.id -eq 'log4net' } | Select-Object -ExpandProperty version -First 1 | Should Be '[1.2.10, 1.3.0)'
                $dependencies.ChildNodes | Where-Object { $_.id -eq 'Lokad.Shared' } | Select-Object -ExpandProperty version -First 1 | Should Be '[1.5.181, 1.6.0)'
                $dependencies.ChildNodes | Where-Object { $_.id -eq 'Mono.Cecil' } | Select-Object -ExpandProperty version -First 1 | Should Be '[0.9.6, 0.10.0)'
                $dependencies.ChildNodes | Where-Object { $_.id -eq 'NuGet.Versioning' } | Select-Object -ExpandProperty version -First 1 | Should Be '[3.4.4-rtm-final, 3.5.0)'
            }

            $assemblyFile = Join-Path $packageUnzipLocation 'lib\net45\NBuildKit.Test.CSharp.Library.dll'
            It 'with the expected files' {
                $assemblyFile | Should Exist
            }

            It 'has files with the right metadata' {
                [Reflection.AssemblyName]::GetAssemblyName($assemblyFile).Version | Should Be '4.0.0.0'

                $file = [System.IO.FileInfo]$assemblyFile
                $file.VersionInfo.FileVersion | Should Be '4.3.2.1'
                $file.VersionInfo.ProductVersion | Should Be '4.3.2-MyPreRelease+1'

                $file.VersionInfo.ProductName | Should Be 'nBuildKit.Test.CSharp.Library'
                $file.VersionInfo.LegalCopyright | Should Be "Copyright (c) - My Company 2015 - $((Get-Date).Year). All rights reserved."
            }
        }
    }

    Context 'the build produces a symbol package' {
        $symbolPackage = Join-Path $workspaceLocation 'build\deploy\nBuildKit.Test.CSharp.Library.4.3.2-MyPreRelease1.symbols.nupkg'

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
                $nuspec = Join-Path $packageUnzipLocation 'nBuildKit.Test.CSharp.library.nuspec'
                $nuspec | Should Exist

                $xmlDoc = [xml](Get-Content $nuspec)
                $xmlDoc.package.metadata.version | Should Be '4.3.2-MyPreRelease1'
                $xmlDoc.package.metadata.releaseNotes | Should Not BeNullOrEmpty

                $dependencies = $xmlDoc.package.metadata.dependencies
                $dependencies.ChildNodes.Count | Should Be 5
                $dependencies.ChildNodes | Where-Object { $_.id -eq 'Autofac' } | Select-Object -ExpandProperty version -First 1 | Should Be '[2.2.4.900, 2.3.0)'
                $dependencies.ChildNodes | Where-Object { $_.id -eq 'log4net' } | Select-Object -ExpandProperty version -First 1 | Should Be '[1.2.10, 1.3.0)'
                $dependencies.ChildNodes | Where-Object { $_.id -eq 'Lokad.Shared' } | Select-Object -ExpandProperty version -First 1 | Should Be '[1.5.181, 1.6.0)'
                $dependencies.ChildNodes | Where-Object { $_.id -eq 'Mono.Cecil' } | Select-Object -ExpandProperty version -First 1 | Should Be '[0.9.6, 0.10.0)'
                $dependencies.ChildNodes | Where-Object { $_.id -eq 'NuGet.Versioning' } | Select-Object -ExpandProperty version -First 1 | Should Be '[3.4.4-rtm-final, 3.5.0)'
            }

            $assemblyFile = Join-Path $packageUnzipLocation 'lib\net45\NBuildKit.Test.CSharp.Library.dll'
            It 'with the expected files' {
                $assemblyFile | Should Exist

                (Join-Path $packageUnzipLocation 'lib\net45\NBuildKit.Test.CSharp.Library.pdb') | Should Exist

                (Join-Path $packageUnzipLocation 'src\NBuildKit.Test.CSharp.Library\Properties\AssemblyInfo.cs') | Should Exist
                (Join-Path $packageUnzipLocation 'src\NBuildKit.Test.CSharp.Library\HelloWorld.cs') | Should Exist
            }

            It 'has files with the right metadata' {
                [Reflection.AssemblyName]::GetAssemblyName($assemblyFile).Version | Should Be '4.0.0.0'

                $file = [System.IO.FileInfo]$assemblyFile
                $file.VersionInfo.FileVersion | Should Be '4.3.2.1'
                $file.VersionInfo.ProductVersion | Should Be '4.3.2-MyPreRelease+1'

                $file.VersionInfo.ProductName | Should Be 'nBuildKit.Test.CSharp.Library'
                $file.VersionInfo.LegalCopyright | Should Be "Copyright (c) - My Company 2015 - $((Get-Date).Year). All rights reserved."
            }
        }
    }

    Context 'the build produces an archive package' {
        $archive = Join-Path $workspaceLocation 'build\deploy\nBuildKit.Test.CSharp-4.3.2.zip'

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
                (Join-Path $consoleLocation 'NBuildKit.Test.CSharp.Console.exe') | Should Exist
                (Join-Path $consoleLocation 'NBuildKit.Test.CSharp.Console.pdb') | Should Exist
                (Join-Path $consoleLocation 'NBuildKit.Test.CSharp.Library.dll') | Should Exist
                (Join-Path $consoleLocation 'NBuildKit.Test.CSharp.Library.pdb') | Should Exist

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
                (Join-Path $wpfLocation 'NBuildKit.Test.CSharp.Wpf.exe') | Should Exist
                (Join-Path $wpfLocation 'NBuildKit.Test.CSharp.Wpf.pdb') | Should Exist
                (Join-Path $wpfLocation 'NBuildKit.Test.CSharp.Library.dll') | Should Exist
                (Join-Path $wpfLocation 'NBuildKit.Test.CSharp.Library.pdb') | Should Exist
            }
        }
    }

    Context 'the deploy executes successfully' {
        $msBuildProperties = @{
            'NBuildKitMinimumVersion' = $nbuildkitminimumversion
            'NBuildKitMaximumVersion' = $nbuildkitmaximumversion
            'DirUserSettings' = (Join-Path $workspaceLocation 'tools')
            'LocalNuGetRepository' = $localNuGetFeed
            'ArtifactsServerPath' = $artefactsPath
            'NugetFeedPath' = $nugetPath
            'SymbolServerPath' = $symbolsPath
        }

        $exitCode = Invoke-MsBuildFromCommandLine `
            -scriptToExecute (Join-Path $workspaceLocation 'entrypoint.msbuild') `
            -target 'deploy' `
            -properties $msBuildProperties `
            -logPath (Join-Path $logLocation 'test.latest.csharp.deploy.log') `
            -Verbose

        $hasBuild = ($exitCode -eq 0)
        It 'and completes with a zero exit code' {
            $exitCode | Should Be 0
        }
    }

    Context 'the deploy pushed to the nuget feed' {
        It 'pushed the nuget package' {
            (Join-Path $nugetPath 'nBuildKit.Test.CSharp.Library.4.3.2-MyPreRelease1.nupkg') | Should Exist
        }
    }

    Context 'the deploy pushed to the symbol store' {
        It 'pushed the symbol package' {
            (Join-Path $symbolsPath 'nBuildKit.Test.CSharp.Library.4.3.2-MyPreRelease1.symbols.nupkg') | Should Exist
        }
    }

    Context 'the deploy pushed to the file system' {
        It 'pushed the archive' {
            (Join-Path $artefactsPath 'nBuildKit.Test.CSharp\4.3.2\nBuildKit.Test.CSharp-4.3.2.zip') | Should Exist
        }
    }
}