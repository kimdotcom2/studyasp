using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using studyasp.Dtos;

namespace studyasp.Repositories
{
    public class BoardRepository
    {

        private readonly string _connString;

        public BoardRepository(string connString)
        {
            _connString = connString;
        }

        private IDbConnection CreateConnection()
        => new SqliteConnection(_connString);

        public async Task<BoardView> GetBoard(string id)
        {
            
            using var conn = CreateConnection();
            var sql = "SELECT * FROM Board WHERE id = @id";
            BoardView board = await conn.QuerySingleAsync<BoardView>(sql, new { id });
            return board;

        }

    }
}