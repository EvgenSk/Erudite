using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

[assembly: FunctionsStartup(typeof(BookwormFunctions.EruditeFunction.Startup))]

namespace BookwormFunctions.EruditeFunction
{
	public class Startup : FunctionsStartup
	{
		private const string DICTIONARIES_DB_CONNECTION = "DictionariesDBConnectionString";
		private const string DATABASE_NAME = "dictionary";
		public override void Configure(IFunctionsHostBuilder builder)
		{
			var config = builder.GetContext().Configuration;
			var dictionariesDbConnectionString = config.GetConnectionString(DICTIONARIES_DB_CONNECTION);
			var db = new MongoClient(dictionariesDbConnectionString).GetDatabase(DATABASE_NAME);
			builder.Services.AddSingleton(db);
		}

		public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
		{
			var context = builder.GetContext();
			builder.ConfigurationBuilder
				.AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
				.AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
				.AddEnvironmentVariables();
		}
	}
}
