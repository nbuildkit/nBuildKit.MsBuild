function Get-UserNuGetSources
{
    [CmdletBinding()]
    param(
        [string] $nugetExe = 'nuget'
    )

    Write-Verbose "Get-UserNuGetSources: param nugetExe = $nugetExe"

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    $output = & $nugetExe sources List -Format short

    # Expecting the output to look something like:
    # E c:\dev\nuget
    # E \\files\tfs\NuGet_Dev
    # E \\files\tfs\NuGet_QA
    # E http://prod.nuget.vista.co/
    # E https://www.nuget.org/api/v2/
    # E https://api.nuget.org/v3/index.json
    # EM C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\
    #
    # We only want the entries starting with 'E ' because those are the
    # user defined feeds
    $result = @()
    foreach($line in $output | Where-Object { $_.StartsWith('E ')})
    {
        $result += $line.SubString(2)
    }

    return $result;
}


function New-BuildServerEnvironmentFiles
{
    [CmdletBinding()]
    param(
        [string] $directoryPath,
        [string] $nuget,
        [string] $symbols,
        [string] $artefacts,
        [string[]] $validNuGetSources
    )

    Write-Verbose "New-BuildServerEnvironmentFiles: param directoryPath = $directoryPath"
    Write-Verbose "New-BuildServerEnvironmentFiles: param nuget = $nuget"
    Write-Verbose "New-BuildServerEnvironmentFiles: param symbols = $symbols"
    Write-Verbose "New-BuildServerEnvironmentFiles: param artefacts = $artefacts"
    Write-Verbose "New-BuildServerEnvironmentFiles: param validNuGetSources = $validNuGetSources"

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    if (-not (Test-Path $directoryPath))
    {
        New-Item -Path $directoryPath -ItemType Directory | Out-Null
    }

    New-BuildServerEnvironmentPreFiles `
        -directoryPath $directoryPath `
        -nuget $nuget `
        -symbols $symbols `
        -artefacts $artefacts `
        @commonParameterSwitches

    New-BuildServerEnvironmentPostFiles `
        -directoryPath $directoryPath `
        -validNuGetSources $validNuGetSources `
        @commonParameterSwitches
}

function New-BuildServerEnvironmentPostFiles
{
    [CmdletBinding()]
    param(
        [string] $directoryPath,
        [string[]] $validNuGetSources
    )

    if (-not (Test-Path $directoryPath))
    {
        New-Item -Path $directoryPath -ItemType Directory | Out-Null
    }

    $content = @'
<?xml version="1.0" encoding="utf-8"?>
<Project
    ToolsVersion="14.0"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
        <FileBuildServerEnvironmentPostShared>$(MSBuildThisFileDirectory)buildserver.environment.shared.post.props</FileBuildServerEnvironmentPostShared>
    </PropertyGroup>
    <Import
        Condition="Exists('$(FileBuildServerEnvironmentPostShared)') AND '$(ExistsBuildServerEnvironmentSharedPostSettings)' != 'true' "
        Project="$(FileBuildServerEnvironmentPostShared)" />






    <!--
        *****************************************
        *                                       *
        *     NBUILDKIT SPECIFIC SETTINGS       *
        *                                       *
        *****************************************
    -->

    <PropertyGroup>
        <ExistsBuildServerEnvironmentPostSettings>true</ExistsBuildServerEnvironmentPostSettings>
        <VersionBuildServerEnvironmentSettings>1.0.0</VersionBuildServerEnvironmentSettings>
    </PropertyGroup>
</Project>
'@
    $content | Set-Content -Path (Join-Path $directoryPath 'buildserver.environment.post.props') -Force

    $content = @'
<?xml version="1.0" encoding="utf-8"?>
<Project
    ToolsVersion="11.0"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <ItemGroup>
${ValidNuGetSources}
    </ItemGroup>






    <!--
        *****************************************
        *                                       *
        *     NBUILDKIT SPECIFIC SETTINGS       *
        *                                       *
        *****************************************
    -->

    <PropertyGroup>
        <!-- Defines whether the current script file has been loaded / imported or not -->
        <ExistsBuildServerEnvironmentSharedPostSettings>true</ExistsBuildServerEnvironmentSharedPostSettings>
        <VersionBuildServerEnvironmentSettings>1.0.0</VersionBuildServerEnvironmentSettings>
    </PropertyGroup>
</Project>
'@

    $validNuGetSourcesAsXmlElement = ''
    if ($validNuGetSources.Count -gt 0)
    {
        Write-Verbose "Processing validNuGetSources ..."
        foreach($feed in $validNuGetSources)
        {
            Write-Verbose "Adding source: $($feed) ..."
            $validNuGetSourcesAsXmlElement += "        <NuGetSources Include='$($feed)' />" + [System.Environment]::NewLine
        }
    }

    $userNuGetFeeds = Get-UserNuGetSources @commonParameterSwitches
    foreach($feed in $userNuGetFeeds)
    {
        Write-Verbose "Adding source: $($feed) ..."
        $validNuGetSourcesAsXmlElement += "        <NuGetSources Include='$($feed)' />" + [System.Environment]::NewLine
    }

    $content = $content.Replace('${ValidNuGetSources}', $validNuGetSourcesAsXmlElement)

    $content | Set-Content -Path (Join-Path $directoryPath 'buildserver.environment.shared.post.props') -Force
}

