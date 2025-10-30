import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 5,
  duration: '30s',
};

const BASE = __ENV.GATEWAY_URL || 'http://localhost:5000';

export default function () {
  const res = http.get(`${BASE}/api/orchestration/tasks`);
  check(res, { 'tasks ok': (r) => r.status === 200 });
  sleep(1);
}
