using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace DapperTest
{
    class Program
    {
        static void Main(string[] args)
        {
	        using (IDbConnection connection = new SqlConnection("Data Source=(local);Initial Catalog=dapper;Integrated Security=True;"))
	        {
		        connection.Query<User>
	        }
        }
    }

	class User
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	class Post
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public string Content { get; set; }
		public User Owner { get; set; }
	}
}
