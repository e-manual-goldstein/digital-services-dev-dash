CREATE EVENT SESSION [{{SESSION_NAME}}] ON SERVER
ADD EVENT sqlserver.rpc_completed(
    ACTION(
        sqlserver.client_app_name,
        sqlserver.database_id,
        sqlserver.database_name,
        sqlserver.query_hash,
        sqlserver.query_plan_hash,
        sqlserver.session_id,
        sqlserver.sql_text,
        sqlserver.username
    )
    WHERE (
        [package0].[greater_than_uint64]([sqlserver].[database_id], (4))
        AND [package0].[equal_boolean]([sqlserver].[is_system], (0))
    )
),
ADD EVENT sqlserver.sp_statement_completed(
    ACTION(
        sqlserver.client_app_name,
        sqlserver.database_id,
        sqlserver.database_name,
        sqlserver.query_hash,
        sqlserver.session_id,
        sqlserver.sql_text,
        sqlserver.username
    )
    WHERE (
        [package0].[greater_than_uint64]([sqlserver].[database_id], (4))
        AND [package0].[equal_boolean]([sqlserver].[is_system], (0))
    )
),
ADD EVENT sqlserver.sql_batch_completed(
    ACTION(
        sqlserver.client_app_name,
        sqlserver.database_id,
        sqlserver.database_name,
        sqlserver.query_hash,
        sqlserver.query_plan_hash,
        sqlserver.session_id,
        sqlserver.sql_text,
        sqlserver.username
    )
    WHERE (
        [package0].[greater_than_uint64]([sqlserver].[database_id], (4))
        AND [package0].[equal_boolean]([sqlserver].[is_system], (0))
    )
),
ADD EVENT sqlserver.sql_statement_completed(
    ACTION(
        sqlserver.client_app_name,
        sqlserver.database_id,
        sqlserver.database_name,
        sqlserver.query_hash,
        sqlserver.session_id,
        sqlserver.sql_text,
        sqlserver.username
    )
    WHERE (
        [package0].[greater_than_uint64]([sqlserver].[database_id], (4))
        AND [package0].[equal_boolean]([sqlserver].[is_system], (0))
    )
)
ADD TARGET package0.ring_buffer(SET max_events_limit=(50000), max_memory=(8192))
WITH (
    MAX_MEMORY=16384 KB,
    EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,
    MAX_DISPATCH_LATENCY=3 SECONDS,
    MAX_EVENT_SIZE=0 KB,
    MEMORY_PARTITION_MODE=NONE,
    TRACK_CAUSALITY=ON,
    STARTUP_STATE=OFF
);
GO
