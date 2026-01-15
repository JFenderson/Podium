import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '30s', target: 10 },  // Warm up
    { duration: '1m', target: 30 },   // Normal load
    { duration: '2m', target: 50 },   // Peak load
    { duration: '30s', target: 0 },   // Cool down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.API_URL || 'http://localhost:5000';

// Helper function to get auth token
function getAuthToken() {
  const loginRes = http.post(
    `${BASE_URL}/api/Auth/login`,
    JSON.stringify({
      email: 'bandstaff@test.com',
      password: 'TestPassword123!',
    }),
    {
      headers: { 'Content-Type': 'application/json' },
    }
  );

  if (loginRes.status === 200) {
    return JSON.parse(loginRes.body).accessToken;
  }
  return null;
}

export default function () {
  const token = getAuthToken();

  if (!token) {
    errorRate.add(1);
    sleep(1);
    return;
  }

  const params = {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  };

  // Test various search endpoints
  const tests = [
    {
      name: 'Search All Students',
      url: `${BASE_URL}/api/Students?page=1&pageSize=20`,
    },
    {
      name: 'Filter by Instrument',
      url: `${BASE_URL}/api/Students?instrument=Trumpet&page=1&pageSize=10`,
    },
    {
      name: 'Filter by School',
      url: `${BASE_URL}/api/Students?school=Berkeley&page=1&pageSize=10`,
    },
    {
      name: 'Filter by Graduation Year',
      url: `${BASE_URL}/api/Students?graduationYear=2025&page=1&pageSize=10`,
    },
    {
      name: 'Combined Filters',
      url: `${BASE_URL}/api/Students?instrument=Clarinet&graduationYear=2025&page=1&pageSize=10`,
    },
  ];

  // Randomly select a test to run
  const test = tests[Math.floor(Math.random() * tests.length)];

  const res = http.get(test.url, params);

  const success = check(res, {
    [`${test.name} - status 200`]: (r) => r.status === 200,
    [`${test.name} - has data`]: (r) => {
      try {
        const body = JSON.parse(r.body);
        // API returns an object with a students property
        return body && (Array.isArray(body.students) && body.students.length >= 0);
      } catch (e) {
        return false;
      }
    },
  });

  errorRate.add(!success);

  // Simulate user think time
  sleep(2 + Math.random() * 3);
}

export function handleSummary(data) {
  return {
    'results/student-search-results.json': JSON.stringify(data),
  };
}
