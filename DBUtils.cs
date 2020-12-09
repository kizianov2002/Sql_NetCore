using Sql_NetCore;
using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Sql_NetFramework.SqlConn
{
    class DBUtils
    {
        public static SqlConnection
            GetDBConnection()
        {
            MyParams myParams = new MyParams();

            string c_string = myParams.Value("ConStr");

            return GetDBConnection( c_string );
        }


        public static SqlConnection 
            GetDBConnection( string c_string )
        {
            string connString = c_string;
            //Console.WriteLine(" -> -> " + connString);

            SqlConnection conn = new SqlConnection(connString);

            return conn;
        }
    }

}