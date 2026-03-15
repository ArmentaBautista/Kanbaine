# Plan de Desarrollo - Kanban Redmine

## Descripción del Proyecto
Aplicación web Blazor Server que implementa un tablero Kanban conectado a una instancia de Redmine, permitiendo a los usuarios visualizar y gestionar sus tareas de forma visual e intuitiva.

---

## Funcionalidades Principales

### 1. Autenticación con Redmine
- Login mediante usuario y contraseña de Redmine
- Validación de credenciales contra la API de Redmine
- Manejo de sesión del usuario autenticado
- Almacenamiento seguro del API Key obtenido
- Logout y cierre de sesión

### 2. Visualización de Tareas
- Obtener tareas asignadas al usuario autenticado
- Mostrar información relevante de cada tarea:
  - Título
  - Descripción (resumida)
  - Prioridad
  - Proyecto
  - Fecha de vencimiento
- Filtrado por proyecto (opcional)

### 3. Tablero Kanban
- Columnas basadas en los estados de Redmine (Nuevo, En Progreso, Resuelto, etc.)
- Drag & Drop para mover tareas entre columnas
- Actualización del estado en Redmine al soltar la tarea
- Indicadores visuales de prioridad

---

## Arquitectura Propuesta

```
KanbanRedmine/
├── Components/
│   ├── Layout/
│   │   └── MainLayout.razor
│   ├── Pages/
│   │   ├── Home.razor
│   │   ├── Login.razor
│   │   └── Kanban.razor
│   └── Shared/
│       ├── KanbanBoard.razor
│       ├── KanbanColumn.razor
│       ├── KanbanCard.razor
│       └── LoadingSpinner.razor
├── Models/
│   ├── RedmineUser.cs
│   ├── RedmineIssue.cs
│   ├── RedmineProject.cs
│   ├── RedmineStatus.cs
│   └── KanbanColumn.cs
├── Services/
│   ├── IRedmineService.cs
│   ├── RedmineService.cs
│   ├── IAuthService.cs
│   └── AuthService.cs
├── State/
│   └── AppState.cs
└── wwwroot/
    ├── css/
    │   └── kanban.css
    └── js/
        └── dragdrop.js
```

---

## Tareas de Desarrollo

### Fase 1: Configuración Inicial ✅
- [x] 1.1 Instalar paquetes NuGet necesarios:
  - ~~`Microsoft.AspNetCore.Components.Authorization`~~ (incluido en .NET 10)
  - ~~`Blazored.LocalStorage`~~ → Usar `ProtectedBrowserStorage` nativo
  - `System.Net.Http.Json` (incluido en .NET 10)
- [x] 1.2 Configurar `appsettings.json` con URL base de Redmine
- [x] 1.3 Crear estructura de carpetas del proyecto

### Fase 2: Modelos de Datos ✅
- [x] 2.1 Crear modelo `RedmineUser` (id, login, firstname, lastname, api_key)
- [x] 2.2 Crear modelo `RedmineIssue` (id, subject, description, status, priority, project, assigned_to, due_date)
- [x] 2.3 Crear modelo `RedmineProject` (id, name, identifier)
- [x] 2.4 Crear modelo `RedmineStatus` (id, name, is_closed)
- [x] 2.5 Crear modelos de respuesta de API (wrappers para deserialización)

### Fase 3: Servicios de Redmine ✅
- [x] 3.1 Crear interfaz `IRedmineService`
- [x] 3.2 Implementar `RedmineService`:
  - Método para autenticar usuario
  - Método para obtener usuario actual
  - Método para obtener issues asignados
  - Método para obtener estados disponibles
  - Método para actualizar estado de un issue
  - Método para obtener proyectos del usuario
- [x] 3.3 Registrar servicio en `Program.cs`

### Fase 4: Autenticación ✅
- [x] 4.1 Crear `CustomAuthenticationStateProvider`
- [x] 4.2 Implementar manejo de sesión con ProtectedSessionStorage
- [x] 4.3 Crear página `Login.razor`
- [x] 4.4 Implementar formulario de login con validación
- [x] 4.5 Manejar errores de autenticación
- [x] 4.6 Implementar logout
- [x] 4.7 Proteger rutas con `[Authorize]` y `AuthorizeRouteView`

### Fase 5: Tablero Kanban - UI ✅
- [x] 5.1 Crear componente `KanbanBoard.razor`
- [x] 5.2 Crear componente `KanbanColumn.razor`
- [x] 5.3 Crear componente `KanbanCard.razor`
- [x] 5.4 Implementar estilos CSS para el tablero
- [x] 5.5 Crear página principal `Kanban.razor`

