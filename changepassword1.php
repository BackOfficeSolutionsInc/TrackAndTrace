<?php

session_start();

if(isset($_POST['change']))
{
    header("location:login.php");


include_once 'connect.php';
 $npass=mysqli_real_escape_string($con, $_POST["npass"]);
 $cpass=mysqli_real_escape_string($con, $_POST["cpass"]);
 $user = mysqli_real_escape_string($con, $_POST["user"]);

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
            $qry ="SELECT * FROM register WHERE user= $user";

}
            $hashedPwd = password_hash($cpass, PASSWORD_DEFAULT);
            $sql = "UPDATE register SET state ='0', pass = '".$hashedPwd."'";
            mysqli_query($con, $sql);
					   header("Location:changePassword.php?change=Changing password success");
						exit();
        }
        }
}
?>