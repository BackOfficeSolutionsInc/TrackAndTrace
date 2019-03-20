
<?php  
session_start();
include('connect.php');

if(isset($_POST["submit"]))
	{
    $id =  $_SESSION['user_id'];
    
    $id = str_pad($id, 4, '0', STR_PAD_LEFT);
    $year = date("Y");
    $month = date("m");
    $day = date("d");
    
     $batch_number=$id.$year.$month.$day."01";
    $query="SELECT IFNULL(max(batch_number),'1')  batch_number FROM tbl_trackandtrace WHERE batch_number like '$id$year$month$day%'";
        $resultSet = mysqli_query($con, $query);
    
    if($resultSet){
        
        $row = mysqli_fetch_assoc($resultSet);
        $batch=$row['batch_number'];
        if($batch!='1'){
            $cnt=intval(substr($batch,-2))+1;
            $batch_number=$id.$year.$month.$day.str_pad($cnt, 2, '0', STR_PAD_LEFT);
        }
    }
    
		if($_FILES['file']['name'])
			{
				$filename = explode(".", $_FILES['file']['name']);
				if($filename[1] == 'csv')
					{
						
						$handle = fopen($_FILES['file']['tmp_name'], "r");
						$lineHead=0;
                        
						while($data = fgetcsv($handle))
                            
							{	
									$trackingnumber = mysqli_real_escape_string($con, $data[1]);  
									$date = mysqli_real_escape_string($con, $data[2]);
									$reference = mysqli_real_escape_string($con, $data[3]);
									$sender = mysqli_real_escape_string($con, $data[4]);
									$consignee = mysqli_real_escape_string($con, $data[5]);
									$stamp_from_csv = mysqli_real_escape_string($con, $data[6]);
									$status = mysqli_real_escape_string($con, $data[7]);
									$location = mysqli_real_escape_string($con, $data[8]);
									$statusicon = mysqli_real_escape_string($con, $data[9]);
                                    $stamp=date('Y/m/d H:i',strtotime($stamp_from_csv));
                            
                            
                                    
                                    if (strpos($stamp_from_csv, 'AM') !== false || strpos($stamp_from_csv, 'PM') !== false) {
                                        echo "<script>alert('Please Remove the AM/PM in the data stamp');</script>";
                                    
                                    } else {
                            
                                    //substr($stamp_from_csv,)
                            
									$query="SELECT 'Y' ans FROM tbl_trackandtrace WHERE TrackingNumber = '$trackingnumber' and stamp = '$stamp' Limit 1";
									$resultSet = mysqli_query($con, $query);
									
									if($resultSet){
										$row = mysqli_fetch_assoc($resultSet);
										if ( $row['ans']=='Y'){
											$query= "Update tbl_trackandtrace set Status='$status', Location='$location', StatusIcon='$statusicon' WHERE trackingnumber = '$trackingnumber' and stamp = '$stamp' ";
											mysqli_query($con, $query);
			
										}else {
											if($lineHead>0){
												
													$query = "INSERT into tbl_trackandtrace
														(TrackingNumber, Date, Reference, Sender, Consignee, Stamp, Status, Location, StatusIcon, batch_number) 
														VALUES ('".strtoupper($trackingnumber)."', '".$date."', '".$reference."', '".$sender."', '".$consignee."', '".$stamp."', '".$status."', '".$location."', '".$statusicon."','".$batch_number."')";
													mysqli_query($con, $query);
											}
											$lineHead=1;
										}

									}
                                    }
                                    }
							fclose($handle);
					}
					echo "<script>alert('Status Update Success');</script>";
			}
	}

if(!isset($_SESSION['user'])){
    header('location:login.php');
}?>  
<!DOCTYPE html>  
<html>  
 <head>  
     <title>
     </title>
        <meta name="msapplication-TileColor" content="#da532c">
        <meta name="theme-color" content="#ffffff">
        <link rel="icon" type="image/png" sizes="32x32" href="images/favicon-32x32.png">
        <link rel="icon" type="image/png" sizes="16x16" href="images/favicon-16x16.png">
        <link rel="manifest" href="/site.webmanifest">
        <link rel="mask-icon" href="/safari-pinned-tab.svg" color="#5bbad5">
        <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css">
                    
        <link rel="stylesheet" type="text/css" href="style/file_style.css">
        <link rel="stylesheet" type="text/css" href="style/banner_style.css">
        <script src="https://ajax.googleapis.com/ajax/libs/jquery/3.1.0/jquery.min.js"></script> 
 </head>  
 <body>
     <?php 
    include('navbar.php');
     ?>

		 <div class="container">
				  <h3 align="center">Import Status Update</h3><br />
				  <form method="post" enctype="multipart/form-data" />
				   <div align="center">  
					   <input type="file" class="custom-file-input" name="file" />
					  <input type="submit" name="submit" value="Import" class="btn btn-default btn-file" />
				   </div>
		 </div>
					
		<div class="footer">
			<p>&copy; Logistikus-Express Philippines, Inc.</p>
		</div>
 </body>  
</html>