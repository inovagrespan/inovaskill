/*
Contexto: import_jobs
Objetivo: controlar execucao de importacoes, status e resultados por linha
Data: 2026-05-16
Dependencia: database/sql/templates/001_templates_core.sql
*/

if object_id('dbo.import_job', 'U') is null
begin
    create table dbo.import_job
    (
        id uniqueidentifier not null primary key,
        template_version_id uniqueidentifier not null,
        file_name varchar(255) not null,
        file_path varchar(500) not null,
        requested_by uniqueidentifier not null,
        status varchar(30) not null,
        started_at datetime2 null,
        finished_at datetime2 null,
        summary_json nvarchar(max) null,
        correlation_id varchar(120) null,
        created_at datetime2 not null,
        updated_at datetime2 not null,
        constraint fk_import_job_template_version
            foreign key (template_version_id) references dbo.template_version(id)
    );
end;
go

if object_id('dbo.import_job_row_result', 'U') is null
begin
    create table dbo.import_job_row_result
    (
        id bigint identity(1,1) not null primary key,
        job_id uniqueidentifier not null,
        row_number int not null,
        status varchar(20) not null,
        normalized_json nvarchar(max) null,
        created_at datetime2 not null,
        constraint fk_import_job_row_result_job
            foreign key (job_id) references dbo.import_job(id)
    );
end;
go

if not exists (select 1 from sys.indexes where name = 'ix_import_job_status_created_at' and object_id = object_id('dbo.import_job'))
begin
    create index ix_import_job_status_created_at
        on dbo.import_job(status, created_at desc);
end;
go

if not exists (select 1 from sys.indexes where name = 'ix_import_job_template_version_id' and object_id = object_id('dbo.import_job'))
begin
    create index ix_import_job_template_version_id
        on dbo.import_job(template_version_id);
end;
go

if not exists (select 1 from sys.indexes where name = 'ix_import_job_row_result_job_row' and object_id = object_id('dbo.import_job_row_result'))
begin
    create index ix_import_job_row_result_job_row
        on dbo.import_job_row_result(job_id, row_number);
end;
go

