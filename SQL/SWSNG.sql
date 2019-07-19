-- <Creates the database>
USE [master]
GO

CREATE DATABASE [SWSNG] CONTAINMENT = NONE ON PRIMARY (
	NAME = N'SWSNG'
	,FILENAME = N'{Path}\SWSNG.mdf'
	,SIZE = 512 KB
	,MAXSIZE = UNLIMITED
	,FILEGROWTH = 128 KB
	) LOG ON (
	NAME = N'SWSNG_log'
	,FILENAME = N'{Path}\SWSNG_log.ldf'
	,SIZE = 512 KB
	,MAXSIZE = UNLIMITED
	,FILEGROWTH = 128 KB
	)
GO

ALTER DATABASE [SWSNG]

SET COMPATIBILITY_LEVEL = {COMPATIBILITY LEVEL}
GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
BEGIN
	EXEC [SWSNG].[dbo].[sp_fulltext_database] @action = 'enable'
END
GO

ALTER DATABASE [SWSNG]

SET ANSI_NULL_DEFAULT OFF
GO

ALTER DATABASE [SWSNG]

SET ANSI_NULLS OFF
GO

ALTER DATABASE [SWSNG]

SET ANSI_PADDING OFF
GO

ALTER DATABASE [SWSNG]

SET ANSI_WARNINGS OFF
GO

ALTER DATABASE [SWSNG]

SET ARITHABORT OFF
GO

ALTER DATABASE [SWSNG]

SET AUTO_CLOSE OFF
GO

ALTER DATABASE [SWSNG]

SET AUTO_SHRINK OFF
GO

ALTER DATABASE [SWSNG]

SET AUTO_UPDATE_STATISTICS ON
GO

ALTER DATABASE [SWSNG]

SET CURSOR_CLOSE_ON_COMMIT OFF
GO

ALTER DATABASE [SWSNG]

SET CURSOR_DEFAULT GLOBAL
GO

ALTER DATABASE [SWSNG]

SET CONCAT_NULL_YIELDS_NULL OFF
GO

ALTER DATABASE [SWSNG]

SET NUMERIC_ROUNDABORT OFF
GO

ALTER DATABASE [SWSNG]

SET QUOTED_IDENTIFIER OFF
GO

ALTER DATABASE [SWSNG]

SET RECURSIVE_TRIGGERS OFF
GO

ALTER DATABASE [SWSNG]

SET DISABLE_BROKER
GO

ALTER DATABASE [SWSNG]

SET AUTO_UPDATE_STATISTICS_ASYNC OFF
GO

ALTER DATABASE [SWSNG]

SET DATE_CORRELATION_OPTIMIZATION OFF
GO

ALTER DATABASE [SWSNG]

SET TRUSTWORTHY OFF
GO

ALTER DATABASE [SWSNG]

SET ALLOW_SNAPSHOT_ISOLATION OFF
GO

ALTER DATABASE [SWSNG]

SET PARAMETERIZATION SIMPLE
GO

ALTER DATABASE [SWSNG]

SET READ_COMMITTED_SNAPSHOT OFF
GO

ALTER DATABASE [SWSNG]

SET HONOR_BROKER_PRIORITY OFF
GO

ALTER DATABASE [SWSNG]

SET RECOVERY SIMPLE
GO

ALTER DATABASE [SWSNG]

SET MULTI_USER
GO

ALTER DATABASE [SWSNG]

SET PAGE_VERIFY CHECKSUM
GO

ALTER DATABASE [SWSNG]

SET DB_CHAINING OFF
GO

ALTER DATABASE [SWSNG]

SET FILESTREAM(NON_TRANSACTED_ACCESS = OFF)
GO

ALTER DATABASE [SWSNG]

SET TARGET_RECOVERY_TIME = 60 SECONDS
GO

ALTER DATABASE [SWSNG]

SET DELAYED_DURABILITY = DISABLED
GO

ALTER DATABASE [SWSNG]

SET QUERY_STORE = OFF
GO

USE [SWSNG]
GO

ALTER DATABASE SCOPED CONFIGURATION

SET LEGACY_CARDINALITY_ESTIMATION = OFF;
GO

ALTER DATABASE SCOPED CONFIGURATION
FOR SECONDARY

SET LEGACY_CARDINALITY_ESTIMATION = PRIMARY;
GO

ALTER DATABASE SCOPED CONFIGURATION

SET MAXDOP = 0;
GO

ALTER DATABASE SCOPED CONFIGURATION
FOR SECONDARY

SET MAXDOP = PRIMARY;
GO

ALTER DATABASE SCOPED CONFIGURATION

SET PARAMETER_SNIFFING = ON;
GO

ALTER DATABASE SCOPED CONFIGURATION
FOR SECONDARY

SET PARAMETER_SNIFFING = PRIMARY;
GO

ALTER DATABASE SCOPED CONFIGURATION

SET QUERY_OPTIMIZER_HOTFIXES = OFF;
GO

ALTER DATABASE SCOPED CONFIGURATION
FOR SECONDARY

SET QUERY_OPTIMIZER_HOTFIXES = PRIMARY;
GO

