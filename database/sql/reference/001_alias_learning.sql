/*
Contexto: reference
Objetivo: aprendizado incremental de aliases por tipo de template
Data: 2026-05-16
Dependencia: nenhuma
*/

if object_id('dbo.alias_learning', 'U') is null
begin
    create table dbo.alias_learning
    (
        id uniqueidentifier not null primary key,
        template_type varchar(50) not null,
        observed_header varchar(150) not null,
        matched_internal_name varchar(100) not null,
        accepted_count int not null constraint df_alias_learning_accepted_count default (0),
        rejected_count int not null constraint df_alias_learning_rejected_count default (0),
        last_seen_at datetime2 not null
    );
end;
go

if not exists (select 1 from sys.indexes where name = 'ix_alias_learning_type_header' and object_id = object_id('dbo.alias_learning'))
begin
    create unique index ix_alias_learning_type_header
        on dbo.alias_learning(template_type, observed_header, matched_internal_name);
end;
go

