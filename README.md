# run application

```bash
cd CourseProject
dotnet run
```

# run tests

```bash
# test
dotnet test

# try it
curl http://localhost:8080/styles.css
```

Files in `webroot/` are served at `http://localhost:8080/`. Server logs requests to `requests.log`.

```bash
# how to launch
cd CourseProject
dotnet build
dotnet run
```

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
