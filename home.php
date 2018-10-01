<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title><?php 
    session_start();
    include('connect.php');
    echo "Welcome " . $_SESSION['user'];
    ?></title>
</head>

<body>
<?php 
    include('navbar.php');
    include('header.php');
    echo "Welcome " . $_SESSION['user'];
    
    ?>

<div><a href="login.php">Logout</a></div>	
</form>
</body>
</html>