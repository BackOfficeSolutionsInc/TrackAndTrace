
$(document).ready(function(){  
	$('.reset_employee').click(function(e){   
	   e.preventDefault();   
	   var empid = $(this).attr('data-emp-id');   
	   var parent = $(this).parent("td").parent("tr");   
	   var vUser ="<?php echo $php_variable; ?>";
        bootbox.dialog({
        
			message: "Are you sure you want to reset the password of " + vUser,
			title: "<i class='glyphicon glyphicon-edit'></i> Edit",

			buttons: {
				success: {
					  label: "No",
					  className: "btn-success",
					  callback: function() {
					  $('.bootbox').modal('hide');
				  }
				},
				danger: {
				  label: "reset!",
				  className: "btn-danger",
				  callback: function() {       
				   $.ajax({        
						type: 'POST',
						url: 'updateRecords.php',
						data: 'empid='+empid        
				   })
				   .done(function(response){        
						bootbox.alert(response);
						parent.fadeOut('slow');        
				   })
				   .fail(function(){        
						bootbox.alert('Error....');               
				   })              
				  }
				}
			}
	   });   
	});  
 });