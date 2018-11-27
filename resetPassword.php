
<?php
	include "functions.php";
	include "connect.php";

	if (isset($_GET['email']) && isset($_GET['token'])) {
		

		$email = $con->real_escape_string($_GET['email']);
		$token = $con->real_escape_string($_GET['token']);

		$sql = $con->query("SELECT user FROM register WHERE
			email='$email' AND token='$token' AND token<>'' AND tokenExpire > NOW()
		");

		if ($sql->num_rows > 0) {
			$newPassword = generateNewString();
			$hashedPwd = password_hash($newPassword , PASSWORD_DEFAULT);
			//$newPasswordEncrypted = password_hash($newPassword, PASSWORD_BCRYPT);
			$con->query("UPDATE register SET token='', pass = '$hashedPwd'
				WHERE email='$email'
			");
            ?>
<html>
    <head>
    <title> Temporary Password </title>
    <link rel="stylesheet" href="https://stackpath.bootstrapcdn.com/bootstrap/4.1.3/css/bootstrap.min.css" integrity="sha384-MCw98/SFnGE8fJT3GXwEOngsV7Zt27NXFoaoApmYm81iuXoPkFOJwJ8ERdknLPMO" crossorigin="anonymous">
    <link rel="stylesheet" type="text/css" href="style/password-center.css">
               
    <script src="https://code.jquery.com/jquery-3.3.1.slim.min.js" integrity="sha384-q8i/X+965DzO0rT7abK41JStQIAqVgRVzpbzo5smXKp4YfRvH+8abtTE1Pi6jizo" crossorigin="anonymous"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.14.3/umd/popper.min.js" integrity="sha384-ZMP7rVo3mIykV+2+9J3UJ46jBk0WLaUAdn689aCwoqbBJiSnjAK/l8WvCWPIPm49" crossorigin="anonymous"></script>
    <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.1.3/js/bootstrap.min.js" integrity="sha384-ChfqqxuZUCnJSK3+MXmPNIyE6ZbWh2IMqE241rYiqJxyMiZ6OW/JmZQ5stwEULTy" crossorigin="anonymous"></script>
    </head>
           <body>
           
           <div class="center"  style="background-color:white";>
           <div class="logo">
               <img src="images/Logistikuslogosmall.png" />
           </div>
           <br>
            <?php
            echo "Your Temporary password is:<strong> $newPassword<br><br></strong><a href='login.php'><button type='button' class='btn btn-secondary'>Click to Login</button></a>";
			
            
		} else
			redirectToLoginPage();
	} else {
		redirectToLoginPage();
	}
?>
               </div>
    </body>
</html>
