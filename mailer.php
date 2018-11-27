<?php
        use PHPMailer\PHPMailer\PHPMailer;
        function sendEmail($email,$msg,$subject){

	        require_once "PHPMailer/PHPMailer.php";
	        require_once "PHPMailer/Exception.php";

	        $mail = new PHPMailer();
	        $mail->addAddress($email);
	        $mail->setFrom("ryan.espina@backofficesolutions.ph", "BSI");
	        $mail->Subject = $subject;
	        $mail->isHTML(true);
	        $mail->Body = $msg;
            $mail->send();
        
    }
?>