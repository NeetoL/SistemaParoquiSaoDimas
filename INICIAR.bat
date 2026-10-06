@echo off
setlocal
chcp 65001 >nul
title Sao Dimas - Iniciar sistema
set "SAODIMAS_INICIAR=%~f0"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; try { $texto=[IO.File]::ReadAllText($env:SAODIMAS_INICIAR); $marcador='#===INICIAR_POWERSHELL==='; $codigo=$texto.Substring($texto.LastIndexOf($marcador)+$marcador.Length); & ([scriptblock]::Create($codigo)) } catch { Write-Host ('ERRO: '+$_.Exception.Message) -ForegroundColor Red; exit 1 }"
if errorlevel 1 (
  echo.
  echo Nao foi possivel iniciar. Confira a mensagem acima.
  pause
  exit /b 1
)
exit /b 0
#===INICIAR_POWERSHELL===
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$raiz = Split-Path -Parent $env:SAODIMAS_INICIAR
$aplicacao = Join-Path $raiz 'SISTEMA'
$executavel = Join-Path $aplicacao 'SaoDimas.MVC.exe'
$dados = Join-Path $raiz 'DADOS'
$logs = Join-Path $raiz 'LOGS'
$porta = 5080
if ($env:SAODIMAS_PORTA) {
    $porta = [int]$env:SAODIMAS_PORTA
    if ($porta -lt 1024 -or $porta -gt 65535) { throw 'A porta deve estar entre 1024 e 65535.' }
}
$url = 'http://127.0.0.1:' + $porta
if (![Environment]::Is64BitOperatingSystem) { throw 'Este sistema requer Windows de 64 bits.' }

function Baixar($urlOrigem, $destino) {
    Invoke-WebRequest -UseBasicParsing -Uri $urlOrigem -OutFile $destino
}

$git = Get-Command git -ErrorAction SilentlyContinue
$repositorio = Test-Path -LiteralPath (Join-Path $raiz '.git')
if ($repositorio -and $git) {
    $alteracoes = & $git.Source -C $raiz status --porcelain
    if ($LASTEXITCODE -ne 0) { throw 'Nao foi possivel verificar o repositorio.' }
    if ($alteracoes) {
        Write-Host 'Alteracoes locais encontradas: usando o codigo local, sem fazer pull.' -ForegroundColor Yellow
    } else {
        Write-Host 'Buscando atualizacoes...'
        & $git.Source -C $raiz pull --ff-only
        if ($LASTEXITCODE -ne 0) { Write-Host 'Nao foi possivel atualizar pelo Git. Usando o codigo local; confira a conexao e a mensagem acima.' -ForegroundColor Yellow }
    }
}

