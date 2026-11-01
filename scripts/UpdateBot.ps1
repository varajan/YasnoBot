
# ================= CONFIGURATION =================

$RepoOwner = "varajan"
$RepoName  = "YasnoBot"
$AssetName = "YasnoBot.zip"

$ExeNames = @(
    "Bot.exe",
)

$PreserveFiles = @(
    "DB.db"
    "logs"
    "botID.txt"
)

$InstallDir = "C:\YasnoBot"
$WorkDir    = "C:\YasnoBotUpdater"

# =================================================

$ErrorActionPreference = "Stop"

$PreserveDir = Join-Path $WorkDir "preserved"
$LogFile     = Join-Path $WorkDir "update.log"
$VersionFile = Join-Path $InstallDir "version.txt"
$ApiUrl      = "https://api.github.com/repos/$RepoOwner/$RepoName/releases/latest"

$ParentDir = Split-Path $InstallDir -Parent
$StageDir  = "$InstallDir.new"
$BackupDir = "$InstallDir.old"

New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null
if (-not (Test-Path -LiteralPath $ParentDir -PathType Container)) {
    New-Item -ItemType Directory -Path $ParentDir -Force | Out-Null
}

function Write-Log {
    param([string]$Message)

    $Line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $Message"
    Add-Content -Path $LogFile -Value $Line -Encoding UTF8
}

function Get-AppProcesses {
    Get-CimInstance Win32_Process |
        Where-Object {
            $_.Name -in $ExeNames -and
            $_.ExecutablePath -and
            $_.ExecutablePath.StartsWith(
                $InstallDir,
                [StringComparison]::OrdinalIgnoreCase
            )
        }
}

function Stop-App {
    $Processes = @(Get-AppProcesses)

    foreach ($Process in $Processes) {
        Write-Log "Stopping process PID=$($Process.ProcessId)"
        Stop-Process -Id $Process.ProcessId -Force
    }

    $Deadline = (Get-Date).AddSeconds(15)

    do {
        Start-Sleep -Milliseconds 500
        $Remaining = @(Get-AppProcesses)
    } while ($Remaining.Count -gt 0 -and (Get-Date) -lt $Deadline)

    if (@(Get-AppProcesses).Count -gt 0) {
        throw "Failed to stop the application processes."
    }
}

function Start-App {
    foreach ($ExeName in $ExeNames) {
        $ExePath = Join-Path $InstallDir $ExeName

        if (!(Test-Path $ExePath)) {
            throw "Executable not found: $ExePath"
        }

        $Running = Get-AppProcesses |
            Where-Object { $_.Name -eq $ExeName }

        if (!$Running) {
            Start-Process `
                -FilePath $ExePath `
                -WorkingDirectory $InstallDir

            Write-Log "Started $ExeName"
        }
        else {
            Write-Log "$ExeName is already running."
        }
    }
}

