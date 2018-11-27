<?php
include_once("connect.php");
if($_REQUEST['empid']) {
	$sql = "DELETE FROM register WHERE user='".$_REQUEST['empid']."'";
	$resultset = mysqli_query($con, $sql) or die("database error:". mysqli_error($con));	
	if($resultset) {
		echo "Record Deleted";
	}
}
?>
