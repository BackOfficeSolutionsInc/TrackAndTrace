<?php
$isSet=false;
$waybill="Waybill Number:";
$dateawb="Date:";
$ref="Reference:";
$cons="Sender:";
$cosign="Consignee:";
$wbn = NULL;
$date = NULL;
$reference = NULL;
$sender = NULL;
$consignee = NULL;
$isSet=true;
$search=NULL;
$resultSet=NULL;
$mysqli=NULL;
if(isset($_POST['submit'])) { 
	$mysqli = NEW MySQLi("localhost","root","","logistikus");
	$search = $mysqli->real_escape_string($_POST['search']);
	$resultSet = $mysqli->query("SELECT DISTINCT wbn, date, reference, sender, consignee FROM trackandtrace WHERE wbn = '$search' LIMIT 1");

	if($resultSet->num_rows > 0 ) {
		while($rows =$resultSet->fetch_assoc()) {
			$wbn = $rows["wbn"];
			$date = $rows["date"];
			$reference = $rows["reference"];
			$sender = $rows["sender"];
			$consignee = $rows["consignee"];
		}
	}
}
?>
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en">
<head>
	<title>Logistikus-Express</title>
	<link rel="stylesheet" type="text/css" href="././style/banner_style.css">
	<link rel="stylesheet" type="text/css" href="././style/status_style.css">
	
	<meta name="description" content="apple juice addict" />
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
										<div class="line"><?php echo $waybill; ?> <span><?php echo $wbn ?></span></div>
										<div class="line"><?php echo $dateawb; ?> <span><?php echo $date ?></span></div>
										<div class="line"><?php echo $ref; ?><span></span></div>
										<div class="line"><?php echo $cons; ?><span><?php echo $sender ?></span></div>
										<div class="line"><?php echo $cosign; ?><span><?php echo $consignee ?></span></div>
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
		$resultSet = $mysqli->query("SELECT DISTINCT wbn, date, reference, sender, consignee, status, stamp, location, status_icon FROM trackandtrace WHERE wbn = '$search' ORDER BY `stamp` DESC");
		if($resultSet->num_rows > 0 ) {
			while($rows =$resultSet->fetch_assoc())
				{     
					$stamp = $rows["stamp"];
					$status = $rows["status"];
					$location = $rows["location"];
					$status_icon = $rows["status_icon"];
?>
									<div class="status piority-success">
										<div class="date">
											<div><?php echo $stamp; ?></div>
										</div>
										<div class="desc">
											<div class="d1">
												<div class="0"><?php echo $status; ?></div>
											</div>
											<div class="d2"> <?php echo $location;  ?></div>
										</div>
										<div class="icon">
											<div>
											<?php 
										if ($status_icon == 1) { ?>
											<img src="images/icon/status/package.png" alt=" ">
											<?php	}elseif ($status_icon == 2) {
											?>
											<img src="images/icon/status/warning.png" alt=" ">
											<?php } elseif ($status_icon == 3) {
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

<div class="footer">
<p>&copy; Copy Right Logistikus-Express.com</p>
</div>
	</body>	
</head>
</html>