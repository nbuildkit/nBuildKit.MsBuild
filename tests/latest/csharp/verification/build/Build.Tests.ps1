<#
    This file contains the 'verification tests' for the 'C# build' section of the nbuildkit verification test suite.
    These tests are executed using Pester (https://github.com/pester/Pester).
#>

param(
    [Parameter(Mandatory)]
    [string] $workspaceLocation
)

function Invoke-NuGetFromCommandLine
{
    [CmdletBinding()]
    param(
        [string] $arguments
    )

    $nugetExe = 'c:\meta\consul\checks\nuget.exe'

    $command = '& "' + $nugetExe + '" ' + $arguments + ' 2>&1'

    try
    {
        $output = Invoke-Expression -Command $command
    }
    catch
    {
        # just ignore that ...
        if ($output -eq '')
        {
            $output = "nuget.exe $arguments failed!"
        }
    }

    $result = New-Object psobject
    Add-Member -InputObject $result -MemberType NoteProperty -Name ExitCode -Value $LastExitCode
    Add-Member -InputObject $result -MemberType NoteProperty -Name Output -Value $output

    return $result
}

Describe 'The C# build process' {
    Context 'produced a NuGet package' {
        $nugetPackage = Join-Path $workspaceLocation 'build\deploy\nBuildKit.Test.CSharp.Library.4.3.2.nupkg'
        $symbolPackage = Join-Path $workspaceLocation 'build\deploy\nBuildKit.Test.CSharp.Library.4.3.2.symbols.nupkg'

        It 'in the expected location' {
            $package | Should Exist
            $symbolPackage | Should Exist
        }

        if (Test-Path $nugetPackage)
        {
            # extract the package
            $packageUnzipLocation = Join-Path $workspaceLocation 'build\temp\unzip\nuget'
            [System.IO.Compression.ZipFile]::ExtractToDirectory($nugetPackage, $packageUnzipLocation)

            It 'with the expected metadata' {
                # Version
                # releasenotes
                # dependencies
            }

            It 'with the expected files' {
                # nuget: dll, xml
                # symbols: dll, pdb, xml, src
            }

            It 'has files with the right metadata' {
                # dll -> version
            }
        }
    }

    Context 'produced an archive package' {

    }
}