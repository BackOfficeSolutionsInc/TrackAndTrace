<?php 

include 'connect.php';

if(isset($_POST['reset'])){

    $allowed = mysqli_query($conn," UPDATE users SET state = "1" WHERE user = '$user' ");

}
