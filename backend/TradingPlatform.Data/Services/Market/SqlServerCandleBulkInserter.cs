using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Services.Market;

public interface ICandleBulkInserter
{
    Task BulkInsertAsync(IEnumerable<CandleEntity> candles, CancellationToken cancellationToken = default);
}

public sealed class SqlServerCandleBulkInserter : ICandleBulkInserter
{
    private readonly string _connectionString;
    private const string TempTableName = "#CandleStaging";

    public SqlServerCandleBulkInserter(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }

    public async Task BulkInsertAsync(IEnumerable<CandleEntity> candles, CancellationToken cancellationToken = default)
    {
        var candleList = candles as IList<CandleEntity> ?? candles.ToList();
        if (candleList.Count == 0)
        {
            return;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await CreateStagingTableAsync(connection, transaction, cancellationToken);
            await BulkCopyIntoStagingAsync(connection, transaction, candleList, cancellationToken);
            await InsertFromStagingAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task CreateStagingTableAsync(SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
    {
        const string createTempTableSql = @"
CREATE TABLE [" + TempTableName + @"] (
    [Symbol] nvarchar(50) NOT NULL,
    [Source] nvarchar(50) NOT NULL,
    [IntervalMinutes] int NOT NULL,
    [OpenTime] datetime2(3) NOT NULL,
    [CloseTime] datetime2(3) NOT NULL,
    [Open] decimal(18,8) NOT NULL,
    [High] decimal(18,8) NOT NULL,
    [Low] decimal(18,8) NOT NULL,
    [Close] decimal(18,8) NOT NULL,
    [Volume] decimal(18,8) NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL
);";

        await using var command = new SqlCommand(createTempTableSql, connection, transaction);
        command.CommandTimeout = 0;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task BulkCopyIntoStagingAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        IList<CandleEntity> candles,
        CancellationToken cancellationToken)
    {
        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, transaction)
        {
            DestinationTableName = TempTableName,
            BulkCopyTimeout = 0,
            BatchSize = candles.Count,
        };

        bulkCopy.ColumnMappings.Add("Symbol", "Symbol");
        bulkCopy.ColumnMappings.Add("Source", "Source");
        bulkCopy.ColumnMappings.Add("IntervalMinutes", "IntervalMinutes");
        bulkCopy.ColumnMappings.Add("OpenTime", "OpenTime");
        bulkCopy.ColumnMappings.Add("CloseTime", "CloseTime");
        bulkCopy.ColumnMappings.Add("Open", "Open");
        bulkCopy.ColumnMappings.Add("High", "High");
        bulkCopy.ColumnMappings.Add("Low", "Low");
        bulkCopy.ColumnMappings.Add("Close", "Close");
        bulkCopy.ColumnMappings.Add("Volume", "Volume");
        bulkCopy.ColumnMappings.Add("CreatedAt", "CreatedAt");

        await bulkCopy.WriteToServerAsync(new CandleEntityDataReader(candles), cancellationToken);
    }

    private static async Task InsertFromStagingAsync(SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
    {
        const string insertSql = @"
INSERT INTO [Candles] ([Symbol], [Source], [IntervalMinutes], [OpenTime], [CloseTime], [Open], [High], [Low], [Close], [Volume], [CreatedAt])
SELECT [Symbol], [Source], [IntervalMinutes], [OpenTime], [CloseTime], [Open], [High], [Low], [Close], [Volume], [CreatedAt]
FROM (
    SELECT *, ROW_NUMBER() OVER (PARTITION BY [Symbol], [Source], [IntervalMinutes], [OpenTime] ORDER BY [CreatedAt] DESC) AS rn
    FROM [" + TempTableName + @"]
) AS s
WHERE rn = 1
  AND NOT EXISTS (
      SELECT 1
      FROM [Candles] c
      WHERE c.[Symbol] = s.[Symbol]
        AND c.[Source] = s.[Source]
        AND c.[IntervalMinutes] = s.[IntervalMinutes]
        AND c.[OpenTime] = s.[OpenTime]
  );";

        await using var command = new SqlCommand(insertSql, connection, transaction);
        command.CommandTimeout = 0;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed class CandleEntityDataReader : IDataReader
    {
        private readonly IList<CandleEntity> _candles;
        private int _index = -1;

        public CandleEntityDataReader(IList<CandleEntity> candles)
        {
            _candles = candles;
        }

        public int FieldCount => 11;
        public bool Read() => ++_index < _candles.Count;
        public bool NextResult() => false;
        public void Dispose() { }
        public void Close() { }
        public DataTable GetSchemaTable() => throw new NotSupportedException();
        public bool IsClosed => false;
        public int RecordsAffected => 0;
        public int Depth => 0;

        public object GetValue(int i)
        {
            var current = _candles[_index];
            return i switch
            {
                0 => current.Symbol,
                1 => current.Source,
                2 => current.IntervalMinutes,
                3 => current.OpenTime,
                4 => current.CloseTime,
                5 => current.Open,
                6 => current.High,
                7 => current.Low,
                8 => current.Close,
                9 => current.Volume,
                10 => current.CreatedAt,
                _ => throw new IndexOutOfRangeException($"Invalid column ordinal: {i}")
            };
        }

        public int GetOrdinal(string name) => name switch
        {
            "Symbol" => 0,
            "Source" => 1,
            "IntervalMinutes" => 2,
            "OpenTime" => 3,
            "CloseTime" => 4,
            "Open" => 5,
            "High" => 6,
            "Low" => 7,
            "Close" => 8,
            "Volume" => 9,
            "CreatedAt" => 10,
            _ => throw new IndexOutOfRangeException($"Invalid column name: {name}")
        };

        public string GetName(int i) => i switch
        {
            0 => "Symbol",
            1 => "Source",
            2 => "IntervalMinutes",
            3 => "OpenTime",
            4 => "CloseTime",
            5 => "Open",
            6 => "High",
            7 => "Low",
            8 => "Close",
            9 => "Volume",
            10 => "CreatedAt",
            _ => throw new IndexOutOfRangeException($"Invalid column ordinal: {i}")
        };

        public Type GetFieldType(int i) => i switch
        {
            0 => typeof(string),
            1 => typeof(string),
            2 => typeof(int),
            3 => typeof(DateTime),
            4 => typeof(DateTime),
            5 => typeof(decimal),
            6 => typeof(decimal),
            7 => typeof(decimal),
            8 => typeof(decimal),
            9 => typeof(decimal),
            10 => typeof(DateTime),
            _ => throw new IndexOutOfRangeException($"Invalid column ordinal: {i}")
        };

        public bool IsDBNull(int i) => false;
        public object this[int i] => GetValue(i);
        public object this[string name] => GetValue(GetOrdinal(name));
        public bool GetBoolean(int i) => (bool)GetValue(i);
        public byte GetByte(int i) => (byte)GetValue(i);
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public char GetChar(int i) => (char)GetValue(i);
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public Guid GetGuid(int i) => (Guid)GetValue(i);
        public short GetInt16(int i) => Convert.ToInt16(GetValue(i));
        public int GetInt32(int i) => Convert.ToInt32(GetValue(i));
        public long GetInt64(int i) => Convert.ToInt64(GetValue(i));
        public float GetFloat(int i) => Convert.ToSingle(GetValue(i));
        public double GetDouble(int i) => Convert.ToDouble(GetValue(i));
        public string GetString(int i) => (string)GetValue(i);
        public decimal GetDecimal(int i) => (decimal)GetValue(i);
        public DateTime GetDateTime(int i) => (DateTime)GetValue(i);
        public string GetDataTypeName(int i) => GetFieldType(i).Name;
        public int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (var i = 0; i < count; i++)
            {
                values[i] = GetValue(i);
            }

            return count;
        }

        public IDataReader GetData(int i) => throw new NotSupportedException();
    }
}
