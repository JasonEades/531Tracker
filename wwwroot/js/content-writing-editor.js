window.contentWritingEditor = (() => {
    function editor() {
        return document.getElementById("content-body");
    }

    function replaceSelection(prefix, suffix, placeholder) {
        const area = editor();
        if (!area) return;

        const start = area.selectionStart;
        const end = area.selectionEnd;
        const selected = area.value.slice(start, end);
        const content = selected || placeholder;
        const insertion = prefix + content + suffix;
        area.setRangeText(insertion, start, end, "select");
        if (!selected && placeholder) {
            area.setSelectionRange(start + prefix.length, start + prefix.length + placeholder.length);
        }
        area.focus();
        area.dispatchEvent(new InputEvent("input", {
            bubbles: true,
            inputType: "insertText",
            data: insertion
        }));
    }

    function insertLinePrefix(prefix, placeholder) {
        const area = editor();
        if (!area) return;

        const start = area.selectionStart;
        const end = area.selectionEnd;
        const lineStart = area.value.lastIndexOf("\n", Math.max(0, start - 1)) + 1;
        const selectedEnd = area.value.indexOf("\n", end);
        const lineEnd = selectedEnd < 0 ? area.value.length : selectedEnd;
        const selected = area.value.slice(lineStart, lineEnd) || placeholder;
        const lines = selected.split("\n").map(line => line.startsWith(prefix) ? line : prefix + line);
        const insertion = lines.join("\n");
        area.setRangeText(insertion, lineStart, lineEnd, "select");
        area.focus();
        area.dispatchEvent(new InputEvent("input", {
            bubbles: true,
            inputType: "insertText",
            data: insertion
        }));
    }

    function insertBlock(prefix, suffix, placeholder) {
        const area = editor();
        if (!area) return;

        const start = area.selectionStart;
        const end = area.selectionEnd;
        const selected = area.value.slice(start, end) || placeholder;
        const insertion = prefix + selected + suffix;
        area.setRangeText(insertion, start, end, "select");
        area.focus();
        area.dispatchEvent(new InputEvent("input", {
            bubbles: true,
            inputType: "insertText",
            data: insertion
        }));
    }

    function insertLink() {
        const area = editor();
        if (!area) return;

        const url = window.prompt("Enter the link URL (https://…):");
        if (!url) return;
        replaceSelection("[", `](${url.replace(/[)\s]/g, character => encodeURIComponent(character))})`, "link text");
    }

    function insertImage() {
        const area = editor();
        if (!area) return;

        const url = window.prompt("Enter the image URL (https://…):");
        if (!url) return;
        const alt = window.prompt("Describe the image for alt text:", "Image") || "Image";
        replaceSelection("![", `](${url.replace(/[)\s]/g, character => encodeURIComponent(character))})`, alt);
    }

    function command(name) {
        const area = editor();
        if (!area) return;
        area.focus();
        document.execCommand(name);
    }

    document.addEventListener("keydown", event => {
        const area = event.target;
        if (!area || area.id !== "content-body" || !(event.ctrlKey || event.metaKey)) return;

        const shortcuts = {
            b: ["**", "**", "bold text"],
            i: ["*", "*", "italic text"],
            k: ["[", "](https://)", "link text"]
        };
        const shortcut = shortcuts[event.key.toLowerCase()];
        if (!shortcut) return;

        event.preventDefault();
        replaceSelection(...shortcut);
    });

    return { replaceSelection, insertLinePrefix, insertBlock, insertLink, insertImage, command };
})();
