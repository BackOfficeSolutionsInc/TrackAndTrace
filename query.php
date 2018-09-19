<?php  
$connect = mysqli_connect("localhost", "root", "", "trackandtrace");
if(isset($_POST["submit"]))
	{
		if($_FILES['file']['name'])
			{
				$filename = explode(".", $_FILES['file']['name']);
				if($filename[1] == 'csv')
					{
						$handle = fopen($_FILES['file']['tmp_name'], "r");
						$header=0;
						while($data = fgetcsv($handle))
							{
									$trackingnumber = mysqli_real_escape_string($connect, $data[1]);  
									$date = mysqli_real_escape_string($connect, $data[2]);
									$reference = mysqli_real_escape_string($connect, $data[3]);
									$sender = mysqli_real_escape_string($connect, $data[4]);
									$consignee = mysqli_real_escape_string($connect, $data[5]);
									$stamp = mysqli_real_escape_string($connect, $data[6]);
									$status = mysqli_real_escape_string($connect, $data[7]);
									$location = mysqli_real_escape_string($connect, $data[8]);
									$statusicon = mysqli_real_escape_string($connect, $data[9]);
									
									$query="SELECT 'Y' ans FROM tbl_trackandtrace WHERE TrackingNumber = '$trackingnumber' and stamp = '$stamp' Limit 1";
									$resultSet = mysqli_query($connect, $query);
									
									if($resultSet){
										$row = mysqli_fetch_assoc($resultSet);
										if ( $row['ans']=='Y'){
											$query= "Update tbl_trackandtrace set Status=$status, Location=$location, StatusIcon=$statusicon) WHERE trackingnumber = $trackingnumber and stamp = $stamp ";
											mysqli_query($connect, $query);
			
										}else {
											$query = "INSERT into tbl_trackandtrace
												(TrackingNumber, Date, Reference, Sender, Consignee, Stamp, Status, Location, StatusIcon) 
												VALUES ('".$trackingnumber."', '".$date."', '".$reference."', '".$sender."', '".$consignee."', '".$stamp."', '".$status."', '".$location."', '".$statusicon."')";
											mysqli_query($connect, $query);
										}
			//$query = "INSERT INTO tbl_trackandtrace (TrackingNumber, Date, Reference, Sender, Consignee, Stamp, Status, Location, StatusIcon) 
			   // VALUES ('".$trackingnumber."', '".$date."', '".$reference."', '".$sender."', '".$consignee."') ON DUPLICATE KEY UPDATE product_code=?,
									}
									
									
									
							}
							fclose($handle);
					}
					echo "<script>alert('Status Update Success');</script>";
			}
	}
?>  
<!DOCTYPE html>  
<html>  
 <head>  
  <title>Logistikus-Express Philippines, Inc.</title>
  <script src="https://ajax.googleapis.com/ajax/libs/jquery/3.1.0/jquery.min.js"></script>  
  <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css" />
  <link rel="stylesheet" type="text/css" href="style/file_style.css">
 </head>  
 <body>
  <div class="container">
		  <h3 align="center">Import Status Update</h3><br />
		  <form method="post" enctype="multipart/form-data">
		   <div align="center">  
			   <input type="file" name="file" />

				<input type="submit" name="submit" value="Import" class="btn btn-default btn-file" />
		   </div>
	</div>
  </form>
	<div class="footer">
		<p>&copy; Logistikus-Express Philippines, Inc.</p>
	</div>
 </body>  
</html>