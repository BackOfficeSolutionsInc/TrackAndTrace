<?php
$ret="error";
if(isset($_REQUEST["id"])){
	$id = $_REQUEST["id"];
	include('connect.php');
	$query = "DELETE FROM tbl_trackandtrace where CustomerID=$id";
	mysqli_query($con, $query);
	$ret="SUCCESS";
}
echo $ret;
?>