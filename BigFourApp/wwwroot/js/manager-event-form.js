(() => {
  const container = document.getElementById("sectionsContainer");
  const template = document.getElementById("sectionTemplate");
  const addBtn = document.getElementById("addSectionBtn");

  if (!container || !template) {
    return;
  }

  addBtn?.addEventListener("click", () => addSection());

  container.addEventListener("click", (event) => {
    const target = event.target;
    if (!(target instanceof HTMLElement)) {
      return;
    }

    if (target.classList.contains("remove-section-btn")) {
      const block = target.closest(".section-block");
      if (!block) {
        return;
      }

      if (container.querySelectorAll(".section-block").length <= 1) {
        return;
      }

      block.remove();
      renumberSections();
    }
  });

  function addSection() {
    const currentCount = container.querySelectorAll(".section-block").length;
    const html = template.innerHTML.replace(/__index__/g, currentCount.toString());
    const wrapper = document.createElement("div");
    wrapper.innerHTML = html.trim();
    const newBlock = wrapper.firstElementChild;
    if (newBlock) {
      container.appendChild(newBlock);
      renumberSections();
    }
  }

  function renumberSections() {
    const blocks = container.querySelectorAll(".section-block");
    blocks.forEach((block, index) => {
      block.setAttribute("data-index", index.toString());

      block.querySelectorAll("label[data-field]").forEach((label) => {
        const field = label.getAttribute("data-field");
        if (!field) {
          return;
        }
        label.setAttribute("for", `Sections_${index}__${field}`);
      });

      block.querySelectorAll("input[data-field]").forEach((input) => {
        const field = input.getAttribute("data-field");
        if (!field) {
          return;
        }
        input.setAttribute("id", `Sections_${index}__${field}`);
        input.setAttribute("name", `Sections[${index}].${field}`);
      });
    });

    const canRemove = blocks.length > 1;
    blocks.forEach((block) => {
      const removeBtn = block.querySelector(".remove-section-btn");
      if (removeBtn instanceof HTMLButtonElement) {
        removeBtn.disabled = !canRemove;
      }
    });
  }

  renumberSections();

  const seatmapInput = document.getElementById("SeatmapImage");
  const seatmapPreview = document.getElementById("seatmapPreview");
  const seatmapPreviewImg = seatmapPreview?.querySelector("img");
  seatmapInput?.addEventListener("change", () => {
    if (!seatmapInput.files || seatmapInput.files.length === 0 || !seatmapPreviewImg) {
      if (seatmapPreview) {
        seatmapPreview.style.display = "none";
      }
      return;
    }

    const file = seatmapInput.files[0];
    const reader = new FileReader();
    reader.onload = e => {
      seatmapPreviewImg.src = e.target?.result || "";
      if (seatmapPreview) {
        seatmapPreview.style.display = "block";
      }
    };
    reader.readAsDataURL(file);
  });

  const eventImagesInput = document.getElementById("EventImages");
  const imagePreviewContainer = document.getElementById("imagePreviewContainer");
  const imagePreviewList = document.getElementById("imagePreviewList");
  eventImagesInput?.addEventListener("change", () => {
    if (!imagePreviewList || !imagePreviewContainer) {
      return;
    }

    imagePreviewList.innerHTML = "";
    const files = eventImagesInput.files;
    if (!files || files.length === 0) {
      imagePreviewContainer.style.display = "none";
      return;
    }

    [...files].forEach(file => {
      const col = document.createElement("div");
      col.className = "col-6 col-md-4";
      const wrapper = document.createElement("div");
      wrapper.className = "border rounded p-2 text-center h-100";
      const img = document.createElement("img");
      img.className = "img-fluid rounded";
      img.alt = file.name;

      const reader = new FileReader();
      reader.onload = e => {
        img.src = e.target?.result || "";
      };
      reader.readAsDataURL(file);

      wrapper.appendChild(img);
      col.appendChild(wrapper);
      imagePreviewList.appendChild(col);
    });

    imagePreviewContainer.style.display = "block";
  });
})();
