# Dynamic Import Engine

Engine Python para importacao dinamica de planilhas CSV/Excel com estruturas variaveis.

O objetivo e mapear colunas externas para um modelo canonico interno usando:

- normalizacao forte de texto;
- aliases persistidos;
- similaridade Jaro-Winkler;
- confirmacao do usuario;
- aprendizado incremental.

## Executar exemplo

```powershell
python examples/run_import.py
```

## API C# integrada

O projeto `../../src/InovaSkillGrespan.Api` expoe uma API ASP.NET Core que recebe um
arquivo por upload e chama a engine Python para processar CSV/Excel.

Para usar SQL Server local, instale a dependencia Python:

```powershell
python -m pip install -r requirements.txt
```

A API configura a engine para usar o banco local `InovaSkillGrespan` via ODBC:

```text
Driver={ODBC Driver 18 for SQL Server};Server=localhost;Database=InovaSkillGrespan;Trusted_Connection=yes;TrustServerCertificate=yes;
```

A engine cria automaticamente a tabela `dbo.column_aliases` quando conecta.

Arquitetura da integracao C#:

```text
src/InovaSkillGrespan.Domain/          Regras de dominio e value objects
src/InovaSkillGrespan.Application/     Casos de uso e portas
src/InovaSkillGrespan.Infrastructure/  Adaptadores: storage local e engine Python
src/InovaSkillGrespan.Api/             Endpoints HTTP, DI e tratamento de erros
```

O fluxo do endpoint segue Clean Architecture/DDD:

```text
HTTP -> ProcessImportFileUseCase -> IUploadedFileStorage
                                  -> IImportEngine
                                  -> Python process_import.py
```

Subir a API:

```powershell
cd ..\..\src\InovaSkillGrespan.Api
dotnet run
```

Enviar um arquivo:

```powershell
curl.exe -F "file=@..\..\engines\EngineProcessData\examples\sample_customers.csv" http://localhost:5088/api/imports
```

O endpoint retorna JSON com `mappingPlan`, `records`, `validationIssues` e um
`summary`. Se precisar confirmar mapeamentos ambiguos, envie tambem o campo
multipart `confirmations` com um objeto JSON, por exemplo:

```powershell
curl.exe -F "file=@..\..\engines\EngineProcessData\examples\sample_customers.csv" -F "confirmations={\"Custumer name\":\"customer_name\"}" http://localhost:5088/api/imports
```

## Estrutura

```text
import_engine/
  domain/          Modelo canonico, schemas e resultado de importacao
  infrastructure/  Readers e repositorios persistentes
  services/        Normalizacao, similaridade, mapeamento e validacao
  pipeline/        Orquestracao do fluxo de importacao
examples/          CSV de exemplo e execucao ponta a ponta
tests/             Testes simples com unittest
```

## Ideia arquitetural

Planilhas diferentes nao ganham adapters proprios. A variacao fica em metadados:

- campos canonicos;
- aliases conhecidos por campo;
- scores de similaridade;
- decisoes confirmadas pelo usuario.

Com isso, novas planilhas melhoram a engine em vez de aumentarem o numero de classes
especificas para fornecedores/clientes.
