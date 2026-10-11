// Highlights the progress-photo drop zone while a file is dragged over it. The transparent
// <input type="file"> covering the zone handles the actual drop, so Blazor's InputFile receives it.
(function () {
    function zoneFor(event) {
        return event.target instanceof Element ? event.target.closest(".photo-dropzone") : null;
    }

    function hasFiles(event) {
        return event.dataTransfer && Array.from(event.dataTransfer.types || []).includes("Files");
    }

    document.addEventListener("dragover", function (event) {
        const zone = zoneFor(event);
        if (zone && hasFiles(event)) zone.classList.add("is-dragover");
    });

    ["dragleave", "drop"].forEach(function (name) {
        document.addEventListener(name, function (event) {
            const zone = zoneFor(event);
            if (zone) zone.classList.remove("is-dragover");
        });
    });
})();
