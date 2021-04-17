using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BookwormFunctions.EruditeFunction
{
	public class Erudite
	{
		private const string FROM_LANG = "fromLang";
		private const string TO_LANG = "toLang";
		private readonly IMongoDatabase _db;

		public Erudite(IMongoDatabase db)
		{
			_db = db;
		}

		[FunctionName("Erudite")]
		[return: ServiceBus("dictionary-articles", Connection = "WordTopicsConnection")]
		public async Task<Message> Run([ServiceBusTrigger("lemmas", "all-lemmas", Connection = "WordTopicsConnection")] Message lemmaMsg, ILogger log)
		{
			if (lemmaMsg.MessageId == "warmup-message")
				return new Message();
			try
			{
				var (label, body) = await GetLemmaAndArticle(lemmaMsg).ConfigureAwait(false) switch
				{
					(string l, null) => (l, System.Text.Encoding.UTF8.GetBytes($"{{\"error\":\"Could not find an article for '{l}'\"}}")),
					(string l, string a) => (l, System.Text.Encoding.UTF8.GetBytes(a))
				};
				return new Message
				{
					Body = body,
					Label = label
				};
			}
			catch (Exception ex)
			{
				log.LogError(ex, ex.Message);
				string message = $"{{ \"error\": \"{ex.Message}\" }}";
				return new Message(System.Text.Encoding.UTF8.GetBytes(message));
			}
		}

		private async Task<(string, string)> GetLemmaAndArticle(Message lemmaMsg)
		{
			var lemma = System.Text.Encoding.UTF8.GetString(lemmaMsg.Body);
			var fromLang = lemmaMsg.UserProperties[FROM_LANG];
			var toLang = lemmaMsg.UserProperties[TO_LANG];

			var article = await GetDictionaryArticle($"{fromLang}-{toLang}", lemma);
			return (lemma, article);
		}

		async Task<string> GetDictionaryArticle(string collectionName, string lemma)
		{
			var filter = Builders<DictionaryArticle>.Filter.Eq(a => a.Word, lemma.ToLowerInvariant());
			var foundAsync = await _db.GetCollection<DictionaryArticle>(collectionName).FindAsync(filter);
			var article = await foundAsync.FirstOrDefaultAsync();
			return JsonSerializer.Serialize(article, new JsonSerializerOptions { IgnoreNullValues = true });
		}

	}

	internal class DictionaryArticle
	{
		[BsonId]
		public string Word { get; set; }
		public object Dictionaries { get; set; }
	}

}
