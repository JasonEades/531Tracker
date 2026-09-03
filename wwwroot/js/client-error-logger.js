// Captures uncaught JS errors and unhandled promise rejections and forwards them to the
// server for logging, since mobile browsers have no accessible dev console.
(function () {
    function send(payload) {
        try {
            fetch('/api/client-log', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload),
                keepalive: true
            }).catch(function () { /* swallow — best effort logging */ });
        } catch (e) { /* ignore */ }
    }

    window.addEventListener('error', function (event) {
        send({
            message: event.message || 'Unknown error',
            source: event.filename || null,
            line: event.lineno || null,
            column: event.colno || null,
            url: location.href,
            stack: event.error && event.error.stack ? event.error.stack : null
        });
    });

    window.addEventListener('unhandledrejection', function (event) {
        var reason = event.reason;
        send({
            message: 'Unhandled promise rejection: ' + (reason && reason.message ? reason.message : String(reason)),
            source: null,
            line: null,
            column: null,
            url: location.href,
            stack: reason && reason.stack ? reason.stack : null
        });
    });

    // Blazor Server circuit is "rejected"/lost — surfaces as the generic error UI.
    document.addEventListener('components-reconnect-state-changed', function (event) {
        if (event.detail && (event.detail.state === 'failed' || event.detail.state === 'rejected')) {
            send({
                message: 'Blazor circuit ' + event.detail.state,
                source: 'blazor-circuit',
                line: null,
                column: null,
                url: location.href,
                stack: null
            });
        }
    });
})();
