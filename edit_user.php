<?php
include 'connect.php';
include 'navbar.php';
$session_username = $_SESSION['user'];
$type = $_SESSION['type'];
if(empty($_SESSION['user'])){
    header("location:login.php");
}
$id =  $_SESSION['user'];
?>
<!DOCTYPE html>
<html lang="en">

<head>
    <title>Edit User</title>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <!-- The above 3 meta tags *must* come first in the head; any other head content must come *after* these tags -->
    <!-- Latest compiled and minified CSS -->
    <link rel="stylesheet" href="style/bootstrap.min.css">

    <!-- Loader -->
    <link rel="stylesheet" href="style/loader.css">

    <link rel="stylesheet" href="style/jquery.dataTables.min.css">
    <link rel="stylesheet" href="style/dataTables.bootstrap.min.css">
    <link rel="stylesheet" href="style/responsive.bootstrap.min.css">

    <script src="js/jquery.dataTables.min.js"></script>
    <script>
        $(document).ready(function() {
                $('#example').DataTable({});
            });

        </script>


</head>

<body>
    <style>
        span.glyphicon-user {
            font-size: 1.5em;
        }
        span.glyphicon-envelope {
            font-size: 1.5em;
        }
        span.glyphicon-lock {
            font-size: 1.5em;
        }
        span.glyphicon-exclamation-sign {
            font-size: 1.5em;
            color: darkorange;
        }
        
    </style>
    <div class="container">

    <div>

    <a href="#add" data-toggle="modal">
    <button type='button' class='btn btn-success btn-sm'><span class='glyphicon glyphicon-plus' aria-hidden='true'></span> Add user</button>
    </a>
    </div>
    <br>
        <table id="example" class="display nowrap" cellspacing="0" width="100%">
            <thead>
                <tr>
                <th align="center">UserID</th>
                <th align="center">UserName</th>
                <th align="center">Email</th>
                <th align="center">Role</th>
                <th align="center">DateCreated</th>
                <th align="center">CreatedBy</th>
                <th align="center">Actions</th>
                </tr>
            </thead>
            <tfoot>
                <tr>
                <th align="center">UserID</th>
                <th align="center">UserName</th>
                <th align="center">Email</th>
                <th align="center">Role</th>
                <th align="center">DateCreated</th>
                <th align="center">CreatedBy</th>
                <th align="center">Actions</th>
                </tr>
            </tfoot>
            <tbody>
                <?php 
                echo "<script>alert('Enter');</script>";
                    $sql = "SELECT user_id, user, email, type, token, date_created, createdBy FROM register";
                    $result = $con->query($sql);
                    if ($result->num_rows > 0) {
                        // output data of each row
                        while($row = $result->fetch_assoc()) {
                            $user_id = $row['user_id'];
                            $user = $row['user'];
                            $email = $row['email'];
                            $type = $row['type'];
                            $token = $row['token'];
                            $date_created = $row['date_created'];
                            $createdBy = $row['createdBy'];
                            
                    ?>
                <tr>
                    <td>
                        <?php echo $user_id; ?>
                    </td>
                    <td>
                        <?php echo $user; ?>
                    </td>
                    <td>
                        <?php echo $email; ?>
                    </td>
                    <td>
                        <?php echo $type; ?>
                    </td>
                    <td>
                        <?php echo $date_created; ?>
                    </td>
                    <td>
                        <?php echo $createdBy; ?>
                    </td>

                    <td>
                       
                        <a href="#edit<?php echo $user_id;?>" data-toggle="modal">
                            <button type='button' class='btn btn-warning btn-sm'><span class='glyphicon glyphicon-edit' aria-hidden='true'></span></button>
                        </a>
                        <a href="#delete<?php echo $user_id;?>" data-toggle="modal">
                            <button type='button' class='btn btn-danger btn-sm'><span class='glyphicon glyphicon-trash' aria-hidden='true'></span></button>
                        </a>
                    </td>
                    <!--Add User Modl -->
                    
                    
                   
                    <!--Edit Item Modal -->
                    <div id="edit<?php echo $user_id; ?>" class="modal fade" role="dialog">
                        <form method="post" class="form-horizontal" role="form">
                            <div class="modal-dialog modal-md">
                                <!-- Modal content-->
                                <div class="modal-content">
                                    <div class="modal-header">
                                        <button type="button" class="close" data-dismiss="modal">&times;</button>
                                        <h1 class="modal-title">Reset Password</h1>
                                    </div>
                                    <div class="modal-body">
                                        <div class="alert alert-danger" role="alert">
                                         <input type="hidden" name="reset_id" value="<?php echo $user_id; ?>">
                                        <div class="alert alert-danger">Are you sure you want to reset password <strong>
                                                <?php echo $user; ?></strong>? </div>
                                      
                                          </div>
                                    </div>
                                    <div class="modal-footer">
                                       
                                        <button type="submit" class="btn btn-primary" name="ResetPassword"><span class="glyphicon glyphicon-edit"> </span> Reset</button>
                                        <button type="button" class="btn btn-warning" data-dismiss="modal"><span class="glyphicon glyphicon-remove-circle"></span> Cancel</button>
                                        
                                    </div>
                                </div>
                            </div>
                        </form>
                    </div>
                    <!--Delete Modal -->
                    <div id="delete<?php echo $user_id; ?>" class="modal fade" role="dialog">
                        <div class="modal-dialog">
                            <form method="post">
                                <!-- Modal content-->
                                <div class="modal-content">
                                    <div class="modal-header">
                                        <button type="button" class="close" data-dismiss="modal">&times;</button>
                                        <h4 class="modal-title">Delete</h4>
                                    </div>
                                    <div class="modal-body">
                                        <input type="hidden" name="delete_id" value="<?php echo $user_id; ?>">
                                        <div class="alert alert-danger">Are you Sure you want Delete <strong>
                                                <?php echo $user; ?></strong>? </div>
                                        <div class="modal-footer">
                                           
                                            <button type="submit" name="delete" class="btn btn-danger"><span class="glyphicon glyphicon-trash">   </span> YES</button>
                                            <button type="button" class="btn btn-default" data-dismiss="modal"><span class="glyphicon glyphicon-remove-circle"> </span> NO</button>
                                            
                                        </div>
                                    </div>
                                </div>
                            </form>
                        </div>
                    </div>
                </tr>
                <?php
                        }
                        

                        //Update User
                        if(isset($_POST['ResetPassword'])){
                            require_once 'mailer.php';
                            
                            
                                echo "<script>alert('Status Update Success');</script>";

                            
                            
                            //Create Token
                            $token = "wertyuioplkjhgfdsazxcvbnmq1234567890";
                            $token = str_shuffle($token);
                            $token = substr($token, 0, 10);
                        
                            $reset_id = $_POST['reset_id'];
                            $sql = "UPDATE register SET state = '1', token = '$token', tokenExpire=DATE_ADD(NOW(), INTERVAL 20 MINUTE)
                            WHERE user_id=$reset_id";
                            echo "<script>alert('$reset_id');</script>";
                            $con->query($sql) ;
                            /*if ($con->query($sql) === TRUE) {
                                
                                echo '<script>window.location.href="edit_user.php"</script>';
                            } else {
                                echo "Error updating record: " . $con->error;
                            }*/ 
                            echo "<script>alert($token + 'test');</script>";
                            $sql = "SELECT email FROM register WHERE user_id=$reset_id LIMIT 1";
                            $result = $con->query($sql);
                            if ($result->num_rows > 0) {
                             
                            // output data of each row
                            while($row = $result->fetch_assoc()) {
                                 
                                $email = $row['email'];
                            }

                            $msg = "
	                           Hi,<br><br>
	            
                                In order to reset your password, please click on the link below:<br>
                                <a href='
                                http://localhost/reset/forgot/resetPassword.php?email=$email&token=$token
                                '>http://localhost/reset/forgot/resetPassword.php?email=$email&token=$token</a><br><br>

                                Kind Regards,<br>
                                Back Office Solutions, Inc.
	        ";
                                $subject = "Reset Password";
                                sendEmail($email,$msg,$subject);
                        }
                        }
                    
   
                        if(isset($_POST['delete'])){
                            // sql to delete a record
                            $delete_id = $_POST['delete_id'];
                            $sql = "DELETE FROM register WHERE user_id='$delete_id' ";
                            if ($con->query($sql) === TRUE) {
                                $sql = "DELETE FROM register WHERE user_id='$delete_id' ";
                                if ($con->query($sql) === TRUE) {
                                    $sql = "DELETE FROM register WHERE user_id='$delete_id' ";
                                    echo '<script>window.location.href="edit_user.php"</script>';
                                } else {
                                    echo "Error deleting record: " . $con->error;
                                }
                            } else {
                                echo "Error deleting record: " . $con->error;
                            }
                        }
                        
                    }
                    

                    //Add User   
                
                if(isset($_POST["add_user"])) {

                 if (empty($user) || empty($email) || empty($pass) || empty($cpass)) {
                
                      echo "<script>alert('User name is already Taken');</script>";
                } else {
                    if (!preg_match("/^[a-zA-Z]*$/", $user) || !preg_match("#[0-9]+#", $pass) ) {
                     
                       
                    } else {
                            $sql = "SELECT * FROM register WHERE user='$user'";
                               $result = mysqli_query($con, $sql);
                               $resulCheck = mysqli_num_rows($result);
                                if ($pass != $cpass) {
                               

                                }
                               if ($resulCheck > 0) {				   
                               


                               }else {
                                   $hashedPwd = password_hash($cpass, PASSWORD_DEFAULT);
                                   $sql  = "INSERT INTO register (user, email, type, pass, date_created, createdBy) VALUES ('".$user."', '".$email."','".$type."', '".$hashedPwd."','".$date."','".$id."');";
                                   mysqli_query($con, $sql);

                               }
                        }
                    }
                }

