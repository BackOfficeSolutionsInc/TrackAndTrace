@if (@CodeSection == @Batch) @then

rem Use %SendKeys% to send keys to the keyboard buffer
set SendKeys=CScript //nologo //E:JScript "%~F0"

rem Start the other program in the same Window
rem start "" /B cmd

:loop

%SendKeys% "%~1{ENTER}"

goto :loop


@end



// JScript section

WScript.StdIn.Read(0);
var strMyName = WScript.StdIn.ReadLine();
var WshShell = WScript.CreateObject("WScript.Shell");
WshShell.SendKeys(strMyName);
