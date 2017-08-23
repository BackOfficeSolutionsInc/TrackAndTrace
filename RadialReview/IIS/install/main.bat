echo "START_MAIN"
echo "START NIST POLLING"
REM From https://forums.aws.amazon.com/thread.jspa?messageID=526510
regedit.exe /S Installers/NistPolling.reg
net stop w32time
w32tm /config /syncfromflags:manual /manualpeerlist:"0.pool.ntp.org,0x1 time.nist.gov,0x1 1.pool.ntp.org,0x1 2.pool.ntp.org,0x1"
net start w32time

REM To review the config
w32tm /query /configuration

echo "END NIST POLLING"
echo "END MAIN"