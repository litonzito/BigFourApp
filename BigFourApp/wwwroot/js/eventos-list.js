document.addEventListener("DOMContentLoaded", function () {

    // Asegurar que el contenedor de List.js exista
    var container = document.getElementById("event-list");
    if (!container) return;

    // Inicialización de List.js
    var options = {
        valueNames: ['name', 'date']
    };

    var eventList = new List('event-list', options);


    // ============================
    // FILTRO POR FECHA
    // ============================
    const dateFilter = document.getElementById("date-filter");
    if (dateFilter) {
        dateFilter.addEventListener("change", function () {

            const value = this.value;
            const today = new Date();

            eventList.filter(function (item) {
                const eventDate = new Date(item.values().date);

                if (value === "") return true;

                if (value === "hoy") {
                    return eventDate.toDateString() === today.toDateString();
                }

                if (value === "prox7") {
                    const week = new Date();
                    week.setDate(today.getDate() + 7);
                    return eventDate >= today && eventDate <= week;
                }

                if (value === "mes") {
                    return (
                        eventDate.getMonth() === today.getMonth() &&
                        eventDate.getFullYear() === today.getFullYear()
                    );
                }

                return true;
            });

        });
    }

});
