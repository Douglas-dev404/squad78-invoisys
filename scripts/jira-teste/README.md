# Jira de testes do InvoiSys

A InvoiSys não dá acesso ao Jira dela, então o grupo usa um **Jira Cloud gratuito próprio**
(plano Free da Atlassian, até 10 usuários) com dados de teste no mesmo formato. O InvoiSys
continua falando com a API real do Jira; só muda qual Jira.

## 1. Criar o site (uma vez, feito por uma pessoa do grupo)

1. Em <https://www.atlassian.com/software/jira/free>, crie a conta e um site, por exemplo
   `squad78-invoisys.atlassian.net`.
2. Gere um API token em <https://id.atlassian.com/manage-profile/security/api-tokens>.
   **Nunca** commite o token nem cole em issue, PR ou chat.
3. Convide o resto do grupo pelo próprio Jira, se quiserem ver os dados.

## 2. Popular com dados de teste

Cria o projeto `INV` (Kanban, gerenciado pela empresa), o tipo de subtarefa
**Release Note**, 3 Releases (fixVersions) e 19 histórias com vocabulário de DF-e:

| Release | Tamanho | O que testa |
|---|---|---|
| `2026.10.0` | 3 histórias, lançada | Release pequena; uma com subtarefa Release Note |
| `2026.11.0` | 6 histórias | Lista e tabela na descrição (ADF), histórias parecidas para o agrupamento, item só técnico |
| `2026.12.0` | 10 histórias | Release grande, NFS-e nacional em 3 histórias, infraestrutura sem impacto para o cliente |

Na raiz do repositório, com Node 22:

```bash
export Jira__BaseUrl=https://squad78-invoisys.atlassian.net
export Jira__Email=voce@exemplo.com
export Jira__ApiToken=cole-o-token-aqui
node scripts/jira-teste/popular-jira.mjs --simular
node scripts/jira-teste/popular-jira.mjs
```

No PowerShell, troque `export X=...` por `$env:X="..."`.

`--simular` só lê o Jira e mostra o que faria. Rodar de novo é seguro: projeto, tipo e
Releases que já existem são reaproveitados, e Release que já tem histórias é pulada.
`Jira__ProjetoChave` (padrão `INV`) escolhe outro projeto.

## 3. Ligar o InvoiSys

No `.env` (local) ou no painel do Render (deploy):

```
Jira__BaseUrl=https://squad78-invoisys.atlassian.net
Jira__Email=voce@exemplo.com
Jira__ApiToken=...
Jira__ProjetoChave=INV
```

A tela **Releases** passa a mostrar o quadro com as versões do projeto. Clicar numa
Release da coluna "No Jira" gera o comunicado (precisa também da chave da IA).

## Limitação

Os dados são inventados pelo grupo. O nome do tipo "Release Note" e o formato das
descrições aqui **não provam** que o Jira real da InvoiSys é igual (pendência da #82).
