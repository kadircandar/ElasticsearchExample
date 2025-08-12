using Elasticsearch.Net;
using ElasticsearchExample.ConfigModels;
using ElasticsearchExample.Interfaces;
using Nest;

namespace ElasticsearchExample.Helper
{
    public class ElasticHelper
    {
        private readonly IElasticClient _client;

        private readonly ElasticsearchOptions _elasticsearchOptions;

        public ElasticHelper(IConfiguration configuration)
        {
            _elasticsearchOptions = configuration.GetSection("Elasticsearch").Get<ElasticsearchOptions>() ?? new ElasticsearchOptions();
            _client = CreateInstance();
        }

        #region Elasticsearch Connection
        private ElasticClient CreateInstance()
        {
            var nodes = new Uri[]
            {
                new Uri($"http://{_elasticsearchOptions.Host}:{_elasticsearchOptions.Port}/")
            };

            var connectionPool = new StaticConnectionPool(nodes);

            var connectionSettings = new ConnectionSettings(connectionPool)
                .BasicAuthentication(_elasticsearchOptions.Username, _elasticsearchOptions.Password) // ile ElasticSearch’e tanımlanmış user ve password ile bağlanılır.

                .DisablePing() // İlk request’den sonra, belirlenen standart sürenin üstünde bir sürede hata fırlatılması sağlanır.

                .DisableDirectStreaming(false) // Bu, Elastic’e request ve response’un ara belleğe alınmasını sağlar ve her iki değerin de sırasıyla
                                               // “RequestBodyInBytes” ve “ResponseBodyInBytes” propertylerinde çağrılabilmesine imkan verir. Bunu elasticsearch’de hata alındığı zaman,
                                               // daha detaylı hatayı alabilmek adına eklenmiştir. Memoryde performans kaybına neden olabilir. Sadece ihtiyaç anında kullanılmalıdır.
                .SniffOnStartup(false) // İlk connection’ın çekilme anında, havuzun kontrol edilmesini engeller. Amaç performanstır.
                .SniffOnConnectionFault(false); // Bağlantı havuzu yeniden beslemeyi destekliyorsa, bir arama başarısız olduğunda ilgili connection havuzundan yeniden denetlenmesini engeller. Amaç yine performanstır.

            return new ElasticClient(connectionSettings);
        }
        #endregion Elasticsearch Connection

        public async Task<bool> CheckIndexAsync(string? indexName)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new Exception("IndexName cannot be null or empty");

            var hasIndex = await _client.Indices.ExistsAsync(indexName); // İlgili index’ın var olup olmadığına bakılıyor.
            return hasIndex.Exists;
        }

        public async Task<bool> DeleteIndexByNameAsync(string indexName)
        {
            if (string.IsNullOrWhiteSpace(indexName)) return false;

            var response = await _client.Indices.DeleteAsync(indexName);

            if (response.IsValid)
                return true;

            return false;
        }

        public async Task<bool> CreateIndexAsync<T>(string indexName) where T : class, IElasticDocument
        {
            if (string.IsNullOrWhiteSpace(indexName)) return false;
 ;
            if (await CheckIndexAsync(indexName))
                return true;

            var createResponse = await _client.Indices.CreateAsync(indexName, c => c
                .Map<T>(m => m.AutoMap()) // T tipinin tüm alanlarını otomatik map eder
                //.Settings(s => s
                //    .NumberOfShards(1)
                //    .NumberOfReplicas(1)
                //)
            );

            return createResponse.IsValid;
        }

        public async Task<bool> BulkIndexAsync<T>(string indexName, List<T> items) where T : class, IElasticDocument
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new Exception("IndexName cannot be null or empty");

            if (items == null || items.Count == 0)
                throw new ArgumentException("Items list cannot be null or empty.", nameof(items));

            // Eğer index varsa sil
            if (await CheckIndexAsync(indexName))
            {
                var deleteResponse = await DeleteIndexByNameAsync(indexName);
                if (!deleteResponse)
                    throw new InvalidOperationException($"Failed to delete index '{indexName}'");
            }

            var createIndexResponse = await _client.Indices
                .CreateAsync(indexName,
                index => index
                         .Map<T>(m => m.AutoMap())
                         //.Settings(s => s
                         //   .NumberOfShards(2) //  2 adet shared oluşturuluyor.
                         //   .NumberOfReplicas(1)) // her bir shared için 1 kopya oluşturulur                         
                );

            var indexBulkResponse = await _client.BulkAsync(b => b
                .Index(indexName)
                .IndexMany<T>(items));

            if (indexBulkResponse.Errors)
            {
                throw new Exception("Some documents could not be indexed");
            }

            return true;
        }

        public async Task<List<T>> GetDocumentsAsync<T>(string indexName, int size = 20) where T : class, IElasticDocument
        {
            var response = await _client.SearchAsync<T>(s => s
            .Index(indexName)
            .Query(q => q.MatchAll()) // Herhangi bir filtreleme yapmadan tüm belgeleri almak istiyorsanız
            .Size(size)); //  Belirli bir sayıdaki belgeyi almanıza olanak tanır.

            return response.Documents.ToList();
        }

        public bool CreateOrUpdateDocument<T>(string indexName, T model) where T : class, IElasticDocument
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new Exception("IndexName cannot be null or empty");

            var document = _client.DocumentExists(DocumentPath<T>.Id(new Id(model)), dd => dd.Index(indexName));

            if (document.Exists)
            {
                var res = _client.Update(DocumentPath<T>.Id(new Id(model)),
                    ss => ss
                    .Index(indexName)
                    .Doc(model)
                    .RetryOnConflict(3));

                if (res.ServerError == null) return true;

                throw new Exception($"Updated Document Failed Index:{indexName}" + res.ServerError.Error.Reason);

            }
            else
            {
                var res = _client.Index(model, ss => ss.Index(indexName));

                if (res.ServerError == null) return true;

                throw new Exception($"Insert Document Failed Index:{indexName}" + res.ServerError.Error.Reason);
            }
        }

        public async Task<bool> DeleteDocumentById<T>(string indexName, string id) where T : class, IElasticDocument
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new Exception("IndexName cannot be null or empty");

            var document = await _client.DocumentExistsAsync(DocumentPath<T>.Id(new Id(id)), dd => dd.Index(indexName));

            if (document.Exists)
            {
                var res = await _client.DeleteAsync(DocumentPath<T>.Id(new Id(id)),
                    ss => ss
                    .Index(indexName));

                if (res.ServerError == null) return true;

                throw new Exception($"Delete Document Failed Index:{indexName}" + res.ServerError.Error.Reason);

            }
            return false;
        }


        public async Task<T?> GetByIdAsync<T>(string indexName, string id) where T : class, IElasticDocument
        {
            var response = await _client.GetAsync<T>(id, g => g.Index(indexName));

            if (!response.IsValid || !response.Found)
                return null;

            return response.Source;
        }

        public List<T> FuzzySearch<T>(string indexName, string seachText, params string[] fields) where T : class, IElasticDocument
        {
            var fd = new FieldsDescriptor<T>();

            fd.Fields(fields);

            var response = _client.Search<T>(s => s
            .Index(indexName)
            .Query(sq => sq
            .MultiMatch(mm => mm
              .Fields(f => fd)
                .Query(seachText)
            .Fuzziness(Fuzziness.Auto))));

            return response.Documents.ToList();
        }
    }
}
