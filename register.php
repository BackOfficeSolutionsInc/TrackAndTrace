<html>
<head>
<title>Create User Account/s</title>
		  <script src="https://ajax.googleapis.com/ajax/libs/jquery/3.1.0/jquery.min.js"></script>  
		  <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css">

					<link rel="icon" type="image/png" sizes="32x32" href="images/favicon-32x32.png">
					<link rel="icon" type="image/png" sizes="16x16" href="images/favicon-16x16.png">
					<link rel="manifest" href="/site.webmanifest">
					<link rel="mask-icon" href="/safari-pinned-tab.svg" color="#5bbad5">
					<meta name="msapplication-TileColor" content="#da532c">
					<meta name="theme-color" content="#ffffff">
    <link rel="stylesheet" type="text/css" href="style/reg_style.css">
<body>
    <div class="registerbox">
    <img src="images/avatar.png" class="avatar">
        <h1>Create User Account</h1>
       <form method="post" action="do_register.php" enctype="multipart/form-data">
	   			<p>User Type</p>
			<div class="styled-select slate">
				  <select type="type" name="txttype" required />
					<option>Select...</option>
					<option>Admin</option>
					<option>User</option>
				  </select>
				</div>
            <p>Username</p>
			<input type="user" name="txtuser" placeholder="Enter Username" maxlength="15" required />
            <p>Password</p>
			<input type="password" name="txtpass" maxlength="15" placeholder="Enter Password" required />
            <p>Confirm Password</p>
			<input type="password" name="cpass" maxlength="15" placeholder="Confirm Password" required />
			<input type="submit" name="submit" class="register_btn" value="REGISTER" />
                        <p>Already a member? <a href="login.php">Sign in</a></p>
        </form>

    </div>

</body>
</head>
</html>
<script>   
		$('input,textarea').focus(function(){
       $(this).removeAttr('placeholder');
    });
</script>