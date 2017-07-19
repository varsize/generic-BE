using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using BlockExplorerAPI.DAL.Abstract;
using BlockExplorerAPI.DAL.DTO;
using BlockExplorerAPI.Utils;

namespace BlockExplorerAPI.DAL
{
    public class DatabaseTransactionRepository : ISimpleTransactionRepository
    {
        private readonly string connectionString;

        public DatabaseTransactionRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void AddRange(IEnumerable<Transaction> transactions)
        {
            if (!transactions.Any())
                return;

            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                using (SqlTransaction dbTransaction = sqlConnection.BeginTransaction())
                {
                    var command = new SqlCommand(@"DELETE FROM Transactions WHERE BlockNumber = @blockNumber", sqlConnection, dbTransaction);
                    command.Parameters.Add(new SqlParameter("blockNumber", transactions.First().BlockNumber));
                    command.ExecuteNonQuery();

                    foreach (var tx in transactions)
                    {
                        Insert(tx, sqlConnection, dbTransaction);
                    }
                    dbTransaction.Commit();
                }
            }
        }
                
        public void Add(Transaction tx)
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                using (SqlTransaction dbTransaction = sqlConnection.BeginTransaction())
                {
                    Insert(tx, sqlConnection, dbTransaction);
                    dbTransaction.Commit();
                }
            }
        }

        private void Insert(Transaction tx, SqlConnection sqlConnection, SqlTransaction dbTransaction)
        {
            var txUpdateCommand = new SqlCommand(@"UPDATE Transactions SET BlockNumber = @blockNumber, BlockHash = @BlockHash, Time = @time, Size = @size WHERE Id = @id", sqlConnection, dbTransaction);
            txUpdateCommand.Parameters.Add(new SqlParameter("id", tx.Id));
            txUpdateCommand.Parameters.Add(new SqlParameter("blockNumber", tx.BlockNumber.Value));
            txUpdateCommand.Parameters.Add(new SqlParameter("blockHash", tx.BlockHash));
            txUpdateCommand.Parameters.Add(new SqlParameter("time", tx.Time));
            txUpdateCommand.Parameters.Add(new SqlParameter("size", tx.Size));

            int rowCount = txUpdateCommand.ExecuteNonQuery();
            if (rowCount == 0)
            {
                var txInsertCommand = new SqlCommand("INSERT INTO Transactions (Id, BlockNumber, BlockHash, Time, Size) VALUES (@id, @blockNumber, @blockHash, @time, @size)", sqlConnection, dbTransaction);
                txInsertCommand.Parameters.Add(new SqlParameter("id", tx.Id));
                txInsertCommand.Parameters.Add(new SqlParameter("blockNumber", tx.BlockNumber.Value));
                txInsertCommand.Parameters.Add(new SqlParameter("blockHash", tx.BlockHash));
                txInsertCommand.Parameters.Add(new SqlParameter("time", tx.Time));
                txInsertCommand.Parameters.Add(new SqlParameter("size", tx.Size));
                txInsertCommand.ExecuteNonQuery();
            }
            else
            {
                var deleteAddressesCommand = new SqlCommand(@"DELETE FROM Addresses WHERE TxId = @id", sqlConnection, dbTransaction);
                deleteAddressesCommand.Parameters.Add(new SqlParameter("id", tx.Id));
                deleteAddressesCommand.ExecuteNonQuery();
            }

            var addressInsertCommand = new SqlCommand("INSERT INTO Addresses (TxId, Side, Address, Value, Vout, ParentTxId) VALUES (@txid, @side, @address, @value, @vout, @parentTxId)", sqlConnection, dbTransaction);
            if (tx.Inputs.Count != 0)
            {
                foreach (var input in tx.Inputs)
                {
                    addressInsertCommand.Parameters.Add(new SqlParameter("txid", input.TxId));
                    addressInsertCommand.Parameters.Add(new SqlParameter("side", false));
                    addressInsertCommand.Parameters.Add(new SqlParameter("address", input.Address));
                    addressInsertCommand.Parameters.Add(new SqlParameter("value", input.Value));
                    addressInsertCommand.Parameters.Add(new SqlParameter("vout", input.Vout));
                    addressInsertCommand.Parameters.Add(input.ParentTxId != null
                        ? new SqlParameter("parentTxId", input.ParentTxId)
                        : new SqlParameter("parentTxId", DBNull.Value));
                    addressInsertCommand.ExecuteNonQuery();
                    addressInsertCommand.Parameters.Clear();
                }
            }
            foreach (var output in tx.Outputs)
            {
                addressInsertCommand.Parameters.Add(new SqlParameter("txid", output.TxId));
                addressInsertCommand.Parameters.Add(new SqlParameter("side", true));
                addressInsertCommand.Parameters.Add(output.Address != null
                        ? new SqlParameter("address", output.Address)
                        : new SqlParameter("address", DBNull.Value));
                addressInsertCommand.Parameters.Add(new SqlParameter("value", output.Value));
                addressInsertCommand.Parameters.Add(new SqlParameter("vout", output.Vout));
                addressInsertCommand.Parameters.Add(new SqlParameter("parentTxId", DBNull.Value));
                addressInsertCommand.ExecuteNonQuery();
                addressInsertCommand.Parameters.Clear();
            }
        }


        public Transaction Find(string txid)
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                var command = new SqlCommand(@"
                    SELECT i.TxId, t.BlockNumber, t.BlockHash, t.Time, t.Size, i.Side, i.Address, i.Value, i.Vout, i.ParentTxId
                    FROM Transactions AS t
                    INNER JOIN Addresses AS i ON t.Id = i.TxId
                    WHERE t.Id = @txid", sqlConnection);
                command.Parameters.Add(new SqlParameter("txid", txid));

                return ReadTransaction(command);
            }
        }

        public List<Transaction> FindAll(IEnumerable<string> txids)
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                var command = new SqlCommand(@"
                    SELECT i.TxId, t.BlockNumber, t.BlockHash, t.Time, t.Size, i.Side, i.Address, i.Value, i.Vout, i.ParentTxId
                    FROM Transactions AS t
                    INNER JOIN Addresses AS i ON t.Id = i.TxId
                    WHERE t.Id IN (@txid)", sqlConnection);
                command.AddArrayParameters("txid", txids);

                return ReadTransactions(command);
            }
        }

        public List<Transaction> FindByAddress(string address)
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                var command = new SqlCommand(@"
                    SELECT i.TxId, t.BlockNumber, t.BlockHash, t.Time, t.Size, i.Side, i.Address, i.Value, i.Vout, i.ParentTxId
                    FROM Transactions AS t
                    INNER JOIN Addresses AS i ON t.Id = i.TxId
                    WHERE t.Id IN (SELECT a.TxId FROM Addresses AS a WHERE a.Address = @address)", sqlConnection);
                command.Parameters.Add(new SqlParameter("address", address));

                return ReadTransactions(command);
            }
        }

        public List<Transaction> GetLast(int count)
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                var command = new SqlCommand(@"
                    SELECT tx.Id, tx.BlockNumber, tx.BlockHash, tx.Time, tx.Size, i.Side, i.Address, i.Value, i.Vout, i.ParentTxId
                    FROM (SELECT TOP (@count) * FROM Transactions ORDER BY Transactions.Time DESC) tx
                    INNER JOIN Addresses AS i ON tx.Id = i.TxId
                    ORDER BY tx.Time DESC", sqlConnection);
                command.Parameters.Add(new SqlParameter("count", count));

                return ReadTransactions(command);
            }
        }

        public int GetBlockNumber()
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                var command = new SqlCommand("SELECT MAX(BlockNumber) FROM Transactions", sqlConnection);
                using (SqlDataReader dataReader = command.ExecuteReader())
                {
                    if (dataReader.Read() && !dataReader.IsDBNull(0))
                        return dataReader.GetInt32(0);
                    return -1;
                }
            }
        }

        private static Transaction ReadTransaction(SqlCommand command)
        {
            using (SqlDataReader dataReader = command.ExecuteReader())
            {
                Transaction transaction = null;
                while (dataReader.Read())
                {
                    ReadDataRow(ref transaction, dataReader);
                }
                return transaction;
            }
        }

        private static List<Transaction> ReadTransactions(SqlCommand command)
        {
            using (SqlDataReader dataReader = command.ExecuteReader())
            {
                var transactions = new Dictionary<string, Transaction>();
                while (dataReader.Read())
                {
                    string transactionId = dataReader.GetString(0);
                    Transaction transaction;
                    bool found = transactions.TryGetValue(transactionId, out transaction);

                    ReadDataRow(ref transaction, dataReader);

                    if (!found)
                        transactions.Add(transaction.Id, transaction);                    
                }
                return transactions.Values.ToList();
            }
        }

        private static void ReadDataRow(ref Transaction transaction, SqlDataReader dataReader)
        {
            if (transaction == null)
            {
                transaction = new Transaction
                {
                    Id = dataReader.GetString(0),
                    BlockNumber = dataReader.GetInt32(1),
                    BlockHash = dataReader.GetString(2),
                    Time = dataReader.GetInt64(3),
                    Size = dataReader.GetInt32(4),
                    Inputs = new List<Input>(),
                    Outputs = new List<Output>(),
                };
            }
            if (!dataReader.GetBoolean(5))
            {
                transaction.Inputs.Add(new Input()
                {
                    Transaction = transaction,
                    TxId = transaction.Id,
                    Address = dataReader.IsDBNull(6) ? null : dataReader.GetString(6),
                    Value = dataReader.GetDecimal(7),
                    Vout = dataReader.GetInt32(8),
                    ParentTxId = dataReader.IsDBNull(9) ? null : dataReader.GetString(9),
                });
            }
            else
            {
                transaction.Outputs.Add(new Output()
                {
                    Transaction = transaction,
                    TxId = transaction.Id,
                    Address = dataReader.IsDBNull(6) ? null : dataReader.GetString(6),
                    Value = dataReader.GetDecimal(7),
                    Vout = dataReader.GetInt32(8),
                });
            }
        }

        public List<Transaction> FindByBlock(string blockHash)
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                var command = new SqlCommand(@"
                    SELECT i.TxId, t.BlockNumber, t.BlockHash, t.Time, t.Size, i.Side, i.Address, i.Value, i.Vout, i.ParentTxId
                    FROM Transactions AS t
                    INNER JOIN Addresses AS i ON t.Id = i.TxId
                    WHERE t.BlockHash = @blockHash", sqlConnection);
                command.Parameters.Add(new SqlParameter("blockHash", blockHash));

                return ReadTransactions(command);
            }
        }
    }
}