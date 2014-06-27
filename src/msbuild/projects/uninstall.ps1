param($installPath, $toolsPath, $package, $project)

# Need to load MSBuild assembly if it’s not loaded yet.
Add-Type -AssemblyName ‘Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a’

# Grab the loaded MSBuild project for the project
$msbuild = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($project.FullName) | Select-Object -First 1

$propertyImport = $msbuild.Xml.Imports | Where-Object { $_.Project.Endswith('${TargetsFile}.props') }

# Remove the import and save the project
if ($propertyImport -ne $null)
{
    $msbuild.Xml.RemoveChild($propertyImport) | out-null
    Write-Host "Removed property import"
}

$targetImport = $msbuild.Xml.Imports | Where-Object { $_.Project.Endswith('${TargetsFile}.targets') }

# Remove the import and save the project
if ($targetImport -ne $null)
{
    $msbuild.Xml.RemoveChild($targetImport) | out-null
    Write-Host "Removed target import"
}

#save the changes.
$project.Save()