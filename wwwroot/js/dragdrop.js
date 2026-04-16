// ============================================
// Funciones auxiliares para Drag & Drop
// ============================================

// -------------------------------------------------------
// Auto-scroll horizontal durante drag (activo siempre,
// sin necesidad de llamar a initialize desde Blazor)
// -------------------------------------------------------
(function () {
    const SCROLL_ZONE  = 120;  // px desde el borde del viewport
    const SCROLL_SPEED = 12;   // px base por tick (~60fps)
    let scrollInterval = null;
    let currentClientX = 0;

    function stopAutoScroll() {
        if (scrollInterval !== null) {
            clearInterval(scrollInterval);
            scrollInterval = null;
        }
    }

    function scrollTick() {
        const board = document.querySelector('.kanban-board');
        if (!board) { stopAutoScroll(); return; }

        const vw = window.innerWidth;
        const distRight = vw - currentClientX;
        const distLeft  = currentClientX;
        let delta = 0;

        if (distRight < SCROLL_ZONE) {
            const ratio = 1 - (distRight / SCROLL_ZONE);
            delta = Math.ceil(SCROLL_SPEED * (1 + ratio * 2));
        } else if (distLeft < SCROLL_ZONE) {
            const ratio = 1 - (distLeft / SCROLL_ZONE);
            delta = -Math.ceil(SCROLL_SPEED * (1 + ratio * 2));
        }

        if (delta !== 0) {
            board.scrollBy({ left: delta, behavior: 'instant' });
        } else {
            stopAutoScroll();
        }
    }

    document.addEventListener('dragover', function (e) {
        currentClientX = e.clientX;
        const inZone = e.clientX < SCROLL_ZONE ||
                       (window.innerWidth - e.clientX) < SCROLL_ZONE;
        if (inZone) {
            if (scrollInterval === null) {
                scrollInterval = setInterval(scrollTick, 16);
            }
        } else {
            stopAutoScroll();
        }
    });

    document.addEventListener('dragend',  stopAutoScroll);
    document.addEventListener('drop',     stopAutoScroll);
})();

window.KanbanDragDrop = {
    /**
     * Referencia al componente .NET para callbacks
     */
    dotNetHelper: null,

    /**
     * Inicializa el drag & drop
     * @param {DotNetObjectReference} dotNetRef - Referencia al componente Blazor
     */
    initialize: function (dotNetRef) {
        this.dotNetHelper = dotNetRef;
        console.log('KanbanDragDrop initialized');
    },

    /**
     * Limpia referencias al desmontar el componente
     */
    dispose: function () {
        this.dotNetHelper = null;
        console.log('KanbanDragDrop disposed');
    },

    /**
     * Maneja el inicio del arrastre
     * @param {DragEvent} event - Evento de arrastre
     * @param {string} issueId - ID del issue que se arrastra
     */
    onDragStart: function (event, issueId) {
        event.dataTransfer.setData('text/plain', issueId);
        event.dataTransfer.effectAllowed = 'move';
        
        // Agregar clase visual
        event.target.classList.add('dragging');
    },

    /**
     * Maneja el fin del arrastre
     * @param {DragEvent} event - Evento de arrastre
     */
    onDragEnd: function (event) {
        event.target.classList.remove('dragging');
        this._stopAutoScroll();
        
        // Limpiar cualquier indicador visual
        document.querySelectorAll('.drag-over').forEach(el => {
            el.classList.remove('drag-over');
        });
    },

    /**
     * Maneja cuando un elemento pasa sobre una columna (no usado para scroll, mantenido por compatibilidad)
     * @param {DragEvent} event - Evento de arrastre
     */
    onDragOver: function (event) {
        event.preventDefault();
        event.dataTransfer.dropEffect = 'move';

        const column = event.target.closest('.kanban-column');
        if (column) {
            column.classList.add('drag-over');
        }
    },

    /**
     * Maneja cuando un elemento sale de una columna
     * @param {DragEvent} event - Evento de arrastre
     */
    onDragLeave: function (event) {
        const column = event.target.closest('.kanban-column');
        if (column && !column.contains(event.relatedTarget)) {
            column.classList.remove('drag-over');
        }
    },

    /**
     * Maneja el drop de un elemento
     * @param {DragEvent} event - Evento de drop
     * @param {string} newStatusId - ID del nuevo estado
     */
    onDrop: async function (event, newStatusId) {
        event.preventDefault();
        
        const column = event.target.closest('.kanban-column');
        if (column) {
            column.classList.remove('drag-over');
        }

        const issueId = event.dataTransfer.getData('text/plain');
        
        if (this.dotNetHelper && issueId) {
            try {
                await this.dotNetHelper.invokeMethodAsync('OnIssueMoved', parseInt(issueId), parseInt(newStatusId));
            } catch (error) {
                console.error('Error al mover issue:', error);
            }
        }
    }
};
