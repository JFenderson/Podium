# Load Testing for Podium

This directory contains K6 load testing scripts for the Podium application.

## Prerequisites

Install K6:

### macOS
```bash
brew install k6
```

### Linux (Debian/Ubuntu)
```bash
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

### Windows
```powershell
choco install k6
```

## Running Tests

### Authentication Load Test
Tests login endpoints under load:
```bash
k6 run scripts/auth-load.js
```

With custom API URL:
```bash
k6 run --env API_URL=https://api.yourserver.com scripts/auth-load.js
```

### Student Search Load Test
Tests student search and filtering endpoints:
```bash
k6 run scripts/student-search-load.js
```

## Test Scenarios

### auth-load.js
- **Purpose**: Test authentication system under load
- **VUs**: Ramps from 0 to 100 users
- **Duration**: ~5 minutes
- **Endpoints Tested**:
  - POST /api/Auth/login
  - GET /api/Auth/me

### student-search-load.js
- **Purpose**: Test student search and filtering
- **VUs**: Ramps from 0 to 50 users
- **Duration**: ~4 minutes
- **Endpoints Tested**:
  - GET /api/Students (various filter combinations)

## Understanding Results

K6 provides several key metrics:

- **http_req_duration**: Response time
  - `avg`: Average response time
  - `p(95)`: 95th percentile (95% of requests faster than this)
  - `p(99)`: 99th percentile
  
- **http_req_failed**: Percentage of failed requests
  - Should be < 1% for healthy systems

- **iterations**: Number of test iterations completed

- **vus**: Virtual users (concurrent users)

## Thresholds

Tests will fail if:
- 95th percentile response time > 500ms
- Request failure rate > 1%
- Error rate > 10%

## Results

Results are saved to `results/` directory as JSON files:
- `auth-load-results.json`
- `student-search-results.json`

## CI/CD Integration

These tests can be run in CI/CD pipelines:

```yaml
- name: Run Load Tests
  run: |
    k6 run --out json=results/auth.json scripts/auth-load.js
    k6 run --out json=results/search.json scripts/student-search-load.js
```

## Adding New Tests

1. Create a new `.js` file in `scripts/`
2. Define test scenarios with stages
3. Set appropriate thresholds
4. Add to this README

## Test Data

Ensure your test environment has:
- Seeded test users (testuser1@test.com through testuser10@test.com)
- Sample student data
- Active band staff accounts

## Performance Targets

- **Response Time**: p95 < 500ms
- **Throughput**: > 100 req/s per service
- **Error Rate**: < 1%
- **Concurrent Users**: Support 100+ simultaneous users
