const assert = require("node:assert/strict");
const { readFileSync } = require("node:fs");
const { join } = require("node:path");
const { test } = require("node:test");
const { runInNewContext } = require("node:vm");

const source = readFileSync(join(__dirname, "..", "wwwroot", "js", "rest-timer-audio.js"), "utf8");

function setup({ supported = true, legacy = false, storage = new Map() } = {}) {
    const listeners = {};
    const contexts = [];
    class AudioContext {
        constructor() {
            this.state = "suspended";
            this.currentTime = 42;
            this.destination = {};
            this.tones = [];
            this.gains = [];
            this.resumeCount = 0;
            contexts.push(this);
        }
        resume() {
            this.resumeCount++;
            this.state = "running";
            return Promise.resolve();
        }
        createOscillator() {
            const tone = {
                frequency: { setValueAtTime: (value, time) => { tone.frequencyValue = [value, time]; } },
                connect: (target) => { tone.target = target; },
                disconnect: () => { tone.disconnected = true; },
                start: (time) => { tone.startTime = time; },
                stop: (time) => { tone.stopTime = time; }
            };
            this.tones.push(tone);
            return tone;
        }
        createGain() {
            const gain = {
                envelope: [],
                gain: {
                    setValueAtTime: (value, time) => gain.envelope.push([value, time]),
                    linearRampToValueAtTime: (value, time) => gain.envelope.push([value, time])
                },
                connect: (target) => { gain.target = target; },
                disconnect: () => { gain.disconnected = true; }
            };
            this.gains.push(gain);
            return gain;
        }
    }
    const window = supported ? { [legacy ? "webkitAudioContext" : "AudioContext"]: AudioContext } : {};
    window.localStorage = {
        getItem: (key) => storage.get(key) ?? null,
        setItem: (key, value) => storage.set(key, value)
    };
    runInNewContext(source, {
        window,
        document: { addEventListener: (name, callback) => { listeners[name] = callback; } }
    });
    return { audio: window.restTimerAudio, listeners, contexts };
}

test("audio is only initialized by a user gesture", () => {
    const { audio, listeners, contexts } = setup();
    assert.equal(audio.beep(), false);
    assert.equal(contexts.length, 0);
    listeners.click();
    listeners.keydown();
    assert.equal(contexts.length, 1);
    assert.equal(contexts[0].resumeCount, 1);
});

test("each beep creates one short tone with an envelope and disconnects after playback", () => {
    const { audio, listeners, contexts } = setup();
    listeners.click();
    const context = contexts[0];
    for (let second = 10; second > 0; second--) {
        assert.equal(audio.beep(), true);
    }
    assert.equal(context.tones.length, 10);
    for (const [index, tone] of context.tones.entries()) {
        const gain = context.gains[index];
        assert.equal(tone.type, "sine");
        assert.deepEqual(tone.frequencyValue, [880, 42]);
        assert.equal(tone.startTime, 42);
        assert.equal(tone.stopTime, 42.15);
        assert.deepEqual(gain.envelope, [[0, 42], [0.2, 42.01], [0, 42.15]]);
        assert.equal(tone.target, gain);
        assert.equal(gain.target, context.destination);
        tone.onended();
        assert.equal(tone.disconnected, true);
        assert.equal(gain.disconnected, true);
    }
});

test("suspended audio does not queue stale beeps and can be unlocked again", () => {
    const { audio, listeners, contexts } = setup();
    listeners.keydown();
    contexts[0].state = "suspended";
    assert.equal(audio.beep(), false);
    assert.equal(contexts[0].tones.length, 0);
    listeners.click();
    assert.equal(audio.beep(), true);
    assert.equal(contexts[0].resumeCount, 2);
});

test("unsupported browsers report audio unavailable without breaking the timer", () => {
    const { audio, listeners, contexts } = setup({ supported: false });
    listeners.click();
    listeners.keydown();
    assert.equal(audio.beep(), false);
    assert.equal(contexts.length, 0);
});

test("legacy WebKit AudioContext is supported", () => {
    const { audio, listeners } = setup({ legacy: true });
    listeners.click();
    assert.equal(audio.beep(), true);
});

test("beeps default to enabled and disabling persists across page loads", () => {
    const storage = new Map();
    const firstPage = setup({ storage });
    assert.equal(firstPage.audio.getEnabled(), true);
    firstPage.audio.setEnabled(false);
    assert.equal(firstPage.audio.getEnabled(), false);
    const nextPage = setup({ storage });
    assert.equal(nextPage.audio.getEnabled(), false);
    nextPage.listeners.click();
    assert.equal(nextPage.contexts.length, 0);
    assert.equal(nextPage.audio.beep(), false);
});

test("disabling stops subsequent beeps and re-enabling restores playback", () => {
    const { audio, listeners, contexts } = setup();
    listeners.click();
    assert.equal(audio.beep(), true);
    audio.setEnabled(false);
    assert.equal(audio.beep(), false);
    assert.equal(contexts[0].tones.length, 1);
    audio.setEnabled(true);
    assert.equal(audio.beep(), true);
    assert.equal(contexts[0].tones.length, 2);
});

test("storage failures are reported instead of claiming the setting was saved", () => {
    const storage = new Map();
    storage.set = () => { throw new Error("Storage blocked"); };
    const { audio } = setup({ storage });
    assert.throws(() => audio.setEnabled(false), /Storage blocked/);
    assert.equal(audio.getEnabled(), true);
});
