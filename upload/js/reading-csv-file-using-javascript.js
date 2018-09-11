function buildTable(results){

	var markup = "<table class='table'>";
	var data = results.data;
	
	for(i=0;i<data.length;i++){
		markup+= "<tr>";
		var row = data[i];
		var cells = row.join(",").split(",");
		
		for(j=0;j<cells.length;j++){
			markup+= "<td>";
			console.log(cells[j]);
			markup+= cells[j];
			markup+= "</th>";
		}
		markup+= "</tr>";
	}
	markup+= "</table>";
	
	
	$("#app").html(markup);
}


$(document).ready(function(){
		$('#submit').on("click",function(e){
			e.preventDefault();
			if (!$('#files')[0].files.length){
				alert("Please choose at least one file to read the data.");
			}
		
			$('#files').parse({
				config: {
					delimiter: "auto",
					complete: buildTable,
				},
				before: function(file, inputElem)
				{
					//console.log("Parsing file...", file);
				},
				error: function(err, file)
				{
					console.log("ERROR:", err, file);
				},
				complete: function()
				{
					//console.log("Done with all files");
				}
			});
		});
});