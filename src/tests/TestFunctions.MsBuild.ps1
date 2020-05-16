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
    param()

    $vswherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    $msBuildPath = & $vswherePath -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | select-object -first 1

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
