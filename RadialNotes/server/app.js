var port = 3000,// || process.env.PORT || 3000,
    http = require('http'),
    fs = require('fs'),
    html = fs.readFileSync('index.html');

var log = function(entry) {
 // fs.appendFileSync('/tmp/sample-app.log', new Date().toISOString() + ' - ' + entry + '\n');
};

var server = http.createServer(function (req, res) {
 if (req.method === 'post') {
     var body = '';

     req.on('data', function(chunk) {
         body += chunk;
     });

     req.on('end', function() {
         if (req.url === '/') {
             log('received message: ' + body);
         } else if (req.url = '/scheduled') {
             log('received task ' + req.headers['x-aws-sqsd-taskname'] + ' scheduled at ' + req.headers['x-aws-sqsd-scheduled-at']);
         }

         res.writehead(200, 'ok', {'content-type': 'text/plain'});
         res.end();
     });
 } else {
     res.writehead(200);
     res.write(html);
     res.end();
 }
});

// Listen on port 3000, IP defaults to 127.0.0.1
server.listen(port);

// Put a friendly message on the terminal
console.log('Server running at http://127.0.0.1:' + port + '/');

console.log("starting placeholder");