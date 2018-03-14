@if (@CodeSection == @Batch) @then

rem Use %SendKeys% to send keys to the keyboard buffer
set SendKeys=CScript //nologo //E:JScript "%~F0"

rem Start the other program in the same Window
rem start "" /B cmd

set /P "=Wait and send a command: " < NUL
ping -n 5 -w 1 127.0.0.1 > NUL
%SendKeys% "%~1{ENTER}"

goto :EOF


@end


// JScript section

var WshShell = WScript.CreateObject("WScript.Shell");
WshShell.SendKeys(WScript.Arguments(0));