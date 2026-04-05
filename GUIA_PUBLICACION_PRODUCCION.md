# Guia paso a paso para publicar KanbanRedmine a produccion

Esta guia esta basada en el estado actual del proyecto:

- Aplicacion ASP.NET Core Web con Blazor Server en .NET 10.
- Render interactivo en servidor.
- Dependencia directa de una instancia Redmine configurada por `Redmine:*`.
- Publicacion validada localmente con `dotnet publish -c Release`.

## 1. Objetivo de despliegue recomendado

Para Windows Server, el camino mas directo y mantenible es:

1. Publicar la aplicacion con `dotnet publish` en modo `Release`.
2. Instalar el runtime/hosting bundle de .NET 10 en el servidor.
3. Hospedar la app en IIS.
4. Exponerla por HTTPS con certificado valido.
5. Configurar la conexion a Redmine mediante `appsettings.Production.json` o variables de entorno.

## 2. Antes de publicar

### 2.1 Confirmar prerrequisitos tecnicos

En el servidor de produccion debes tener:

1. Windows Server con IIS habilitado.
2. ASP.NET Core Hosting Bundle de .NET 10 instalado.
3. Un certificado TLS valido para el dominio productivo.
4. Acceso de red desde el servidor hacia la instancia Redmine.
5. Permisos para crear carpetas, app pool, sitio IIS y logs.

### 2.2 Validar Redmine

Antes de publicar, confirma lo siguiente en Redmine:

1. La API REST esta habilitada.
2. Los usuarios que entraran a la aplicacion pueden autenticarse contra Redmine.
3. Los usuarios tienen permisos para ver y editar issues.
4. La URL base de Redmine que usara produccion responde desde el servidor destino.
5. Si Redmine esta expuesto por HTTPS, usa esa URL en produccion.

Ejemplo esperado:

```text
https://redmine.midominio.com/
```

### 2.3 Definir los valores de configuracion productiva

Este proyecto lee la seccion `Redmine` desde configuracion. Como minimo debes definir:

```json
{
  "Redmine": {
    "BaseUrl": "https://redmine.midominio.com",
    "ApiPath": "/",
    "TimeoutSeconds": 30
  }
}
```

Notas:

1. `BaseUrl` debe apuntar al sitio real de Redmine en produccion.
2. `ApiPath` hoy se concatena directo a `BaseUrl`; si tu Redmine vive en raiz, deja `"/"`.
3. Si Redmine responde lento o esta detras de VPN, ajusta `TimeoutSeconds` con criterio.

## 3. Preparar el servidor

### 3.1 Instalar IIS

Si el servidor aun no tiene IIS:

1. Abre `Server Manager`.
2. Entra a `Add roles and features`.
3. Habilita `Web Server (IIS)`.
4. Incluye `Application Development` y los componentes basicos de administracion.
5. Completa la instalacion.

### 3.2 Instalar .NET 10 Hosting Bundle

1. Descarga el `ASP.NET Core Hosting Bundle` correspondiente a .NET 10.
2. Ejecuta el instalador como administrador.
3. Reinicia IIS con:

```powershell
iisreset
```

### 3.3 Crear estructura de carpetas

Ejemplo recomendado:

```text
C:\apps\KanbanRedmine\current
C:\apps\KanbanRedmine\releases
C:\apps\KanbanRedmine\logs
```

Uso sugerido:

1. `current`: version activa.
2. `releases`: historico de paquetes desplegados.
3. `logs`: logs del sitio y del stdout si llegas a habilitarlo temporalmente.

## 4. Preparar la publicacion desde tu maquina de trabajo

### 4.1 Detener instancias locales si vas a compilar en Debug

En este repositorio ya aparecio un bloqueo del archivo `bin\Debug\net10.0\KanbanRedmine.exe` por un proceso en ejecucion. Si vas a compilar o publicar usando binarios `Debug`, primero deten la app.

Para produccion, usa siempre `Release`.

### 4.2 Publicar el proyecto

Desde la raiz del repositorio ejecuta:

```powershell
dotnet publish .\KanbanRedmine.csproj -c Release -o .\publish
```

Ese comando genera una carpeta lista para copiar al servidor.