function ObterVersaoCodigo {
    if ($repositorio -and $git) {
        $arquivos = @(& $git.Source -C $raiz ls-files)
        if ($LASTEXITCODE -ne 0) { throw 'Nao foi possivel identificar os arquivos do sistema.' }
    } else {
        $arquivos = @(Get-ChildItem -LiteralPath $raiz -Directory -Filter 'SaoDimas.*' | ForEach-Object {
            Get-ChildItem -LiteralPath $_.FullName -File -Recurse | Where-Object {
                $_.FullName -notmatch '\\(bin|obj|node_modules|App_Data)\\' -and $_.FullName -notmatch '\\wwwroot\\(img|css|lib)\\'
            } | ForEach-Object { $_.FullName.Substring($raiz.Length + 1) }
        })
    }
    $conteudo = foreach ($arquivo in ($arquivos | Sort-Object)) {
        if (($arquivo -notmatch '^SaoDimas\.' -and $arquivo -notmatch '^(Directory\.Build\.(props|targets)|global\.json|[Nn]u[Gg]et\.config)$') -or $arquivo -match '^SaoDimas\.Tests[\\/]' -or $arquivo -match '[\\/]App_Data[\\/]') { continue }
        $caminho = Join-Path $raiz $arquivo
        if (Test-Path -LiteralPath $caminho -PathType Leaf) { $arquivo + ':' + (Get-FileHash -LiteralPath $caminho -Algorithm SHA256).Hash }
    }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($conteudo -join "`n"))))).Replace('-', '') }
    finally { $sha.Dispose() }
}
$projeto = Join-Path $raiz 'SaoDimas.MVC\SaoDimas.MVC.csproj'
$versaoCodigo = if (Test-Path -LiteralPath $projeto) { ObterVersaoCodigo } else { $null }
$marcadorVersao = Join-Path $aplicacao 'versao-codigo.txt'
$versaoInstalada = if (Test-Path -LiteralPath $marcadorVersao) { (Get-Content -LiteralPath $marcadorVersao -Raw).Trim() } else { $null }
if (!(Test-Path -LiteralPath $executavel) -or ($versaoCodigo -and $versaoCodigo -ne $versaoInstalada)) {
    $projeto = Join-Path $raiz 'SaoDimas.MVC\SaoDimas.MVC.csproj'
    if (!(Test-Path -LiteralPath $projeto)) {
        throw 'Copie a pasta completa da instalacao, incluindo SISTEMA e INICIAR.bat. O BAT sozinho nao contem o aplicativo.'
    }
    Write-Host 'Preparando a versao atual e as dependencias. Pode ser necessario acesso a internet.'
    $ferramentas = Join-Path $raiz '.ferramentas'
    $dotnetPasta = Join-Path $ferramentas 'dotnet'
    $nodePasta = Join-Path $ferramentas 'node'
    New-Item -ItemType Directory -Path $ferramentas -Force | Out-Null
    $dotnetExe = Join-Path $dotnetPasta 'dotnet.exe'
    if (!(Test-Path -LiteralPath $dotnetExe)) {
        Write-Host 'Instalando .NET 10 na pasta do sistema...'
        $instalador = Join-Path $ferramentas 'dotnet-install.ps1'
        Baixar 'https://dot.net/v1/dotnet-install.ps1' $instalador
        & $instalador -Channel '10.0' -InstallDir $dotnetPasta -Architecture x64 -NoPath
        if (!(Test-Path -LiteralPath $dotnetExe)) { throw 'A instalacao do .NET nao foi concluida.' }
    }
    if (!(Test-Path -LiteralPath (Join-Path $nodePasta 'node.exe'))) {
        Write-Host 'Instalando Node.js LTS na pasta do sistema...'
        $versoes = Invoke-RestMethod 'https://nodejs.org/dist/index.json'
        $versao = ($versoes | Where-Object { $_.version -match '^v22\.' -and $_.lts } | Select-Object -First 1).version
        if (!$versao -or $versao -notmatch '^v22\.\d+\.\d+$') { throw 'Nao foi possivel identificar a versao do Node.js.' }
        $nome = 'node-' + $versao + '-win-x64'
        $zip = Join-Path $ferramentas ($nome + '.zip')
        $baseDownload = 'https://nodejs.org/dist/' + $versao
        Baixar ($baseDownload + '/' + $nome + '.zip') $zip
        $somas = (Invoke-WebRequest -UseBasicParsing ($baseDownload + '/SHASUMS256.txt')).Content
        $esperado = (($somas -split "`n" | Where-Object { $_.Trim().EndsWith($nome + '.zip') }) -split '\s+')[0]
        $obtido = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
        if (!$esperado -or $obtido -ne $esperado) { throw 'A verificacao do download do Node.js falhou.' }
        Expand-Archive -LiteralPath $zip -DestinationPath $ferramentas -Force
        $extraido = Join-Path $ferramentas $nome
        New-Item -ItemType Directory -Path $nodePasta -Force | Out-Null
        Get-ChildItem -LiteralPath $extraido -Force | Copy-Item -Destination $nodePasta -Recurse -Force
    }
    $env:DOTNET_ROOT = $dotnetPasta
    $env:PATH = $dotnetPasta + ';' + $nodePasta + ';' + $env:PATH
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    Push-Location $raiz
    try {
        Write-Host 'Preparando o aplicativo. Aguarde...'
        $novaPublicacao = Join-Path $raiz ('SISTEMA_NOVO-' + [Guid]::NewGuid().ToString('N'))
        & $dotnetExe publish $projeto -c Release -r win-x64 --self-contained true -o $novaPublicacao --artifacts-path (Join-Path $ferramentas 'build')
        if ($LASTEXITCODE -ne 0) { throw 'A preparacao falhou. A versao anterior foi preservada. Verifique a conexao e tente novamente.' }
        if (!(Test-Path -LiteralPath (Join-Path $novaPublicacao 'SaoDimas.MVC.exe'))) { throw 'A nova versao esta incompleta.' }
        $versaoCodigo | Set-Content -LiteralPath (Join-Path $novaPublicacao 'versao-codigo.txt') -Encoding ASCII
        Get-Process -Name 'SaoDimas.MVC' -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $executavel } | ForEach-Object {
            Stop-Process -Id $_.Id -ErrorAction Stop
            if (!$_.WaitForExit(10000)) { throw 'O sistema anterior nao encerrou. Tente novamente.' }
        }
        $pastaAnterior = Join-Path $raiz ('SISTEMA_ANTERIOR-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
        if (Test-Path -LiteralPath $aplicacao) { Move-Item -LiteralPath $aplicacao -Destination $pastaAnterior }
        try { Move-Item -LiteralPath $novaPublicacao -Destination $aplicacao }
        catch {
            if (Test-Path -LiteralPath $pastaAnterior) { Move-Item -LiteralPath $pastaAnterior -Destination $aplicacao }
            throw
        }
        Write-Host 'Aplicacao atualizada. Dados preservados.' -ForegroundColor Green
    } finally { Pop-Location }
}

