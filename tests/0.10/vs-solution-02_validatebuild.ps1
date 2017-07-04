[CmdletBinding()]
param(
    # The minimum version of nBuildKit that should be used for the test
    [string] $repositoryVersion,

    # The maximum version of nBuildKit that should be used for the test
    [string] $releaseVersion,

    # The URL of the remote repository that contains the test code. This repository will be mirror cloned into
    # a local repository so that the tests can push to the repository without destroying the original
    [string] $remoteRepositoryUrl,

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
    [string] $tempLocation,

    # The branch on which the test is executed.
    [string] $branchToTestOn
)

$testId = 'vs-solution-02'
$testWorkspaceLocation = Join-Path $workspaceLocation $testId

$assemblyVersion = "$(([System.Version]$releaseVersion).Major).0.0.0"
$assemblyFileVersion = "$(([System.Version]$releaseVersion).Major).0.0.0"

$languages = @('CSharp', 'VbNet')
foreach($language in $languages)
{
    if ($language.ToLowerInvariant() -eq 'csharp')
    {
        $nugetVersion = "$($releaseVersion)-rtm"
        Context "the build produces NuGet packages for the $($language) project" {
            $nugetPackage = Join-Path $testWorkspaceLocation "build\deploy\nBuildKit.Test.$($language).Library.$($nugetVersion).nupkg"

            It 'in the expected location' {
                $nugetpackage | Should Exist
            }

            if (Test-Path $nugetPackage)
            {
                $packageUnzipLocation = Join-Path $testWorkspaceLocation "build\temp\unzip\nuget\$($language)"
                if (-not (Test-Path $packageUnziplocation))
                {
                    New-Item -Path $packageUnzipLocation -ItemType Directory | Out-Null
                }
                [System.IO.Compression.ZipFile]::ExtractToDirectory($nugetPackage, $packageUnzipLocation)

                It 'with the expected metadata' {
                    $nuspec = Join-Path $packageUnzipLocation "nBuildKit.Test.$($language.ToLowerInvariant()).Library.nuspec"
                    $nuspec | Should Exist

                    $xmlDoc = [xml](Get-Content $nuspec)
                    $xmlDoc.package.metadata.version | Should Be $nugetVersion
                    $xmlDoc.package.metadata.releaseNotes | Should BeNullOrEmpty

                    $dependencies = $xmlDoc.package.metadata.dependencies
                    $dependencies.ChildNodes.Count | Should Be 5
                    $dependencies.ChildNodes | Where-Object { $_.id -eq 'Autofac' } | Select-Object -ExpandProperty version -First 1 | Should Be '[2.2.4.900, 2.3.0)'
                    $dependencies.ChildNodes | Where-Object { $_.id -eq 'log4net' } | Select-Object -ExpandProperty version -First 1 | Should Be '[1.2.10, 1.3.0)'
                    $dependencies.ChildNodes | Where-Object { $_.id -eq 'Lokad.Shared' } | Select-Object -ExpandProperty version -First 1 | Should Be '[1.5.181, 1.6.0)'
                    $dependencies.ChildNodes | Where-Object { $_.id -eq 'Mono.Cecil' } | Select-Object -ExpandProperty version -First 1 | Should Be '[0.9.6, 0.10.0)'
                    $dependencies.ChildNodes | Where-Object { $_.id -eq 'NuGet.Versioning' } | Select-Object -ExpandProperty version -First 1 | Should Be '[3.4.4-rtm-final, 3.5.0)'
                }

                $assemblyFile = Join-Path $packageUnzipLocation "lib\net45\nBuildKit.Test.$($language).Library.dll"
                It 'with the expected files' {
                    $assemblyFile | Should Exist
                }

                It 'has files with the right metadata' {
                    [Reflection.AssemblyName]::GetAssemblyName($assemblyFile).Version | Should Be $assemblyVersion

                    $file = [System.IO.FileInfo]$assemblyFile
                    $file.VersionInfo.FileVersion | Should Be $assemblyFileVersion
                    $file.VersionInfo.ProductVersion | Should Be "$($releaseVersion)+0"

                    $file.VersionInfo.ProductName | Should Be "nBuildKit.Test.$($language).Library"
                    $file.VersionInfo.CompanyName | Should Be "My Company"
                    $file.VersionInfo.LegalCopyright | Should Be "Copyright (c) - My Company 2015 - $((Get-Date).Year). All rights reserved."
                }
            }
        }

        Context "the build produces symbol packages for the $($language) project" {
            $symbolPackage = Join-Path $testWorkspaceLocation "build\deploy\nBuildKit.Test.$($language).Library.$($nugetVersion).symbols.nupkg"

            It 'in the expected location' {
                $symbolPackage | Should Exist
            }

            if (Test-Path $symbolPackage)
            {
                # extract the package
                $packageUnzipLocation = Join-Path $testWorkspaceLocation "build\temp\unzip\symbols\$($language)"
                if (-not (Test-Path $packageUnziplocation))
                {
                    New-Item -Path $packageUnzipLocation -ItemType Directory | Out-Null
                }
                [System.IO.Compression.ZipFile]::ExtractToDirectory($symbolPackage, $packageUnzipLocation)

                It 'with the expected metadata' {
                    $nuspec = Join-Path $packageUnzipLocation "nBuildKit.Test.$($language.ToLowerInvariant()).Library.nuspec"
                    $nuspec | Should Exist

                    $xmlDoc = [xml](Get-Content $nuspec)
                    $xmlDoc.package.metadata.version | Should Be $nugetVersion

                    $dependencies = $xmlDoc.package.metadata.dependencies
                    $dependencies.ChildNodes.Count | Should Be 5
                    $dependencies.ChildNodes | Where-Object { $_.id -eq 'Autofac' } | Select-Object -ExpandProperty version -First 1 | Should Be '[2.2.4.900, 2.3.0)'
                    $dependencies.ChildNodes | Where-Object { $_.id -eq 'log4net' } | Select-Object -ExpandProperty version -First 1 | Should Be '[1.2.10, 1.3.0)'
                    $dependencies.ChildNodes | Where-Object { $_.id -eq 'Lokad.Shared' } | Select-Object -ExpandProperty version -First 1 | Should Be '[1.5.181, 1.6.0)'
                    $dependencies.ChildNodes | Where-Object { $_.id -eq 'Mono.Cecil' } | Select-Object -ExpandProperty version -First 1 | Should Be '[0.9.6, 0.10.0)'
                    $dependencies.ChildNodes | Where-Object { $_.id -eq 'NuGet.Versioning' } | Select-Object -ExpandProperty version -First 1 | Should Be '[3.4.4-rtm-final, 3.5.0)'
                }

                $assemblyFile = Join-Path $packageUnzipLocation "lib\net45\nBuildKit.Test.$($language).Library.dll"
                It 'with the expected files' {
                    $assemblyFile | Should Exist

                    (Join-Path $packageUnzipLocation "lib\net45\nBuildKit.Test.$($language).Library.pdb") | Should Exist
                }

                It 'has files with the right metadata' {
                    [Reflection.AssemblyName]::GetAssemblyName($assemblyFile).Version | Should Be $assemblyVersion

                    $file = [System.IO.FileInfo]$assemblyFile
                    $file.VersionInfo.FileVersion | Should Be $assemblyFileVersion
                    $file.VersionInfo.ProductVersion | Should Be "$($releaseVersion)+0"

                    $file.VersionInfo.ProductName | Should Be "nBuildKit.Test.$($language).Library"
                    $file.VersionInfo.CompanyName | Should Be "My Company"
                    $file.VersionInfo.LegalCopyright | Should Be "Copyright (c) - My Company 2015 - $((Get-Date).Year). All rights reserved."
                }
            }
        }
    }

    if ($language.ToLowerInvariant() -eq 'vbnet')
    {
        $nugetVersion = $releaseVersion
        Context "the build produces NuGet packages for the $($language) project" {
            $nugetPackage = Join-Path $testWorkspaceLocation "build\deploy\nBuildKit.Test.$($language).Library.$($nugetVersion).nupkg"

            It 'in the expected location' {
                $nugetpackage | Should Exist
            }

            if (Test-Path $nugetPackage)
            {
                $packageUnzipLocation = Join-Path $testWorkspaceLocation "build\temp\unzip\nuget\$($language)"
                if (-not (Test-Path $packageUnziplocation))
                {
                    New-Item -Path $packageUnzipLocation -ItemType Directory | Out-Null
                }
                [System.IO.Compression.ZipFile]::ExtractToDirectory($nugetPackage, $packageUnzipLocation)

                It 'with the expected metadata' {
                    $nuspec = Join-Path $packageUnzipLocation "nBuildKit.Test.$($language.ToLowerInvariant()).Library.nuspec"
                    $nuspec | Should Exist

                    $xmlDoc = [xml](Get-Content $nuspec)
                    $xmlDoc.package.metadata.version | Should Be $nugetVersion
                    $xmlDoc.package.metadata.releaseNotes | Should BeNullOrEmpty

                    $dependencies = $xmlDoc.package.metadata.dependencies
                    $dependencies.ChildNodes.Count | Should Be 0
                }

                $assemblyFile = Join-Path $packageUnzipLocation "lib\net45\nBuildKit.Test.$($language).Library.dll"
                It 'with the expected files' {
                    $assemblyFile | Should Exist
                }

                It 'has files with the right metadata' {
                    [Reflection.AssemblyName]::GetAssemblyName($assemblyFile).Version | Should Be $assemblyVersion

                    $file = [System.IO.FileInfo]$assemblyFile
                    $file.VersionInfo.FileVersion | Should Be $assemblyFileVersion
                    $file.VersionInfo.ProductVersion | Should Be "$($releaseVersion)+0"

                    $file.VersionInfo.ProductName | Should Be "nBuildKit.Test.$($language).Library"
                    $file.VersionInfo.CompanyName | Should Be "My Company"
                    $file.VersionInfo.LegalCopyright | Should Be "Copyright (c) - My Company 2015 - $((Get-Date).Year). All rights reserved."
                }
            }
        }

        Context "the build produces symbol packages for the $($language) project" {
            $symbolPackage = Join-Path $testWorkspaceLocation "build\deploy\nBuildKit.Test.$($language).Library.$($nugetVersion).symbols.nupkg"

            It 'in the expected location' {
                $symbolPackage | Should Exist
            }

            if (Test-Path $symbolPackage)
            {
                # extract the package
                $packageUnzipLocation = Join-Path $testWorkspaceLocation "build\temp\unzip\symbols\$($language)"
                if (-not (Test-Path $packageUnziplocation))
                {
                    New-Item -Path $packageUnzipLocation -ItemType Directory | Out-Null
                }
                [System.IO.Compression.ZipFile]::ExtractToDirectory($symbolPackage, $packageUnzipLocation)

                It 'with the expected metadata' {
                    $nuspec = Join-Path $packageUnzipLocation "nBuildKit.Test.$($language.ToLowerInvariant()).Library.nuspec"
                    $nuspec | Should Exist

                    $xmlDoc = [xml](Get-Content $nuspec)
                    $xmlDoc.package.metadata.version | Should Be $nugetVersion

                    $dependencies = $xmlDoc.package.metadata.dependencies
                    $dependencies.ChildNodes.Count | Should Be 0
                }

                $assemblyFile = Join-Path $packageUnzipLocation "lib\net45\nBuildKit.Test.$($language).Library.dll"
                It 'with the expected files' {
                    $assemblyFile | Should Exist

                    (Join-Path $packageUnzipLocation "lib\net45\nBuildKit.Test.$($language).Library.pdb") | Should Exist
                }

                It 'has files with the right metadata' {
                    [Reflection.AssemblyName]::GetAssemblyName($assemblyFile).Version | Should Be $assemblyVersion

                    $file = [System.IO.FileInfo]$assemblyFile
                    $file.VersionInfo.FileVersion | Should Be $assemblyFileVersion
                    $file.VersionInfo.ProductVersion | Should Be "$($releaseVersion)+0"

                    $file.VersionInfo.ProductName | Should Be "nBuildKit.Test.$($language).Library"
                    $file.VersionInfo.CompanyName | Should Be "My Company"
                    $file.VersionInfo.LegalCopyright | Should Be "Copyright (c) - My Company 2015 - $((Get-Date).Year). All rights reserved."
                }
            }
        }
    }
}