Si quieres una salida con version:

```powershell
dotnet publish .\KanbanRedmine.csproj -c Release -o .\artifacts\2026-04-02-release
```

### 4.3 Revisar el resultado

Confirma que en la carpeta publicada existan al menos:

1. `KanbanRedmine.dll`
2. `KanbanRedmine.exe`
3. `appsettings.json`
4. `web.config`
5. Carpeta `wwwroot` y recursos estaticos

## 5. Configurar la version de produccion

### 5.1 Crear `appsettings.Production.json`

En la carpeta publicada agrega un archivo `appsettings.Production.json` con valores productivos.

Ejemplo:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "kanban.midominio.com",
  "Redmine": {
    "BaseUrl": "https://redmine.midominio.com",
    "ApiPath": "/",
    "TimeoutSeconds": 30
  }
}
```

Recomendaciones:

1. No publiques la URL interna de desarrollo en produccion.
2. Cambia `AllowedHosts` a tu dominio real y evita `*` si ya conoces el hostname final.
3. Usa HTTPS hacia Redmine si esta disponible.

### 5.2 Alternativa con variables de entorno

Si prefieres no dejar estos valores en archivo, puedes definir:

```text
ASPNETCORE_ENVIRONMENT=Production
Redmine__BaseUrl=https://redmine.midominio.com
Redmine__ApiPath=/
Redmine__TimeoutSeconds=30
```

Esto sirve bien cuando tu equipo administra configuracion desde IIS o desde el sistema operativo.

## 6. Copiar artefactos al servidor

### 6.1 Crear una release versionada

Ejemplo:

```text
C:\apps\KanbanRedmine\releases\2026-04-02-001
```

### 6.2 Copiar el contenido publicado

1. Copia todos los archivos de la carpeta `publish` a la release nueva.
2. Agrega o ajusta `appsettings.Production.json` en esa carpeta.
3. Verifica que la identidad del app pool tenga permisos de lectura sobre la carpeta.

### 6.3 Activar la release

Opciones:

1. Copiar esa release a `C:\apps\KanbanRedmine\current`.
2. O usar una estrategia de carpeta activa administrada por tu proceso de despliegue.

Si no tienes automatizacion, la opcion mas simple es dejar el sitio apuntando a `current` y reemplazar su contenido en cada despliegue controlado.

## 7. Crear el sitio en IIS

### 7.1 Crear el Application Pool

En IIS:

1. Abre `Application Pools`.
2. Crea uno nuevo llamado `KanbanRedmineAppPool`.
3. Usa `.NET CLR Version: No Managed Code`.
4. Deja `Managed pipeline mode: Integrated`.
5. Inicia el app pool.

### 7.2 Crear el sitio web

1. Abre `Sites`.
2. Selecciona `Add Website...`.
3. Nombre: `KanbanRedmine`.
4. Physical path: `C:\apps\KanbanRedmine\current`.
5. Binding HTTPS con el hostname productivo.
6. Asigna el certificado correspondiente.
7. Asocia el sitio al app pool `KanbanRedmineAppPool`.

### 7.3 Ajustar permisos NTFS

La identidad del app pool necesita al menos lectura y ejecucion sobre:

```text
C:\apps\KanbanRedmine\current
```

Si habilitas logs stdout temporalmente, tambien necesitara escritura sobre la carpeta de logs.

## 8. Configurar entorno de produccion en IIS

### 8.1 Variable de entorno principal

En `Configuration Editor` o en la configuracion del sitio, aseguran que exista:

```text
ASPNETCORE_ENVIRONMENT = Production
```

### 8.2 Verificar HTTPS

Esta aplicacion ejecuta `UseHttpsRedirection()` y activa HSTS fuera de desarrollo. Por eso debes publicar ya con HTTPS funcional.

Checklist:

1. El binding HTTPS existe.
2. El certificado no esta vencido.
3. El nombre del certificado coincide con el dominio.
4. El puerto 443 esta permitido en firewall.

## 9. Encender y validar la aplicacion

### 9.1 Reiniciar IIS o reciclar el sitio

Puedes usar:

```powershell
iisreset
```

O reciclar solo el app pool desde IIS.

### 9.2 Prueba funcional minima

Valida en este orden:

1. Abrir la URL productiva.
2. Confirmar que carga la pantalla de login.
3. Iniciar sesion con un usuario real de Redmine.
4. Verificar que el tablero Kanban carga issues.
5. Verificar que la vista Dashboard carga.
6. Mover una tarea entre columnas y confirmar que el cambio impacta en Redmine.
7. Crear una tarea nueva desde la interfaz y confirmar que aparece en Redmine.
8. Ejecutar logout y volver a login.

### 9.3 Diagnostico rapido si falla

Si el sitio no levanta:

1. Revisa `Windows Event Viewer`.
2. Revisa el log de IIS.
3. Verifica que el Hosting Bundle de .NET 10 este instalado.
4. Verifica que `ASPNETCORE_ENVIRONMENT=Production` este aplicado.
5. Verifica que `Redmine__BaseUrl` o `appsettings.Production.json` tengan la URL correcta.
6. Prueba conectividad desde el servidor hacia Redmine.

Prueba simple de conectividad desde el servidor:

```powershell
Invoke-WebRequest https://redmine.midominio.com -UseBasicParsing
```

## 10. Habilitar logs temporales si algo no arranca

Si necesitas diagnostico de arranque en IIS, puedes habilitar `stdoutLogEnabled` temporalmente en el `web.config` generado por publish.

Usalo solo mientras diagnosticas, porque crece rapido.

Ejemplo de valor esperado:

```xml
<aspNetCore processPath=".\KanbanRedmine.exe"
            stdoutLogEnabled="true"
            stdoutLogFile=".\logs\stdout"
            hostingModel="inprocess" />
