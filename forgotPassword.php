<?php
    use PHPMailer\PHPMailer\PHPMailer;
    require_once 'connect.php';

    if (isset($_POST['email'])) {

        $email = $con->real_escape_string($_POST['email']);

        $sql = $con->query("SELECT id FROM users WHERE email='$email'");
        if ($sql->num_rows > 0) {

            $token = generateNewString();

	        $con->query("UPDATE users SET token='$token', 
                      tokenExpire=DATE_ADD(NOW(), INTERVAL 5 MINUTE)
                      WHERE email='$email'
            ");

	        require_once "PHPMailer/PHPMailer.php";
	        require_once "PHPMailer/Exception.php";

	        $mail = new PHPMailer();
	        $mail->addAddress($email);
	        $mail->setFrom("ryan.espina@backofficesolutions.ph", "BSI");
	        $mail->Subject = "Reset Password";
	        $mail->isHTML(true);
	        $mail->Body = "
	            Hi,<br><br>
	            
	            In order to reset your password, please click on the link below:<br>
	            <a href='
	            http://localhost/reset/forgot/resetPassword.php?email=$email&token=$token
	            '>http://localhost/reset/forgot/resetPassword.php?email=$email&token=$token</a><br><br>
	            
	            Kind Regards,<br>
	            My Name
	        ";

	        if ($mail->send())
    	        exit(json_encode(array("status" => 1, "msg" => 'Check Your Email')));
    	    else
    	        exit(json_encode(array("status" => 0, "msg" => 'Try again')));
        } else
            exit(json_encode(array("status" => 0, "msg" => 'Check Your Email adress')));
    }
?>
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Forgot Password</title>
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css">
    <link rel="stylesheet" href="style/file_style.css"/>
    <script src="https://ajax.googleapis.com/ajax/libs/jquery/3.1.0/jquery.min.js"></script>
</head>
<body>
    <?php
    
 session_start();
if(!isset($_SESSION['user'])){
    header('location:login.php');
    }
    include("navbar.php");
    include("header.php");
    
    ?>
    <div class="container" style="margin-top: 100px;">
        <div class="row justify-content-center">
            
            <div class="col-md-6 col-md-offset-3" align="center">
                <!--<img src="../images/Logistikuslogosmall.png" align="center" /><br><br>-->
                <input class="form-control" id="email" placeholder="Enter your email adress"><br>
                <input type="button" class="btn btn-primary" value="Reset Password">
                <br><br>
                <p id="response"></p>
            </div>
        </div>
    </div>
    <script src="http://code.jquery.com/jquery-3.3.1.min.js" integrity="sha256-FgpCb/KJQlLNfOu91ta32o/NMZxltwRo8QtmkMRdAu8=" crossorigin="anonymous"></script>
    <script type="text/javascript">
        var email = $("#email");

        $(document).ready(function () {
            $('.btn-primary').on('click', function () {
                if (email.val() != "") {
                    email.css('border', '1px solid green');

                    $.ajax({
                       url: 'forgotPassword.php',
                       method: 'POST',
                       dataType: 'json',
                       data: {
                           email: email.val()
                       }, success: function (response) {
                            if (!response.success)
                                $("#response").html(response.msg).css('color', "red");
                            else
                                $("#response").html(response.msg).css('color', "green");
                        }
                    });
                } else
                    email.css('border', '1px solid red');
            });
        });
    </script>
</body>
</html>
