<?php 

include_once 'connect.php';

if(isset($_POST["change"])) {
	

	//$oldpass= mysqli_real_escape_string($con, $_POST["oldpass"]);
    $npass=mysqli_real_escape_string($con, $_POST["npass"]);
	$cpass=mysqli_real_escape_string($con, $_POST["cpass"]);
	

	if (empty($npass) || empty($cpass)) {
		header("Location:changePassword.php?change=empty");
		exit();
	} else {
		
		if (!preg_match("#[0-9]+#", $npass) || !preg_match("#[0-9]+#", $cpass)) {
			header("Location:changePassword.php?change=invalid password");
            exit(); 
        } else if (strspn($npass,"';'")>0  || strspn($npass,"';'")>0 ) {
            header("Location: login.php?login=empty");
            exit();      
            } else {
            if ($npass != $cpass) {
            header("Location:changePassword.php?changePassword=New Password do not match");
            exit(); 

				$sql = "SELECT pass, state FROM register WHERE user='{$_SESSION['user']}'";
				   $result = mysqli_query($con, $sql);
				   $resultCheck = mysqli_num_rows($result);
                   if ($resultCheck == 1) {	
                   header("Location: changePassword.php?changePassword=error");
			       exit();
					}
 
				               

				   }else {
                       $user = $rows['user'];
                       $pass = $rows['pass'];
                       $email = $rows['email'];
                       $state = $rows['state']; 
					   $hashedPwd = password_hash($cpass, PASSWORD_DEFAULT);
					   $sql  = "UPDATE register SET state =0, pass = '$hashedPwd' WHERE user='$user' ";
					   mysqli_query($con, $sql);
					   header("Location:changePassword.php?change=Changing password success");
						exit();
                   }
				 }
                }
    }else {
		echo "Login";
		 }
?>