New-Item -ItemType Directory -Path $dados, $logs -Force | Out-Null
# Repara imagens de documentos numa publicacao existente, sem alterar os originais.
$recursosReparados = $false
foreach ($nomeImagem in @('dizimo-expressao-fe.jpeg', 'pix-qrcode.png')) {
    $fonteImagem = Join-Path $raiz ('SaoDimas.MVC\wwwroot\src\marca\' + $nomeImagem)
    $destinoImagem = Join-Path $aplicacao ('wwwroot\img\' + $nomeImagem)
    if (Test-Path -LiteralPath $fonteImagem) {
        if (!(Test-Path -LiteralPath $destinoImagem) -or (Get-FileHash -LiteralPath $fonteImagem).Hash -ne (Get-FileHash -LiteralPath $destinoImagem).Hash) {
            New-Item -ItemType Directory -Path (Split-Path -Parent $destinoImagem) -Force | Out-Null
            Copy-Item -LiteralPath $fonteImagem -Destination $destinoImagem -Force
            $recursosReparados = $true
        }
    }
    if (!(Test-Path -LiteralPath $destinoImagem)) { throw ('Falta a imagem ' + $nomeImagem + '. Atualize a pasta completa do sistema.') }
}
# Lazy<Image> conserva falhas em memoria: reiniciar apenas o processo deste pacote.
if ($recursosReparados -and (Test-Path -LiteralPath (Join-Path $logs 'processo.txt'))) {
    $idAnterior = [int](Get-Content -LiteralPath (Join-Path $logs 'processo.txt'))
    $processoAnterior = Get-Process -Id $idAnterior -ErrorAction SilentlyContinue
    if ($processoAnterior -and $processoAnterior.Path -eq $executavel) {
        Stop-Process -Id $idAnterior -ErrorAction Stop
        $processoAnterior.WaitForExit(5000) | Out-Null
        Write-Host 'Imagens reparadas. Reiniciando o sistema...'
    }
}
$arquivoDados = Join-Path $dados 'sistema.json'
$dadosOriginais = Join-Path $raiz 'SaoDimas.MVC\App_Data\sistema.json'
if (!(Test-Path -LiteralPath $arquivoDados) -and (Test-Path -LiteralPath $dadosOriginais)) {
    Copy-Item -LiteralPath $dadosOriginais -Destination $arquivoDados
}
# Inclui os cadastros iniciais ausentes e preserva os registros existentes.
$cadastrosIniciais = Join-Path $raiz 'dados-iniciais\cadastros.json'
if (Test-Path -LiteralPath $cadastrosIniciais) {
    $bloqueio = [IO.File]::Open($arquivoDados + '.lock', [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $origem = Get-Content -LiteralPath $cadastrosIniciais -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($origem.versao -ne 1 -or !$origem.comunidades -or !$origem.dizimistas) { throw 'Dados iniciais invalidos.' }
        if (!(Test-Path -LiteralPath $arquivoDados)) {
            Copy-Item -LiteralPath $cadastrosIniciais -Destination $arquivoDados
            Write-Host ($origem.dizimistas.Count.ToString() + ' cadastros iniciais incluidos.')
        } else {
            $atual = Get-Content -LiteralPath $arquivoDados -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($atual.versao -ne 1) { throw 'Formato dos dados atuais nao reconhecido.' }
            $adicionados = 0
            foreach ($comunidade in $origem.comunidades) {
                if (!($atual.comunidades | Where-Object { $_.id -eq $comunidade.id })) { $atual.comunidades = @($atual.comunidades) + @($comunidade) }
            }
            foreach ($pessoa in $origem.dizimistas) {
                $existe = @($atual.dizimistas | Where-Object {
                    $_.nome -eq $pessoa.nome -and $_.comunidadeId -eq $pessoa.comunidadeId -and
                    (($_.id -eq $pessoa.id) -or ($_.telefone -eq $pessoa.telefone -and $_.endereco -eq $pessoa.endereco))
                })
                if ($existe.Count -gt 0) { continue }
                if ($atual.dizimistas | Where-Object { $_.id -eq $pessoa.id }) {
                    $maiorId = ($atual.dizimistas | Measure-Object -Property id -Maximum).Maximum
                    $pessoa.id = [int][Math]::Max($atual.sequenciaDizimista, $maiorId) + 1
                }
                $atual.dizimistas = @($atual.dizimistas) + @($pessoa)
                $atual.sequenciaDizimista = [int][Math]::Max($atual.sequenciaDizimista, $pessoa.id)
                $adicionados++
            }
            if ($adicionados -gt 0) {
                $pastaBackups = Join-Path $dados 'backups'
                New-Item -ItemType Directory -Path $pastaBackups -Force | Out-Null
                $backup = Join-Path $pastaBackups ('antes-cadastros-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '.json')
                $temporario = $arquivoDados + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
                [IO.File]::WriteAllText($temporario, ($atual | ConvertTo-Json -Depth 100), [Text.UTF8Encoding]::new($false))
                try { [IO.File]::Replace($temporario, $arquivoDados, $backup) }
                finally { if (Test-Path -LiteralPath $temporario) { Remove-Item -LiteralPath $temporario } }
                Write-Host ($adicionados.ToString() + ' dizimistas incluidos; cadastros anteriores preservados.')
            }
        }
    } finally { $bloqueio.Dispose() }
}
# Uma nova execucao nunca substitui cadastros ou backups existentes em DADOS.
$cliente = New-Object Net.Sockets.TcpClient
try { $cliente.Connect('127.0.0.1', $porta); $ocupada = $true } catch { $ocupada = $false } finally { $cliente.Dispose() }
if ($ocupada) {
    try {
        $pagina = Invoke-WebRequest -UseBasicParsing ($url + '/Conta/Login') -TimeoutSec 5
        if ($pagina.Content -notmatch 'titulo-login' -or $pagina.Content -notmatch 'Sao Dimas|São Dimas') { throw 'Outro aplicativo usa a porta.' }
    } catch { throw ('A porta ' + $porta + ' esta em uso. Feche o outro aplicativo ou configure SAODIMAS_PORTA.') }
    Write-Host ('O sistema ja esta em funcionamento: ' + $url)
    if (!$env:SAODIMAS_SEM_NAVEGADOR) { Start-Process $url }
    return
}

# Acesso somente neste computador. Desenvolvimento permite cookie no HTTP local;
# nao publicar essa configuracao na internet ou na rede.
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = $url
$env:Persistencia__Diretorio = $dados
$processo = Start-Process -FilePath $executavel -WorkingDirectory $aplicacao -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logs 'sistema.log') -RedirectStandardError (Join-Path $logs 'erros.log')
$pronto = $false
for ($tentativa = 0; $tentativa -lt 60; $tentativa++) {
    $processo.Refresh()
    if ($processo.HasExited) { throw ('O sistema parou ao iniciar. Consulte ' + (Join-Path $logs 'erros.log')) }
    try {
        $resposta = Invoke-WebRequest -UseBasicParsing ($url + '/Conta/Login') -TimeoutSec 1
        if ($resposta.StatusCode -eq 200 -and $resposta.Content -match 'titulo-login') { $pronto = $true; break }
    } catch { Start-Sleep -Milliseconds 500 }
}
if (!$pronto) {
    if (!$processo.HasExited) { $processo.Kill() }
    throw 'O sistema nao respondeu no prazo esperado. Confira a pasta LOGS.'
}
$processo.Id | Set-Content -LiteralPath (Join-Path $logs 'processo.txt')
Write-Host ('Sistema iniciado: ' + $url) -ForegroundColor Green
Write-Host ('Cadastros e backups: ' + $dados)
if (!$env:SAODIMAS_SEM_NAVEGADOR) { Start-Process $url }
