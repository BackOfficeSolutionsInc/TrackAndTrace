<html>
<head>
<title>Login Form Design</title>
		  <script src="https://ajax.googleapis.com/ajax/libs/jquery/3.1.0/jquery.min.js"></script>  
		  <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css">

					<link rel="icon" type="image/png" sizes="32x32" href="images/favicon-32x32.png">
					<link rel="icon" type="image/png" sizes="16x16" href="images/favicon-16x16.png">
					<link rel="manifest" href="/site.webmanifest">
					<link rel="mask-icon" href="/safari-pinned-tab.svg" color="#5bbad5">
					<meta name="msapplication-TileColor" content="#da532c">
					<meta name="theme-color" content="#ffffff">
    <link rel="stylesheet" type="text/css" href="style/login_style.css">
<body>
    <div class="loginbox">
    <img src="images/avatar.png" class="avatar">
        <h1>Login Here</h1>
        <form>
            <p>Username</p>
            <input type="text" name="" placeholder="Enter Username">
            <p>Password</p>
            <input type="password" name="" placeholder="Enter Password">
            <input type="submit" name="" value="Login">
            <a href="#">Lost your password?</a><br>
            <a href="#">Don't have an account?</a>
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