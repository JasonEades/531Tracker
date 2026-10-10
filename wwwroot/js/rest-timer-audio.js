window.restTimerAudio = (() => {
    let context = null;
    const preferenceKey = "restTimerAudio.enabled";

    function getEnabled() {
        return window.localStorage.getItem(preferenceKey) !== "false";
    }

    function setEnabled(enabled) {
        window.localStorage.setItem(preferenceKey, String(enabled));
    }

    function unlock() {
        if (!getEnabled()) {
            return;
        }

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

    function beep() {
        if (!getEnabled() || !context || context.state !== "running") {
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

    return { beep, getEnabled, setEnabled };
})();
