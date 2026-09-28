CREATE EVENT SESSION [{{SESSION_NAME}}] ON SERVER
ADD EVENT sqlserver.sql_statement_completed(
    ACTION(
        sqlserver.database_name,
        sqlserver.client_app_name,
        sqlserver.username,
        sqlserver.session_id,
        sqlserver.sql_text
    )
    WHERE ([sqlserver].[is_system]=(0))
),
ADD EVENT sqlserver.rpc_completed(
    ACTION(
        sqlserver.database_name,
        sqlserver.client_app_name,
        sqlserver.username,
        sqlserver.session_id,
        sqlserver.sql_text
    )
    WHERE ([sqlserver].[is_system]=(0))
)
ADD TARGET package0.ring_buffer(SET max_events=10000, max_memory=4096)
WITH (STARTUP_STATE=OFF);
