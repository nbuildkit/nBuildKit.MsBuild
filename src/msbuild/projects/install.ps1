param($installPath, $toolsPath, $package, $project)

# Grab the loaded MSBuild project for the project
$msbuild = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($project.FullName) | Select-Object -First 1

# calculate the relative path between the project and the props file
$projectUri = New-Object System.Uri -ArgumentList $project.FullName
Write-Host "project file is at: $projectUri"

$file = "import"

Write-Host "Inserting import.props file ... "
$propsFilePath = Join-Path (Join-Path $installPath "build") "$file.props"
if (Test-Path $propsFilePath)
{
    $propsFileUri = New-Object System.Uri -ArgumentList $propsFilePath
    Write-Host "properties file is at: $propsFileUri"

    $relativeFilePath = $projectUri.MakeRelativeUri($propsFileUri).ToString()
    Write-Host "Relative path from projects file to props file is: $relativeFilePath"

    # create the Import node
    $importElement = $msbuild.Xml.CreateImportElement($relativeFilePath)
    $importElement.Condition = " Exists('$relativeFilePath') "

    $itemGroupNode = $msbuild.Xml.ItemGroups | Select-Object -First 1
    Write-Host ("Found first item group")

    $msbuild.Xml.InsertBeforeChild($importElement, $itemGroupNode)
    Write-Host ("Inserted insert.props before the first item group")
}

# Calculate the relative path between the project and the targets file
Write-Host "Inserting import.targets file ... "
$targetsFilePath = Join-Path (Join-Path $installPath "build") "$file.targets"
if (Test-Path $targetsFilePath)
{
    $targetsFileUri = New-Object System.Uri -ArgumentList $targetsFilePath
    Write-Host "Targets file is at: $targetsFileUri"

    $relativeFilePath = $projectUri.MakeRelativeUri($targetsFileUri).ToString()
    Write-Host "Relative path from projects file to targets file is: $relativeFilePath"

    $importElement = $msbuild.Xml.CreateImportElement($relativeFilePath)
    $importElement.Condition = " Exists('$relativeFilePath') "

    $msbuild.Xml.AppendChild($importElement);
    Write-Host ("Inserted insert.targets at the end of the project file.")
}

$project.Save()