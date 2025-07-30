namespace Fiap.Events.Infra.Repository.Base
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        private readonly IDbConnection _connection;
        private readonly string _tableName;
        private readonly string _keyColumn;

        public BaseRepository(IDbConnection connection)
        {
            _connection = connection;
            _tableName = GetTableName();
            _keyColumn = GetKeyColumnName();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            var sql = $"SELECT * FROM {_tableName}";
            return await _connection.QueryAsync<T>(sql);
        }

        public async Task<T> GetByIdAsync(int id)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE {_keyColumn} = @Id";
            return await _connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
        }

        public async Task<IEnumerable<T>> FindAsync(string whereClause, object parameters = null)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE {whereClause}";
            return await _connection.QueryAsync<T>(sql, parameters);
        }

        private string GetTableName()
        {
            var type = typeof(T);

            var tableAttr = type.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
            if (tableAttr != null)
                return tableAttr.Name;

            return type.Name + "s";
        }

        private string GetKeyColumnName()
        {
            var type = typeof(T);
            var properties = type.GetProperties();

            var keyProperty = properties.FirstOrDefault(p =>
                p.GetCustomAttributes(typeof(KeyAttribute), false).Any());

            if (keyProperty != null)
                return keyProperty.Name;

            var idProperty = properties.FirstOrDefault(p =>
                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals($"{type.Name}Id", StringComparison.OrdinalIgnoreCase));

            return idProperty?.Name ?? "Id";
        }
    }
}
