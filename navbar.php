
<html>
<head>
<title>
</title>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
    
        <script src="https://ajax.googleapis.com/ajax/libs/jquery/3.3.1/jquery.min.js"></script>
        <script src="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/js/bootstrap.min.js"></script>
    
        <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css">
        <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css">
        <link rel="stylesheet" type="text/css" href="style/style_nav.css">
</head>
<body>
    
<nav class="navbar navbar-default">
  <div class="container-fluid">
    <!-- Brand and toggle get grouped for better mobile display -->
    <div class="navbar-header">
      <button type="button" class="navbar-toggle collapsed" data-toggle="collapse" data-target="#bs-example-navbar-collapse-1" aria-expanded="false">
        <span class="sr-only">Toggle navigation</span>
        <span class="icon-bar"></span>
        <span class="icon-bar"></span>
        <span class="icon-bar"></span>
      </button>
      <a class="navbar-brand" href="#">
    <img alt="Brand" src="images/Logistikuslogosmall.png"></a>
    </div>

    <!-- Collect the nav links, forms, and other content for toggling -->
    <div class="collapse navbar-collapse" id="bs-example-navbar-collapse-1">
      <ul class="nav navbar-nav">
        <li class="active"><a href="#">Home <span class="sr-only">(current)</span></a></li>
        <li><a href="index.php">Track</a></li>
          <?php if(isset($_SESSION['type']) && ($_SESSION['type']=='admin'))	{ 
            
          echo '<li class="dropdown">
          <a href="#" class="dropdown-toggle" data-toggle="dropdown" role="button" aria-haspopup="true" aria-expanded="false">Userm Maintenance <span class="caret"></span></a>
          <ul class="dropdown-menu">
                <li><a href="register.php">Add User</a></li>
				<li class="disabled"><a href="#">Edit User</a></li>
				<li class="disabled"><a href="#">Grant Role</a></li>
          </ul>
          <li class="dropdown">
          <a href="#" class="dropdown-toggle" data-toggle="dropdown" role="button" aria-haspopup="true" aria-expanded="false">Maintenance <span class="caret"></span></a>
          <ul class="dropdown-menu">
				<li><a href="csv.php">Upload</a></li>
				<li class="disabled"><a href="#">Scan Barcode</a></li>
				<li><a href="delete.php">Delete Entry</a></li>
          </ul>
        </li>
    </ul>';
         } ?>
		<?php if(isset($_SESSION['type']) && ($_SESSION['type']=='user'))
		{ ?>
          <li class="dropdown">
          <a href="#" class="dropdown-toggle" data-toggle="dropdown" role="button" aria-haspopup="true" aria-expanded="false">Maintenance <span class="caret"></span></a>
          <ul class="dropdown-menu">
				<!--<li><a href="csv.php">Upload</a></li> -->
				<li class="disabled"><a href="#">Scan Barcode</a></li>
				<li><a href="delete.php">Delete Entry</a></li>
          </ul>
        </li>
    </ul>
      <?php } ?>
      <div class="user" id="user">
            <ul class="navbar-nav navbar-right">
                 <li><?php echo "Welcome " . $_SESSION['user']; ?></li>
             </ul>  
         </div>
        <?php if (isset($_SESSION['user'])) {
            
        echo '<ul class="nav navbar-nav navbar-right">
            <li><a href="do_logout.php">Logout</a></li>          
            ';
        
        }else {
        echo '
            <li><a href="login.php">Login</a></li>          
            </ul>';
}
$txt = "user id date";
 $myfile = file_put_contents('logs.txt', $txt.PHP_EOL , FILE_APPEND | LOCK_EX);?>

    </div><!-- /.navbar-collapse -->
  </div><!-- /.container-fluid -->
</nav> 
</body>
</html>
