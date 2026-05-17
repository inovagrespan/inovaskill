# SQL por Contexto

Esta pasta centraliza todos os scripts SQL do projeto.

## Estrutura
- `templates/`: estruturas de templates, versoes, campos, aliases e regras.
- `import_jobs/`: jobs de importacao, status e historico de execucao.
- `logs/`: logs tecnicos e funcionais da pipeline.
- `reference/`: tabelas de apoio e seeds de referencia.

## Convencao
1. Criar scripts incrementais por contexto:
   - `001_*.sql`, `002_*.sql`, ...
2. Priorizar scripts idempotentes.
3. Evitar editar scripts antigos ja aplicados; criar novo script para evolucao.

