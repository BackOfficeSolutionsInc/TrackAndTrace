<?php
session_start(); 
session_destroy();
if(isset($_GET['redirect'])) {
 header('Location: '.base64_decode($_GET['redirect']));  
} else {
 header('Location: login.php');  
}
?>