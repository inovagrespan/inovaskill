/*
Contexto: logs
Objetivo: registrar logs por etapa e erros de importacao
Data: 2026-05-16
Dependencia: database/sql/import_jobs/001_import_jobs_core.sql
*/

if object_id('dbo.import_job_log', 'U') is null
begin
    create table dbo.import_job_log
    (
        id bigint identity(1,1) not null primary key,
        job_id uniqueidentifier not null,
        stage varchar(50) not null,
        level varchar(20) not null,
        message nvarchar(max) not null,
        details_json nvarchar(max) null,
        created_at datetime2 not null,
        constraint fk_import_job_log_job
            foreign key (job_id) references dbo.import_job(id)
    );
end;
go

if object_id('dbo.import_job_error', 'U') is null
begin
    create table dbo.import_job_error
    (
        id bigint identity(1,1) not null primary key,
        job_id uniqueidentifier not null,
        row_number int null,
        column_name varchar(150) null,
        error_code varchar(80) not null,
        error_message nvarchar(max) not null,
        raw_value nvarchar(max) null,
        severity varchar(20) not null,
        created_at datetime2 not null,
        constraint fk_import_job_error_job
            foreign key (job_id) references dbo.import_job(id)
    );
end;
go

if not exists (select 1 from sys.indexes where name = 'ix_import_job_log_job_stage' and object_id = object_id('dbo.import_job_log'))
begin
    create index ix_import_job_log_job_stage
        on dbo.import_job_log(job_id, stage, created_at);
end;
go

if not exists (select 1 from sys.indexes where name = 'ix_import_job_error_job_row' and object_id = object_id('dbo.import_job_error'))
begin
    create index ix_import_job_error_job_row
        on dbo.import_job_error(job_id, row_number);
end;
go

