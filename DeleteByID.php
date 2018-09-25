<?php
$ret="error";
if(isset($_REQUEST["id"])){
	$id = $_REQUEST["id"];
	$connect = mysqli_connect("localhost", "root", "", "trackandtrace");
	$query = "DELETE FROM tbl_trackandtrace where CustomerID=$id";
	mysqli_query($connect, $query);
	$ret="SUCCESS";
}
echo $ret;
?>