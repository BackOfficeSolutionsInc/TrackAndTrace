<?php  
 //export.php  
 if(!empty($_FILES["excel_file"]))  
 {  
      $connect = mysqli_connect("localhost", "root", "", "upload_logex");  
      $file_array = explode(".", $_FILES["excel_file"]["name"]);  
      if($file_array[1] == "xlsx")  
      {  
           include("PHPExcel/IOFactory.php");  
           $output = '';  
           $output .= "  
           <label class='text-success'>Data Inserted</label>  
                <table class='table table-bordered'>  
                     <tr>  
                          <th>TrackingNumber</th>  
                          <th>Date</th>  
                          <th>Reference</th>  
                          <th>Sender</th>  
                          <th>Consignee</th>
						  <th>Stamp</th>
						  <th>Status</th>
						  <th>Location</th>
						  <th>StatusIcon</th>
                     </tr>  
                     ";  
           $object = PHPExcel_IOFactory::load($_FILES["excel_file"]["tmp_name"]);  
           foreach($object->getWorksheetIterator() as $worksheet)  
           {  
                $highestRow = $worksheet->getHighestRow();  
                for($row=2; $row<=$highestRow; $row++)  
                {  
                     $name = mysqli_real_escape_string($connect, $worksheet->getCellByColumnAndRow(1, $row)->getValue());  
                     $date = mysqli_real_escape_string($connect, $worksheet->getCellByColumnAndRow(2, $row)->getFormattedValue());  
                     $reference = mysqli_real_escape_string($connect, $worksheet->getCellByColumnAndRow(3, $row)->getValue());  
                     $sender = mysqli_real_escape_string($connect, $worksheet->getCellByColumnAndRow(4, $row)->getValue());  
                     $consignee = mysqli_real_escape_string($connect, $worksheet->getCellByColumnAndRow(5, $row)->getValue()); 
					 $stamp = mysqli_real_escape_string($connect, $worksheet->getCellByColumnAndRow(6, $row)->getFormattedValue());
					 $status = mysqli_real_escape_string($connect, $worksheet->getCellByColumnAndRow(7, $row)->getValue());
					 $location = mysqli_real_escape_string($connect, $worksheet->getCellByColumnAndRow(8, $row)->getValue());
					 $statusicon = mysqli_real_escape_string($connect, $worksheet->getCellByColumnAndRow(9, $row)->getValue());
                     $query = "  
                     INSERT INTO tbl_trackandtrace 
                     (TrackingNumber, Date, Reference, Sender, Consignee, Stamp, Status, Location, StatusIcon)   
                     VALUES ('".$name."', '".$date."', '".$reference."', '".$sender."', '".$consignee."', '".$stamp."', '".$status."', '".$location."', '".$statusicon."')  
                     ";  
                     mysqli_query($connect, $query);  
                     $output .= '  
                     <tr>  
                          <td>'.$name.'</td>  
                          <td>'.$date.'</td>  
                          <td>'.$reference.'</td>  
                          <td>'.$sender.'</td>  
                          <td>'.$consignee.'</td>  
						  <td>'.$stamp.'</td> 
						  <td>'.$status.'</td>
						  <td>'.$location.'</td>
						  <td>'.$statusicon.'</td>
                     </tr>  
                     ';  
                }  
           }  
           $output .= '</table>';  
           echo $output;  
      }  
      else  
      {  
           echo '<label class="text-danger">Invalid File</label>';  
      }  
 }  
 ?>  