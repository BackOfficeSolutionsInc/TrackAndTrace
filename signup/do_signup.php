if (($_POST['submit'])) {

    include_once '../connect.php';

}else {
    header("Location: ../signup.php");
exit();
}