function New-BuildServerEnvironmentPreFiles
{
    [CmdletBinding()]
    param(
        [string] $directoryPath,
        [string] $nuget,
        [string] $symbols,
        [string] $artefacts
    )

    Write-Verbose "New-BuildServerEnvironmentPreFiles: param directoryPath = $directoryPath"
    Write-Verbose "New-BuildServerEnvironmentPreFiles: param nuget = $nuget"
    Write-Verbose "New-BuildServerEnvironmentPreFiles: param symbols = $symbols"
    Write-Verbose "New-BuildServerEnvironmentPreFiles: param artefacts = $artefacts"

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    if (-not (Test-Path $directoryPath))
    {
        New-Item -Path $directoryPath -ItemType Directory | Out-Null
    }

    $content = @'
<?xml version="1.0" encoding="utf-8"?>
<Project
    ToolsVersion="14.0"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
        <IsOnBuildServer>true</IsOnBuildServer>
    </PropertyGroup>

    <PropertyGroup>
        <FileBuildServerEnvironmentPreShared>$(MSBuildThisFileDirectory)buildserver.environment.shared.pre.props</FileBuildServerEnvironmentPreShared>
    </PropertyGroup>
    <Import
        Condition="Exists('$(FileBuildServerEnvironmentPreShared)') AND '$(ExistsBuildServerEnvironmentSharedPreSettings)' != 'true' "
        Project="$(FileBuildServerEnvironmentPreShared)" />






    <!--
        *****************************************
        *                                       *
        *     NBUILDKIT SPECIFIC SETTINGS       *
        *                                       *
        *****************************************
    -->

    <PropertyGroup>
        <ExistsBuildServerEnvironmentPreSettings>true</ExistsBuildServerEnvironmentPreSettings>
        <VersionBuildServerEnvironmentSettings>1.0.0</VersionBuildServerEnvironmentSettings>
    </PropertyGroup>
</Project>
'@
    $content | Set-Content -Path (Join-Path $directoryPath 'buildserver.environment.pre.props') -Force

    $content = @'
<?xml version="1.0" encoding="utf-8"?>
<Project
    ToolsVersion="11.0"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!--
        **** ENVIRONMENT ****
    -->
    <!--
        This property group defines all the service addresses for different services as they are used from a build agent.
        These values overwrite the default values.
    -->
    <PropertyGroup>
        <UriArtefacts>$ARTEFACTS$</UriArtefacts>
        <UriNuGet>$NUGET$</UriNuGet>
        <UriSymbols>$SYMBOLS$</UriSymbols>
    </PropertyGroup>





    <!--
        *****************************************
        *                                       *
        *     NBUILDKIT SPECIFIC SETTINGS       *
        *                                       *
        *****************************************
    -->

    <PropertyGroup>
        <!-- Defines whether the current script file has been loaded / imported or not -->
        <ExistsBuildServerEnvironmentSharedPreSettings>true</ExistsBuildServerEnvironmentSharedPreSettings>
        <VersionBuildServerEnvironmentSettings>1.0.0</VersionBuildServerEnvironmentSettings>
    </PropertyGroup>
</Project>
'@

    $content = $content.Replace('$NUGET$', $nuget)
    $content = $content.Replace('$SYMBOLS$', $symbols)
    $content = $content.Replace('$ARTEFACTS$', $artefacts)

    $content | Set-Content -Path (Join-Path $directoryPath 'buildserver.environment.shared.pre.props') -Force
}

function New-EnvironmentFile
{
    [CmdletBinding()]
    param(
        [string] $filePath
    )

    Write-Verbose "New-EnvironmentFile: param filePath = $filePath"

    $ErrorActionPreference = 'Stop'
    $commonParameterSwitches =
        @{
            Verbose = $PSBoundParameters.ContainsKey('Verbose');
            ErrorAction = "Stop"
        }

    $content = @'
<?xml version="1.0" encoding="utf-8"?>
<Project
    DefaultTargets="Run"
    ToolsVersion="11.0"
    xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <!--
        **** ENVIRONMENT - DEVELOPER MACHINE ****
    -->
    <!--
        This property group defines all the service addresses for different services as they are used from a developer machine.
        These values are overwritten on the build server with values that are appropriate.
    -->
    <PropertyGroup Condition=" '$(IsOnBuildServer)' != 'true' ">
        <!--
            The DNS names for different services as they are used from a developer machine.
        -->
        <DnsTfsServer Condition=" '$(DnsTfsServer)' == '' OR '$(DnsTfsServer)' == 'UNDEFINED' ">tfs</DnsTfsServer>

        <UriTfsServer Condition=" '$(UriTfsServer)' == '' OR '$(UriTfsServer)' == 'UNDEFINED' ">http://$(DnsTfsServer):8080/tfs</UriTfsServer>
    </PropertyGroup>





    <!--
        *****************************************
        *                                       *
        *     NBUILDKIT SPECIFIC SETTINGS       *
        *                                       *
        *****************************************
    -->

    <PropertyGroup>
        <!-- Defines whether the current script file has been loaded / imported or not -->
        <ExistsEnvironmentSettings>true</ExistsEnvironmentSettings>

        <!-- Defines the version number of the configuration file -->
        <NBuildKitConfigurationVersion>1.0</NBuildKitConfigurationVersion>
    </PropertyGroup>
</Project>
'@

    $content | Set-Content -Path $filePath -Force
}
