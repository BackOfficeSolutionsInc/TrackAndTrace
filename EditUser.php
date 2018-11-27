<?php
include 'connect.php';
include 'navbar.php';
include 'updateRecord.php'
$session_username = $_SESSION['user'];
$type = $_SESSION['type'];
if(empty($_SESSION['user'])){
    header("location:login.php");
}
$id =  $_SESSION['user_id'];
$user = $user;

?>
<!DOCTYPE html>
<html lang="en">

<head>
    <title>Test</title>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <!-- The above 3 meta tags *must* come first in the head; any other head content must come *after* these tags -->
    <!-- Latest compiled and minified CSS -->
    <link rel="stylesheet" href="style/bootstrap.min.css">

    <!-- Loader -->
    <link rel="stylesheet" href="style/loader.css">
    <link rel="stylesheet" type="text/css" href="dashboard/vendor/font-awesome/css/font-awesome.min.css">
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
                    $sql = "SELECT * FROM register";
                    $result = $con->query($sql);
                    if ($result->num_rows > 0) {
                        // output data of each row
                        while($row = $result->fetch_assoc()) {
                            $user_id = $row['user_id'];
                            $user = $row['user'];
                            $email = $row['email'];
                            $type = $row['type'];
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
                                          <p>Are you sure you want to reset password <?php echo $user; ?></p>
                                          </div>
                                    </div>
                                    <div class="modal-footer">
                                        <button type="submit" class="btn btn-primary" name="update_user"><span class="glyphicon glyphicon-edit"></span> Edit</button>
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
                                            <button type="submit" name="delete" class="btn btn-danger"><span class="glyphicon glyphicon-trash"></span> YES</button>
                                            <button type="button" class="btn btn-default" data-dismiss="modal"><span class="glyphicon glyphicon-remove-circle"></span> NO</button>
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
                        if(isset($_POST['update_user'])){
                            $user_id = $row['user_id'];
                            $user = $row['user'];
                            $type = $row['type'];
                            $date_created = $row['date_created'];
                            $createdBy = $row['createdBy'];
                            $token = $row['token'];
                            
                            //Creating Token
                            $token = "werTyuioplkjhgfDsazxcvbnmQ1234567890";
                            $token = str_shuffle($token);
                            $token = substr($token, 0, 10);
                            echo "<script>alert('$token');</script>";
                            
                            $sql = "UPDATE register SET token='$token', 
                                tokenExpire=DATE_ADD(NOW(), INTERVAL 5 MINUTE)
                                WHERE email='$email' AND user='{$_SESSION['user']}'";
                            if ($con->query($sql) === TRUE) {
                                echo '<script>window.location.href="editUser.php"</script>';
                            } else {
                                echo "Error updating record: " . $con->error;
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
                                    echo '<script>window.location.href="editUser.php"</script>';
                                } else {
                                    echo "Error deleting record: " . $con->error;
                                }
                            } else {
                                echo "Error deleting record: " . $con->error;
                            }
                        }
                    }

                    //Add User        
                    if(isset($_POST['add_user'])){
                        $user = $_POST['user'];
                        $email = $_POST['email'];
                        $type = $_POST['type'];
                        $pass = $_POST['pass'];
                        $date_created = $_POST['date_created'];
                        $createdBy = $_POST['createdBy'];
                        $hashedPwd = password_hash($pass, PASSWORD_DEFAULT);
					   $sql  = "INSERT INTO register (user, email, type, pass, date_created, createdBy) VALUES ('".$user."', '".$email."','".$type."', '".$hashedPwd."','".$date."','".$id."');";
                        if ($con->query($sql) === TRUE) {
                            $add_user_query = "INSERT INTO tbl_inventory(item_name,item_code,date,qty)VALUES ('$item_name','$item_code','$date','0')";

                            if ($con->query($add_inventory_query) === TRUE) {
                                echo '<script>window.location.href="editUser.php"</script>';
                            } else {
                                echo "Error: " . $sql . "<br>" . $con->error;
                            }
                        } else {
                            echo "Error: " . $sql . "<br>" . $con->error;
                        }
                    }

                    

                    if(isset($_POST['minus_inventory'])) {
                        $minus_stocks_id = clean($_POST['minus_stocks_id']);
                        $remarks = clean($_POST["remarks"]);
                        $quantity = clean($_POST['quantity']);
                        $sql = "INSERT INTO tbl_issuance(date,item_name,item_code,qty, sender_receiver,in_out,            remarks)VALUES ('$date_time','$item_name','$item_code','$quantity','$received_by','out','$remarks')";
                        if ($con->query($sql) === TRUE) {
                            $add_inv = "UPDATE tbl_inventory SET qty=(qty - '$quantity') WHERE id='$minus_stocks_id' ";
                            if ($con->query($add_inv) === TRUE) {
                                echo '<script>window.location.href="inventory.php"</script>';
                            } else {
                                echo "Error updating record: " . $con->error;
                            }
                        } else {
                            echo "Error: " . $sql . "<br>" . $con->error;
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
                    
                    <div class="modal-footer">
                        <button type="submit" class="btn btn-primary" name="add_user"><span class="glyphicon glyphicon-plus"></span> Add</button>
                        <button type="button" class="btn btn-warning" data-dismiss="modal"><span class="glyphicon glyphicon-remove-circle"></span> Cancel</button>
                    </div>
                </form>
            </div>
        </div>
    </div>

</body>

</html>