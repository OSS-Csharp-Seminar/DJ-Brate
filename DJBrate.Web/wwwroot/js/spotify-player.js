let dotNetRef = null;
let player = null;
let deviceId = null;
let lastPosition = 0;
let endedFired = false;
let progressInterval = null;

function startProgressTimer() {
    stopProgressTimer();
    progressInterval = setInterval(async () => {
        if (!player || !dotNetRef) return;
        const state = await player.getCurrentState();
        if (!state) return;
        dotNetRef.invokeMethodAsync('OnPlaybackUpdate', state.position, state.duration, state.paused);
    }, 500);
}

function stopProgressTimer() {
    if (progressInterval) {
        clearInterval(progressInterval);
        progressInterval = null;
    }
}

function initPlayer() {
    if (!window.Spotify || !dotNetRef) return;
    if (player) { player.disconnect(); player = null; }

    player = new Spotify.Player({
        name: 'DJ Brate',
        volume: 0.8,
        getOAuthToken: async (cb) => {
            const token = await dotNetRef.invokeMethodAsync('GetAccessToken');
            cb(token);
        }
    });

    player.addListener('ready', ({ device_id }) => {
        deviceId = device_id;
    });

    player.addListener('not_ready', () => {
        deviceId = null;
    });

    player.addListener('player_state_changed', (state) => {
        if (!state || !dotNetRef) return;

        const pos = state.position;
        const dur = state.duration;
        const paused = state.paused;

        dotNetRef.invokeMethodAsync('OnPlaybackUpdate', pos, dur, paused);

        if (!paused) {
            lastPosition = pos;
            endedFired = false;
            startProgressTimer();
        } else {
            stopProgressTimer();
            if (pos < 1000 && lastPosition > 5000 && !endedFired) {
                endedFired = true;
                dotNetRef.invokeMethodAsync('OnPreviewEnded');
            }
        }
    });

    player.connect();
}

export function init(ref) {
    dotNetRef = ref;
    endedFired = false;

    if (window.Spotify) {
        initPlayer();
        return;
    }

    window.onSpotifyWebPlaybackSDKReady = initPlayer;

    if (!document.querySelector('script[src*="sdk.scdn.co"]')) {
        const script = document.createElement('script');
        script.src = 'https://sdk.scdn.co/spotify-player.js';
        script.async = true;
        document.body.appendChild(script);
    }
}

export async function play(trackId) {
    if (!deviceId || !dotNetRef) return;
    endedFired = false;
    lastPosition = 0;

    const token = await dotNetRef.invokeMethodAsync('GetAccessToken');
    await fetch(`https://api.spotify.com/v1/me/player/play?device_id=${deviceId}`, {
        method: 'PUT',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ uris: [`spotify:track:${trackId}`] })
    });
}

export async function pause() {
    if (!player) return;
    await player.pause();
}

export async function togglePlay() {
    if (!player) return;
    await player.togglePlay();
}

export async function seek(positionMs) {
    if (!player) return;
    await player.seek(positionMs);
}

export function dispose() {
    stopProgressTimer();
    dotNetRef = null;
    if (player) {
        player.pause();
        player.disconnect();
        player = null;
    }
    deviceId = null;
}
