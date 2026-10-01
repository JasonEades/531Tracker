window.voiceNotes = (() => {
    let recognition = null;
    let dotNetReference = null;
    let shouldContinue = false;
    let stoppedNotified = false;
    let callbackQueue = Promise.resolve();

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
            let finalText = "";
            let interimText = "";

            for (let index = event.resultIndex; index < event.results.length; index++) {
                const text = event.results[index][0].transcript;
                if (event.results[index].isFinal) {
                    finalText += text;
                } else {
                    interimText += text;
                }
            }

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
