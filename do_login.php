<?php 

session_start();

if(isset($_POST["submit"])){
	
	include('connect.php');
	
	$user=mysqli_real_escape_string($con, $_POST["user"]);
	$pass=mysqli_real_escape_string($con, $_POST["pass"]);
	
	if (empty($user) || empty($pass)) {
		header("Location: login.php?login=empty");
		exit();
	} else {
		$sql ="SELECT * FROM register WHERE user='$user'";
		$result = mysqli_quety(c$con, $sql);
		$resultCheck = mysqli_num_rows($result);
		if ($resultCheck < 1) {
			header("Location: login.php?login=error");
			exit();
		}else {
			if ($row = mysqli_fetch_assoc($result)) {
				$hashedPwdCheck = password_verify($pass, $row['pass']);
				if ($hashedPwdCheck == false) {
					header("Location: login.php?login=error");
					exit();
				}elseif ($hashedPwdCheck == true) {
					//User Login
					$_SESSION['user'] = $row['user'];
					$_SESSION['type'] = $row['type'];
					$_SESSION['pass'] = $row['pass'];
					header("Location: login.php?login=success");
					exit();
				}
			}
		}
	}
} else {
	header("Location: login.php?login=error");
	exit();
}
	$query=mysqli_query($con,"SELECT user,pass,type FROM register");
	while($row=mysqli_fetch_array($query))
	{
		$db_user=$row["user"];
		$db_pass=$row["pass"];
		$db_type=$row["type"];
		
		if($user==$db_user && $pass==$db_pass){
			session_start();
			$_SESSION["user"]=$db_user;
			$_SESSION["type"]=$db_type;
			
			if($_SESSION["type"]=='admin'){
				header("Location:home_admin.php");
			}
			else
				header("Location:home.php");
		}
		else
			echo("Please check Username and Password");
	}}