?>
            </tbody>
        </table>
    </div>
    <!--Add User Modal -->
    
    <div id="add" class="modal fade" role="dialog">
        <div class="modal-dialog modal-lg">
            <!-- Modal content-->
            <div class="modal-content">
                <form method="post" class="form-horizontal" role="form">
                    <div class="modal-header">
                        <button type="button" class="close" data-dismiss="modal">&times;</button>
                        <h4 class="modal-title">Add User</h4>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label class="control-label col-sm-2" for="user_name"><span class="glyphicon glyphicon-user" aria-hidden="true"></span></label>
                            <div class="col-sm-4">
                                <input type="text" class="form-control" id="user_name" name="user_name" placeholder="Enter Username" autocomplete="off" autofocus required> </div>
                            <label class="control-label col-sm-2" for="item_code"><span class="glyphicon glyphicon-envelope" aria-hidden="true"></span></label>
                            <div class="col-sm-4">
                                <input type="text" class="form-control" id="email" name="email" placeholder="Enter email" autocomplete="off" required> </div>
                        </div>
                        <div class="form-group text-center">
                            <label class="control-label col-sm-2" for="password"><span class="glyphicon glyphicon-lock" aria-hidden="true"></span></label>
                            <div class="col-sm-4">
                                <input type="password" class="form-control" id="password" name="password" placeholder="Password" autocomplete="off" required> </div>
                            <label class="control-label col-sm-2" for="confirmpassword"><span class="glyphicon glyphicon-lock" aria-hidden="true"></span></label>
                            <div class="col-sm-4">
                                <input type="password" class="form-control" id="cpass" name="cpass" placeholder="Confirm Password" autocomplete="off" required> </div>
                        </div>
                        <div class="form-group">
                            <label class="col-sm-2 control-label" for="role"><span class="glyphicon glyphicon-exclamation-sign" aria-hidden="true"></span></label>
                            <div class="col-sm-2">
                                <select class="form-control" name="role" id="role">
                                <option selected>Select Role...</option>
                                <option value="1">Admin</option>
                                <option value="2">User</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="submit" class="btn btn-primary" name="add_user"><span class="glyphicon glyphicon-plus"></span> Add User</button>
                        <button type="button" class="btn btn-warning" data-dismiss="modal"><span class="glyphicon glyphicon-remove-circle"></span> Cancel</button>
                    </div>
                </form>
            </div>
        </div>
    </div>
</body>
</html>
