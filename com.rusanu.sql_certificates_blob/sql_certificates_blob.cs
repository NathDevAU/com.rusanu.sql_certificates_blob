// Copyright (c) 2012. Remus Rusanu 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Net.NetworkInformation;
using Microsoft.SqlServer.Server;

namespace com.rusanu.sql_certificates_blob
{
    /// <summary>
    /// Implements procedures for uploading and downloading of certificates into/from SQL server 
    /// Requires EXTERNAL_ACCESS to access temp files directory
    /// </summary>
    public class sql_certificates_helper
    {
        /// <summary>
        /// Saves the certificate into a BLOB
        /// </summary>
        [SqlProcedure]
        public static void ssb_get_certificate_blob(
            SqlString dbName,
            SqlString certName,
            out SqlBytes blob)
        {
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder();
            scsb.ContextConnection = true;
            SqlConnection connection = new SqlConnection(scsb.ConnectionString);
            connection.Open();

            using (connection)
            {
                connection.ChangeDatabase(dbName.Value);

                string tempPath = Path.GetTempPath();
                string certFile = Path.Combine(
                    tempPath, Path.GetRandomFileName());

                try
                {
                    if (false == Directory.Exists(tempPath))
                    {
                        Directory.CreateDirectory(tempPath);
                    }

                    SqlCommand cmd = new SqlCommand(
                        String.Format(
                            @"BACKUP CERTIFICATE [{0}] TO FILE = '{1}';",
                            certName.Value.Replace("]", "]]"),
                            certFile),
                        connection);

                    cmd.ExecuteNonQuery();

                    blob = new SqlBytes(new FileStream(certFile, FileMode.Open));
                }
                finally
                {
                    if (File.Exists(certFile))
                    {
                        File.Delete(certFile);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a certificate from a BLOB
        /// </summary>
        [SqlProcedure]
        public static void ssb_create_certificate_from_blob(
            SqlString dbName,
            SqlString certName,
            SqlString userName,
            SqlBinary blob)
        {
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder();
            scsb.ContextConnection = true;
            SqlConnection connection = new SqlConnection(scsb.ConnectionString);
            connection.Open();

            using (connection)
            {
                connection.ChangeDatabase(dbName.Value);

                string tempPath = Path.GetTempPath();
                string certFile = Path.Combine(
                    tempPath, Path.GetRandomFileName());
                try
                {
                    if (false == Directory.Exists(tempPath))
                    {
                        Directory.CreateDirectory(tempPath);
                    }

                    using (FileStream fs = new FileStream(certFile, FileMode.CreateNew, FileAccess.Write))
                    {
                        fs.Write(blob.Value, 0, blob.Length);
                        fs.Close();
                    }

                    SqlCommand cmd = new SqlCommand(
                        String.Format(
                            @"CREATE CERTIFICATE [{0}] AUTHORIZATION [{1}] FROM FILE = '{2}';",
                            certName.Value.Replace("]", "]]"),
                            userName.Value.Replace("]", "]]"),
                        certFile),
                        connection);

                    cmd.ExecuteNonQuery();
                }
                finally
                {
                    if (File.Exists(certFile))
                    {
                        File.Delete(certFile);
                    }
                }
            }
        }
    }
}