ALTER DATABASE [SWSNG]

SET READ_WRITE
GO
-- </Creates the database>

-- <Creates the SerialNo table>
USE [SWSNG]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SerialNo] (
	[id] [int] Identity(0, 1) NOT NULL
	,[serialNo] [int] NOT NULL
	,[scheme] [nvarchar](32) NOT NULL
	,[format] [INT] NOT NULL
	,[delimiter] [nvarchar](8) NOT NULL
	,CONSTRAINT [PK_SerialNo] PRIMARY KEY CLUSTERED ([id] ASC) WITH (
		PAD_INDEX = OFF
		,STATISTICS_NORECOMPUTE = OFF
		,IGNORE_DUP_KEY = OFF
		,ALLOW_ROW_LOCKS = ON
		,ALLOW_PAGE_LOCKS = ON
		) ON [PRIMARY]
	) ON [PRIMARY]
GO
-- </Creates the SerialNo table>

-- <Inserts the example data>
INSERT INTO [dbo].[SerialNo]
SELECT 1
	,N'P000001'
	,3
	,N'-'
GO

INSERT INTO [dbo].[SerialNo]
SELECT 1
	,N'P000002'
	,3
	,N'-'
GO
-- <Inserts the example data>

-- <Creates the stored procedures>
USE [SWSNG]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[GetNextSerialNo] @scheme NVARCHAR(64)
	,@serialNo NVARCHAR(MAX) OUTPUT
	,@errCode NVARCHAR(MAX) OUTPUT
	,@errMsg NVARCHAR(MAX) OUTPUT
AS
BEGIN
	SET NOCOUNT ON;
	SET @errCode = 0;
	SET @errMsg = N'OK';

	BEGIN TRANSACTION;

	BEGIN TRY
		DECLARE @count INT = (
				SELECT Count([id])
				FROM [dbo].[SerialNo] WITH (
						TABLOCK
						,HOLDLOCK
						)
				WHERE [scheme] = @scheme
				)

		IF @errCode = 0
			IF @count = 0
			BEGIN
				SET @errCode = 1;
				SET @errMsg = N'Scheme not found';
			END

		IF @errCode = 0
			IF @count > 1
			BEGIN
				SET @errCode = 2;
				SET @errMsg = N'This scheme was generated more than once';
			END

		IF @errCode = 0
		BEGIN
			DECLARE @_id INT = (
					SELECT [id]
					FROM [dbo].[SerialNo]
					WHERE [scheme] = @scheme
					);
			DECLARE @_serialNo INT = (
					SELECT [serialNo]
					FROM [dbo].[SerialNo]
					WHERE [id] = @_id
					);
			DECLARE @_format INT = (
					SELECT [format]
					FROM [dbo].[SerialNo]
					WHERE [id] = @_id
					);
			DECLARE @_delimiter NVARCHAR(8) = (
					SELECT [delimiter]
					FROM [dbo].[SerialNo]
					WHERE [id] = @_id
					);

			IF @_id IS NULL
				OR @_serialNo IS NULL
				OR @_format IS NULL
				OR @_delimiter IS NULL
			BEGIN
				SET @errCode = 3;
				SET @errMsg = N'Not all required values available';
			END
		END

		IF @errCode = 0
		BEGIN
			DECLARE @zeroLength INT = LEN(CAST(@_serialNo AS NVARCHAR(MAX)))

			IF @zeroLength > @_format
			BEGIN
				SET @errCode = 4;
				SET @errMsg = N'The counter exceeds the maximum possible serial number';
			END
		END

		IF @errCode = 0
		BEGIN
			DECLARE @zeroStr NVARCHAR(MAX) = N'';
			DECLARE @zeroCounter INT = 0;

			WHILE @zeroCounter < @_format - @zeroLength
			BEGIN
				SET @zeroCounter = @zeroCounter + 1;
				SET @zeroStr = (@zeroStr) + N'0';
			END

			SET @serialNo = @scheme + @_delimiter + @zeroStr + CAST(@_serialNo AS NVARCHAR(MAX));

			UPDATE [dbo].[SerialNo]
			SET [serialNo] = @_serialNo + 1
			WHERE [id] = @_id;
		END

		COMMIT TRANSACTION;
	END TRY

	BEGIN CATCH
		IF @@TRANCOUNT = 1
		BEGIN
			ROLLBACK TRANSACTION;

			SET @errCode = 666;
			SET @errMsg = N'Try catch error in [GetNextSerialNo]';
		END
	END CATCH
END
GO

CREATE PROCEDURE [dbo].[GetNoRanges] @errCode INT OUTPUT
	,@errMsg NVARCHAR(MAX) OUTPUT
AS
BEGIN
	SET NOCOUNT ON;
	SET @errCode = 0;
	SET @errMsg = N'OK';

	BEGIN TRY
		SELECT [scheme]
		FROM [dbo].[SerialNo]
		ORDER BY [scheme] ASC
	END TRY

	BEGIN CATCH
		SET @errCode = 666;
		SET @errMsg = N'Try catch error in [GetNoRanges]';
	END CATCH
END
GO
-- </Creates the stored procedures>
