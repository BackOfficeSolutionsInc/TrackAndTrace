<?php
$isSet=false;
$waybill="Waybill Number:";
$dateawb="Date:";
$ref="Reference:";
$cons="Sender:";
$cosign="Consignee:";
$TrackingNumber= NULL;
$Date = NULL;
$Reference = NULL;
$Sender = NULL;
$Consignee = NULL;
$isSet=true;
$search=NULL;
$resultSet=NULL;
$mysqli=NULL;
?>
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en">
		<head>
		<title>Logistikus-Express</title>
		<link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css"/>
					<script src="https://ajax.googleapis.com/ajax/libs/jquery/3.3.1/jquery.min.js"></script>
					<script src="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/js/bootstrap.min.js"></script>
		<meta name="msapplication-TileColor" content="#da532c">
		<meta name="theme-color" content="#ffffff">
		<meta name="description" content="apple juice addict">

		<link rel="icon" type="image/png" sizes="32x32" href="images/favicon-32x32.png">
		<link rel="icon" type="image/png" sizes="16x16" href="images/favicon-16x16.png">
		<link rel="manifest" href="/site.webmanifest">
		<link rel="mask-icon" href="/safari-pinned-tab.svg" color="#5bbad5">

		<link rel="stylesheet" type="text/css" href="style/banner_style.css">		
		<link rel="stylesheet" type="text/css" href="style/status_style.css">

		</head>
		<body>
	
		<div class="sector-banner">
			<div class="logo">
			<a href="http://www.logistikus-express.com"><img src="images/Logistikuslogo.png" class="center"></a>
			</div>
			<form method="POST">
				<div class="box">
					<h1>Track Your Parcel</h1>
						<div class="txt">Type your tracking number below</div>
					<div class="form">
						<div class="col1"><input type="text" name="search" class="iCode" placeholder="Enter your Tracking #" autocomplete="off" /></div>
						<div class="col2"><input type="submit" name="submit" class="btn" value="Track" /></div>
					</div>
				</div>
			</form>
		</div>

	           <style>  
                .notfound  
                {  
                     padding:20px;  
                     margin-top:90px;  
					 postion: absolute;
					 font-size:20px;
                }  
           </style> 

<?php

if(isset($_POST['submit'])) { 
	$mysqli = NEW MySQLi("localhost","root","","trackandtrace");
	$search = $mysqli->real_escape_string($_POST['search']);
	$resultSet = $mysqli->query("SELECT DISTINCT TrackingNumber, Date, Reference, Sender, Consignee FROM tbl_trackandtrace WHERE TrackingNumber = '$search' LIMIT 1");

	if($resultSet->num_rows > 0 ) {
			$rows =$resultSet->fetch_assoc();
			$TrackingNumber = $rows["TrackingNumber"];
			$Date = $rows["Date"];
			$Reference = $rows["Reference"];
			$Sender = $rows["Sender"];
			$Consignee = $rows["Consignee"];
			$dateStamp=date_create($Date);
		
		?>
		
		<?php if(isset($_POST['submit'])) {  ?>
		<div class="sector-frame">
		<div class="warpper" id="trackArea">
			<div class="sector-frame">
				<div class="warpper">
					<div class="section-status">
						<div class="wrapper">
							<div class="row">
								<div class="col">
									<div class="info">
										<div class="line"><?php echo $waybill; ?> <span><?php echo $TrackingNumber ?></span></div>
										<div class="line"><?php echo $dateawb; ?> <span><?php echo date_format($dateStamp,"Y/m/d h:i:s A"); ?></span></div>
										<div class="line"><?php echo $ref; ?><span></span></div>
										<div class="line"><?php echo $cons; ?> <span><?php echo $Sender ?></span></div>
										<div class="line"><?php echo $cosign; ?> <span><?php echo $Consignee ?></span></div>
									</div>
									<div class="fb">
										<div class="logo"><a href="#"><img alt="Like us" src="images/icon/fbicon.png"></a></div>
										<div class="txt">
											<div class="t1">Like us on</div>
											<div class="t2">/Logistikus-express<br></div>
										</div>
									</div>
								</div>
								<div class="col colStatus">							
		<?php	
		$resultSet = $mysqli->query("SELECT DISTINCT TrackingNumber, Date, Reference, Sender, Consignee, Status, Stamp, Location, StatusIcon FROM tbl_trackandtrace WHERE TrackingNumber = '$search' ORDER BY `stamp` DESC");
		if($resultSet->num_rows > 0 ) {
			
			while($rows =$resultSet->fetch_assoc())
				{  
					
						$Stamp = $rows["Stamp"];
						$Status = $rows["Status"];
						$Location = $rows["Location"];
						$StatusIcon = $rows["StatusIcon"];
						$dateStamp=date_create($Stamp);
						
			?>
										<div class="status piority-success">
											<div class="date">
												<div><?php echo date_format($dateStamp,"Y/m/d h:i:s A"); ?></div>
												<div></div>
											</div>
											<div class="desc">
												<div class="d1">
													<div class="0"><?php echo $Status; ?></div>
												</div>
												<div class="d2"> <?php echo $Location;  ?></div>
											</div>
											<div class="icon">
												<div>
												<?php 
											if ($StatusIcon == 1) { ?>
												<img src="images/icon/status/package.png" alt=" ">
												<?php	}
												elseif ($StatusIcon == 2) {
												?>
												<img src="images/icon/status/warning.png" alt=" ">
												<?php } 
												elseif ($StatusIcon == 3) {
												?>
												<img src="images/icon/status/complete.png" alt=" ">
												<?php }
												?>
												
												</div>
											</div>
										</div>
										
		<?php
				}
		}
		?>							

									</div>
								</div>
							</div>
						</div>
					</div>
				</div>
			

			</div>
		</div>
		<?php }?>


		
			<div class="nf"><?php		
				}	
				else {
						echo '<div class="notfound" align="center">Tracking number not found</div>';
				}
				
			}

			?></div>
		<div class="footer">
		<p>&copy; Copyright Logistikus Express Philippines, Inc.</p>
		</div>
			</body>	
		
		</html>
