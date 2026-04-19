const CACHE_NAME = 'blacktech-portal-v1';
const OFFLINE_URL = '/offline';

// Assets to pre-cache (shell assets only - no financial data)
const PRECACHE_ASSETS = [
  '/',
  '/offline',
  '/css/site.css',
  '/favicon.ico'
];

// Install: pre-cache shell assets
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME).then(cache => {
      return cache.addAll(PRECACHE_ASSETS);
    }).then(() => self.skipWaiting())
  );
});

// Activate: clean up old caches
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(keys => {
      return Promise.all(
        keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key))
      );
    }).then(() => self.clients.claim())
  );
});

// Fetch: network-first strategy for all requests
// For financial app, always prefer fresh data from network.
// Only fall back to offline page if network is unavailable.
self.addEventListener('fetch', event => {
  // Only handle GET requests and same-origin requests
  if (event.request.method !== 'GET') return;

  const url = new URL(event.request.url);

  // Bypass service worker for CDN assets (Bootstrap, Bootstrap Icons)
  if (url.hostname !== self.location.hostname) return;

  event.respondWith(
    fetch(event.request)
      .then(response => {
        // Cache static assets (CSS/JS/icons) from our own origin
        if (
          url.pathname.startsWith('/css/') ||
          url.pathname.startsWith('/js/') ||
          url.pathname.startsWith('/lib/') ||
          url.pathname === '/favicon.ico'
        ) {
          const clone = response.clone();
          caches.open(CACHE_NAME).then(cache => cache.put(event.request, clone));
        }
        return response;
      })
      .catch(() => {
        // Network failed — return offline page for navigation requests
        if (event.request.mode === 'navigate') {
          return caches.match(OFFLINE_URL);
        }
        // For assets, try cache before giving up
        return caches.match(event.request);
      })
  );
});
