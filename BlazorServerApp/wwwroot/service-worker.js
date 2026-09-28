// Minimal service worker for PWA installability.
//
// This app runs as Blazor Server: pages are rendered on the server and kept
// alive over a live SignalR circuit, so there is no meaningful "offline mode"
// to provide - without a connection the app cannot function. This worker's
// job is therefore just to satisfy the browser's installability checks
// (a registered service worker with a fetch handler) and to give a little
// resilience for static assets, not to make the app work offline.

const CACHE_NAME = 'app-shell-v1';
const APP_SHELL = [
  '/',
  '/app.css',
  '/themes.css',
  '/favicon.png',
  '/manifest.webmanifest'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => cache.addAll(APP_SHELL))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(
        keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))
      ))
      .then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (event) => {
  if (event.request.method !== 'GET') {
    return;
  }

  // Never cache the SignalR/Blazor circuit traffic - it must always hit the
  // network live.
  const url = new URL(event.request.url);
  if (url.pathname.startsWith('/_blazor') || url.pathname.startsWith('/_framework')) {
    return;
  }

  event.respondWith(
    fetch(event.request)
      .then((response) => {
        const clone = response.clone();
        caches.open(CACHE_NAME).then((cache) => cache.put(event.request, clone));
        return response;
      })
      .catch(() => caches.match(event.request))
  );
});
