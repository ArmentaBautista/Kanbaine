// ============================================
// Funciones auxiliares para Drag & Drop
// ============================================

/**
 * Inicializa el sistema de drag & drop para el tablero Kanban
 */
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
     * Limpia las referencias al desmontar el componente
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
        
        // Limpiar cualquier indicador visual
        document.querySelectorAll('.drag-over').forEach(el => {
            el.classList.remove('drag-over');
        });
    },

    /**
     * Maneja cuando un elemento pasa sobre una columna
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
