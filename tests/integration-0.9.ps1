<#
    This file contains the 'verification tests' for the nbuildkit verification test suite
    which test against a repository that contains the 0.9 version of the configuration files.

    These tests are executed using Pester (https://github.com/pester/Pester).
#>

param(
    # The minimum version of nBuildKit that should be used for the test
    [string] $repositoryVersion,

    # The maximum version of nBuildKit that should be used for the test
    [string] $releaseVersion,

    # The path to the directory that contains the NuGet packages that need to be tested
    [string] $localNuGetFeed,

    # The valid nuget sources, other than the local one, separated by a semi-colon
    [string] $validNuGetSources,

    # The configuration version that should be tested
    [string] $configurationVersionToTest,

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

     # The path to where the nuget packages can be deployed at the end of the test
    [string] $nugetPath,

    # The path to where the nuget symbol packages can be deployed at the end of the test
    [string] $symbolsPath,

    # The path to where the artefacts can be deployed at the end of the test
    [string] $artefactsPath,

    # The location where the log files should be placed
    [string] $logLocation,

    # A temporary directory that can be used for the current test
    [string] $tempLocation
)

Write-Host "integration-0.9 - param repositoryVersion = $repositoryVersion"
Write-Host "integration-0.9 - param releaseVersion = $releaseVersion"
Write-Host "integration-0.9 - param localNuGetFeed = $localNuGetFeed"
Write-Host "integration-0.9 - param validNuGetSources = $validNuGetSources"
Write-Host "integration-0.9 - param configurationVersionToTest = $configurationVersionToTest"
Write-Host "integration-0.9 - param remoteRepositoryUrl = $remoteRepositoryUrl"
Write-Host "integration-0.9 - param activeBranch = $activeBranch"
Write-Host "integration-0.9 - param repositoryLocation = $repositoryLocation"
Write-Host "integration-0.9 - param workspaceLocation = $workspaceLocation"
Write-Host "integration-0.9 - param symbolsPath = $symbolsPath"
Write-Host "integration-0.9 - param artefactsPath = $artefactsPath"
Write-Host "integration-0.9 - param logLocation = $logLocation"
Write-Host "integration-0.9 - param tempLocation = $tempLocation"

#
# LOAD HELPER SCRIPTS
#
. (Join-Path $PSScriptRoot '..\src\tests\TestFunctions.Git.ps1')
. (Join-Path $PSScriptRoot '..\src\tests\TestFunctions.MsBuild.ps1')
. (Join-Path $PSScriptRoot '..\src\tests\TestFunctions.PrepareWorkspace.ps1')
. (Join-Path $PSScriptRoot "TestFunctions.Prepare.ps1")

Add-Type -AssemblyName System.IO.Compression.FileSystem


#
# PREPARE THE ENVIRONMENT
#

if (-not (Test-Path $repositoryLocation))
{
    New-Item -Path $repositoryLocation -ItemType Directory | Out-Null
}

if (-not (Test-Path $workspaceLocation))
{
    New-Item -Path $workspaceLocation -ItemType Directory | Out-Null
}

if (-not (Test-Path $logLocation))
{
    New-Item -Path $logLocation -ItemType Directory | Out-Null
}

if (-not (Test-Path $tempLocation))
{
    New-Item -Path $tempLocation -ItemType Directory | Out-Null
}

[array]$additionalNuGetSources = @()
if (($localNuGetFeed -ne $null) -and ($localNuGetFeed -ne ''))
{
    $additionalNuGetSources += $localNuGetFeed
}

foreach($source in ($validNuGetSources -split ';'))
{
    if (($source -ne $null) -and ($source -ne ''))
    {
        $additionalNuGetSources += $source
    }
}

$environmentFile = (Join-Path $tempLocation 'environment.props')
New-EnvironmentFile `
    -filePath $environmentFile


#
# DEFINE TESTS TO EXECUTE
#

$tests = @(
    'vs-solution-01'
    'vs-solution-02'
)

#
# DEFINE VERSION NUMBERS
#

$branchToTestOn = "release/$($releaseVersion)"

#
# EXECUTE TESTS
#

foreach($testPair in $tests)
{

    if (Test-Path $nugetPath)
    {
        Remove-Item -Path $nugetPath -Force -Recurse -ErrorAction SilentlyContinue
    }

    if (Test-Path $symbolsPath)
    {
        Remove-Item -Path $symbolsPath -Force -Recurse -ErrorAction SilentlyContinue
    }

    if (Test-Path $artefactsPath)
    {
        Remove-Item -Path $artefactsPath -Force -Recurse -ErrorAction SilentlyContinue
    }

    New-Item -Path $nugetPath -ItemType Directory | Out-Null
    New-Item -Path $symbolsPath -ItemType Directory | Out-Null
    New-Item -Path $artefactsPath -ItemType Directory | Out-Null

    $testId = $testPair

    $testRepositoryLocation = Join-Path $repositoryLocation $testId
    $testWorkspaceLocation = Join-Path $workspaceLocation $testId
    $testTempLocation = Join-Path $tempLocation $testId

    $nugetQAPath = Join-Path $testTempLocation 'nugetqa'
    if (-not (Test-Path $nugetQAPath))
    {
        New-Item -Path $nugetQAPath -ItemType Directory | Out-Null
    }

    $nugetProductionPath = Join-Path $testTempLocation 'nugetproduction'
    if (-not (Test-Path $nugetProductionPath))
    {
        New-Item -Path $nugetProductionPath -ItemType Directory | Out-Null
    }

    $symbolsQAPath = Join-Path $testTempLocation 'symbolsqa'
    if (-not (Test-Path $symbolsQAPath))
    {
        New-Item -Path $symbolsQAPath -ItemType Directory | Out-Null
    }

    $symbolsProductionPath = Join-Path $testTempLocation 'symbolsproduction'
    if (-not (Test-Path $symbolsProductionPath))
    {
        New-Item -Path $symbolsProductionPath -ItemType Directory | Out-Null
    }

    New-BuildServerEnvironmentFiles `
        -directoryPath $testTempLocation `
        -nuget $nugetPath `
        -symbols $symbolsPath `
        -artefacts $artefactsPath `
        -validNuGetSources $additionalNuGetSources `
        -Verbose

    Describe "For the test with configuration version $($configurationVersionToTest) for the $($testId) directory" {
        $originalDevelopSha = ''
        $originalWorkingBranchSha = ''
        Context 'the preparation of the workspace' {
            New-Workspace `
                -remoteRepositoryUrl $remoteRepositoryUrl `
                -activeBranch $activeBranch `
                -gitflowFinishingReleaseVersion $repositoryVersion `
                -branchToTestOn $branchToTestOn `
                -originBranch 'develop' `
                -repositoryLocation $testRepositoryLocation `
                -workspaceLocation $testWorkspaceLocation `
                -tempLocation $testTempLocation

            $currentDirectory = $pwd
            try
            {
                Set-Location $testWorkspaceLocation

                # Need to nuke the other directories otherwise we find too many nuspec files and things will be sad
                $directoriesToRemove = Get-ChildItem -Path "$($testWorkspaceLocation)\$($configurationVersionToTest)" -Directory |
                    Where-Object { $_.Name -ne $testId }
                foreach($directoryToRemove in $directoriesToRemove)
                {
                    $removedFiles = Get-ChildItem $directoryToRemove.FullName -File -Recurse | Select-Object -ExpandProperty FullName
                    Remove-Item -Path $directoryToRemove.FullName -Recurse
                }

                $output = & git add -u 2>&1
                $outputText = $output | Out-String
                if ($outputText -ne '')
                {
                    Write-Verbose $outputText
                }

                if ($LASTEXITCODE -ne 0)
                {
                    throw "Git stage failed. Output was: $($outputText)"
                }

                New-GitCommit -message "Deleting the non-test directories"
                Push-ToRemote -origin 'origin' -Verbose
            }
            finally
            {
                Set-Location $currentDirectory
            }

            It 'has created the local repository' {
                $testRepositoryLocation | Should Exist
                "$testRepositoryLocation\HEAD" | Should Exist
            }

            It 'has created the workspace' {
                $testWorkspaceLocation | Should Exist
                "$testWorkspaceLocation\.git" | Should Exist
                "$testWorkspaceLocation\entrypoint.msbuild" | Should Exist
            }

            It 'has set the workspace origin to the local repository' {
                $origin = Get-Origin -workspace $testWorkspaceLocation
                $origin | Should Be $testRepositoryLocation
            }

            It 'has created a release branch' {
                $currentBranch = Get-CurrentBranch -workspace $testWorkspaceLocation
                $currentBranch | Should Not BeNullOrEmpty
                $currentBranch.StartsWith('release/') | Should Be $true
            }
        }

        $originalMasterSha = Get-CurrentCommit -branch 'master' -workspace $testWorkspaceLocation
        $originalDevelopSha = Get-CurrentCommit -branch 'develop' -workspace $testWorkspaceLocation
        $originalWorkingBranchSha = Get-CurrentCommit -branch $branchToTestOn -workspace $testWorkspaceLocation

        $testWorkspace = Join-Path $testWorkspaceLocation "$($configurationVersionToTest)\$($testId)"
        Context 'the build executes successfully' {
            $msBuildProperties = @{
                'PackageMinimumVersion' = $repositoryVersion
                'PackageMaximumVersion' = $releaseVersion
                'LocalNuGetRepository' = $localNuGetFeed
                'IsOnBuildServer' = 'true'
                'GitBranchExpected' = $branchToTestOn
                'GitRevNoExpected' = $originalWorkingBranchSha
                'DirBuildServerSettings' = $testTempLocation
                'DirUserSettings' = $testWorkspace
            }

            $exitCode = Invoke-MsBuildFromCommandLine `
                -scriptToExecute (Join-Path $testWorkspaceLocation 'entrypoint.msbuild') `
                -target 'build' `
                -properties $msBuildProperties `
                -logPath (Join-Path $logLocation "integration-0.9.0.1.$($testId).build.log")

            $hasBuild = ($exitCode -eq 0)
            It 'and completes with a zero exit code' {
                $exitCode | Should Be 0
            }
        }

        Context 'the build has performed a merge' {
            $developSha = Get-CurrentCommit -branch 'develop' -workspace $testWorkspaceLocation
            $parentCommits = Get-Parents -currentCommitId $developSha -workspace $testWorkspaceLocation
            It 'the release was merged to develop' {
                ,$parentCommits | Should BeOfType System.Array
                ,$parentCommits.Length | Should Be 2
            }

            It 'the latest develop commit has as parents the release branch and the develop branch' {
                $parentCommits[0] | Should Be $originalDevelopSha
                $parentCommits[1] | Should Be $originalWorkingBranchSha
            }

            It 'the current branch is master' {
                $currentBranch = Get-CurrentBranch -workspace $testWorkspaceLocation
                $currentBranch | Should Not BeNullOrEmpty
                $currentBranch | Should Be 'master'
            }

            $parentCommits = Get-Parents -workspace $testWorkspaceLocation
            It 'the current commit is a merge commit' {
                ,$parentCommits | Should BeOfType System.Array
                ,$parentCommits.Length | Should Be 2
            }

            It 'the current commit has as parents the release branch and the master branch' {
                $parentCommits[0] | Should Be $originalMasterSha
                $parentCommits[1] | Should Be $originalWorkingBranchSha
            }
        }

        $validationScript = Join-Path $PSScriptRoot "0.9\$($testId)_validatebuild.ps1"
        & $validationScript `
            -repositoryVersion $repositoryVersion `
            -releaseVersion $releaseVersion `
            -remoteRepositoryUrl $remoteRepositoryUrl `
            -workspaceLocation $workspaceLocation `
            -nugetPath $nugetPath `
            -symbolsPath $symbolsPath `
            -artefactsPath $artefactsPath `
            -logLocation $logLocation `
            -tempLocation $tempLocation `
            -branchToTestOn $branchToTestOn

        Context 'the build produces a merge archive package' {
            $archive = Join-Path $testWorkspaceLocation "build\deploy\gitmerge-nBuildKit.Test-$($branchToTestOn.Replace('/', '_')).zip"

            It 'in the expected location' {
                $archive | Should Exist
            }

            if (Test-Path $archive)
            {
                # extract the package
                $packageUnzipLocation = Join-Path $testWorkspaceLocation 'build\temp\unzip\archive\git'
                if (-not (Test-Path $packageUnziplocation))
                {
                    New-Item -Path $packageUnzipLocation -ItemType Directory | Out-Null
                }
                [System.IO.Compression.ZipFile]::ExtractToDirectory($archive, $packageUnzipLocation)

                It 'with the expected files' {
                    (Join-Path $packageUnzipLocation 'vcs.mergeinfo.xml') | Should Exist
                    (Join-Path $packageUnzipLocation '.git') | Should Exist
                }
            }
        }

        Context 'the deploy executes successfully' {
            $msBuildProperties = @{
                'PackageMinimumVersion' = $repositoryVersion
                'PackageMaximumVersion' = $releaseVersion
                'LocalNuGetRepository' = $localNuGetFeed
                'IsOnBuildServer' = 'true'
                'GitBranchExpected' = $branchToTestOn
                'GitRevNoExpected' = $originalWorkingBranchSha
                'GitRemoteRepository' = $repositoryLocation
                'DirBuildServerSettings' = $testTempLocation
                'DirUserSettings' = $testWorkspace
            }

            $exitCode = Invoke-MsBuildFromCommandLine `
                -scriptToExecute (Join-Path $testWorkspaceLocation 'entrypoint.msbuild') `
                -target 'deploy' `
                -properties $msBuildProperties `
                -logPath (Join-Path $logLocation "integration-0.9.0.1.$($testId).deploy.log")

            $hasBuild = ($exitCode -eq 0)
            It 'and completes with a zero exit code' {
                $exitCode | Should Be 0
            }
        }

        $validationScript = Join-Path $PSScriptRoot "0.9\$($testId)_validatedeploy.ps1"
        & $validationScript `
            -repositoryVersion $repositoryVersion `
            -releaseVersion $releaseVersion `
            -remoteRepositoryUrl $remoteRepositoryUrl `
            -workspaceLocation $workspaceLocation `
            -nugetPath $nugetPath `
            -symbolsPath $symbolsPath `
            -artefactsPath $artefactsPath `
            -logLocation $logLocation `
            -tempLocation $tempLocation `
            -branchToTestOn $branchToTestOn

        Context 'the deploy pushed to the remote repository' {
            $tempWorkspace = Join-Path $tempLocation 'verification'
            if (Test-Path $tempWorkspace)
            {
                Remove-Item -Path $tempWorkspace -Force -Recurse -ErrorAction SilentlyContinue
            }

            Clone-Repository `
                -url $testRepositoryLocation `
                -destination $tempWorkspace

            $workspaceSha = Get-CurrentCommit -branch 'master' -workspace $testWorkspaceLocation
            $remoteSha = Get-CurrentCommit -branch 'master' -workspace $tempWorkspace
            It 'pushed the master branch' {
                $remoteSha | Should Be $workspaceSha
            }

            $originalWorkingDirectory = $pwd
            try
            {
                Set-Location $tempWorkspace

                Checkout-Branch `
                    -Branch 'develop'
            }
            finally
            {
                Set-Location $originalWorkingDirectory
            }

            $workspaceSha = Get-CurrentCommit -branch 'develop' -workspace $testWorkspaceLocation
            $remoteSha = Get-CurrentCommit -branch 'develop' -workspace $tempWorkspace
            It 'pushed the develop branch' {
                $remoteSha | Should Be $workspaceSha
            }
        }
    }
}
