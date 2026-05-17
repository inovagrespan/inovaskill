/*
Contexto: templates
Objetivo: seeds iniciais para templates customers, products e sales
Data: 2026-05-16
Dependencia: database/sql/templates/001_templates_core.sql
*/

if not exists (select 1 from dbo.template where type = 'customers')
begin
    declare @system_user uniqueidentifier = '00000000-0000-0000-0000-000000000001';
    declare @customers_template_id uniqueidentifier = newid();
    declare @customers_version_id uniqueidentifier = newid();

    insert into dbo.template (id, name, type, status, created_by, created_at, updated_at)
    values (@customers_template_id, 'Clientes Padrao', 'customers', 'active', @system_user, sysutcdatetime(), sysutcdatetime());

    insert into dbo.template_version (id, template_id, version_number, is_active, config_json, created_by, created_at)
    values (
        @customers_version_id,
        @customers_template_id,
        1,
        1,
        N'{
          "fields": [
            {"internalName":"customer_name","dataType":"string","required":true,"aliases":["cliente","nome_cliente","nome do cliente"]},
            {"internalName":"email","dataType":"string","required":true,"aliases":["email","e-mail"]},
            {"internalName":"document_number","dataType":"string","required":false,"aliases":["cpf","cnpj","documento"]}
          ]
        }',
        @system_user,
        sysutcdatetime()
    );
end;
go

if not exists (select 1 from dbo.template where type = 'products')
begin
    declare @system_user uniqueidentifier = '00000000-0000-0000-0000-000000000001';
    declare @products_template_id uniqueidentifier = newid();
    declare @products_version_id uniqueidentifier = newid();

    insert into dbo.template (id, name, type, status, created_by, created_at, updated_at)
    values (@products_template_id, 'Produtos Padrao', 'products', 'active', @system_user, sysutcdatetime(), sysutcdatetime());

    insert into dbo.template_version (id, template_id, version_number, is_active, config_json, created_by, created_at)
    values (
        @products_version_id,
        @products_template_id,
        1,
        1,
        N'{
          "fields": [
            {"internalName":"sku","dataType":"string","required":true,"aliases":["sku","codigo","codigo produto"]},
            {"internalName":"product_name","dataType":"string","required":true,"aliases":["produto","nome produto"]},
            {"internalName":"price","dataType":"decimal","required":false,"aliases":["preco","valor","price"]}
          ]
        }',
        @system_user,
        sysutcdatetime()
    );
end;
go

if not exists (select 1 from dbo.template where type = 'sales')
begin
    declare @system_user uniqueidentifier = '00000000-0000-0000-0000-000000000001';
    declare @sales_template_id uniqueidentifier = newid();
    declare @sales_version_id uniqueidentifier = newid();

    insert into dbo.template (id, name, type, status, created_by, created_at, updated_at)
    values (@sales_template_id, 'Vendas Padrao', 'sales', 'active', @system_user, sysutcdatetime(), sysutcdatetime());

    insert into dbo.template_version (id, template_id, version_number, is_active, config_json, created_by, created_at)
    values (
        @sales_version_id,
        @sales_template_id,
        1,
        1,
        N'{
          "fields": [
            {"internalName":"customer_name","dataType":"string","required":true,"aliases":["cliente","nome cliente"]},
            {"internalName":"sale_date","dataType":"string","required":true,"aliases":["data venda","data"]},
            {"internalName":"amount","dataType":"decimal","required":true,"aliases":["valor","total","amount"]}
          ]
        }',
        @system_user,
        sysutcdatetime()
    );
end;
go
