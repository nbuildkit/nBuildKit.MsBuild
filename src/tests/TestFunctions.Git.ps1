<#
    .SYNOPSIS

    Checks out the given branch.


    .DESCRIPTION

    The Checkout-Branch function checks out the given branch. This function assumes that the
    current directory is the workspace directory.


    .PARAMETER branch

    The name of the branch that should be checked out.


    .EXAMPLE

    Checkout-Branch -branch 'feature/myfeature'
#>
function Checkout-Branch
{
    [CmdletBinding()]
    param(
        [string] $branch
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    $output = & git checkout --quiet $branch 2>&1

    $outputText = $output | Out-String
    if ($outputText -ne '')
    {
        Write-Verbose $outputText
    }

    if ($LASTEXITCODE -ne 0)
    {
        throw "Git checkout failed. Output was: $($outputText)"
    }
}

<#
    .SYNOPSIS

    Clones the given repository


    .DESCRIPTION

    The Clone-Repository function clones the given repository to the destination folder.


    .PARAMETER url

    The URL of the repository that should be cloned


    .PARAMETER destination

    The full path to the directory where the repository should be cloned.


    .PARAMETER bare

    A switch that indicates whether or not the repository should be cloned as a 'bare' repository.


    .EXAMPLE

    Clones the repository as a working repository.

    Clone-Repository -url 'https://github.com/nbuildkit/nbuildkit.msbuild' -destination 'c:\workspace'


    .EXAMPLE

    Clones the repository as a bare repository

    Clone-Repository -url 'https://github.com/nbuildkit/nbuildkit.msbuild' -destination 'c:\workspace' -bare
#>
function Clone-Repository
{
    [CmdletBinding()]
    param(
        [string] $url,
        [string] $destination,
        [switch] $bare
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    $bareText = ''
    if ($bare)
    {
        $bareText = '--mirror'
    }

    $output = & git clone --quiet $bareText $url $destination 2>&1
    $outputText = $output | Out-String
    if ($outputText -ne '')
    {
        Write-Verbose $outputText
    }

    if ($LASTEXITCODE -ne 0)
    {
        throw "Git clone failed. Output was: $($outputText)"
    }
}

<#
    .SYNOPSIS

    Follows the git flow approach to complete feature, hotfix and release branches.


    .DESCRIPTION

    The Finish-GitFlow function completes the git flow process on the given branch with the goal of getting
    the changes in the branch in to the master branch.


    .PARAMETER branch

    The branch which should have its changes merged to the master branch.


    .PARAMETER releaseVersion

    The version of the release branch that should be created when merging changes in a feature branch.


    .EXAMPLE

    Completes the feature branch by merging the changes to the 'develop' branch and then creating
    a 'release/1.3.0' branch and merging that branch to the 'develop' and 'master' branches.

    Finish-GitFlow -branch 'feature/myfeature' -releaseVersion '1.3.0'


    .EXAMPLE

    Completes the hotfix branch by merging the changes to the 'develop' and 'master' branches.

    Finish-GitFlow -branch 'hotfix/1.2.3'


    .EXAMPLE

    Completes the release branch by merging the changes to the 'develop' and 'master' branches.

    Finish-GitFlow -branch 'release/1.3.0'
#>
function Finish-GitFlow
{
    [CmdletBinding()]
    param(
        [string] $branch,
        [string] $releaseVersion = '1000.0.0'
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    Checkout-Branch `
        -Branch 'develop' `
        @commonParameterSwitches

    Checkout-Branch `
        -Branch 'master' `
        @commonParameterSwitches

    Checkout-Branch `
        -Branch $branch `
        @commonParameterSwitches

    switch($branch.Substring(0, $branch.IndexOf('/')))
    {
        'feature' {
            MergeTo-Branch `
                -source $branch `
                -destination 'develop' `
                @commonParameterSwitches

            Remove-Branch `
                -name $branch `
                @commonParameterSwitches

            $defaultReleaseBranch = "release/$($releaseVersion)"
            New-Branch `
                -name $defaultReleaseBranch `
                -source 'develop' `
                @commonParameterSwitches

            Finish-GitFlow `
                -branch $defaultReleaseBranch `
                @commonParameterSwitches
        }

        {($_ -eq 'hotfix') -or ($_ -eq 'release')} {
            MergeTo-Branch `
                -source $branch `
                -destination 'develop' `
                @commonParameterSwitches

            MergeTo-Branch `
                -source $branch `
                -destination 'master' `
                @commonParameterSwitches

            $tagName = $branch.Substring($branch.IndexOf('/') + 1)
            New-Tag `
                -name $tagName `
                -source 'master' `
                @commonParameterSwitches

            Remove-Branch `
                -name $branch `
                @commonParameterSwitches
        }
    }
}

<#
    .SYNOPSIS

    Gets the name of the current branch.


    .DESCRIPTION

    The Get-CurrentBranch function gets the name of the current branch.


    .PARAMETER workspace

    The directory containing the git repository.


    .EXAMPLE

    Get-CurrentBranch -workspace 'c:\workspace'
#>
function Get-CurrentBranch
{
    [CmdletBinding()]
    param(
        [string] $workspace
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    $currentDirectory = $pwd
    try
    {
        Set-Location $workspace

        $output = & git rev-parse --abbrev-ref HEAD
        return $output
    }
    finally
    {
        Set-Location $currentDirectory
    }
}

<#
    .SYNOPSIS

    Gets the SHA of the last commit.


    .DESCRIPTION

    The Get-CurrentCommit function gets the SHA of the last commit.


    .PARAMETER branch

    The name of the branch for which the last commit SHA should be retrieved. If no branch name is specified
    'HEAD' is used.


    .PARAMETER workspace

    The directory containing the git repository.


    .EXAMPLE

    Gets the SHA1 of the last commit on the current branch.

    Get-CurrentCommit -workspace 'c:\workspace'


    .EXAMPLE

    Gets the SHA1 of the last commit on the 'develop' branch.

    Get-CurrentCommit -workspace 'c:\workspace' -branch 'develop'
#>
function Get-CurrentCommit
{
    [CmdletBinding()]
    param(
        [string] $branch = 'HEAD',
        [string] $workspace
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    $currentDirectory = $pwd
    try
    {
        Set-Location $workspace

        $output = & git rev-parse $branch
        return $output
    }
    finally
    {
        Set-Location $currentDirectory
    }
}

<#
    .SYNOPSIS

    Gets the URL of the remote called 'origin'.


    .DESCRIPTION

    The Get-Origin function gets the url of the remote called 'origin'.


    .PARAMETER workspace

    The directory containing the git repository.


    .EXAMPLE

    Get-CurrentBranch -workspace 'c:\workspace'
#>
function Get-Origin
{
    [CmdletBinding()]
    param(
        [string] $workspace
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    $currentDirectory = $pwd
    try
    {
        Set-Location $workspace

        $output = & git config --get remote.origin.url
        return $output
    }
    finally
    {
        Set-Location $currentDirectory
    }
}

<#
    .SYNOPSIS

    Gets the SHA1 values of the parent(s) of the given commit.


    .DESCRIPTION

    The Get-Parents function gets the SHA1 values of the parent(s) of the given commit.


    .PARAMETER currentCommitId

    The SHA1 of the commit for which the parent commits should be obtained.


    .PARAMETER workspace

    The directory containing the git repository.


    .EXAMPLE

    Gets the SHA1 of the parent commits for the last commit on the current branch.

    Get-Parents -workspace 'c:\workspace'


    .EXAMPLE

    Gets the SHA1 of the parent commits for the given commit

    Get-Parents -currentCommitId '12345566' -workspace 'c:\workspace'
#>
function Get-Parents
{
    [CmdletBinding()]
    param(
        [string] $currentCommitId,
        [string] $workspace
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    if ($currentCommitId -eq '')
    {
        $currentCommitId = Get-CurrentCommit -workspace $workspace
    }

    $currentDirectory = $pwd
    try
    {
        Set-Location $workspace

        $output = & git rev-list $currentCommitId --parents -n 1
        return $output -split ' ' | Select-Object -Last 2
    }
    finally
    {
        Set-Location $currentDirectory
    }
}

<#
    .SYNOPSIS

    Merges the changes in the source branch to the destination branch


    .DESCRIPTION

    The MergeTo-Branch function merges the changes in the source branch to the destination branch.


    .PARAMETER source

    The name of the source branch


    .PARAMETER destination

    The destination branch


    .EXAMPLE

    MergeTo-Branch -source 'feature/myfeature' -destination 'develop'
#>
function MergeTo-Branch
{
    [CmdletBinding()]
    param(
        [string] $source,
        [string] $destination
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    Checkout-Branch -branch $destination @commonParameterSwitches

    $output = & git merge --no-ff --commit --quiet $source 2>&1
    $outputText = $output | Out-String
    if ($outputText -ne '')
    {
        Write-Verbose $outputText
    }

    if ($LASTEXITCODE -ne 0)
    {
        throw "Git merge failed. Output was: $($outputText)"
    }
}

<#
    .SYNOPSIS

    Creates a new branch of the given source branch at the last commit on that branch.


    .DESCRIPTION

    The New-Branch function creates a new branch of the given source branch at the last commit on that branch.


    .PARAMETER name

    The name of the new branch.


    .PARAMETER source

    The name of the source branch.


    .EXAMPLE

    New-Branch -name 'feature/myfeature' -source 'develop'
#>
function New-Branch
{
    [CmdletBinding()]
    param(
        [string] $name,
        [string] $source
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    Checkout-Branch -branch $source @commonParameterSwitches

    $output = & git checkout --quiet -b $name 2>&1
    $outputText = $output | Out-String
    if ($outputText -ne '')
    {
        Write-Verbose $outputText
    }

    if ($LASTEXITCODE -ne 0)
    {
        throw "Git new branch failed. Output was: $($outputText)"
    }
}

<#
    .SYNOPSIS

    Commits the currently staged changes with the given commit message.


    .DESCRIPTION

    The New-GitCommit function commits the currently staged chagnes with the given commit message.


    .PARAMETER message

    The commit message.


    .EXAMPLE

    New-GitCommit -message 'this is a commit'
#>
function New-GitCommit
{
    [CmdletBinding()]
    param(
        [string] $message
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    $output = & git commit -m $message 2>&1
    $outputText = $output | Out-String
    if ($outputText -ne '')
    {
        Write-Verbose $outputText
    }

    if ($LASTEXITCODE -ne 0)
    {
        throw "Git commit failed. Output was: $($outputText)"
    }
}

<#
    .SYNOPSIS

    Creates a new tag on the given source branch or commit.


    .DESCRIPTION

    The New-Tag function creates a new tag on the given source branch or commit.


    .PARAMETER name

    The tag name.


    .PARAMETER source

    The name of the branch on which the last commit should be tagged, or the SHA1 of the commit
    that should be tagged.


    .EXAMPLE

    New-Tag -name '1.2.3' -source 'master'
#>
function New-Tag
{
    [CmdletBinding()]
    param(
        [string] $name,
        [string] $source
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    Checkout-Branch -branch $source @commonParameterSwitches

    $output = & git tag $name 2>&1
    $outputText = $output | Out-String
    if ($outputText -ne '')
    {
        Write-Verbose $outputText
    }

    if ($LASTEXITCODE -ne 0)
    {
        throw "Git tag failed. Output was: $($outputText)"
    }
}

<#
    .SYNOPSIS

    Pushes all changes to the given remote.


    .DESCRIPTION

    The Push-ToRemote function pushes all changes to the given remote.


    .PARAMETER origin

    The name of the remote to push to.


    .EXAMPLE

    Push-ToRemote -origin 'origin'
#>
function Push-ToRemote
{
    [CmdletBinding()]
    param(
        [string] $origin
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    $output = & git push --all --porcelain $origin 2>&1
    $outputText = $output | Out-String
    if ($outputText -ne '')
    {
        Write-Verbose $outputText
    }

    if ($LASTEXITCODE -ne 0)
    {
        throw "Git push failed. Output was: $($outputText)"
    }
}

<#
    .SYNOPSIS

    Removes the given branch.


    .DESCRIPTION

    The Remove-Branch function removes the given branch


    .PARAMETER name

    The name of the branch that should be removed.


    .EXAMPLE

    Remove-Branch -name 'feature/myfeature'
#>
function Remove-Branch
{
    [CmdletBinding()]
    param(
        [string] $name
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    $output = & git branch -d $name 2>&1
    $outputText = $output | Out-String
    if ($outputText -ne '')
    {
        Write-Verbose $outputText
    }

    if ($LASTEXITCODE -ne 0)
    {
        throw "Git branch failed. Output was: $($outputText)"
    }
}

<#
    .SYNOPSIS

    Stages the changes to the given file.


    .DESCRIPTION

    The Stage-Changes function stages the changes to the given file.


    .PARAMETER relativeFilePath

    The relative path from the workspace to the file that should be stages.


    .EXAMPLE

    Stage-Changes -relativeFilePath 'src/myfile.txt'
#>
function Stage-Changes
{
    [CmdletBinding()]
    param(
        [string] $relativeFilePath
    )

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    $output = & git add $relativeFilePath 2>&1
    $outputText = $output | Out-String
    if ($outputText -ne '')
    {
        Write-Verbose $outputText
    }

    if ($LASTEXITCODE -ne 0)
    {
        throw "Git stage failed. Output was: $($outputText)"
    }
}
