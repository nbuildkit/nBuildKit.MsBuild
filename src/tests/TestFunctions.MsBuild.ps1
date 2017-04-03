<#
    .SYNOPSIS

    Locates the most recent version of MsBuild.exe


    .DESCRIPTION

    The Get-MsBuildPath function returns the path of the latest version of MsBuild.exe


    .PARAMETER use32BitMsBuild

    A flag that indicates whether or not the 32-bit version of MsBuild should be prefered.
#>
function Get-MsBuildPath
{
    [CmdletBinding()]
    param(
        [switch] $use32BitMsBuild
    )

    $registryPathToMsBuildToolsVersions = 'HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\'
    if ($use32BitMsBuild)
    {
        # If the 32-bit path exists, use it, otherwise stick with the current path (which will be the 64-bit path on
        # 64-bit machines, and the 32-bit path on 32-bit machines).
        $registryPathTo32BitMsBuildToolsVersions = 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\MSBuild\ToolsVersions\'
        if (Test-Path -Path $registryPathTo32BitMsBuildToolsVersions)
        {
            $registryPathToMsBuildToolsVersions = $registryPathTo32BitMsBuildToolsVersions
        }
    }

    # Get the path to the directory that the latest version of MsBuild is in.
    $msBuildToolsVersionsStrings = Get-ChildItem -Path $registryPathToMsBuildToolsVersions |
        Where-Object { $_ -match '[0-9]+\.[0-9]' } |
        Select-Object -ExpandProperty PsChildName

    $msBuildToolsVersions = @{}
    $msBuildToolsVersionsStrings |
        ForEach-Object {
            $msBuildToolsVersions.Add($_ -as [double], $_)
        }

    $largestMsBuildToolsVersion = ($msBuildToolsVersions.GetEnumerator() |
        Sort-Object -Descending -Property Name |
        Select-Object -First 1).Value

    $registryPathToMsBuildToolsLatestVersion = Join-Path -Path $registryPathToMsBuildToolsVersions -ChildPath ("{0:n1}" -f $largestMsBuildToolsVersion)
    $msBuildToolsVersionsKeyToUse = Get-Item -Path $registryPathToMsBuildToolsLatestVersion
    $msBuildDirectoryPath = $msBuildToolsVersionsKeyToUse |
        Get-ItemProperty -Name 'MSBuildToolsPath' |
        Select -ExpandProperty 'MSBuildToolsPath'

    if (($msBuildDirectoryPath -eq $null) -or ($msBuildDirectoryPath -eq ''))
    {
        throw 'The registry on this system does not appear to contain the path to the MsBuild.exe directory.'
    }

    # Get the path to the MsBuild executable.
    $msBuildPath = Join-Path -Path $msBuildDirectoryPath -ChildPath 'msbuild.exe'
    if(-not (Test-Path $msBuildPath -PathType Leaf))
    {
        throw "MsBuild.exe was not found on this system at the path specified in the registry, '$msBuildPath'."
    }

    return $msBuildPath
}

<#
    .SYNOPSIS

    Invokes the latest version of MsBuild.exe with the given parameters.


    .DESCRIPTION

    The Invoke-MsBuildFromCommandLine function invokes the latest version of MsBuild.exe with the given parameters.


    .PARAMETER scriptToExecute

    The full path to the MsBuild script that should be executed.


    .PARAMETER target

    The target that should be executed.


    .PARAMETER properties

    The hash table that maps property names to command line property values.


    .PARAMETER logPath

    The full path to the log file.


    .EXAMPLE

    Invoke-MsBuildFromCommandLine `
        -scriptToExecute 'c:\temp\myscript.msbuild' `
        -target 'build' `
        -properties @{ 'myproperty1' = 'value1'; 'myproperty2' = 'value2' } `
        -logPath 'c:\temp\logs'
#>
function Invoke-MsBuildFromCommandLine
{
    [CmdletBinding()]
    param(
        [string] $scriptToExecute,
        [string] $target,
        [hashtable] $properties,
        [string] $logPath
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    $propertiesAsString = ''
    foreach($map in $properties.GetEnumerator())
    {
        $propertiesAsString += "/p:$($map.Key)=`"$($map.Value)`" "
    }

    $msBuildPath = Get-MsBuildPath @commonParameterSwitches

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $msBuildPath
    $startInfo.Arguments = "$($scriptToExecute) /t:$($target) $($propertiesAsString) /flp:LogFile=`"$($logPath)`";Verbosity=diagnostic /noconsolelogger /nologo"
    $startInfo.WorkingDirectory = $workspaceLocation
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true

    Write-Verbose "Starting $($startInfo.FileName) with arguments $($startInfo.Arguments)"
    $process = New-Object System.Diagnostics.Process

    $exitCode = -1

    # Adding event handers for stdout and stderr.
    $scripBlock = {
        if (![String]::IsNullOrEmpty($EventArgs.Data))
        {
            Write-Host $EventArgs.Data
        }
    }

    $stdOutEvent = Register-ObjectEvent `
        -InputObject $process `
        -Action $scripBlock `
        -EventName 'OutputDataReceived'
    $stdErrEvent = Register-ObjectEvent `
        -InputObject $process `
        -Action $scripBlock `
        -EventName 'ErrorDataReceived'
    try
    {
        $process.StartInfo = $startInfo

        $process.Start() | Out-Null
        $process.BeginOutputReadLine()
        $process.BeginErrorReadLine()

        $process.WaitForExit()
        $exitCode = $process.ExitCode
    }
    finally
    {
        $process.Dispose()

        Unregister-Event -SourceIdentifier $stdOutEvent.Name
        Unregister-Event -SourceIdentifier $stdErrEvent.Name
    }

    return $exitCode
}
