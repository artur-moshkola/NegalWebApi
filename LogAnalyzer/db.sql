/*
drop table if exists RawLogs;
*/

if not exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA=N'dbo' and TABLE_NAME=N'RawLogs')
create table RawLogs (
	[Package] nvarchar(64) not null,
	[Name] nvarchar(380) not null,
		constraint PK_RawLogs primary key ([Package], [Name]),
	[RequestId] nvarchar(24) not null,
	[TS] datetime2,
	[Method] nvarchar(1024),
	[MessageType] nvarchar(16),
	HttpStatusCode int,
	ProcTime int,
	[Data] nvarchar(max),
	[DataType] nvarchar(16)
)
go

create or alter procedure ImportFile
	@Package nvarchar(64),
	@Name nvarchar(380),
	@RequestId nvarchar(24),
	@TS datetime2 = null,
	@Method nvarchar(1024) = null,
	@MessageType nvarchar(16) = null,
	@HttpStatusCode int = null,
	@ProcTime int = null,
	@Data nvarchar(max) = null,
	@DataType nvarchar(16) = null
as
begin
	set nocount on;

	insert into RawLogs ([Package], [Name], [RequestId], [TS], [Method], MessageType, HttpStatusCode, ProcTime, [Data], [DataType])
	values (@Package, @Name, @RequestId, @TS, @Method, @MessageType, @HttpStatusCode, @ProcTime, @Data, @DataType);

end;
go

create or alter view Requests as
select rq.[Package], rq.[RequestId], rq.[Method],
	[Request]=rq.[Data],
	[ResponseCode]=rs.HttpStatusCode,
	[Response]=rs.[Data],
	[Exception]=ex.[Data],
	ProcTime=coalesce(rs.ProcTime, ex.ProcTime),
	[DeviceId]=json_value(rq.[Data], '$.gooidKey')
from RawLogs rq
left join RawLogs rs on rs.[Package]=rq.[Package] and rs.[RequestId]=rq.[RequestId] and rs.Method=rq.Method and rs.[MessageType]=N'Response'
left join RawLogs ex on ex.[Package]=rq.[Package] and ex.[RequestId]=rq.[RequestId] and ex.Method=rq.Method and ex.[MessageType]=N'Exception'
where rq.MessageType=N'Request';

go