```

Luego:

1. Crea la carpeta `logs`.
2. Da permisos de escritura a la identidad del app pool.
3. Reproduce el error.
4. Deshabilita nuevamente `stdoutLogEnabled`.

## 11. Checklist de salida a produccion

Antes de abrir a usuarios finales, confirma:

1. El servidor tiene .NET 10 Hosting Bundle.
2. El sitio responde por HTTPS sin advertencias.
3. `ASPNETCORE_ENVIRONMENT` esta en `Production`.
4. La URL de Redmine en produccion es correcta.
5. El login contra Redmine funciona.
6. Se listan issues reales.
7. Se pueden mover issues y guardar cambios.
8. Se pueden crear tareas nuevas.
9. El app pool tiene permisos suficientes.
10. Hay un plan de rollback preparado.

## 12. Rollback recomendado

Si una release falla:

1. Deten el sitio o recicla el app pool.
2. Restaura la carpeta `current` con la release anterior estable.
3. Vuelve a iniciar el sitio.
4. Repite la prueba minima de login y carga del tablero.

Por eso conviene mantener releases versionadas en:

```text
C:\apps\KanbanRedmine\releases
```

## 13. Comandos utiles

Publicar en Release:

```powershell
dotnet publish .\KanbanRedmine.csproj -c Release -o .\publish
```

Publicar en carpeta versionada:

```powershell
dotnet publish .\KanbanRedmine.csproj -c Release -o .\artifacts\2026-04-02-release
```

Probar localmente la carpeta publicada:

```powershell
cd .\publish
.\KanbanRedmine.exe
```

Reiniciar IIS:

```powershell
iisreset
```

## 14. Recomendaciones especificas para este proyecto

1. Cambia la URL actual de Redmine a una URL productiva real antes del despliegue.
2. Evita dejar `AllowedHosts` en `*` si ya conoces el dominio final.
3. Usa siempre `Release` para producir los artefactos.
4. No publiques mientras exista un ejecutable `Debug` bloqueado por otro proceso si vas a reutilizar esa salida.
5. Si el servidor no puede llegar a Redmine, la app cargara pero el login y la operacion del tablero fallaran.

## 15. Resultado esperado

Al terminar esta guia, debes tener:

1. Un sitio IIS sirviendo KanbanRedmine por HTTPS.
2. Un entorno `Production` activo.
3. Conexion funcional hacia Redmine.
4. Login, lectura y actualizacion de issues operando correctamente.