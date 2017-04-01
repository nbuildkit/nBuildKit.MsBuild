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
