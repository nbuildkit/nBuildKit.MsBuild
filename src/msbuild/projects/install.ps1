param($installPath, $toolsPath, $package, $project)

$filesToInsert = @( ${TargetsFiles} )

# Need to load MSBuild assembly if it"s not loaded yet.
Add-Type -AssemblyName "Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"

# Grab the loaded MSBuild project for the project
$msbuild = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($project.FullName) | Select-Object -First 1

foreach($file in $filesToInsert)
{
    # calculate the relative path between the project and the props file
    $projectUri = New-Object System.Uri -ArgumentList $project.FullName
    Write-Host "project file is at: $projectUri" 

    $propsFileUri = New-Object System.Uri -ArgumentList (Join-Path (Join-Path $installPath "build") "$file.props")
    Write-Host "properties file is at: $propsFileUri" 

    $relativeFilePath = $projectUri.MakeRelativeUri($propsFileUri).ToString()
    Write-Host "Relative path from projects file to props file is: $relativeFilePath" 

    # create the Import node
    $importElement = $msbuild.Xml.CreateImportElement($relativeFilePath)
    $importElement.Condition = " Exists('$relativeFilePath') "

    $itemGroupNode = $msbuild.Xml.ItemGroups | Select-Object -First 1
    Write-Host ("Found first item group at: " + $itemGroupNode.ToString())

    $msbuild.Xml.InsertBeforeChild($importElement, $itemGroupNode)
    Write-Host ("Inserting before the first item group: " + $importElement.ToString())

    # Calculate the relative path between the project and the targets file
    $targetsFileUri = New-Object System.Uri -ArgumentList (Join-Path (Join-Path $installPath "build") "$file.targets")
    Write-Host "properties file is at: $targetsFileUri" 

    $relativeFilePath = $projectUri.MakeRelativeUri($targetsFileUri).ToString()
    Write-Host "Relative path from projects file to targets file is: $relativeFilePath" 

    $importElement = $msbuild.Xml.CreateImportElement($relativeFilePath)
    $importElement.Condition = " Exists('$relativeFilePath') "
    
    $msbuild.Xml.AppendChild($importElement)
}

$project.Save()