// JScript section

WScript.StdIn.Read(0);
var strMyName = WScript.StdIn.ReadLine();
var WshShell = WScript.CreateObject("WScript.Shell");
WshShell.SendKeys(strMyName);
WScritp.StdOut.WriteLine("Line " + (stdin.Line - 1) + ": " + str);
