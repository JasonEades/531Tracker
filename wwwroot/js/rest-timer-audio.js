window.restTimerAudio = (() => {
    let context = null;
    const preferenceKey = userId => `restTimerAudio.enabled.${encodeURIComponent(userId)}`;

    function getEnabled(userId) {
        if (!userId) {
            return false;
        }

        return window.localStorage.getItem(preferenceKey(userId)) !== "false";
    }

    function setEnabled(userId, enabled) {
        if (!userId) {
            throw new TypeError("A signed-in user is required to save this preference.");
        }

        window.localStorage.setItem(preferenceKey(userId), String(enabled));
    }

    let currentUserId = null;

    function setCurrentUser(userId) {
        currentUserId = userId || null;
    }

    function unlock() {
        if (!getEnabled(currentUserId)) return;

        const AudioContext = window.AudioContext || window.webkitAudioContext;
        if (!AudioContext) {
            return;
        }

        context ??= new AudioContext();
        if (context.state === "suspended") {
            // Run during a user gesture, not a server-driven timer tick.
            void context.resume();
        }
    }

    document.addEventListener("click", unlock, { capture: true });
    document.addEventListener("keydown", unlock, { capture: true });

    function beep(userId) {
        if (!getEnabled(userId) || !context || context.state !== "running") {
            return false;
        }

        const oscillator = context.createOscillator();
        const gain = context.createGain();
        const now = context.currentTime;
        oscillator.type = "sine";
        oscillator.frequency.setValueAtTime(880, now);
        gain.gain.setValueAtTime(0, now);
        gain.gain.linearRampToValueAtTime(0.2, now + 0.01);
        gain.gain.linearRampToValueAtTime(0, now + 0.15);
        oscillator.connect(gain);
        gain.connect(context.destination);
        oscillator.onended = () => {
            oscillator.disconnect();
            gain.disconnect();
        };
        oscillator.start(now);
        oscillator.stop(now + 0.15);
        return true;
    }

    return { beep, getEnabled, setEnabled, setCurrentUser };
})();
