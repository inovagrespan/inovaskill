# Import Engine Contract (v1)

Contrato oficial entre API ASP.NET Core e engine Python.

## Estrutura
- `v1-request.json`: payload enviado para a engine.
- `v1-response.json`: payload retornado pela engine.

## Regras
1. Toda alteracao de contrato deve gerar nova versao (`v2`, `v3`, ...).
2. `contractVersion` e obrigatorio em request/response.
3. Erros e warnings devem retornar codigo, mensagem, severidade e contexto.

