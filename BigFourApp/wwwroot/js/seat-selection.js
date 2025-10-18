//Script que gestiona el carrito del lado del cliente, actualiza subtotales 
// y muestra un modal resumen con los asientos elegidos 
(function () {
  const cart = new Map();

  const cartList = document.getElementById("cartList");
  const subtotalLabel = document.getElementById("subtotalLabel");
  const openSummaryBtn = document.getElementById("openSummaryBtn");

  const summaryModalEl = document.getElementById("summaryModal");
  const summaryList = document.getElementById("summaryList");
  const summarySubtotal = document.getElementById("summarySubtotal");

  // Suscribe cambios en los checkboxes de asientos
  document.querySelectorAll(".seat-checkbox").forEach(cb => {
    cb.addEventListener("change", onSeatToggle);
  });

  function onSeatToggle(e) {
    const cb = e.target;
    const seatId = cb.dataset.seatId;
    const label = cb.dataset.seatLabel;
    const price = parseFloat(cb.dataset.seatPrice || "0");

    if (cb.checked) cart.set(seatId, { seatId, label, price });
    else cart.delete(seatId);

    renderCart();
  }

  function renderCart() {
    cartList.innerHTML = "";
    let subtotal = 0;

    [...cart.values()].forEach(item => {
      subtotal += item.price;

      const li = document.createElement("li");
      li.className = "list-group-item d-flex justify-content-between align-items-center";

      const left = document.createElement("div");
      left.innerHTML = `<div class="fw-semibold">${item.label}</div>
                        <div class="text-muted small">$${item.price.toFixed(2)}</div>`;

      const removeBtn = document.createElement("button");
      removeBtn.className = "btn btn-sm btn-outline-danger";
      removeBtn.type = "button";
      removeBtn.textContent = "Quitar";
      removeBtn.addEventListener("click", () => {
        const cb = document.querySelector(`.seat-checkbox[data-seat-id="${item.seatId}"]`);
        if (cb) cb.checked = false;
        cart.delete(item.seatId);
        renderCart();
      });

      li.appendChild(left);
      li.appendChild(removeBtn);
      cartList.appendChild(li);
    });

    subtotalLabel.textContent = `$${subtotal.toFixed(2)}`;
  }

  // Modal de resumen (sin llamada al servidor)
  openSummaryBtn?.addEventListener("click", () => {
    // Llena la lista del resumen
    summaryList.innerHTML = "";
    let subtotal = 0;
    const arr = [...cart.values()];
    arr.forEach(item => {
      subtotal += item.price;
      const li = document.createElement("li");
      li.className = "list-group-item d-flex justify-content-between align-items-center";
      li.innerHTML = `<span>${item.label}</span><span>$${item.price.toFixed(2)}</span>`;
      summaryList.appendChild(li);
    });
    summarySubtotal.textContent = `$${subtotal.toFixed(2)}`;

    // Muestra el modal
    const modal = bootstrap.Modal.getOrCreateInstance(summaryModalEl);
    modal.show();
  });

    // Envia los asientos seleccionados al servidor cuando el usuario confirma el resumen
    const proceedBtn = document.getElementById("proceedCheckoutBtn");
    const eventId = document.getElementById("eventId")?.value;

    proceedBtn?.addEventListener("click", () => {
        const selectedSeatIds = [...cart.values()].map(item => item.seatId);

        if (selectedSeatIds.length === 0) {
            alert("Selecciona al menos un asiento antes de continuar.");
            return;
        }

        fetch("/Seat/Checkout", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                eventId: eventId,
                seatIds: selectedSeatIds
            })
        })
            .then(response => {
                if (response.redirected) {
                    window.location.href = response.url;
                } else if (response.ok) {
                    return response.text();
                } else {
                    alert("Error al ir al checkout.");
                }
            })
            .then(html => {
                if (html) document.body.innerHTML = html;
            })
            .catch(err => console.error("Error al ir al checkout:", err));
    });

})();
