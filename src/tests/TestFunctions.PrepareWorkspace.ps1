<#
    .SYNOPSIS

    Creates a new commit by updating the given file with the given text.


    .DESCRIPTION

    The Add-CommitToCurrentBranch function adds the given content to the selected file, commits the change and then
    pushes the change to the 'origin' remote repository.


    .PARAMETER relativeFilePath

    The relative file path to the file that should be updated.


    .PARAMETER text

    The text that should be added to the file.


    .PARAMETER commitMessage

    The text that should be provided as the commit message.


    .PARAMETER workspaceLocation

    The full path to the location of the workspace.


    .EXAMPLE

    Add-CommitToCurrentBranch `
        -relativeFilePath 'README.md' `
        -text 'This is an update' `
        -commitMessage 'Updated the readme' `
        -workspaceLocation 'c:\temp\myworkspace'
#>
function Add-CommitToCurrentBranch
{
    [CmdletBinding()]
    param(
        [string] $relativeFilePath,
        [string] $text,
        [string] $commitMessage,
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

<#
    .SYNOPSIS

    Creates a new workspace from a given repository.


    .DESCRIPTION

    The New-Workspace function clones a given repository and creates a workspace based on the cloned repository
    for executing tests in.


    .PARAMETER remoteRepositoryUrl

    The URL of the remote repository that contains the test code. This repository will be mirror cloned into
    a local bare repository so that the tests can push to the repository without destroying the original


    .PARAMETER activeBranch

    The active branch from which the code should be taken. This branch will be merged into develop / master
    according to the gitversion rules in the cloned remote repository. From there the test can make changes
    to the repository


    .PARAMETER gitflowFinishingReleaseVersion

    The version for the release branch that should be used when finishing the gitflow on a feature branch.


    .PARAMETER branchToTestOn

    The branch that should be created to run all the tests on. By default this will be a feature branch


    .PARAMETER originBranch

    The branch from which the testing branch should be taken.


    .PARAMETER repositoryLocation

    The local location where the cloned repository can be placed


    .PARAMETER workspaceLocation

    The local location where the workspace for the current test can be placed


    .PARAMETER tempLocation

    A temporary directory that can be used for the current test


    .EXAMPLE

    The following command creates a bare clone of the 'Test.Integration.Latest.MsBuild.CSharp' repository
    in the 'c:\repository' directory. It then creates a temporary workspace in 'c:\temp\mergebranches'
    and completes the GitFlow process starting at the 'feature/myfeature' branch by merging the feature
    branch into develop, then creating a 'release/1.2.0' branch and merging that into master. The results
    of the merges are pushed to the bare clone in the 'c:\repository' directory.

    Finally the actual test workspace is created by cloning from the 'c:\repository' directory into the
    'c:\workspace' directory.

    New-Workspace `
        -remoteRepositoryUrl 'http://github.com/nbuildkit/Test.Integration.Latest.MsBuild.CSharp' `
        -activeBranch 'feature/myfeature' `
        -gitflowFinishingReleaseVersion '1.2.0' `
        -branchToTestOn 'hotfix/1.2.1' `
        -originBranch 'master' `
        -repositoryLocation 'c:\repository' `
        -workspaceLocation 'c:\workspace' `
        -tempLocation 'c:\temp'
#>
function New-Workspace
{
    [CmdletBinding()]
    param(
        [string] $remoteRepositoryUrl,
        [string] $activeBranch,
        [string] $gitflowFinishingReleaseVersion = '1000.0.0',
        [string] $branchToTestOn = 'feature/test',
        [string] $originBranch = 'develop',
        [string] $repositoryLocation,
        [string] $workspaceLocation,
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
        if (Test-RemoteBranch -url $repositoryLocation -branch $activeBranch)
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
