using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Norm
{
    /// <summary>
    /// The connection string builder.
    /// </summary>
    public class ConnectionStringBuilder : IOptionsContainer
    {
        private const string DEFAULT_DATABASE = "admin";
        private const int DEFAULT_PORT = 27017;
        private const string PROTOCOL = "mongodb://";
        private static readonly IDictionary<string, Action<string, IOptionsContainer>> _optionsHandler = new Dictionary<string, Action<string, IOptionsContainer>>
              {
                  {"strict", (v, b) => b.SetStrictMode(bool.Parse(v))},
                  {"querytimeout", (v, b) => b.SetQueryTimeout(int.Parse(v))},
                  {"expando", (v, b) => b.SetEnableExpandoProperties(bool.Parse(v))},
                  {"pooling", (v, b) => b.SetPooled(bool.Parse(v))},
                  {"poolsize", (v, b) => b.SetPoolSize(int.Parse(v))},
                  {"timeout", (v, b) => b.SetTimeout(int.Parse(v))},
                  {"lifetime", (v, b) => b.SetLifetime(int.Parse(v))},
              }; 

        /// <summary>
        /// Prevents a default instance of the <see cref="ConnectionStringBuilder"/> class from being created.
        /// </summary>
        private ConnectionStringBuilder()
        {
        }

        /// <summary>
        /// Gets a server list.
        /// </summary>
        public IList<Server> Servers { get; private set; }

        /// <summary>
        /// Gets the user name.
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// Gets the password.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Gets database name.
        /// </summary>
        public string Database { get; private set; }

        /// <summary>
        /// Gets the query timeout.
        /// </summary>
        public int QueryTimeout { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to enable ExpandoProperties.
        /// </summary>
        public bool EnableExpandoProperties { get; private set; }

        /// <summary>
        /// Gets a value indicating whether strict mode is enabled.
        /// </summary>
        public bool StrictMode { get; private set; }

        /// <summary>
        /// Gets a value indicating whether connections are pooled.
        /// </summary>
        public bool Pooled { get; private set; }

        /// <summary>
        /// Gets the connection pool size.
        /// </summary>
        public int PoolSize { get; private set; }

        /// <summary>
        /// Gets the connection timeout.
        /// </summary>
        public int Timeout { get; private set; }

        /// <summary>
        /// Gets the connection lifetime.
        /// </summary>
        public int Lifetime { get; private set; }

        /// <summary>
        /// Creates a connection string builder.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns></returns>
        /// <exception cref="MongoException">
        /// </exception>
        public static ConnectionStringBuilder Create(string connectionString)
        {
            var connection = connectionString;

            if (!connection.StartsWith(PROTOCOL, StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    connection = ConfigurationManager.ConnectionStrings[connectionString].ConnectionString;
                }
                catch (NullReferenceException)
                {
                    throw new MongoException("Connection string is missing");
                }

                if (string.IsNullOrEmpty(connection) ||
                    !connection.StartsWith(PROTOCOL, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new MongoException("Invalid connection string: the protocol must be mongodb://");
                }
            }

            var parts = connection.Split(new[] { '?' }, StringSplitOptions.RemoveEmptyEntries);
            var options = parts.Length == 2 ? parts[1] : null;
            var sb = new StringBuilder(parts[0].Substring(PROTOCOL.Length));
            var builder = new ConnectionStringBuilder
            {
                QueryTimeout = 30,
                Timeout = 30,
                EnableExpandoProperties = false,
                StrictMode = true,
                Pooled = true,
                PoolSize = 25,
                Lifetime = 15,
            };
           
            // var coreBuilder = new StringBuilder();
            builder.BuildAuthentication(sb/*, coreBuilder*/)
                .BuildDatabase(sb)
                .BuildServerList(sb);

            BuildOptions(builder, options);
            return builder;
        }

        /// <summary>
        /// Set the query timeout.
        /// </summary>
        /// <param name="timeout">
        /// The timeout.
        /// </param>
        public void SetQueryTimeout(int timeout)
        {
            QueryTimeout = timeout;
        }

        /// <summary>
        /// Sets enable expando properties.
        /// </summary>
        /// <param name="enabled">
        /// The enabled.
        /// </param>
        public void SetEnableExpandoProperties(bool enabled)
        {
            EnableExpandoProperties = enabled;
        }

        /// <summary>
        /// Sets strict mode.
        /// </summary>
        /// <param name="strict">
        /// The strict.
        /// </param>
        public void SetStrictMode(bool strict)
        {
            StrictMode = strict;
        }

        /// <summary>
        /// Set the pool size.
        /// </summary>
        /// <param name="size">
        /// The size.
        /// </param>
        public void SetPoolSize(int size)
        {
            PoolSize = size;
        }

        /// <summary>
        /// Sets the pooled flag.
        /// </summary>
        /// <param name="pooled">
        /// The pooled.
        /// </param>
        public void SetPooled(bool pooled)
        {
            Pooled = pooled;
        }

        /// <summary>
        /// Sets the timeout.
        /// </summary>
        /// <param name="timeout">
        /// The timeout.
        /// </param>
        public void SetTimeout(int timeout)
        {
            Timeout = timeout;
        }

        /// <summary>
        /// Sets the lifetime.
        /// </summary>
        /// <param name="lifetime">
        /// The lifetime.
        /// </param>
        public void SetLifetime(int lifetime)
        {
            Lifetime = lifetime;
        }

        /// <summary>
        /// The build options.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="MongoException">
        /// </exception>
        internal static void BuildOptions(IOptionsContainer container, string options)
        {
            if (string.IsNullOrEmpty(options))
            {
                return;
            }

            // don't like how I did this
            var parts = options.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kvp = part.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (kvp.Length != 2)
                {
                    throw new MongoException("Invalid connection option: " + part);
                }

                _optionsHandler[kvp[0].ToLower()](kvp[1], container);
            }
        }

        /// <summary>
        /// The build authentication.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <returns></returns>
        /// <exception cref="MongoException">
        /// </exception>
        private ConnectionStringBuilder BuildAuthentication(StringBuilder sb/*, StringBuilder coreBuilder*/)
        {
            var connectionString = sb.ToString();
            var separator = connectionString.IndexOf('@');
            if (separator == -1)
            {
                return this;
            }

            var parts = connectionString.Substring(0, separator).Split(':');
            if (parts.Length != 2)
            {
                throw new MongoException(
                    "Invalid connection string: authentication should be in the form of username:password");
            }

            UserName = parts[0];
            Password = parts[1];
            sb.Remove(0, separator + 1);
            return this;
        }

        /// <summary>
        /// Builds a database.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <returns></returns>
        private ConnectionStringBuilder BuildDatabase(StringBuilder sb)
        {
            var connectionString = sb.ToString();
            var separator = connectionString.IndexOf('/');
            if (separator == -1)
            {
                Database = DEFAULT_DATABASE;
            }
            else
            {
                Database = connectionString.Substring(separator + 1);
                sb.Remove(separator, sb.Length - separator);
            }

            return this;
        }

        /// <summary>
        /// The build server list.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <exception cref="MongoException">
        /// </exception>
        private void BuildServerList(StringBuilder sb)
        {
            var connectionString = sb.ToString();
            var servers = connectionString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (servers.Length == 0)
            {
                throw new MongoException("Invalid connection string: at least 1 server is required");
            }

            var list = new List<Server>(servers.Length);
            foreach (var server in servers)
            {
                var parts = server.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 2)
                {
                    throw new MongoException(
                        string.Format("Invalid connection string: {0} is not a valid server configuration", server));
                }

                list.Add(new Server
                             {
                                 Host = parts[0],
                                 Port = parts.Length == 2 ? int.Parse(parts[1]) : DEFAULT_PORT
                             });
            }

            Servers = list.AsReadOnly();
            return;
        }
    }
}