param($installPath, $toolsPath, $package, $project)

# Need to load MSBuild assembly if it’s not loaded yet.
Add-Type -AssemblyName "Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"

# Grab the loaded MSBuild project for the project
$msbuild = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($project.FullName) | Select-Object -First 1

$filesToRemove = @( ${TargetsFiles} )
foreach($file in $filesToRemove)
{
    $propertyImport = $msbuild.Xml.Imports | Where-Object { $_.Project.Endswith("$file.props") }

    # Remove the import and save the project
    if ($propertyImport -ne $null)
    {
        $msbuild.Xml.RemoveChild($propertyImport) | out-null
        Write-Host "Removed $file.props"
    }

    $targetImport = $msbuild.Xml.Imports | Where-Object { $_.Project.Endswith("$file.targets") }

    # Remove the import and save the project
    if ($targetImport -ne $null)
    {
        $msbuild.Xml.RemoveChild($targetImport) | out-null
        Write-Host "Removed $file.targets"
    }
}

#save the changes.
$project.Save()