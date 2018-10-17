<?php
class Logger {

    private
        $file,
        $prefix;

    public function __construct($filename) {
        $this->file = $filename;
    }

    public function setTimestamp($format) {
        $this->prefix = date($format)." &raquo; ";
    }

    public function putLog($insert) {
        if (isset($this->prefix)) {
            file_put_contents($this->file, $this->prefix.$insert."<br>", FILE_APPEND);
        } else {
            echo "<script>alert(\"Timestamp is not set yet.\");</script>", die;
        }
    }

    public function getLog() {
        $content = @file_get_contents($this->file);
        return $content;
    }

}

?>