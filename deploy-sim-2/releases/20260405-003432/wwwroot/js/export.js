/**
 * Funciones de exportación del tablero Kanban
 */

// Esperar a que html2canvas esté disponible
async function loadHtml2Canvas() {
    if (window.html2canvas) return window.html2canvas;
    
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/html2canvas/1.4.1/html2canvas.min.js';
        script.onload = () => resolve(window.html2canvas);
        script.onerror = () => reject(new Error('No se pudo cargar html2canvas'));
        document.head.appendChild(script);
    });
}

/**
 * Exportar el tablero Kanban como imagen PNG
 */
window.exportKanbanAsImage = async function(elementSelector, filename) {
    try {
        const html2canvas = await loadHtml2Canvas();
        const element = document.querySelector(elementSelector);
        
        if (!element) {
            console.error('Elemento no encontrado:', elementSelector);
            return false;
        }
        
        // Crear canvas con html2canvas
        const canvas = await html2canvas(element, {
            backgroundColor: '#f3f4f6',
            scale: 2, // Mayor resolución
            useCORS: true,
            logging: false,
            scrollX: 0,
            scrollY: 0,
            windowWidth: element.scrollWidth,
            windowHeight: element.scrollHeight
        });
        
        // Convertir a imagen y descargar
        const link = document.createElement('a');
        link.download = filename || 'kanban-board.png';
        link.href = canvas.toDataURL('image/png');
        link.click();
        
        return true;
    } catch (error) {
        console.error('Error al exportar imagen:', error);
        return false;
    }
};

/**
 * Exportar el tablero Kanban como PDF
 */
window.exportKanbanAsPdf = async function(elementSelector, filename) {
    try {
        const html2canvas = await loadHtml2Canvas();
        
        // Cargar jsPDF dinámicamente
        if (!window.jspdf) {
            await new Promise((resolve, reject) => {
                const script = document.createElement('script');
                script.src = 'https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js';
                script.onload = () => resolve();
                script.onerror = () => reject(new Error('No se pudo cargar jsPDF'));
                document.head.appendChild(script);
            });
        }
        
        const element = document.querySelector(elementSelector);
        if (!element) {
            console.error('Elemento no encontrado:', elementSelector);
            return false;
        }
        
        // Crear canvas
        const canvas = await html2canvas(element, {
            backgroundColor: '#f3f4f6',
            scale: 2,
            useCORS: true,
            logging: false,
            scrollX: 0,
            scrollY: 0,
            windowWidth: element.scrollWidth,
            windowHeight: element.scrollHeight
        });
        
        // Crear PDF
        const { jsPDF } = window.jspdf;
        const imgData = canvas.toDataURL('image/png');
        
        // Calcular dimensiones para ajustar al PDF
        const imgWidth = canvas.width;
        const imgHeight = canvas.height;
        
        // Usar orientación horizontal para tableros
        const isLandscape = imgWidth > imgHeight;
        const pdf = new jsPDF({
            orientation: isLandscape ? 'landscape' : 'portrait',
            unit: 'mm',
            format: 'a4'
        });
        
        const pdfWidth = pdf.internal.pageSize.getWidth();
        const pdfHeight = pdf.internal.pageSize.getHeight();
        
        // Escalar imagen para ajustar al PDF
        const ratio = Math.min(pdfWidth / imgWidth * 2, pdfHeight / imgHeight * 2);
        const scaledWidth = imgWidth * ratio / 2;
        const scaledHeight = imgHeight * ratio / 2;
        
        // Centrar en la página
        const x = (pdfWidth - scaledWidth) / 2;
        const y = 10; // Margen superior
        
        pdf.addImage(imgData, 'PNG', x, y, scaledWidth, scaledHeight);
        pdf.save(filename || 'kanban-board.pdf');
        
        return true;
    } catch (error) {
        console.error('Error al exportar PDF:', error);
        return false;
    }
};

/**
 * Imprimir el tablero directamente
 */
window.printKanban = function(elementSelector) {
    const element = document.querySelector(elementSelector);
    if (!element) {
        console.error('Elemento no encontrado:', elementSelector);
        return false;
    }
    
    const printContent = element.innerHTML;
    const printWindow = window.open('', '_blank');
    
    printWindow.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>Tablero Kanban</title>
            <link rel="stylesheet" href="/css/kanban.css">
            <link rel="stylesheet" href="/KanbanRedmine.styles.css">
            <style>
                body { padding: 20px; background: #f3f4f6; }
                .kanban-board { display: flex; gap: 1rem; }
                @media print {
                    body { padding: 0; }
                    .kanban-board { page-break-inside: avoid; }
                }
            </style>
        </head>
        <body>
            <div class="kanban-board">${printContent}</div>
            <script>
                setTimeout(() => {
                    window.print();
                    window.close();
                }, 500);
            </script>
        </body>
        </html>
    `);
    
    return true;
};
