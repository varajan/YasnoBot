# Set-ExecutionPolicy RemoteSigned -Scope LocalMachine -Force

$TaskName = "Update bot"
$ScriptPath = "C:\YasnoBotUpdater\UpdateBot.ps1"
$ActionArg = "-ExecutionPolicy Bypass -File `"$ScriptPath`""

$Action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument $ActionArg
$Trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes 15) -RepetitionDuration ([TimeSpan]::MaxValue)
$Principal = New-ScheduledTaskPrincipal -UserId "NT AUTHORITY\SYSTEM" -LogonType ServiceAccount -RunLevel Highest
Register-ScheduledTask -TaskName $TaskName -Action $Action -Trigger $Trigger -Principal $Principal -Force

Write-Host "Done!" -ForegroundColor Green
