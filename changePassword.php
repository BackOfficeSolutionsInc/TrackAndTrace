

<!DOCTYPE html>
<html lang="">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Change Password</title>
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css">
    <link rel="stylesheet" href="style/file_style.css"/>
    <link rel="stylesheet" type="text/css" href="style/change.css">
    <script src="https://ajax.googleapis.com/ajax/libs/jquery/3.1.0/jquery.min.js"></script>
</head>
 
<body>

    <?php session_start();
    
    ?>
    <!--This is Main Menu Area-->
    
    <?php 
    include 'navbar.php';
    ?>
    

    <!--This is Change Password Area-->
   <!-- <img src="../images/Logistikuslogosmall.png" align="center"/><br> -->
    <div class="center"  style="background-color:white";>
           <div class="logo">
               <img src="images/Logistikuslogosmall.png" />
           </div>
             <form action='change-password.php' method='POST'>
        <div class = "row">
        
        </div>
           <p>Username</p>
			<input class="form-control" type="user" name="txtuser" placeholder="Enter Username" maxlength="15" required /><br/ >
            <p>Password</p>
			<input class="form-control" type="password" name="npass" maxlength="15" placeholder="Enter Password" required /><br/ >
            <p>Confirm Password</p>
			<input class="form-control" type="password" name="cpass" maxlength="15" placeholder="Confirm Password" required />
             <br /><br />
            <input type='submit' class="btn btn-primary" name='change' value='Change' />

 </form>
</div>
</body>
</html>

 
            
