<?php

        $connect = mysql_connect('localhost', '', '');
        if (!$connect) {
            die('Could not connect to MySQL: ' . mysql_error());
        }

        $cid = mysql_select_db('test', $connect);


        define('CSV_PATH', '/home/ubc/Documents/');

        $csv_file = CSV_PATH . "test.csv";
        $csvfile  = fopen($csv_file, 'r');
        $theData  = fgets($csvfile);

        $i = 0;
        while (!feof($csvfile)) {

            $csv_data[] = fgets($csvfile, 1024);
            $csv_array  = explode(",", $csv_data[$i]);
            $insert_csv = array();

            $insert_csv['name']  = $csv_array[0];
            $insert_csv['email'] = $csv_array[1];

            $query = mysql_query("select name from test where name='" . $insert_csv['name'] . "'");
            $count = mysql_num_rows($query);

            if ($count == 0) {
                $query = "INSERT INTO test(name,email)VALUES('" . $insert_csv['name'] . "','" . $insert_csv['email'] . "')";

                $n = mysql_query($query, $connect);

            } else {
                $sql = "update test set email='" . $insert_csv['email'] . "'";
                        $qu  = mysql_query($sql);
            }
 $i++;
            }
        }

        fclose($csvfile);

        echo "File data successfully imported to database!!";
        mysql_close($connect);
        ?>