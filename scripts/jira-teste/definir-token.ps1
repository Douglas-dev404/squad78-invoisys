# Grava o API token do Jira no .env da raiz sem mostrar o token na tela.
# Uso (na raiz do repositório): & .\scripts\jira-teste\definir-token.ps1

$arquivo = Join-Path (Resolve-Path "$PSScriptRoot\..\..") '.env'
if (-not (Test-Path $arquivo)) {
    Write-Host "Não achei o .env em $arquivo. Copie o .env.example para .env primeiro." -ForegroundColor Red
    exit 1
}

$seguro = Read-Host 'Cole o token do Jira e aperte Enter (ele não aparece na tela)' -AsSecureString
$ponteiro = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($seguro)
try {
    $token = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ponteiro).Trim()
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ponteiro)
}

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host 'Nenhum token colado. Nada foi alterado.' -ForegroundColor Yellow
    exit 1
}

$linhas = [IO.File]::ReadAllLines($arquivo)
if ($linhas -match '^Jira__ApiToken=') {
    $linhas = $linhas | ForEach-Object { if ($_ -match '^Jira__ApiToken=') { "Jira__ApiToken=$token" } else { $_ } }
} else {
    $linhas += "Jira__ApiToken=$token"
}
[IO.File]::WriteAllLines($arquivo, [string[]]$linhas)

Write-Host "Token gravado no .env ($($token.Length) caracteres). Pode voltar ao chat e avisar." -ForegroundColor Green
