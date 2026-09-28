// Cross-platform "Install app" support.
//
// - Android / desktop Chrome, Edge, other Chromium browsers: listens for the
//   `beforeinstallprompt` event, stashes it, and replays it when the user
//   clicks the install button.
// - iOS / iPadOS Safari (and other browsers with no install prompt API):
//   there is no programmatic install trigger, so the button instead shows
//   "Add to Home Screen" instructions.
// - Already installed / running standalone: the button hides itself.

window.pwaInstall = (function () {
    let deferredPrompt = null;
    let dotNetRef = null;

    function isStandalone() {
        return window.matchMedia('(display-mode: standalone)').matches
            || window.navigator.standalone === true; // iOS Safari
    }

    function isIos() {
        return /iphone|ipad|ipod/i.test(window.navigator.userAgent)
            && !window.MSStream;
    }

    window.addEventListener('beforeinstallprompt', (event) => {
        event.preventDefault();
        deferredPrompt = event;
        dotNetRef?.invokeMethodAsync('OnInstallAvailabilityChanged', true);
    });

    window.addEventListener('appinstalled', () => {
        deferredPrompt = null;
        dotNetRef?.invokeMethodAsync('OnInstallAvailabilityChanged', false);
    });

    return {
        init(ref) {
            dotNetRef = ref;
            return {
                canPrompt: deferredPrompt !== null,
                isIos: isIos(),
                isStandalone: isStandalone()
            };
        },

        async promptInstall() {
            if (!deferredPrompt) {
                return 'unavailable';
            }

            deferredPrompt.prompt();
            const choice = await deferredPrompt.userChoice;
            deferredPrompt = null;
            return choice.outcome; // 'accepted' | 'dismissed'
        },

        registerServiceWorker() {
            if ('serviceWorker' in navigator) {
                navigator.serviceWorker.register('/service-worker.js').catch((err) => {
                    console.error('Service worker registration failed:', err);
                });
            }
        }
    };
})();