### Fase 6: Funcionalidad Drag & Drop ✅
- [x] 6.1 Implementar drag & drop nativo con HTML5
- [x] 6.2 Manejar eventos de arrastre en componentes
- [x] 6.3 Actualizar estado visual al arrastrar
- [x] 6.4 Llamar a la API de Redmine al soltar
- [x] 6.5 Manejar errores y rollback si falla la actualización

### Fase 7: Integración y Pruebas ✅
- [x] 7.1 Integrar todos los componentes
- [x] 7.2 Pruebas de autenticación (manejo de errores de red mejorado)
- [x] 7.3 Pruebas de carga de tareas
- [x] 7.4 Pruebas de drag & drop
- [x] 7.5 Manejo de errores de red/API (NotificationService + OperationResult)

---

## Funcionalidades Opcionales

### Mejoras de UX
- [x] **OPT-1**: Indicador de carga mientras se obtienen datos
- [x] **OPT-2**: Notificaciones toast para acciones exitosas/fallidas
- [ ] **OPT-3**: Modo oscuro / tema personalizable
- [ ] **OPT-4**: Animaciones suaves en transiciones

### Filtros y Búsqueda
- [x] **OPT-5**: Filtrar tareas por proyecto
- [x] **OPT-6**: Filtrar tareas por prioridad
- [x] **OPT-7**: Búsqueda de tareas por texto
- [x] **OPT-8**: Filtrar por rango de fechas

### Visualización Avanzada
- [x] **OPT-9**: Vista detallada de tarea en modal
- [ ] **OPT-10**: Mostrar imagen de avatar del usuario asignado
- [ ] **OPT-11**: Badges de colores según prioridad
- [ ] **OPT-12**: Contador de tareas por columna
- [ ] **OPT-13**: Límite WIP (Work In Progress) por columna con advertencias visuales

### Funcionalidades Adicionales
- [ ] **OPT-14**: Actualización automática del tablero (polling o SignalR)
- [x] **OPT-15**: Añadir comentarios a tareas desde la app
- [x] **OPT-16**: Ver y cambiar la prioridad de tareas
- [x] **OPT-17**: Asignar/reasignar tareas a otros usuarios
- [x] **OPT-18**: Exportar vista del tablero a imagen/PDF
- [ ] **OPT-19**: Guardar configuración de filtros por usuario

### Métricas y Reportes
- [x] **OPT-20**: Dashboard con estadísticas (tareas por estado, por proyecto)
- [x] **OPT-21**: Gráfico de burndown básico
- [x] **OPT-22**: Historial de cambios de estado

### Personalización
- [ ] **OPT-23**: Permitir reordenar columnas
- [ ] **OPT-24**: Ocultar/mostrar columnas específicas
- [ ] **OPT-25**: Personalizar colores de estados

---

## Endpoints de Redmine API Necesarios

| Funcionalidad | Método | Endpoint |
|---------------|--------|----------|
| Autenticar | GET | `/users/current.json` (con auth básica) |
| Obtener issues | GET | `/issues.json?assigned_to_id=me&status_id=open` |
| Obtener estados | GET | `/issue_statuses.json` |
| Actualizar issue | PUT | `/issues/{id}.json` |
| Obtener proyectos | GET | `/projects.json` |

**Nota**: La API de Redmine requiere autenticación. Se puede usar:
- HTTP Basic Auth con usuario/contraseña
- API Key en header `X-Redmine-API-Key`

---

## Configuración de Redmine Requerida

1. **Habilitar API REST**: Administración → Configuración → API → Activar servicio web REST
2. **Permisos de usuario**: El usuario debe tener permisos para ver y editar issues

---

## Dependencias Recomendadas

```xml
<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="9.0.0" />
<PackageReference Include="Blazored.LocalStorage" Version="4.5.0" />
```

---

## Consideraciones Técnicas

### Seguridad
- Las credenciales se transmiten solo para obtener el API key
- El API key se almacena en memoria/localStorage encriptado
- No se almacenan contraseñas en el cliente

### Rendimiento
- Implementar caché local de datos para reducir llamadas a la API
- Paginación si hay muchas tareas
- Lazy loading de descripciones completas

### Compatibilidad
- Verificar versión de Redmine (API puede variar)
- Manejar campos personalizados si existen

---

## Orden de Implementación Sugerido

1. **Sprint 1**: Fases 1-3 (Configuración y Servicios)
2. **Sprint 2**: Fase 4 (Autenticación completa)
3. **Sprint 3**: Fases 5-6 (UI Kanban y Drag & Drop)
4. **Sprint 4**: Fase 7 + Opcionales prioritarios

---

## Próximos Pasos Inmediatos

1. Agregar paquetes NuGet al proyecto
2. Configurar URL de Redmine en appsettings.json
3. Crear la estructura de carpetas
4. Comenzar con los modelos de datos
