<?php 
session_start();
if(isset($_POST["add_user"])) {
	
	$id =  $_SESSION['user'];
    $date = date('Y-m-d H:i:s');

	include_once 'connect.php';
	
	$user= mysqli_real_escape_string($con, $_POST["txtuser"]);
    $email= mysqli_real_escape_string($con, $_POST["txtemail"]);
	$type= mysqli_real_escape_string($con, $_POST["txttype"]);
    $pass=mysqli_real_escape_string($con, $_POST["txtpass"]);
	$cpass=mysqli_real_escape_string($con, $_POST["cpass"]);
	

	if (empty($user) || empty($email) || empty($pass) || empty($cpass)) {
		header("Location:register.php?signup=empty");
		exit();
	} else {
		if (!preg_match("/^[a-zA-Z]*$/", $user) || !preg_match("#[0-9]+#", $pass) ) {
			header("Location:register.php?signup=invalid");
			exit();
		} else {
				$sql = "SELECT * FROM register WHERE user='$user'";
				   $result = mysqli_query($con, $sql);
				   $resulCheck = mysqli_num_rows($result);
				   	if ($pass != $cpass) {
						header("Location:register.php?signup=Password do not match");
						exit();
					}
				   if ($resulCheck > 0) {				   
					   header("Location:register.php?signup=Username is Taken");
						exit();

				   }else {
					   $hashedPwd = password_hash($cpass, PASSWORD_DEFAULT);
					   $sql  = "INSERT INTO register (user, email, type, pass, date_created, createdBy) VALUES ('".$user."', '".$email."','".$type."', '".$hashedPwd."','".$date."','".$id."');";
					   mysqli_query($con, $sql);
					   
					   header("Location:register.php?signup=Adding username success");
						exit();
						
				   }
			}
		}
	}else {
		header("Location:register.php");
		exit();
		 }
	include_once 'register.php';
?>