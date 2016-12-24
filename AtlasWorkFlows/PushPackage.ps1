# Push the package if the version number has changed.
#
[CmdletBinding()]
param()

# Get the current package numbers
$pkgName = "AtlasSSH"
$current = $(nuget list $pkgName)
$version = $current.Split()[1]
Write-Verbose "Nuget has version $version of Package $pkgName"

# Get the current contents of the nuspec file
$nuspec = Get-Content .\AtlasWorkFlows.nuspec | Select-String $version
if ($nuspec.Count -gt 0) {
	Write-Verbose "Version $version is already on the myget server. Will not push."
} else {
    rm .\*.nupkg
    nuget pack -IncludeReferencedProjects -Prop Configuration=Release .\AtlasWorkflows.csproj
	nuget push .\AtlasSSH.*.nupkg -Source https://www.nuget.org
}
