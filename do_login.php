<?php 

session_start();

if(isset($_POST["submit"])){
	
	include('connect.php');
	
	$user=mysqli_real_escape_string($con, $_POST["txtuser"]);
    $pass=mysqli_real_escape_string($con, $_POST["txtpass"]);

    if (empty($user) || empty($pass)) {
		header("Location: login.php?login=empty");
		exit();
        
	} else {
        
		$sql ="SELECT * FROM register WHERE user='$user'";
		$result = mysqli_query($con, $sql);
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
                if($_SESSION["type"]=='admin'){
                header("Location:home_admin.php");
                    
			     }else
				    header("Location:home.php");
		      }
					
			}
		}
	}
} else {
	header("Location: login.php?login=error");
	exit();
}
	