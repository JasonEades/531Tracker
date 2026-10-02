window.voiceNotes = (() => {
    let recognition = null;
    let dotNetReference = null;
    let shouldContinue = false;
    let stoppedNotified = false;
    let callbackQueue = Promise.resolve();
    let processedFinalResults = new Set();
    let emittedWords = [];
    let lastEmissionAt = 0;

    function normalizeWord(word) {
        return word.toLocaleLowerCase().replace(/^[^\p{L}\p{N}]+|[^\p{L}\p{N}]+$/gu, "");
    }

    function removeRecentOverlap(text) {
        const words = text.trim().split(/\s+/).filter(Boolean);
        if (words.length === 0) {
            return "";
        }

        const normalizedWords = words.map(normalizeWord);
        const now = Date.now();
        let overlap = 0;

        // SpeechRecognition implementations can resend a cumulative final phrase.
        // Only remove overlap from immediately adjacent results so users can
        // intentionally repeat a phrase later in the same recording.
        if (now - lastEmissionAt < 8000) {
            const maxOverlap = Math.min(emittedWords.length, normalizedWords.length);
            for (let length = maxOverlap; length > 0; length--) {
                const previousStart = emittedWords.length - length;
                const matches = normalizedWords
                    .slice(0, length)
                    .every((word, index) => word === emittedWords[previousStart + index]);
                if (matches) {
                    overlap = length;
                    break;
                }
            }
        }

        if (overlap === words.length) {
            return "";
        }

        const newWords = words.slice(overlap);
        emittedWords.push(...newWords.map(normalizeWord));
        emittedWords = emittedWords.slice(-150);
        lastEmissionAt = now;
        return newWords.join(" ");
    }

    function notifyStopped() {
        if (stoppedNotified || !dotNetReference) {
            return;
        }

        stoppedNotified = true;
        dotNetReference.invokeMethodAsync("VoiceNotesStopped");
    }

    function friendlyError(error) {
        switch (error) {
            case "not-allowed":
            case "service-not-allowed":
                return "Microphone access was denied. Allow microphone access for this site and try again.";
            case "audio-capture":
                return "No working microphone was found.";
            case "network":
                return "Voice recognition could not reach the browser's speech service.";
            default:
                return `Voice dictation stopped (${error}).`;
        }
    }

    function createRecognition() {
        const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!Recognition) {
            return null;
        }

        const instance = new Recognition();
        instance.continuous = true;
        instance.interimResults = true;
        instance.lang = document.documentElement.lang || navigator.language || "en-US";

        instance.onresult = event => {
            const finalParts = [];
            let interimText = "";

            for (let index = event.resultIndex; index < event.results.length; index++) {
                const text = event.results[index][0].transcript;
                if (event.results[index].isFinal) {
                    if (!processedFinalResults.has(index)) {
                        processedFinalResults.add(index);
                        finalParts.push(text);
                    }
                } else {
                    interimText += `${text} `;
                }
            }

            const finalText = removeRecentOverlap(finalParts.join(" "));
            if (dotNetReference) {
                callbackQueue = callbackQueue.catch(() => {}).then(() =>
                    dotNetReference?.invokeMethodAsync(
                        "ReceiveVoiceTranscript",
                        finalText.trim(),
                        interimText.trim()));
            }
        };

        instance.onerror = event => {
            if (event.error === "no-speech" || event.error === "aborted") {
                return;
            }

            shouldContinue = false;
            dotNetReference?.invokeMethodAsync("VoiceNotesFailed", friendlyError(event.error));
        };

        instance.onend = () => {
            if (shouldContinue) {
                window.setTimeout(() => {
                    if (!shouldContinue || !recognition) {
                        return;
                    }

                    try {
                        processedFinalResults.clear();
                        recognition.start();
                    } catch {
                        shouldContinue = false;
                        notifyStopped();
                    }
                }, 200);
                return;
            }

            notifyStopped();
        };

        return instance;
    }

    return {
        start: dotNetRef => {
            if (shouldContinue) {
                return true;
            }

            recognition ??= createRecognition();
            if (!recognition) {
                return false;
            }

            dotNetReference = dotNetRef;
            shouldContinue = true;
            stoppedNotified = false;
            processedFinalResults.clear();
            emittedWords = [];
            lastEmissionAt = 0;

            try {
                recognition.start();
                return true;
            } catch {
                shouldContinue = false;
                return false;
            }
        },

        stop: () => {
            shouldContinue = false;
            recognition?.stop();
        },

        dispose: () => {
            shouldContinue = false;
            recognition?.abort();
            recognition = null;
            dotNetReference = null;
        }
    };
})();