function Save-PreservedFiles {
    if (Test-Path $PreserveDir) {
        Remove-Item $PreserveDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $PreserveDir -Force |
        Out-Null

    foreach ($RelativePath in $PreserveFiles) {
        $Source = Join-Path $InstallDir $RelativePath
        $Destination = Join-Path $PreserveDir $RelativePath

        if (Test-Path $Source) {
            $DestinationParent = Split-Path $Destination -Parent

            New-Item -ItemType Directory -Path $DestinationParent -Force |
                Out-Null

            Copy-Item -Path $Source -Destination $Destination -Recurse -Force

            Write-Log "Preserved: $RelativePath"
        }
    }
}

function Restore-PreservedFiles {
    foreach ($RelativePath in $PreserveFiles) {
        $Source = Join-Path $PreserveDir $RelativePath
        $Destination = Join-Path $InstallDir $RelativePath

        if (Test-Path $Source) {
            $DestinationParent = Split-Path $Destination -Parent

            New-Item -ItemType Directory -Path $DestinationParent -Force |
                Out-Null

            Copy-Item -Path $Source -Destination $Destination -Recurse -Force

            Write-Log "Restored: $RelativePath"
        }
    }
}

try {
    # Get the latest published release
    $Headers = @{
        "Accept" = "application/vnd.github+json"
        "User-Agent" = "YasnoBot-Updater"
    }

    $Release = Invoke-RestMethod `
        -Uri $ApiUrl `
        -Headers $Headers `
        -TimeoutSec 30

	$LatestVersion = [string]$Release.tag_name

	if ([string]::IsNullOrWhiteSpace($LatestVersion)) {
		throw "GitHub did not return a release version."
	}

	$CurrentVersion = ""

	if (Test-Path -LiteralPath $VersionFile) {
		$CurrentVersion = (Get-Content -LiteralPath $VersionFile -Raw).Trim()
	}

	Write-Log "Installed version: '$CurrentVersion'; latest version: '$LatestVersion'"

	$AllExecutablesExist = $true

	foreach ($ExeName in $ExeNames) {
		if (-not (Test-Path -LiteralPath (Join-Path $InstallDir $ExeName) -PathType Leaf)) {
			$AllExecutablesExist = $false
			break
		}
	}

	if ($CurrentVersion -eq $LatestVersion -and $AllExecutablesExist) {
		Write-Log "Already up to date. Download skipped."
		Start-App
		return
	}

	Write-Log "Update required. Downloading version $LatestVersion"

	$Asset = @(
		$Release.assets |
			Where-Object { $_.name -eq $AssetName }
	) | Select-Object -First 1

	if (!$Asset) {
		throw "Asset $AssetName was not found in release $LatestVersion."
	}

	$ZipPath = Join-Path $WorkDir "download.zip"

	Write-Log "Downloading from $($Asset.browser_download_url)"

	Invoke-WebRequest `
		-Uri $Asset.browser_download_url `
		-OutFile $ZipPath `
		-TimeoutSec 300

    $ZipPath = Join-Path $WorkDir "download.zip"

    # Download the release archive
    Invoke-WebRequest `
        -Uri $Asset.browser_download_url `
        -OutFile $ZipPath `
        -TimeoutSec 300

    if (!(Test-Path $ZipPath) -or (Get-Item $ZipPath).Length -eq 0) {
        throw "The ZIP archive was not downloaded or is empty."
    }

    # Extract into a staging directory
    if (Test-Path $StageDir) {
        Remove-Item $StageDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $StageDir | Out-Null

    Expand-Archive `
        -Path $ZipPath `
        -DestinationPath $StageDir `
        -Force

    # Verify that all required executables are present
    foreach ($ExeName in $ExeNames) {
        $NewExe = Join-Path $StageDir $ExeName

        if (!(Test-Path $NewExe)) {
            throw "Executable $ExeName was not found in the archive root."
        }
    }

    # Store the installed release version
    Set-Content `
        -Path (Join-Path $StageDir "version.txt") `
        -Value $LatestVersion `
        -Encoding UTF8

    # Stop applications and preserve local files
    Stop-App
    Save-PreservedFiles

    # Remove the previous backup
    if (Test-Path $BackupDir) {
        Remove-Item $BackupDir -Recurse -Force
    }

    # Back up the current installation
    if (Test-Path $InstallDir) {
        Move-Item -Path $InstallDir -Destination $BackupDir
    }

    try {
        # Install the new version
        Move-Item -Path $StageDir -Destination $InstallDir

        Restore-PreservedFiles

        Start-App

        Write-Log "Update to $LatestVersion completed successfully."

        # Keep the previous installation as a backup
    }
    catch {
        Write-Log "Failed to start the new version: $_"

        # Restore the previous installation
        if (Test-Path $InstallDir) {
            Remove-Item $InstallDir -Recurse -Force
        }

        if (Test-Path $BackupDir) {
            Move-Item -Path $BackupDir -Destination $InstallDir
            Start-App
        }

        throw
    }
}
catch {
    Write-Log "ERROR: $_"

    # Restore the previous installation if the installation directory is missing
    if (!(Test-Path $InstallDir) -and (Test-Path $BackupDir)) {
        Move-Item -Path $BackupDir -Destination $InstallDir
    }

    # Start the applications if the installation is available
    $CanStart = $true

    foreach ($ExeName in $ExeNames) {
        if (!(Test-Path (Join-Path $InstallDir $ExeName))) {
            $CanStart = $false
            break
        }
    }

    if ($CanStart) {
        try {
            Start-App
        }
        catch {
            Write-Log "Failed to start the applications: $_"
        }
    }
}
finally {
    # Remove temporary files
    if (Test-Path $StageDir) {
        Remove-Item $StageDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    $ZipPath = Join-Path $WorkDir "download.zip"

    if (Test-Path $ZipPath) {
        Remove-Item $ZipPath -Force -ErrorAction SilentlyContinue
    }
}
