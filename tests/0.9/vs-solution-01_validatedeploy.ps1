[CmdletBinding()]
param(
    # The minimum version of nBuildKit that should be used for the test
    [string] $repositoryVersion,

    # The maximum version of nBuildKit that should be used for the test
    [string] $releaseVersion,

    # The URL of the remote repository that contains the test code. This repository will be mirror cloned into
    # a local repository so that the tests can push to the repository without destroying the original
    [string] $remoteRepositoryUrl,

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
    [string] $tempLocation,

    # The branch on which the test is executed.
    [string] $branchToTestOn
)

Context 'the deploy pushed to the nuget feed' {
    It 'pushed the nuget package' {
        (Join-Path $nugetPath "nBuildKit.Test.CSharp.Library.$($releaseVersion)-rtm.nupkg") | Should Exist
        (Join-Path $nugetPath "nBuildKit.Test.VbNet.Library.$($releaseVersion).nupkg") | Should Exist
    }
}

Context 'the deploy pushed to the symbol store' {
    It 'pushed the symbol package' {
        (Join-Path $symbolsPath "nBuildKit.Test.CSharp.Library.$($releaseVersion)-rtm.symbols.nupkg") | Should Exist
        (Join-Path $symbolsPath "nBuildKit.Test.VbNet.Library.$($releaseVersion).symbols.nupkg") | Should Exist
    }
}

Context 'the deploy pushed to the file system' {
    It 'pushed the archive' {
        (Join-Path $artefactsPath "nBuildKit.Test\$($releaseVersion)\gitmerge-nBuildKit.Test-$($branchToTestOn.Replace('/', '_')).zip") | Should Exist
    }
}
