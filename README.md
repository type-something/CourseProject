a small .NET 8 console app that acts as a simple HTTP server using raw TCP sockets. it looks in a webroot/ folder for html, css, and js files and serves them to your browser. it also handles the usual error cases (404, 403, 405) so you get a proper response if something’s missing or the request isn’t allowed. extra bits included:

- easy mime-type detection for .html, .css, and .js

- if there’s an error.html file in your webroot/, it’s used for any 4xx error instead of a plain message

- paths are url-decoded and checked so you can’t use ../ to break out of webroot/

- every request is appended to a requests.log file for later review

- uses Task.Run for each connection, and prints live stats on ThreadPool usage and OS threads so you can see how it handles load

Files in `webroot/` are served at `http://localhost:8080/`. Server logs requests to `requests.log`.

# how to launch

```bash
cd CourseProject
dotnet build
dotnet run
```

# run application

```bash
cd CourseProject
dotnet run
```

# run tests

```bash
# test
dotnet test
```

# manual testing

```bash
# try it
curl http://localhost:8080/index.html
curl http://localhost:8080/error.html
curl http://localhost:8080/nonexist_34ru34nrj3fi3n4f03i4
curl http://localhost:8080/styles.css

# Making many requests so that you can check multi threading
for i in {1..20}; do
  curl -s http://localhost:8080/index.html >/dev/null &
done
wait

# Smarter load test
# Requires `brew install wrk`
wrk -t4 -c50 -d10s http://127.0.0.1:8080/index.html
```

```bash
# will not pass tests due to course project requirements
curl -v -X POST    http://localhost:8080/index.html
curl -v -X PUT     http://localhost:8080/index.html
curl -v -X DELETE  http://localhost:8080/index.html
curl -v -X PATCH   http://localhost:8080/index.html
curl -v -X OPTIONS http://localhost:8080/index.html
curl -v -X HEAD    http://localhost:8080/index.html
```

## Testing

```bash
dotnet test
```

## Additional test Requests

```bash
curl http://localhost:8080/styles.css

# path validation
curl -v http://localhost:8080/../../passwd

# url encoding
curl -v http://localhost:8080/%2e%2e/%2e%2e/passwd
```

## Logging

The server logs all requests to `requests.log` in the following format:

```
{UTC_ISO_TIMESTAMP} {CLIENT_IP} {REQUEST_LINE}
```

## Error Handling

- 403 Forbidden: For invalid paths or directory traversal attempts
- 404 Not Found: For non-existent files
- 405 Method Not Allowed: For non-GET requests

\
