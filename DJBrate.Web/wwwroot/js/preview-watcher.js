let dotNetRef = null;
let controller = null;
let controllerPromise = null;
let apiPromise = null;
let endedFired = false;
let currentTrackId = null;

function loadIframeApi() {
    if (apiPromise) return apiPromise;
    apiPromise = new Promise(resolve => {
        if (window.__SpotifyIframeApi) {
            resolve(window.__SpotifyIframeApi);
            return;
        }
        window.onSpotifyIframeApiReady = (api) => {
            window.__SpotifyIframeApi = api;
            resolve(api);
        };
        const script = document.createElement('script');
        script.src = 'https://open.spotify.com/embed/iframe-api/v1';
        script.async = true;
        document.body.appendChild(script);
    });
    return apiPromise;
}

function getEmbedElement() {
    return document.getElementById('spotify-embed');
}

async function ensureController() {
    if (controller) return controller;
    if (controllerPromise) return controllerPromise;

    controllerPromise = (async () => {
        const api = await loadIframeApi();
        const element = getEmbedElement();
        if (!element) {
            controllerPromise = null;
            return null;
        }
        return new Promise(resolve => {
            api.createController(element, { width: '100%', height: 80 }, (c) => {
                controller = c;
                c.addListener('playback_update', (e) => {
                    const data = e.data ?? {};
                    const duration = Number(data.duration ?? 0);
                    const position = Number(data.position ?? 0);
                    const isPaused = Boolean(data.isPaused ?? data.is_paused ?? false);

                    dotNetRef?.invokeMethodAsync('OnPlaybackUpdate', position, duration, isPaused);

                    if (duration <= 0) return;
                    if (position >= duration && !endedFired) {
                        endedFired = true;
                        dotNetRef?.invokeMethodAsync('OnPreviewEnded');
                    } else if (position < duration / 2) {
                        endedFired = false;
                    }
                });
                resolve(c);
            });
        });
    })();
    return controllerPromise;
}

export function init(ref) {
    dotNetRef = ref;
    endedFired = false;
}

export async function play(trackId) {
    const c = await ensureController();
    if (!c) return;
    endedFired = true;
    if (trackId !== currentTrackId) {
        currentTrackId = trackId;
        c.loadUri('spotify:track:' + trackId);
    }
    c.play();
}

export async function pause() {
    if (!controller) return;
    controller.pause();
}

export async function togglePlay() {
    if (!controller) return;
    controller.togglePlay();
}

export async function seek(positionMs) {
    if (!controller) return;
    controller.seek(positionMs / 1000);
}

export function dispose() {
    dotNetRef = null;
    if (controller) controller.pause();
}
