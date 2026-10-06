using System.Data.Common;

namespace DeSanti.Application.Interfaces;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
