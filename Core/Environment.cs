﻿using System.ComponentModel;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Cloudy
{
    /// <summary>
    /// The <see cref="SkipServiceScanAttribute"/> is used here so that the default 
    /// convention of exporting to the DI container everything that implements 
    /// an interface doesn't apply to this class, since it's instantiated 
    /// explicitly from Startup so that we can access its variables during 
    /// initial bootstrapping.
    /// </summary>
    [SkipServiceScan]
    class Environment : IEnvironment
    {
        static IConfiguration config;

        static Environment() => config = BuildConfiguration();

        public string GetVariable(string name) =>
            Ensure.NotEmpty(config![Ensure.NotEmpty(name, nameof(name))], name);

        public T GetVariable<T>(string name, T defaultValue = default)
        {
            var value = config![Ensure.NotEmpty(name, nameof(name))];

            if (value != null)
            {
                if (value is T typed)
                    return typed;

                var converter = TypeDescriptor.GetConverter(typeof(T));

                return (T)converter.ConvertFromString(value);
            }

            return defaultValue;
        }

        public static void Refresh() => config = BuildConfiguration();

        static IConfiguration BuildConfiguration()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // In locally run tests, the file will be alongside the assembly.
            // In Azure, it will be one level up.
            if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
                basePath = new DirectoryInfo(basePath).Parent.FullName;

            var builder = new ConfigurationBuilder()
                 .SetBasePath(basePath)
                 .AddJsonFile("tests.settings.json", optional: true, reloadOnChange: true)
                 .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                 .AddJsonFile("secrets.settings.json", optional: true, reloadOnChange: true)
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .AddEnvironmentVariables();

            config = builder.Build();

            if (!string.IsNullOrEmpty(config["AZURE_CLIENT_ID"]) &&
                !string.IsNullOrEmpty(config["AZURE_CLIENT_SECRET"]))
            {
                // Use the config above to initialize the keyvault config extension.
                builder.AddAzureKeyVault(
                    $"https://{config["AzureKeyVaultName"]}.vault.azure.net/",
                    config["AZURE_CLIENT_ID"],
                    config["AZURE_CLIENT_SECRET"]);

                // Now build the final version again that includes the keyvault provider.
                config = builder.Build();
            }

            return config;
        }
    }

    interface IEnvironment
    {
        string GetVariable(string name);
        T GetVariable<T>(string name, T defaultValue = default);
    }
}
