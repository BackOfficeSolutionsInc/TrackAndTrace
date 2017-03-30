$SMTPServer = "smtp.gmail.com"
$SMTPPort = "587"
$Username = "clay.upton@radialreview.com"
$Password = "iigdwnmmjyjpcxsv"

$to = "clay.upton+eventlog@mytractiontools.com"
$subject = "Unhandled Application Error @ "
$subject += (Get-Date).AddHours(-5)
$body = (wevtutil qe Application "/q:*[System [(EventID=1309)]]" /f:text /rd:true /c:1) | Out-String

$message = New-Object System.Net.Mail.MailMessage
$message.subject = $subject
$message.body = $body
$message.to.add($to)
$message.from = $Username


$smtp = New-Object System.Net.Mail.SmtpClient($SMTPServer, $SMTPPort);
$smtp.EnableSSL = $true
$smtp.Credentials = New-Object System.Net.NetworkCredential($Username, $Password);
$smtp.send($message)
write-host "Mail Sent"