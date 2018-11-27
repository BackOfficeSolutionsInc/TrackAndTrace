<?php 

include_once 'connect.php';

if(isset($_POST["change"])) {
	

	$user= mysqli_real_escape_string($con, $_POST["txtuser"]);
    $npass=mysqli_real_escape_string($con, $_POST["npass"]);
	$cpass=mysqli_real_escape_string($con, $_POST["cpass"]);
	

	if (empty($user) || empty($npass) || empty($cpass)) {
		header("Location:changePassword.php?change=empty");
		exit();
	} else {
		
		if (!preg_match("/^[a-zA-Z]*$/", $user) || !preg_match("#[0-9]+#", $npass) || !preg_match("#[0-9]+#", $cpass)) {
			header("Location:changePassword.php?change=invalid password");
            exit(); 
        } else if (strspn($user,"';'")>0  || strspn($npass,"';'")>0  || strspn($cpass,"';'")>0 ) {
            header("Location: login.php?login=empty");
            exit();      
            } else {
            if ($npass != $cpass) {
            header("Location:changePassword.php?changePassword=New Password do not match");
            exit(); 
                $user_id=mysqli_real_escape_string($con, $_POST["user_id"]);
                $user=mysqli_real_escape_string($con, $_POST["user"]);
                $pass=mysqli_real_escape_string($con, $_POST["pass"]);
                $email=mysqli_real_escape_string($con, $_POST["email"]);
                $state=mysqli_real_escape_string($con, $_POST["state"]);
				$sql = "SELECT user, pass, state FROM register WHERE email='$email'";
				   $result = mysqli_query($con, $sql);
				   $resultCheck = mysqli_num_rows($result);
                   if ($resultCheck == 1) {	
                   header("Location: changePassword.php?changePassword=error");
			       exit();
                       $user_id = $rows['user_id'];
                       $user = $rows['user'];
                       $pass = $rows['pass'];
                       $email = $rows['email'];
                       $state = $rows['state']; 
					}
 
				               

				   }else {

					   $hashedPwd = password_hash($cpass, PASSWORD_DEFAULT);
					   $sql  = "UPDATE register SET state ='0', pass = '$hashedPwd' where user='$user' ";
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