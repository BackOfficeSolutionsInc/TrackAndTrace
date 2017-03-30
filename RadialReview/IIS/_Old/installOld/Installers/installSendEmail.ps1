$command = "PowerShell -NoProfile -ExecutionPolicy RemoteSigned -WindowStyle Hidden -File 'C:\install\scripts\SendEmail.ps1'"

SCHTASKS /Create /TN "Error Monitor" /TR $command /SC ONEVENT /RL Highest /EC Application /MO "*[System[(EventID=1309)]]" /f