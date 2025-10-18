// checkout.js
// Validación visual Bootstrap para formularios del Checkout

(() => {
    'use strict'

    // Selecciona todos los formularios con validación personalizada
    const forms = document.querySelectorAll('.needs-validation')

    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            // Si el formulario no es válido, evita envío
            if (!form.checkValidity()) {
                event.preventDefault()
                event.stopPropagation()
            }

            // Marca el formulario como validado
            form.classList.add('was-validated')
        }, false)
    })
})()
