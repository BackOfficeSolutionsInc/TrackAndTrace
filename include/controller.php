<?php
session_start();
require 'connection.php';
date_default_timezone_set("Asia/Manila");
$date = date("Y-m-d");
$date_time = date("Y-m-d h:i:s");

$usernameErr = $passwordErr = $current_passwordErr = $new_passwordErr = $repeat_passwordErr = $edit_item_idErr = $item_nameErr = $item_categoryErr = $item_descriptionErr = $item_critical_lvlErr = $quantityErr = $uomErr = $received_by = "";
$username = $txtpassword  = $current_password  = $new_password  = $repeat_password  = $edit_item_id  = $item_name  = $item_category  = $item_description  = $item_critical_lvl  = $quantity = $received_by = $remarks = "";

function clean($data) {
    $data = trim($data);
    $data = stripslashes($data);
    $data = htmlspecialchars($data);
    return $data;
}

if ($_SERVER["REQUEST_METHOD"] == "POST") {
    if (empty($_POST["username"])) {
        $usernameErr = "Username is required";
    } else {
        $username = clean($_POST["username"]);
    }

    if (empty($_POST["txtpassword"])) {
        $passwordErr = "password is required";
    } else {
        $txtpassword = clean($_POST["txtpassword"]);
    }

    if (empty($_POST["current_password"])) {
        $current_passwordErr = "Current password is required";
    } else {
        $current_password = clean($_POST["current_password"]);
    }

    if (empty($_POST["new_password"])) {
        $new_passwordErr = "New password is required";
    } else {
        $new_password = clean($_POST["new_password"]);
    }

    if (empty($_POST["repeat_password"])) {
        $repeat_passwordErr = "password is required";
    } else {
        $repeat_password = clean($_POST["repeat_password"]);
    }

    if (empty($_POST["edit_item_id"])) {
        $edit_item_id = "";
    } else {
        $edit_item_id = clean($_POST["edit_item_id"]);
    }

    if (empty($_POST["item_name"])) {
        $item_nameErr = "Item Name is required";
    } else {
        $item_name = clean($_POST["item_name"]);
    }


    if (empty($_POST["item_category"])) {
        $item_categoryErr = "Category is required";
    } else {
        $item_category = clean($_POST["item_category"]);
    }

    if (empty($_POST["item_description"])) {
        $item_descriptionErr = "Item Description is required";
    } else {
        $item_description = clean($_POST["item_description"]);
    }

    if (empty($_POST["item_critical_lvl"])) {
        $item_critical_lvlErr = "Item Critical Level is required";
    } else {
        $item_critical_lvl = clean($_POST["item_critical_lvl"]);
    }

    if (empty($_POST["quantity"])) {
        $quantityErr = "Quantity is required";
    } else {
        $quantity = clean($_POST["quantity"]);
    }

    if (empty($_POST["received_by"])) {
        $ureceived_by = "Who received is required";
    } else {
        $received_by = clean($_POST["received_by"]);
    }


}   


//Login Query
if(isset($_POST['login'])){
    $sql = "SELECT * FROM tbl_user WHERE username='$username'";
    $result = $conn->query($sql);

    if ($result->num_rows > 0) {
        // output data of each row
        while($row = $result->fetch_assoc()) {
            if($row['password'] == $txtpassword){
                $_SESSION['user_name'] = $row['username'];
                $_SESSION['role'] = $row['role'];
                header("location:inventory.php");
            } else {
                $passwordErr = '<div class="alert alert-warning">
                        <strong>Login!</strong> Failed.
                        </div>';
                $username = $row['username'];
            }
        }
    } else {
        $usernameErr = '<div class="alert alert-danger">
  <strong>Username</strong> Not Found.
</div>';
        $username = "";
    }
}

?>