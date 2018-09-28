<?php 
session_start();
?>

<html>
<head>
<title>Login Form Design</title>
    <link rel="stylesheet" type="text/css" href="style/login_style.css">
<body>
    <div class="loginbox">
    <img src="images/avatar.png" class="avatar">
        <h1>Login Here</h1>
        <form action="index.php" method="POST">
            <p>Username</p>
			<input type="user" name="txtuser" placeholder="Enter Username" required />
            <p>Password</p>
			<input type="password" name="txtpass" placeholder="Enter Password" required />
            <input type="submit" name="" value="Login">
            <a href="#">Lost your password?</a><br>
            <a href="#">Don't have an account?</a>
        </form>
        
    </div>

</body>
</head>
</html>