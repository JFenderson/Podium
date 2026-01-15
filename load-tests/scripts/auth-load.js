import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.1/index.js';

// Custom metrics
const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '30s', target: 20 },  // Ramp up to 20 users
    { duration: '1m', target: 50 },   // Ramp up to 50 users
    { duration: '2m', target: 100 },  // Ramp up to 100 users
    { duration: '1m', target: 100 },  // Stay at 100 users
    { duration: '30s', target: 0 },   // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
    http_req_failed: ['rate<0.01'],   // Less than 1% of requests should fail
    errors: ['rate<0.1'],             // Less than 10% error rate
  },
};

const BASE_URL = __ENV.API_URL || 'http://localhost:5000';

export default function () {
  // Login request
  const loginPayload = JSON.stringify({
    email: `testuser${(__VU % 10) + 1}@test.com`,
    password: 'TestPassword123!',
  });

  const loginRes = http.post(
    `${BASE_URL}/api/Auth/login`,
    loginPayload,
    {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'Login' },
    }
  );

  const loginSuccess = check(loginRes, {
    'login status is 200': (r) => r.status === 200,
    'has access token': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.accessToken !== undefined;
      } catch (e) {
        return false;
      }
    },
  });

  errorRate.add(!loginSuccess);

  if (loginSuccess) {
    const token = JSON.parse(loginRes.body).accessToken;

    // Authenticated request to get user profile
    const profileRes = http.get(`${BASE_URL}/api/Auth/me`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      tags: { name: 'GetProfile' },
    });

    const profileSuccess = check(profileRes, {
      'profile status is 200': (r) => r.status === 200,
    });

    errorRate.add(!profileSuccess);
  }

  // Think time - simulate user reading the page
  sleep(1 + Math.random() * 2);
}

export function handleSummary(data) {
  return {
    'results/auth-load-results.json': JSON.stringify(data),
    stdout: textSummary(data, { indent: ' ', enableColors: true }),
  };
}
