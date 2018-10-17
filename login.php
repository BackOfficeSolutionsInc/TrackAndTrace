<?php

if(isset($_SESSION['user'])){
    session_start();
    header('location:login.php');
}
?>


<html>
<head>
    <script language = "javascript" type ="text/javascript">
    window.history.forward();
    </script>
<title>Login</title>
    <link rel="stylesheet" type="text/css" href="style/login_style.css">
<body>
    <div class="loginbox">
    <img src="images/avatar.png" class="avatar">
       <h1>Login Here</h1>
    <form method="post" action="do_login.php"  enctype="multipart/form-data" >
        <p>Username</p>
        <input type="text" name="txtuser" placeholder="Enter Username" required /><br><br>
        <p>Password</p>
        <input type="password" name="txtpass" placeholder="Enter Password" required /> <br><br>
		<input type="submit" name="submit" value="LOGIN" />
        <a href="#">Lost your password?</a><br>
        <!--<a href="register.php">Don't have an account?</a>-->											
</form>
        
    </div>

</body>
</head>
</html>

