function AppendTo-ReadMe
{
    [CmdletBinding()]
    param(
        [string] $text,

        [string] $commitMessage,

        # The local location where the workspace for the current test can be placed
        [string] $workspaceLocation
    )

    $originalWorkingDirectory = $pwd
    try
    {
        Set-Location $workspaceLocation

        Add-Content -Path (Join-Path $workspaceLocation 'README.md') -Value $text
        Stage-Changes -relativeFilePath 'README.md'
        New-GitCommit -Message $commitMessage

        Push-ToRemote -origin 'origin'
    }
    finally
    {
        Set-Location $originalWorkingDirectory
    }
}

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

        # The version for the release branch that should be used when finishing the gitflow on a feature branch.
        [string] $gitflowFinishingReleaseVersion = '1000.0.0',

        # The branch that should be created to run all the tests on. By default this will be a feature branch
        [string] $branchToTestOn = 'feature/test',

        # The branch from which the testing branch should be taken.
        [string] $originBranch = 'develop',

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
        -url $remoteRepositoryUrl `
        -destination $repositoryLocation `
        -bare `
        @commonParameterSwitches

    if ($activeBranch.StartsWith('feature') -or $activeBranch.StartsWith('hotfix') -or $activeBranch.StartsWith('release'))
    {
        # From the bare clone create a workspace in temp
        $tempWorkspace = Join-Path $tempLocation 'mergebranches'
        Clone-Repository `
            -url $repositoryLocation `
            -destination $tempWorkspace `
            @commonParameterSwitches

        $originalWorkingDirectory = $pwd
        try
        {
            Set-Location $tempWorkspace

            Finish-GitFlow `
                -branch $activeBranch `
                -releaseVersion $gitflowFinishingReleaseVersion `
                @commonParameterSwitches

            Push-ToRemote `
                -origin 'origin' `
                @commonParameterSwitches
        }
        finally
        {
            Set-Location $originalWorkingDirectory
        }
    }

    # Clone to create test workspace
    Clone-Repository `
        -Url $repositoryLocation `
        -Destination $workspaceLocation `
        @commonParameterSwitches

    # Create a branch from the correct parent branch
    $originalWorkingDirectory = $pwd
    try
    {
        Set-Location $workspaceLocation

        New-Branch `
            -name $branchToTestOn `
            -source $originBranch `
            @commonParameterSwitches
    }
    finally
    {
        Set-Location $originalWorkingDirectory
    }
}
