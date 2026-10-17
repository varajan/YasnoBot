dotnet publish -c Release

del /q publish\BotId.txt
del /q publish\DB.*
del /q publish\logs\*.*

powershell -NoProfile -Command "Compress-Archive -Path 'publish\*' -DestinationPath 'YasnoBot.zip' -Force"