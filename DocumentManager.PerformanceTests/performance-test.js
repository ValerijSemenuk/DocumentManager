import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 10 }, // до 10 кор
    { duration: '1m', target: 10 },  // тримаємо 10
    { duration: '10s', target: 0 },  // спад
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // швидше за 500мс
  },
};

const BASE_URL = 'http://localhost:5160';

export default function () {
  // гет корн папки
  let res1 = http.get(`${BASE_URL}/api/folders`);
  check(res1, { 'GET /folders status 200': (r) => r.status === 200 });

  // 2. створ папку
  let folderPayload = JSON.stringify({
    name: `k6-folder-${Date.now()}-${__VU}-${__ITER}`,
    parentFolderId: null,
    createdBy: 'k6',
  });
  let res2 = http.post(`${BASE_URL}/api/folders`, folderPayload, {
    headers: { 'Content-Type': 'application/json' },
  });
  check(res2, { 'POST /folders status 200': (r) => r.status === 200 });
  
  let folderId = null;
  try {
    folderId = res2.json().id;
  } catch (e) {
    console.error('Failed to parse folder response', res2.body);
    return;
  }

  // 3. створ док
  let docPayload = JSON.stringify({
    folderId: folderId,
    name: `k6-doc-${Date.now()}.txt`,
    contentType: 'text/plain',
    sizeBytes: 1000,
  });
  let res3 = http.post(`${BASE_URL}/api/documents`, docPayload, {
    headers: { 'Content-Type': 'application/json' },
  });
  check(res3, { 'POST /documents status 200': (r) => r.status === 200 });
  
  let docId = null;
  try {
    docId = res3.json().id;
  } catch (e) {
    console.error('Failed to parse doc response', res3.body);
    return;
  }

  // 4. гет метадані
  let res4 = http.get(`${BASE_URL}/api/documents/${docId}`);
  check(res4, { 'GET /documents/{id} status 200': (r) => r.status === 200 });

  // 5. версіонування
  let updatePayload = JSON.stringify({
    name: `updated-${Date.now()}.txt`,
    sizeBytes: 2000,
  });
  let res5 = http.put(`${BASE_URL}/api/documents/${docId}`, updatePayload, {
    headers: { 'Content-Type': 'application/json' },
  });
  check(res5, { 'PUT /documents/{id} status 200': (r) => r.status === 200 });

  // 6. історія версій
  let res6 = http.get(`${BASE_URL}/api/documents/${docId}/versions`);
  check(res6, { 'GET /documents/{id}/versions status 200': (r) => r.status === 200 });

  sleep(1);
}