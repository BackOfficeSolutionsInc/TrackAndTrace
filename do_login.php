<?php 

session_start();
 

if(isset($_POST["submit"])){
	
	include('connect.php');
	    
	$user=mysqli_real_escape_string($con, $_POST["txtuser"]);
    $pass=mysqli_real_escape_string($con, $_POST["txtpass"]);

    if (empty($user) || empty($pass)) {
		header("Location: login.php?login=empty");
		exit();
    } else if ( strspn($user,"';'")>0 || strspn($pass,"';'")>0 ) {
		header("Location: login.php?login=empty");
		exit();       
	} else {
 		$sql ="SELECT * FROM register WHERE user='$user' OR email = '$user'";
		$result = mysqli_query($con, $sql);
		$resultCheck = mysqli_num_rows($result);
		if ($resultCheck < 1) {
			header("Location: login.php?login=error");
            
			exit();
		}else {
                // check if password is correct
           		if ($row = mysqli_fetch_assoc($result)) {
				$hashedPwdCheck = password_verify($pass, $row['pass']);
				if ($hashedPwdCheck == false) {
					header("Location: login.php?login=error");
					exit();
                    
               //Dehashing Password login
				}elseif ($hashedPwdCheck == true) {
					//User Login-Password
                    if( $row['state']==1){
                        header("Location: changePassword.php");
                        
                        exit();
                    }else {
					   $_SESSION['user'] = $row['user'];
					   $_SESSION['type'] = $row['type'];
					   $_SESSION['pass'] = $row['pass'];
                       $_SESSION['user_id'] =  $row['user_id'];
                      
                        echo "<script type='text/javascript'>alert('successfully Login !')</script>"; 
                       if($_SESSION["type"]=='admin'){
                       header("Location:csv.php");
                
			     }else
                    echo "<script>alert('"+$user_id['user_id']+"')</script>";     
				    header("Location:csv.php");
                    }
		      }
					
			}
		}
	}
} else {
	header("Location: login.php?login=error");
	exit();
}