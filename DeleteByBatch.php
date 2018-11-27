<?php
$ret="error";
if(isset($_REQUEST["ID"])){
	$id = $_REQUEST["ID"];
	include('connect.php');
	$query = "DELETE FROM tbl_trackandtrace where batch_number=$batch_number";
	mysqli_query($con, $query);
	$ret="SUCCESS";
}
echo $ret;
?>