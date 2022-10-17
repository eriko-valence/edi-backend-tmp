CREATE EXTERNAL DATA SOURCE [emsDevSrc]
    WITH (
    TYPE = RDBMS,
    LOCATION = N'srv-ems-dev.database.windows.net',
    DATABASE_NAME = N'dbsql-nhgh-ems-dev',
    CREDENTIAL = [emsDev]
    );



