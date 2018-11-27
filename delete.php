<?php
 session_start();
if(!isset($_SESSION['user'])){
    header('location:login.php');
}?>
<?php include("navbar.php");
      include("header.php"); 
      include("connect.php");
       ?>


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
	$resultSet;
	if(isset($_POST["submit"])){
		$search = $con->real_escape_string($_POST['search']);
		$resultSet = $con->query("SELECT * FROM tbl_trackandtrace WHERE TrackingNumber = '$search'");
		
	
	}
?>

<h1><center>Delete Tracking Number</center></h1>
 <div class="right">
     <form method="POST">
    <div class="input-group input-group-lg"  style="max-width:250px;">
      <input  type="text" name="search" class="form-control col-md-10 col-sm-10 col-xs-12 delete"  style="width:300px;margin-left:10px" placeholder="Enter Tracking #">
      <div class="input-group-btn">
        <button class="btn btn-default" type="submit" name="submit" value="Seach"><i class="glyphicon glyphicon-search"></i></button>
      </div>
    </div>
     </form>
 </div>
	 
	<div id="result">
		<div class="table-responsive">
			<table class="table table bordered">
				<tbody  style="text-align: center">
					<tr>
						<th  style="text-align: center">ID</th>
						<th style="text-align: center">Tracking Number</th>
						<th style="text-align: center">Date</th>
						<th style="text-align: center">Sender</th>
						<th style="text-align: center">Consignee</th>
						<th style="text-align: center">Stamp</th>
						<th style="text-align: center">Status</th>
						<th style="text-align: center">Location</th>
						<th style="text-align: center">Delete</th>
					</tr>
					<?php if(isset($_POST["submit"])){ 
							if($resultSet->num_rows > 0 ) {
								while($rows =$resultSet->fetch_assoc()) {
					?>
					<tr id="row<?php echo $rows["CustomerID"]; ?>" >
						<td><?php echo $rows["CustomerID"]; ?></td>
						<td><?php echo $rows["TrackingNumber"]; ?></td>
						<td><?php echo $rows["Date"]; ?></td>
						<td><?php echo $rows["Sender"]; ?></td>
						<td><?php echo $rows["Consignee"]; ?></td>
						<td><?php echo $rows["Stamp"]; ?></td>
						<td><?php echo $rows["Status"]; ?></td>
						<td><?php echo $rows["Location"]; ?></td>
						<td><img src="images/bin.png" onclick="deleteById( <?php echo $rows['CustomerID']; ?> )" /></td>
					</tr>
					<?php 		}
							}
					} ?>
				</tbody>
			</table>
		</div>
	</div>
			
		
	
</body>
<script>
function deleteById(id){
	 var row = document.getElementById("row" + id);
	 row.style.display='none';
	if (id == 0) { 
        
        return;
    } else {
        var xmlhttp = new XMLHttpRequest();
        xmlhttp.onreadystatechange = function() {
            if (this.readyState == 4 && this.status == 200) {
                alert(this.responseText);
            }
        };
        xmlhttp.open("POST", "DeleteByID.php?id=" + id, true);
        xmlhttp.send();
    }
	
}

</script>

</html>