﻿param (
  [string]$buildNumber = "0",
  [string]$extensionVersion = "2.0.$buildNumber",
  [bool]$includeVersion = $true
)

$currentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildOutput = Join-Path $currentDir "buildoutput"

function ZipContent([string] $sourceDirectory, [string] $target)
{
  Write-Host $sourceDirectory
  Write-Host $target
  
  if (Test-Path $target) {
    Remove-Item $target
  }
  Add-Type -assembly "system.io.compression.filesystem"
  [IO.Compression.ZipFile]::CreateFromDirectory($sourceDirectory, $target)
}

function CrossGen([string] $runtime, [string] $publishTarget, [string] $privateSiteExtensionPath)
{
    Write-Host "publishTarget: " $publishTarget
    Write-Host "privateSiteExtensionPath: " $privateSiteExtensionPath

    $selfContained = Join-Path $publishTarget "self-contained"
    $crossGen = "$publishTarget\download\crossgen\crossgen.exe"
	$symbolsPath = Join-Path $publishTarget "Symbols"
    new-item -itemtype directory -path $symbolsPath

    DownloadNupkg "https://dotnet.myget.org/F/dotnet-core/api/v2/package/runtime.$runtime.Microsoft.NETCore.Jit/2.1.0-rtm-26528-02" @("runtimes\$runtime\native\clrjit.dll")  @("$publishTarget\download\clrjit")
    DownloadNupkg "https://dotnet.myget.org/F/dotnet-core/api/v2/package/runtime.$runtime.Microsoft.NETCore.Runtime.CoreCLR/2.1.0-rtm-26528-02"  @("tools\crossgen.exe")  @("$publishTarget\download\crossgen")
    DownloadNupkg "https://www.nuget.org/api/v2/package/Microsoft.Build.Tasks.Core/15.1.1012" @("lib\netstandard1.3\Microsoft.Build.Tasks.Core.dll")  @("$selfContained")
    DownloadNupkg "https://www.nuget.org/api/v2/package/Microsoft.Build.Utilities.Core/15.1.1012" @("lib\netstandard1.3\Microsoft.Build.Utilities.Core.dll")  @("$selfContained")
    if ($runtime -eq "win-x86") {
        DownloadNupkg "https://dotnet.myget.org/F/aspnetcore-dev/api/v2/package/Microsoft.AspNetCore.AspNetCoreModuleV2/2.1.0-a-oob-2-1-oob-16914" @("contentFiles\any\any\x86\aspnetcorev2.dll", "contentFiles\any\any\x86\aspnetcorev2_inprocess.dll") @("$privateSiteExtensionPath\ancm", "$privateSiteExtensionPath\ancm")
    } else {
        DownloadNupkg "https://dotnet.myget.org/F/aspnetcore-dev/api/v2/package/Microsoft.AspNetCore.AspNetCoreModuleV2/2.1.0-a-oob-2-1-oob-16914" @("contentFiles\any\any\x64\aspnetcorev2.dll", "contentFiles\any\any\x64\aspnetcorev2_inprocess.dll") @("$privateSiteExtensionPath\ancm", "$privateSiteExtensionPath\ancm")
    }

    # Publish self-contained app with all required dlls for crossgen
    dotnet publish .\src\WebJobs.Script.WebHost\WebJobs.Script.WebHost.csproj -r $runtime -o "$selfContained" -v q /p:BuildNumber=$buildNumber    

    # Modify web.config for inproc
    dotnet tool install -g dotnet-xdt --version 2.1.0-rc.1
    dotnet-xdt -s "$privateSiteExtensionPath\web.config" -t "$privateSiteExtensionPath\web.InProcess.xdt" -o "$privateSiteExtensionPath\web.config"

    $successfullDlls =@()
    $failedDlls = @()
    Get-ChildItem $privateSiteExtensionPath -Filter *.dll | 
    Foreach-Object {       
        $prm = "/JITPath", "$publishTarget\download\clrjit\clrjit.dll", "/Platform_Assemblies_Paths", "$selfContained", "/nologo", "/in", $_.FullName
        # output for Microsoft.Azure.WebJobs.Script.WebHost.dll is Microsoft.Azure.WebJobs.Script.WebHost.exe.dll by default
        if ($_.FullName -like "*Microsoft.Azure.WebJobs.Script.WebHost.dll") {
            $prm += "/out"        
            $prm += Join-Path $privateSiteExtensionPath "Microsoft.Azure.WebJobs.Script.WebHost.ni.dll"
        }
        # Fix output for System.Private.CoreLib.dll
        if ($_.FullName -like "*System.Private.CoreLib.dll") {
            $prm += "/out"        
            $prm += Join-Path $privateSiteExtensionPath "System.Private.CoreLib.ni.dll"
        }

        & $crossGen $prm >> $buildOutput\crossgenout.$runtime.txt  2>&1

        $niDll = Join-Path $privateSiteExtensionPath $([io.path]::GetFileNameWithoutExtension($_.FullName) + ".ni.dll")
        if ([System.IO.File]::Exists($niDll)) {
            Remove-Item $_.FullName
            Rename-Item -Path $niDll -NewName $_.FullName

            & $crossGen "/Platform_Assemblies_Paths", "$selfContained", "/CreatePDB", "$symbolsPath", $_.FullName >> $buildOutput\crossgenout-PDBs.$runtime.txt 2>&1

            $successfullDlls+=[io.path]::GetFileName($_.FullName)
        } else {
            $failedDlls+=[io.path]::GetFileName($_.FullName)
        }                
    }

    # print results of crossgen process
    $successfullDllsCount = $successfullDlls.length
    $failedDllsCount = $failedDlls.length
    Write-Host "CrossGen($runtime) results: Successfull: $successfullDllsCount, Failed: $failedDllsCount"
    if ($failedDlls.length -gt 0) {
        Write-Host "Failed CrossGen dlls:"
        Write-Host $failedDlls
    }
    
    
    # If not self-contained
    Copy-Item -Path $privateSiteExtensionPath\runtimes\win\native\*  -Destination $privateSiteExtensionPath -Force       

    #read-host "Press ENTER to continue..."
    Remove-Item -Recurse -Force $selfContained -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $publishTarget\download -ErrorAction SilentlyContinue
}

