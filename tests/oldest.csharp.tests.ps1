<#
    This file contains the 'verification tests' for the 'C# build' section of the nbuildkit verification test suite.
    These tests are executed using Pester (https://github.com/pester/Pester).
#>

param(
    [Parameter(Mandatory)]
    [string] $nbuildkitminimumversion,

    [Parameter(Mandatory)]
    [string] $nbuildkitmaximumversion,

    [Parameter(Mandatory)]
    [string] $projectWorkspaceLocation,

    [Parameter(Mandatory)]
    [string] $testOutputLocation,

    [Parameter(Mandatory)]
    [string] $testWorkspaceLocation
)

. (Join-Path $PSScriptRoot 'TestFunctions.MsBuild.ps1')

Add-Type -AssemblyName System.IO.Compression.FileSystem

Describe 'For the C# test' {

    Context 'the build executes successfully' {
        $msBuildProperties = @{
            'NBuildKitMinimumVersion' = $nbuildkitminimumversion
            'NBuildKitMaximumVersion' = $nbuildkitmaximumversion
            'DirUserSettings' = (Join-Path $testWorkspaceLocation 'tools')
        }

        $exitCode = Invoke-MsBuildFromCommandLine `
            -scriptToExecute (Join-Path $testWorkspaceLocation 'entrypoint.msbuild') `
            -target 'build' `
            -properties $msBuildProperties `
            -logPath (Join-Path $projectWorkspaceLocation 'build\logs\test.oldest.csharp.build.log') `
            -Verbose

        $hasBuild = ($exitCode -eq 0)
        It 'and completes with a zero exit code' {
            $exitCode | Should Be 0
        }
    }

    Context 'the build produces a NuGet package' {
        $nugetPackage = Join-Path $testWorkspaceLocation 'build\deploy\nBuildKit.Test.CSharp.Library.4.3.2-MyPreRelease1.nupkg'

        It 'in the expected location' {
            $nugetpackage | Should Exist
        }

        if (Test-Path $nugetPackage)
        {
            # extract the package
            $packageUnzipLocation = Join-Path $testWorkspaceLocation 'build\temp\unzip\nuget'
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
        $symbolPackage = Join-Path $testWorkspaceLocation 'build\deploy\nBuildKit.Test.CSharp.Library.4.3.2-MyPreRelease1.symbols.nupkg'

        It 'in the expected location' {
            $symbolPackage | Should Exist
        }

        if (Test-Path $symbolPackage)
        {
            # extract the package
            $packageUnzipLocation = Join-Path $testWorkspaceLocation 'build\temp\unzip\symbols'
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
        $archive = Join-Path $testWorkspaceLocation 'build\deploy\nBuildKit.Test.CSharp-4.3.2.zip'

        It 'in the expected location' {
            $archive | Should Exist
        }

        if (Test-Path $archive)
        {
            # extract the package
            $packageUnzipLocation = Join-Path $testWorkspaceLocation 'build\temp\unzip\archive'
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
            'DirUserSettings' = (Join-Path $testWorkspaceLocation 'tools')
        }

        $exitCode = Invoke-MsBuildFromCommandLine `
            -scriptToExecute (Join-Path $testWorkspaceLocation 'entrypoint.msbuild') `
            -target 'deploy' `
            -properties $msBuildProperties `
            -logPath (Join-Path $projectWorkspaceLocation 'build\logs\test.oldest.csharp.deploy.log') `
            -Verbose

        $hasBuild = ($exitCode -eq 0)
        It 'and completes with a zero exit code' {
            $exitCode | Should Be 0
        }
    }

    Context 'the deploy pushed to the nuget feed' {
        It 'pushed the nuget package' {
            (Join-Path $projectWorkspaceLocation 'build\temp\tests\oldest\csharp\nuget\nBuildKit.Test.CSharp.Library.4.3.2-MyPreRelease1.nupkg') | Should Exist
        }
    }

    Context 'the deploy pushed to the symbol store' {
        It 'pushed the symbol package' {
            (Join-Path $projectWorkspaceLocation 'build\temp\tests\oldest\csharp\symbols\nBuildKit.Test.CSharp.Library.4.3.2-MyPreRelease1.symbols.nupkg') | Should Exist
        }
    }

    Context 'the deploy pushed to the file system' {
        It 'pushed the archive' {
            (Join-Path $projectWorkspaceLocation 'build\temp\tests\oldest\csharp\artifacts\nBuildKit.Test.CSharp\4.3.2\nBuildKit.Test.CSharp-4.3.2.zip') | Should Exist
        }
    }
}