@echo off
echo Building Telegram Unlock...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize+ /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll,Microsoft.CSharp.dll /out:TelegramUnlock.exe src\TelegramUnlock.cs
if %ERRORLEVEL% EQU 0 (
    echo Build successful: TelegramUnlock.exe
) else (
    echo Build failed!
)
pause
