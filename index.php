<?php
$search="";
$urlDirect=false;
if(isSet($_GET['urlDirect'])){
		$urlDirect=true;
		$search=$_GET['search'];
	}else{
		$urlDirect=false;
}

?>
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en"><head>
		<title>Logistikus-Express</title>
        <meta charset="utf-8">
		<meta name="viewport" content="width=device-width, initial-scale=1">
		<link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css">
            
		<link rel="icon" type="image/png" sizes="32x32" href="images/favicon-32x32.png">
		<link rel="icon" type="image/png" sizes="16x16" href="images/favicon-16x16.png">
		<link rel="manifest" href="/site.webmanifest">
		<link rel="mask-icon" href="/safari-pinned-tab.svg" color="#5bbad5">

		<link rel="stylesheet" type="text/css" href="style/banner_style.css">		
		<link rel="stylesheet" type="text/css" href="style/status_style.css">

        <script src="https://ajax.googleapis.com/ajax/libs/jquery/3.3.1/jquery.min.js"></script>
        <script src="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/js/bootstrap.min.js"></script>
		</head>
		<body>
	
		<div class="sector-banner">
			<div class="logo">
			<a href="http://www.logistikus-express.com"><img src="images/Logistikuslogo.png" class="img-responsive"></a>
			</div>
			<form>
				<div class="box">
					<h1>Track Your Parcel</h1>
						<div class="txt">Type your tracking number below</div>
					<div class="form">
						<div class="col1"><input type="text" id="search" class="iCode" value="<?php echo htmlspecialchars($search); ?>"  placeholder="Enter your Tracking #" autocomplete="off"></div>
						<div class="col2"><input type="button" id="track" class="btn" value="Track"></div>
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

		
				<div class="sector-frame">
		<div class="warpper" id="trackArea">
			<div class="sector-frame">
				<div class="warpper">
					<div class="section-status">
						<div class="wrapper">
							<div class="row">
								<div class="col" id='divMain' >
									<div class="info">
										<div class="line"><span id='waybillLabel' style='color:black'></span><span id='waybill'></span></div>
										<div class="line"><span id='dateLabel' style='color:black'></span><span id='date'></span></div>
										<div class="line"><span id='referenceLabel' style='color:black'></span><span></span></div>
										<div class="line"><span id='senderLabel' style='color:black'></span><span id='sender'></span></div>
										<div class="line"><span id='consigneeLabel' style='color:black'></span><span id='consignee'></span></div>
									</div>
									<div class="fb" id='fbdiv' style="display: none;"  >
										<div class="logo"><a href="#"><img alt="Like us" src="images/icon/fbicon.png"></a></div>
										<div class="txt">
											<div class="t1">Like us on</div>
											<div class="t2" >/Logistikus-express<br></div>
										</div>
									</div>
								</div>
								<div class="col colStatus" id="div1">							
								    
									</div>
								</div>
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>
		<!-- <div class="nf"  id='nfdiv' style="display: none;"  >		
        <div class="notfound" align="center">Tracking number not found</div>
	    </div>-->		
        <div class="nf"></div>
		<div class="footer">
		<p>© Copyright Logistikus Express Philippines, Inc.</p>
		</div>		
</body>
<?php
if($urlDirect){
	echo "<script>$(document).ready(function(){ Track(); });</script>";
	
}
?>
<script>
    $('#search').on('keyup  change',function(){

     var text = $.trim($(this).val() )
     this.value="";      
     this.value=text;

    validateSearch.init(this.value);
    if(validateSearch.done()){
        console.log('this is correct');
    }
});
    
    
        $(document).ready(function() {
        $("#track").click(function() {
        $( "#div1" ).empty();
        Track();
             
            });
        });          

    function Track(){
    var filename='https://api-alexsys.codedisruptors.com:8084/transaction/' + $("#search").val();
    $.ajax({
    type: 'GET',
    url: filename,
    dataType: 'json',
    success: function (data) {
    $.each(data, function(index, element) {
        
        $("#nfdiv").css("display", "block");
		$("#fbdiv").css("display", "block");
		$('#waybillLabel').show();
		$('#waybillLabel').text('Waybill Number: ');
		$('#dateLabel').text('Date: ');
		$('#referenceLabel').text('Reference: ');
		$('#senderLabel').text('Sender: ');
		$('#consigneeLabel').text('Consignee: ');
		
        $('#waybill').text(element.transaction_code);
        
        if(typeof(element.picked_up_date)!="undefined"){
            var pud=element.picked_up_date;
            var pudcut=pud.substring(0,10)
            var date1 = new Date(pudcut);
            //var date1 = new Date(element.picked_up_date);
            $('#date').text(date1.toLocaleDateString());
        }
       
        $('#sender').text(element.shipper_name);
        $('#consignee').text(element.consignee_name);
		
        $.each(element.transaction_movements, function(index, movement) {
            var strDateTime = movement.created;
            var date = new Date(strDateTime);
             var str ="<div class='status piority-success'>";
				str +="<div class='date'>";
                str +="<div>" + date.toLocaleString() + "</div>";
                str +="<div></div>";
				str +="</div>";
				str +="<div class='desc'>";
				str +="<div class='d1'>";
            var stat;
            var img;
            var rece='';
            var reason='';
            switch(movement.transaction_status){
                case "POD":                    
                    stat="<strong>Successfully delivered</strong>";
                    rece="<div class='1' style='font-size:12px'><strong>Received by:</strong> " + element.receiver + "</div>";
                    img='images/icon/status/complete.png';
                    break;
                case "PODEX": 
                    stat="";
                    reason="<div class='2'>" + movement.reason + "</div>";
                    img='images/icon/status/warning.png';
                    break;
                case "RTS":
                    stat="Return to sender";
                    img='images/icon/status/warning.png';
                    break;
                case "ON HOLD":
                    stat="On-hold";
                    img='images/icon/status/warning.png';
                    break;
                case "SOPD":
                    stat="Out for delivery";
                    img='images/icon/status/package.png';
                    break;
                case "SIPL":
                    stat="Arrived at destination station";
                    img='images/icon/status/arrive.png';
                    break;
                case "SOPL":
                    stat="In transit to destination station";
                    img='images/icon/status/arrive.png';
                    break;
                case "SIP":
                    stat="Arrived at hub/transit station";
                    img='images/icon/status/arrive.png';
                    break;
                default:
                    stat=movement.transaction_status;
                    
            }
                
				str +="<div class='0'>" + stat + "</div>";
                str+=rece;
                str +=reason;
				str +="</div>";
            var province="";
            if(movement.transaction_status=="POD"){
                if(typeof(movement.location.province)!="undefined"){
                    province=movement.location.province;
                }
                str +="<div class='d2'>" + province + "</div>";
            }else if(movement.transaction_status=="RTS"){
               
                str +="<div class='d2'>" + province + "</div>";
            }
            else{
                
                if(movement.location!=null){
                    var nme="";
                    if(typeof(movement.location.name)!="undefined"){
                        nme=movement.location.name + ', ' ;
                    }
                
                    if(typeof(movement.location.province)!="undefined"){
                        province=movement.location.province ;
                    }
				    str +="<div class='d2'>" + nme + province + "</div>";
                }
            }
				str +="</div>";
				str +="<div class='icon'>";
				str +="<div>";
				str +="<img src='"+ img + "'>";
				str +="</div>";
				str +="</div>";
				str +="</div>";
            $('#div1').append(str);
        });
                });
            }
        });
    }
    function elementValue(element){
    $("#shipper_country").val(element.shipper_country);
    }
</script>
</html>