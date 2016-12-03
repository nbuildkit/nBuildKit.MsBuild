function New-Workspace
{
    [CmdletBinding()]
    param(
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

        # A temporary directory that can be used for the current test
        [string] $tempLocation
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    # Create a bare clone of the repository
    Clone-Repository `
        -Url $remoteRepositoryUrl `
        -Destination $repositoryLocation `
        -Bare `
        -Verbose

    if ($activeBranch.StartsWith('feature') -or $activeBranch.StartsWith('hotfix') -or $activeBranch.StartsWith('release'))
    {
        # From the bare clone create a workspace in temp
        $tempWorkspace = Join-Path $tempLocation 'mergebranches'
        Clone-Repository `
            -url $repositoryLocation `
            -destination $tempWorkspace `
            -Verbose

        $originalWorkingDirectory = $pwd
        try
        {
            Set-Location $tempWorkspace

            Finish-GitFlow `
                -branch $activeBranch `
                @commonParameterSwitches

            Push-ToRemote `
                -origin 'origin' `
                -Verbose
        }
        finally
        {
            Set-Location $originalWorkingDirectory
        }
    }

    # Clone to create test workspace
    Clone-Repository `
        -Url $remoteRepositoryUrl `
        -Destination $workspaceLocation `
        -Verbose

    # Create a branch from the correct parent branch
    $originalWorkingDirectory = $pwd
    try
    {
        Set-Location $workspaceLocation

        New-Branch `
            -name 'feature/test-nbuildkit' `
            -source 'develop' `
            @commonParameterSwitches
    }
    finally
    {
        Set-Location $originalWorkingDirectory
    }
}