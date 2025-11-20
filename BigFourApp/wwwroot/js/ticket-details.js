document.addEventListener('DOMContentLoaded', function () {
    var canvas = document.getElementById('qrCanvas');
    var code = canvas ? canvas.getAttribute('data-code') : '';
    if (!canvas || !code) return;
    new QRious({
        element: canvas,
        value: code,
        size: 220,
        level: 'H',
        background: 'transparent',
        foreground: '#0b1f5e'
    });
});