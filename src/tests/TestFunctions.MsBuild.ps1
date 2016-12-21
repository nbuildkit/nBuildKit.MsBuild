function Get-MsBuildPath
{
    [CmdletBinding()]
    param()

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    Write-Verbose "Searching for msbuild.exe ..."

    $windows = [Environment]::GetFolderPath([Environment+SpecialFolder]::Windows)
    $programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)

    $potentialMsBuildPaths = @(
        "$programFilesX86\MSBuild\15.0\Bin\amd64\msbuild.exe",
        "$programFilesX86\MSBuild\15.0\Bin\msbuild.exe",
        "$programFilesX86\MSBuild\14.0\Bin\amd64\msbuild.exe",
        "$programFilesX86\MSBuild\14.0\Bin\msbuild.exe",
        "$programFilesX86\MSBuild\12.0\Bin\amd64\msbuild.exe",
        "$programFilesX86\MSBuild\12.0\Bin\msbuild.exe",
        "$windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe",
        "$windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"
    )

    foreach($path in $potentialMsBuildPaths)
    {
        if (Test-Path $path)
        {
            Write-Verbose "Found msbuild.exe at: $path"
            return $path
        }
    }

    throw "Could not locate msbuild.exe"
}

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