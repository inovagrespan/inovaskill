/*
Contexto: templates
Objetivo: estrutura inicial de templates dinamicos e versionamento
Data: 2026-05-16
Dependencia: nenhuma
*/

if object_id('dbo.template', 'U') is null
begin
    create table dbo.template
    (
        id uniqueidentifier not null primary key,
        name varchar(120) not null,
        type varchar(50) not null,
        status varchar(20) not null,
        created_by uniqueidentifier not null,
        created_at datetime2 not null,
        updated_at datetime2 not null
    );
end;
go

if object_id('dbo.template_version', 'U') is null
begin
    create table dbo.template_version
    (
        id uniqueidentifier not null primary key,
        template_id uniqueidentifier not null,
        version_number int not null,
        is_active bit not null constraint df_template_version_is_active default (0),
        config_json nvarchar(max) not null,
        created_by uniqueidentifier not null,
        created_at datetime2 not null,
        constraint fk_template_version_template
            foreign key (template_id) references dbo.template(id),
        constraint uq_template_version_template_number
            unique (template_id, version_number)
    );
end;
go

if object_id('dbo.template_field', 'U') is null
begin
    create table dbo.template_field
    (
        id uniqueidentifier not null primary key,
        template_version_id uniqueidentifier not null,
        internal_name varchar(100) not null,
        data_type varchar(30) not null,
        required bit not null constraint df_template_field_required default (0),
        nullable bit not null constraint df_template_field_nullable default (1),
        position int not null,
        constraint fk_template_field_template_version
            foreign key (template_version_id) references dbo.template_version(id)
    );
end;
go

if object_id('dbo.template_field_alias', 'U') is null
begin
    create table dbo.template_field_alias
    (
        id uniqueidentifier not null primary key,
        template_field_id uniqueidentifier not null,
        alias_text varchar(150) not null,
        confidence_boost decimal(5,2) null,
        constraint fk_template_field_alias_template_field
            foreign key (template_field_id) references dbo.template_field(id)
    );
end;
go

if object_id('dbo.template_rule', 'U') is null
begin
    create table dbo.template_rule
    (
        id uniqueidentifier not null primary key,
        template_version_id uniqueidentifier not null,
        field_internal_name varchar(100) null,
        rule_type varchar(50) not null,
        rule_config nvarchar(max) not null,
        severity varchar(20) not null,
        constraint fk_template_rule_template_version
            foreign key (template_version_id) references dbo.template_version(id)
    );
end;
go

if not exists (select 1 from sys.indexes where name = 'ix_template_type_status' and object_id = object_id('dbo.template'))
begin
    create index ix_template_type_status on dbo.template(type, status);
end;
go

if not exists (select 1 from sys.indexes where name = 'ix_template_field_version_position' and object_id = object_id('dbo.template_field'))
begin
    create index ix_template_field_version_position on dbo.template_field(template_version_id, position);
end;
go

