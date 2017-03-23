C:\Windows\System32\xcopy.exe /Y "c:\install\Files\ec2service\config.xml" "C:\Program Files\Amazon\Ec2ConfigService\Settings\config.xml"
C:\Windows\System32\xcopy.exe /Y "c:\install\Files\ec2service\AWS.EC2.Windows.CloudWatch.json" "C:\Program Files\Amazon\Ec2ConfigService\Settings\AWS.EC2.Windows.CloudWatch.json"

net stop ec2config
net start ec2config