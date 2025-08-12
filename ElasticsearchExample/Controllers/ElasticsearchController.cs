using ElasticsearchExample.Helper;
using ElasticsearchExample.Models;
using ElasticsearchExample.TestData;
using Microsoft.AspNetCore.Mvc;

namespace ElasticsearchExample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ElasticsearchController : ControllerBase
    {    
        private readonly ElasticHelper _elastic;

        public ElasticsearchController(ElasticHelper elastic)
        {
            _elastic = elastic;
        }

        [HttpGet("CheckIndex")]
        public async Task<IActionResult> CheckIndex(string indexName)
        {
            var result = await _elastic.CheckIndexAsync(indexName);

            if (result)
                return Ok();

            return NoContent();
        }

        [HttpPost("CreateIndex")]
        public async Task<IActionResult> CreateIndex(string indexName)
        {
            var result = await _elastic.CreateIndexAsync<Product>(indexName);

            if (result)
                return Ok();

            return NoContent();
        }

        [HttpDelete("DeleteIndexByName")]
        public async Task<IActionResult> DeleteIndexByNameAsync(string indexName)
        {
            var result = await _elastic.DeleteIndexByNameAsync(indexName);

            if (result)
                return Ok();

            return NoContent();
        }

        [HttpPost("BulkIndex")]
        public async Task<IActionResult> BulkIndex(string indexName)
        {
            var testProducts = TestDataGenerator.GenerateProducts(10);

            var result = await _elastic.BulkIndexAsync<Product>(indexName, testProducts);

            if (result)
                return Ok();

            return NoContent();
        }

        [HttpGet("GetDocuments")]
        public async Task<ActionResult<List<Product>>> GetDocuments(string indexName, int size = 20)
        {        
            var result = await _elastic.GetDocumentsAsync<Product>(indexName, size);

            if (result == null || result.Count == 0)
                return NotFound();

            return result;
        }

        [HttpPost("CreateOrUpdateDocument")]
        public IActionResult CreateOrUpdateDocument(string indexName)
        {
            var testProduct = TestDataGenerator.GenerateProduct();

            var result = _elastic.CreateOrUpdateDocument<Product>(indexName, testProduct);

            if (result)
                return Ok();

            return NoContent();
        }

        [HttpDelete("DeleteDocumentById")]
        public async Task<IActionResult> DeleteDocumentById(string indexName, string id)
        {
            var result = await _elastic.DeleteDocumentById<Product>(indexName, id);

            if (result)
                return Ok();

            return NoContent();
        }

        [HttpGet("GetByIdAsync")]
        public async Task<ActionResult<Product>> GetByIdAsync(string indexName, string id)
        {
            var result = await _elastic.GetByIdAsync<Product>(indexName, id);

            if(result == null)
            {
                return NoContent();
            }

            return result;
        }

        [HttpGet("FuzzySearch")]
        public  ActionResult<List<Product>> FuzzySearch(string indexName, string seachText)
        {
            var result = _elastic.FuzzySearch<Product>(indexName, seachText);

            if (result == null)
            {
                return NoContent();
            }

            return result;
        }
    }
}
