document.addEventListener('DOMContentLoaded', function () {
    var tabs = document.querySelectorAll('#ticketTabs .nav-link');
    tabs.forEach(function (btn) {
        btn.addEventListener('click', function () {
            tabs.forEach(function (b) { b.classList.remove('active'); });
            btn.classList.add('active');
            document.querySelectorAll('.ticket-pane').forEach(function (pane) {
                pane.classList.remove('show');
            });
            var target = btn.getAttribute('data-target');
            var pane = document.querySelector(target);
            if (pane) pane.classList.add('show');
        });
    });
});
