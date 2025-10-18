document.addEventListener("DOMContentLoaded", () => {
    const btn = document.getElementById("downloadReceiptBtn");
    if (!btn) return;

    btn.addEventListener("click", async () => {
        const { jsPDF } = window.jspdf;
        const content = document.getElementById("receipt-content") || document.body;

        try {
            const canvas = await html2canvas(content, { scale: 2 });
            const imgData = canvas.toDataURL("image/png");

            const pdf = new jsPDF("p", "mm", "a4");
            const imgProps = pdf.getImageProperties(imgData);
            const pdfWidth = pdf.internal.pageSize.getWidth();
            const pdfHeight = (imgProps.height * pdfWidth) / imgProps.width;

            pdf.addImage(imgData, "PNG", 0, 0, pdfWidth, pdfHeight);
            pdf.save(`Recibo_${new Date().toISOString().slice(0, 10)}.pdf`);
        } catch (err) {
            console.error("Error al generar el PDF:", err);
            alert("Hubo un problema al generar el recibo.");
        }
    });
});
