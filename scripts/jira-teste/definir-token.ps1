# Grava um segredo (token do Jira, chave da IA) no .env da raiz sem mostrar na tela.
# Uso (na raiz do repositório):
#   powershell -ExecutionPolicy Bypass -File .\scripts\jira-teste\definir-token.ps1
#   powershell -ExecutionPolicy Bypass -File .\scripts\jira-teste\definir-token.ps1 -Variavel OpenRouter__ApiKey
param([string]$Variavel = 'Jira__ApiToken')

$arquivo = Join-Path (Resolve-Path "$PSScriptRoot\..\..") '.env'
if (-not (Test-Path $arquivo)) {
    Write-Host "Não achei o .env em $arquivo. Copie o .env.example para .env primeiro." -ForegroundColor Red
    exit 1
}

$seguro = Read-Host "Cole o valor de $Variavel e aperte Enter (ele não aparece na tela)" -AsSecureString
$ponteiro = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($seguro)
try {
    $token = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ponteiro).Trim()
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ponteiro)
}

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host 'Nada colado. Nada foi alterado.' -ForegroundColor Yellow
    exit 1
}

$linhas = [IO.File]::ReadAllLines($arquivo)
if ($linhas -match "^$Variavel=") {
    $linhas = $linhas | ForEach-Object { if ($_ -match "^$Variavel=") { "$Variavel=$token" } else { $_ } }
} else {
    $linhas += "$Variavel=$token"
}
[IO.File]::WriteAllLines($arquivo, [string[]]$linhas)

Write-Host "$Variavel gravado no .env ($($token.Length) caracteres). Pode voltar ao chat e avisar." -ForegroundColor Green
