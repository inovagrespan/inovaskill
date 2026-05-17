/*
Contexto: reference
Objetivo: criar tabela de destino products para importacao
Data: 2026-05-16
Dependencia: nenhuma
*/

if object_id('dbo.products', 'U') is null
begin
    create table dbo.products
    (
        id uniqueidentifier not null
            constraint pk_products primary key
            constraint df_products_id default newid(),
        sku varchar(80) not null,
        product_name varchar(200) not null,
        category varchar(120) null,
        brand varchar(120) null,
        price decimal(18,2) null,
        active bit not null constraint df_products_active default (1),
        source_system varchar(120) null,
        created_at datetime2 not null constraint df_products_created_at default sysutcdatetime(),
        updated_at datetime2 not null constraint df_products_updated_at default sysutcdatetime()
    );
end;
go

if not exists (select 1 from sys.indexes where name = 'ux_products_sku' and object_id = object_id('dbo.products'))
begin
    create unique index ux_products_sku on dbo.products(sku);
end;
go

