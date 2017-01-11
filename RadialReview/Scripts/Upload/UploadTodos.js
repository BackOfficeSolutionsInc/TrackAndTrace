
var uploader = UploadSteps({
	defaultData: { recurrenceId: window.recurrenceId, type: "Todos", csv: false },
    uploadFileUrl: "/Upload/UploadRecurrenceFile",
    uploadSelectionUrl: "/Upload/ProcessTodosSelection",
    confirmSelectionUrl: "/Upload/SubmitTodos",
    afterUpload: function (d) {
        uploader.addSelectionStep("Select todos (Do not select header)", validateTodo);
        debugger;
        if (d.Data.FileType == "CSV") {
            uploader.addSelectionStep("Select due date (Do not select header)", validateDate, true);
            uploader.addSelectionStep("Select owners (Do not select header)", validateUsers, true);
            uploader.addSelectionStep("Select details (Do not select header)", validateDetails, true);
        }
    }
});

function validateTodo(rect) {
    var allTrue = true;
    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);

    issuesRect = rect;
    uploader.addSelectionData("todos", issuesRect);
    return allTrue;
}

function validateUsers(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    userRect = rect;
    uploader.addSelectionData("users", userRect);
    return allTrue;
}

function validateDetails(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    detailsRect = rect;
    uploader.addSelectionData("details", detailsRect);
    return allTrue;
}
function validateDate(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    duedateRect = rect;
    uploader.addSelectionData("duedate", duedateRect);
    return allTrue;
}