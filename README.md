<h1>Work:</h1>
<ul>
<li>
Handcrafted TCP server for static files (GET-only, path-sanitized, 403/404/405).
</li>
<li>
Built-in logging, MIME detection, and custom error.html.
</li>
<li>
Multithreaded handling for high concurrency.
</li>
</ul>

# how to launch

```bash
cd CourseProject
dotnet build
dotnet run

# try it
curl http://localhost:8080/index.html
curl http://localhost:8080/error.html
curl http://localhost:8080/nonexist_34ru34nrj3fi3n4f03i4
curl http://localhost:8080/styles.css
```

```bash
# making many requests so that you can check multi threading
for i in {1..20}; do
  curl -s http://localhost:8080/index.html >/dev/null &
done
wait

# best way to check
# requires wrk package
wrk -t4 -c50 -d10s http://127.0.0.1:8080/index.html
```

# will not work out

```bash
curl -v -X POST    http://localhost:8080/index.html
curl -v -X PUT     http://localhost:8080/index.html
curl -v -X DELETE  http://localhost:8080/index.html
curl -v -X PATCH   http://localhost:8080/index.html
curl -v -X OPTIONS http://localhost:8080/index.html
curl -v -X HEAD    http://localhost:8080/index.html
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

Files in `webroot/` are served at `http://localhost:8080/`. Server logs requests to `requests.log`.

## Testing

```bash
dotnet test
```

## Test Requests

```bash
curl http://localhost:8080/styles.css

# path validation
curl -v http://localhost:8080/../../etc/passwd

# url encoding
curl -v http://localhost:8080/%2e%2e/%2e%2e/etc/passwd
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
