window.contentImageLightbox = (() => {
    let images = [];
    let activeIndex = 0;
    let overlay = null;
    let opener = null;

    function close() {
        if (!overlay) return;
        overlay.remove();
        overlay = null;
        document.body.style.overflow = "";
        document.removeEventListener("keydown", onKeyDown);
        opener?.focus();
        opener = null;
    }

    function move(step) {
        if (images.length < 2) return;
        activeIndex = (activeIndex + step + images.length) % images.length;
        renderImage();
    }

    function onKeyDown(event) {
        if (event.key === "Escape") close();
        else if (event.key === "ArrowLeft") move(-1);
        else if (event.key === "ArrowRight") move(1);
    }

    function renderImage() {
        if (!overlay) return;
        const image = images[activeIndex];
        const picture = overlay.querySelector(".content-lightbox-image");
        const caption = overlay.querySelector(".content-lightbox-caption");
        const counter = overlay.querySelector(".content-lightbox-counter");
        picture.src = image.url;
        picture.alt = image.caption || "";
        caption.textContent = image.caption || "";
        counter.textContent = images.length > 1 ? `${activeIndex + 1} / ${images.length}` : "";
        overlay.querySelector(".content-lightbox-previous").hidden = images.length < 2;
        overlay.querySelector(".content-lightbox-next").hidden = images.length < 2;
    }

    function open(galleryImages, index) {
        if (!Array.isArray(galleryImages) || galleryImages.length === 0) return;
        close();
        opener = document.activeElement;
        images = galleryImages;
        activeIndex = Math.max(0, Math.min(index, images.length - 1));
        overlay = document.createElement("div");
        overlay.className = "content-lightbox";
        overlay.setAttribute("role", "dialog");
        overlay.setAttribute("aria-modal", "true");
        overlay.setAttribute("aria-label", "Photo viewer");
        overlay.innerHTML = `
            <button type="button" class="content-lightbox-close" aria-label="Close photo viewer">×</button>
            <button type="button" class="content-lightbox-previous" aria-label="Previous photo">‹</button>
            <figure class="content-lightbox-figure">
                <img class="content-lightbox-image" alt="">
                <figcaption class="content-lightbox-caption"></figcaption>
            </figure>
            <button type="button" class="content-lightbox-next" aria-label="Next photo">›</button>
            <span class="content-lightbox-counter"></span>`;
        overlay.addEventListener("click", event => {
            if (event.target === overlay) close();
        });
        overlay.querySelector(".content-lightbox-close").addEventListener("click", close);
        overlay.querySelector(".content-lightbox-previous").addEventListener("click", () => move(-1));
        overlay.querySelector(".content-lightbox-next").addEventListener("click", () => move(1));
        document.body.appendChild(overlay);
        document.body.style.overflow = "hidden";
        document.addEventListener("keydown", onKeyDown);
        renderImage();
        overlay.querySelector(".content-lightbox-close").focus();
    }

    return { open, close };
})();