function DownloadNupkg([string] $nupkgPath, [string[]]$from, [string[]]$to) {
    $tempFolderName = [System.IO.Path]::GetFileNameWithoutExtension([System.IO.Path]::GetTempFileName())
    $tempFolder = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath() ,$tempFolderName)
    [System.IO.Directory]::CreateDirectory($tempFolder)
    Remove-Item $tempFolder
    $tempFile = [System.IO.Path]::GetTempFileName() |
        Rename-Item -NewName { $_ -replace 'tmp$', 'zip' } -PassThru

    Write-Host "Downloading '$nupkgPath' to '$tempFile'"
    Invoke-WebRequest -Uri $nupkgPath -OutFile $tempFile
    Write-Host "Extracting from '$tempFile' to '$tempFolder'"
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($tempFile, $tempFolder)
    
    #copy nupkg files
    for ($i=0; $i -lt $from.length; $i++) {
        New-Item $to -Type Directory -Force
        Copy-Item -Path $("$tempFolder\" + $from[$i]) -Destination $($to[$i] + "\") -Force -Verbose
    }
}

function BuildPackages([string] $runtime<#, [bool] $isSelfContained#>) {
    $runtimeSuffix = ""
    if (![string]::IsNullOrEmpty($runtime)) {
        $runtimeSuffix = ".$runtime"
    } else {
        $runtimeSuffix = ".no-runtime"
    }

    $publishTarget = "$buildOutput\publish$runtimeSuffix"
    $siteExtensionPath = "$publishTarget\SiteExtensions"
    $privateSiteExtensionPath = "$siteExtensionPath\Functions"
    
    dotnet publish .\src\WebJobs.Script.WebHost\WebJobs.Script.WebHost.csproj -o "$privateSiteExtensionPath" -v q /p:BuildNumber=$buildNumber /p:IsPackable=false -c Release
    #dotnet publish .\src\WebJobs.Script.WebHost\WebJobs.Script.WebHost.csproj -r $runtime -o "$privateSiteExtensionPath" -v q /p:BuildNumber=$buildNumber /p:IsPackable=false -c Release

    # replace IL dlls with crossgen dlls
    if (![string]::IsNullOrEmpty($runtime)) {
        CrossGen $runtime $publishTarget $privateSiteExtensionPath
    }

    # Do not put siffux to win-x86 zips names
    if ($runtime -eq "win-x86") {
        $runtimeSuffix = "";
    }

    ZipContent $privateSiteExtensionPath "$buildOutput\Functions.Binaries.$extensionVersion-alpha$runtimeSuffix.zip"

    # Project cleanup (trim some project files - this should be revisited)
    Remove-Item -Recurse -Force "$privateSiteExtensionPath\publish" -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "$privateSiteExtensionPath\runtimes\linux" -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "$privateSiteExtensionPath\runtimes\osx" -ErrorAction SilentlyContinue

    # Create site extension packages
    ZipContent $publishTarget "$buildOutput\Functions.Private.$extensionVersion-alpha$runtimeSuffix.zip"

    #Build site extension
    Write-Host "privateSiteExtensionPath: " $privateSiteExtensionPath
    Rename-Item "$privateSiteExtensionPath" "$siteExtensionPath\$extensionVersion-alpha"
    Copy-Item .\src\WebJobs.Script.WebHost\extension.xml "$siteExtensionPath"
    ZipContent $siteExtensionPath "$buildOutput\Functions.$extensionVersion-alpha$runtimeSuffix.zip"

}

dotnet --version
dotnet build .\WebJobs.Script.sln -v q /p:BuildNumber="$buildNumber"

$projects = 
  "WebJobs.Script",
  "WebJobs.Script.WebHost",
  "WebJobs.Script.Grpc"
  
foreach ($project in $projects)
{
  $cmd = "pack", "src\$project\$project.csproj", "-o", "..\..\buildoutput", "--no-build"
  
  if ($includeVersion)
  {
    $cmd += "--version-suffix", "-$buildNumber"
  }
  
  & dotnet $cmd  
}

$bypassPackaging = $env:APPVEYOR_PULL_REQUEST_NUMBER -and -not $env:APPVEYOR_PULL_REQUEST_TITLE.Contains("[pack]")

if ($bypassPackaging){
    Write-Host "Bypassing artifact packaging and CrossGen for pull request." -ForegroundColor Yellow
} else {
    # build IL extensions
    BuildPackages ""

    #build win-x86 extensions
    BuildPackages "win-x86"
}