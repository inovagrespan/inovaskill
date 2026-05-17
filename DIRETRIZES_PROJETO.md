# Diretrizes do Projeto InovaSkill

## Objetivo
Este arquivo define as diretrizes obrigatorias para evolucao da plataforma de importacao configuravel.

## Regras Arquiteturais Atuais
1. Manter o padrao ja existente em camadas:
   - `Api` (HTTP)
   - `Application` (casos de uso)
   - `Domain` (regras de negocio)
   - `Infrastructure` (adaptadores externos)
   - `engines/EngineProcessData` (engine Python)
2. Nao criar adapters especificos por planilha. O comportamento deve ser dirigido por template configuravel.
3. Nao usar fila neste momento.
   - A API ASP.NET Core chama o processo Python diretamente.
   - Processamento continua sincrono com timeout e tratamento de erro.
4. Evolucoes devem seguir o design e convencoes ja presentes no repositorio.

## Regras para SQL
1. Todo SQL novo deve ser salvo em `database/sql`.
2. Scripts devem ser separados por contexto de negocio:
   - `database/sql/templates`
   - `database/sql/import_jobs`
   - `database/sql/logs`
   - `database/sql/reference`
3. Nome de arquivo padrao:
   - `NNN_descricao_curta.sql`
   - Exemplo: `001_templates_core.sql`
4. Scripts devem ser idempotentes quando possivel (`if not exists`, checagens de objeto).
5. Cada script deve conter cabecalho com:
   - contexto
   - objetivo
   - data
   - dependencia (se houver)

## Regras de Evolucao de Template
1. Template e entidade de negocio.
2. Versao de template e imutavel depois de publicada.
3. Job de importacao sempre referencia uma versao especifica de template.
4. Alteracao de regra/campo gera nova versao, nunca edicao destrutiva da versao publicada.

## Regras de Integracao C# -> Python
1. Contrato de entrada/saida deve ser JSON versionado.
2. Erros devem ser estruturados com codigo, mensagem, contexto e severidade.
3. Logs por etapa de pipeline devem ser persistidos com `job_id` e `correlation_id`.

