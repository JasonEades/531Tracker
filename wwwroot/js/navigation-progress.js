// Immediate navigation feedback. Blazor Server navigation waits on a network
// round trip (and server prerender) before any new content appears, which on
// slow mobile connections looks like a frozen app. Show the indicator on tap,
// not when the server responds.
(function () {
    const SAFETY_TIMEOUT_MS = 12000;
    let indicator = null;
    let safetyTimer = null;

    function getIndicator() {
        indicator = indicator || document.getElementById("nav-progress");
        return indicator;
    }

    function show() {
        const el = getIndicator();
        if (!el) return;
        el.classList.add("active");
        clearTimeout(safetyTimer);
        safetyTimer = setTimeout(hide, SAFETY_TIMEOUT_MS);
    }

    function hide() {
        const el = getIndicator();
        clearTimeout(safetyTimer);
        if (el) el.classList.remove("active");
    }

    function isInternalNavigation(anchor, event) {
        if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return false;
        if (anchor.hasAttribute("download")) return false;
        const target = anchor.getAttribute("target");
        if (target && target !== "_self") return false;
        const href = anchor.getAttribute("href");
        if (!href || href.startsWith("#") || /^(mailto|tel|javascript):/i.test(href)) return false;

        const url = new URL(anchor.href, location.href);
        if (url.origin !== location.origin) return false;
        // Same page (including hash-only jumps) does not navigate.
        return url.pathname + url.search !== location.pathname + location.search;
    }

    document.addEventListener("click", function (event) {
        const anchor = event.target instanceof Element ? event.target.closest("a[href]") : null;
        if (anchor && isInternalNavigation(anchor, event)) {
            show();
        }
    }, true);

    // Covers programmatic NavigationManager.NavigateTo calls and completion.
    function attachBlazorEvents() {
        if (!window.Blazor || typeof Blazor.addEventListener !== "function") {
            return false;
        }
        Blazor.addEventListener("enhancednavigationstart", show);
        Blazor.addEventListener("enhancednavigationend", hide);
        return true;
    }

    if (!attachBlazorEvents()) {
        const retry = setInterval(function () {
            if (attachBlazorEvents()) clearInterval(retry);
        }, 50);
        setTimeout(function () { clearInterval(retry); }, 10000);
    }

    // Back/forward cache restores and aborted navigations must not leave the loader stuck.
    window.addEventListener("pageshow", hide);
    window.addEventListener("pagehide", hide);
})();
