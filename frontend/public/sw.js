// Service worker tối giản cho MeUp — network-first cho tài nguyên cùng origin,
// fallback về cache khi offline. KHÔNG can thiệp API (khác origin) để tránh dữ liệu cũ.
const CACHE = 'meup-cache-v1';

self.addEventListener('install', (e) => self.skipWaiting());
self.addEventListener('activate', (e) => e.waitUntil(self.clients.claim()));

self.addEventListener('fetch', (event) => {
  const req = event.request;
  const url = new URL(req.url);
  // Chỉ xử lý GET cùng origin (bỏ qua API ở origin khác, POST/PUT…).
  if (req.method !== 'GET' || url.origin !== self.location.origin) return;

  event.respondWith(
    fetch(req)
      .then((res) => {
        const copy = res.clone();
        caches.open(CACHE).then((c) => c.put(req, copy)).catch(() => {});
        return res;
      })
      .catch(() =>
        caches.match(req).then((cached) => cached || (req.mode === 'navigate' ? caches.match('/') : Response.error())),
      ),
  );
});
