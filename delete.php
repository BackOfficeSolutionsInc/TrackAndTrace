
<?php include("header.php"); ?>

<html>
<head>
<title>Delete Data</title>
</head>
		  <script src="https://ajax.googleapis.com/ajax/libs/jquery/3.1.0/jquery.min.js"></script>  
		  <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css">

					<link rel="icon" type="image/png" sizes="32x32" href="images/favicon-32x32.png">
					<link rel="icon" type="image/png" sizes="16x16" href="images/favicon-16x16.png">
					<link rel="manifest" href="/site.webmanifest">
					<link rel="mask-icon" href="/safari-pinned-tab.svg" color="#5bbad5">
					<meta name="msapplication-TileColor" content="#da532c">
					<meta name="theme-color" content="#ffffff">
					  <link rel="stylesheet" type="text/css" href="style/banner_style.css">
					  <link rel="stylesheet" type="text/css" href="style/file_style.css">
<body>
<?php 
	$connect = mysqli_connect("localhost", "root", "", "trackandtrace");
?>


	<form method="POST">
				<div  align="center" class="container">
					<h1>Delete Data</h1>

					<div class="form">
						<input type="text" name="search" class="delete" placeholder="Enter Tracking #" autocomplete="off">
						<input type="submit" name="submit" value="Seach">
					</div>
				</div>
			</form>
			<div id="result"><div class="table-responsive">
					<table class="table table bordered">
						<tbody  style="text-align: center"><tr>
							<th  style="text-align: center">ID</th>
							<th style="text-align: center">Tracking Number</th>
							<th style="text-align: center">Date</th>
							<th style="text-align: center">Sender</th>
							<th style="text-align: center">Consignee</th>
							<th style="text-align: center">Stamp</th>
							<th style="text-align: center">Status</th>
							<th style="text-align: center">Location</th>
							<th style="text-align: center">Status Icon</th>
							<th style="text-align: center">Delete</th>
						</tr>
			<tr>
				<td>01</td>
				<td>LOG123</td>
				<td>2018/09/19 11:01:00 AM</td>
				<td>Sasha</td>
				<td>Nestor</td>
				<td>2018/09/14 02:30:00 PM</td>
				<td>Arrived at Hub/Transit station</td>
				<td>Pasig City</td>
				<td>1</td>
				<td><img src="images/bin.png"></td>
			</tr>
				</tbody></table></div></div>
			
			</div>
	
</body>

</html>