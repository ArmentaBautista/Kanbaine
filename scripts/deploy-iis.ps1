param(
    [Parameter(Mandatory = $false)]
    [string]$PublishPath = "C:\apps\KanbanRedmine\incoming\publish",

    [Parameter(Mandatory = $false)]
    [string]$DeployRoot = "C:\apps\KanbanRedmine",

    [Parameter(Mandatory = $false)]
    [string]$SiteName = "KanbanRedmine",

    [Parameter(Mandatory = $false)]
    [string]$AppPoolName = "KanbanRedmineAppPool",

    [Parameter(Mandatory = $false)]
    [string]$HostName = "",

    [Parameter(Mandatory = $false)]
    [int]$HttpsPort = 443,

    [Parameter(Mandatory = $false)]
    [string]$CertificateThumbprint = "",

    [Parameter(Mandatory = $false)]
    [string]$RedmineBaseUrl = "",

    [Parameter(Mandatory = $false)]
    [string]$RedmineApiPath = "/",

    [Parameter(Mandatory = $false)]
    [int]$RedmineTimeoutSeconds = 30,

    [Parameter(Mandatory = $false)]
    [switch]$SkipIisSetup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "[DEPLOY] $Message" -ForegroundColor Cyan
}

function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path -Path $Path)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Set-AppPoolEnvVar {
    param(
        [string]$PoolName,
        [string]$Name,
        [string]$Value
    )

    $appcmd = Join-Path $env:SystemRoot "System32\inetsrv\appcmd.exe"

    & $appcmd set config -section:system.applicationHost/applicationPools "/-[name='$PoolName'].environmentVariables.[name='$Name']" /commit:apphost | Out-Null
    & $appcmd set config -section:system.applicationHost/applicationPools "/+[name='$PoolName'].environmentVariables.[name='$Name',value='$Value']" /commit:apphost | Out-Null
}

Write-Step "Validando carpeta de publicacion"
if (-not (Test-Path -Path $PublishPath)) {
    throw "No existe la carpeta PublishPath: $PublishPath"
}

$requiredFiles = @("KanbanRedmine.dll", "KanbanRedmine.exe", "web.config")
foreach ($file in $requiredFiles) {
    $candidate = Join-Path $PublishPath $file
    if (-not (Test-Path -Path $candidate)) {
        throw "Falta el archivo requerido en publish: $candidate"
    }
}

$currentPath = Join-Path $DeployRoot "current"
$releasesPath = Join-Path $DeployRoot "releases"
$logsPath = Join-Path $DeployRoot "logs"

Write-Step "Creando estructura de directorios"
Ensure-Directory -Path $DeployRoot
Ensure-Directory -Path $currentPath
Ensure-Directory -Path $releasesPath
Ensure-Directory -Path $logsPath

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$releasePath = Join-Path $releasesPath $timestamp
Ensure-Directory -Path $releasePath

Write-Step "Copiando artefactos a la release $timestamp"
Copy-Item -Path (Join-Path $PublishPath "*") -Destination $releasePath -Recurse -Force

if (-not $SkipIisSetup) {
    Write-Step "Configurando IIS"
    Import-Module WebAdministration

    if (-not (Test-Path -Path "IIS:\AppPools\$AppPoolName")) {
        New-WebAppPool -Name $AppPoolName | Out-Null
    }

    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name managedPipelineMode -Value "Integrated"

    if (-not (Test-Path -Path "IIS:\Sites\$SiteName")) {
        New-Website -Name $SiteName -Port 80 -PhysicalPath $currentPath -ApplicationPool $AppPoolName | Out-Null
    }

    Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name physicalPath -Value $currentPath
    Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name applicationPool -Value $AppPoolName

    Set-AppPoolEnvVar -PoolName $AppPoolName -Name "ASPNETCORE_ENVIRONMENT" -Value "Production"

    if (-not [string]::IsNullOrWhiteSpace($RedmineBaseUrl)) {
        Set-AppPoolEnvVar -PoolName $AppPoolName -Name "Redmine__BaseUrl" -Value $RedmineBaseUrl
        Set-AppPoolEnvVar -PoolName $AppPoolName -Name "Redmine__ApiPath" -Value $RedmineApiPath
        Set-AppPoolEnvVar -PoolName $AppPoolName -Name "Redmine__TimeoutSeconds" -Value $RedmineTimeoutSeconds
    }

    if ((-not [string]::IsNullOrWhiteSpace($HostName)) -and (-not [string]::IsNullOrWhiteSpace($CertificateThumbprint))) {
        $httpsBindingInformation = "*:{0}:{1}" -f $HttpsPort, $HostName
        $existingHttps = Get-WebBinding -Name $SiteName -Protocol "https" -ErrorAction SilentlyContinue |
            Where-Object { $_.bindingInformation -eq $httpsBindingInformation }

        if (-not $existingHttps) {
            New-WebBinding -Name $SiteName -Protocol "https" -Port $HttpsPort -HostHeader $HostName | Out-Null
        }

        $cert = Get-ChildItem -Path "Cert:\LocalMachine\My" |
            Where-Object { $_.Thumbprint -eq $CertificateThumbprint }

        if (-not $cert) {
            throw "No se encontro el certificado con thumbprint $CertificateThumbprint en Cert:\LocalMachine\My"
        }

        $sslBindingPath = "IIS:\SslBindings\0.0.0.0!$HttpsPort!$HostName"
        if (Test-Path -Path $sslBindingPath) {
            Remove-Item -Path $sslBindingPath -Force
        }

        New-Item -Path $sslBindingPath -Thumbprint $CertificateThumbprint -SSLFlags 1 | Out-Null
    }
}

if (-not $SkipIisSetup) {
    try {
        Import-Module WebAdministration -ErrorAction SilentlyContinue
        if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
            Write-Step "Deteniendo sitio para actualizar current"
            Stop-Website -Name $SiteName
        }
    } catch {
        Write-Warning "No se pudo detener el sitio '$SiteName'. Continuando con despliegue de archivos. Detalle: $($_.Exception.Message)"
    }
}

$backupCurrentPath = Join-Path $releasesPath ("current-backup-" + $timestamp)
if (Test-Path -Path (Join-Path $currentPath "KanbanRedmine.dll")) {
    Write-Step "Respaldando version actual en $backupCurrentPath"
    Ensure-Directory -Path $backupCurrentPath
    Copy-Item -Path (Join-Path $currentPath "*") -Destination $backupCurrentPath -Recurse -Force
}

Write-Step "Limpiando carpeta current"
Get-ChildItem -Path $currentPath -Force | Remove-Item -Recurse -Force

Write-Step "Activando release $timestamp"
Copy-Item -Path (Join-Path $releasePath "*") -Destination $currentPath -Recurse -Force

if (-not $SkipIisSetup) {
    try {
        Import-Module WebAdministration -ErrorAction SilentlyContinue
        if (Get-Website -Name $SiteName -ErrorAction SilentlyContinue) {
            Start-Website -Name $SiteName
        }

        if (Test-Path -Path "IIS:\AppPools\$AppPoolName") {
            Start-WebAppPool -Name $AppPoolName | Out-Null
        }
    } catch {
        Write-Warning "No se pudo iniciar el sitio o app pool automaticamente. Revisar IIS manualmente. Detalle: $($_.Exception.Message)"
    }
}

Write-Step "Despliegue finalizado"
Write-Host "Release activa: $releasePath"
Write-Host "Ruta en ejecucion: $currentPath"
