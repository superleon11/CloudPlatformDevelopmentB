using JordanODonnellCourseworkB.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Blob;

namespace JordanODonnellCourseworkB.Controllers
{
    public class SampleController : ApiController
    {
        private const String partitionName = "Samples_Partition_1";
        private BlobStorageService _blobStorageService = new BlobStorageService();

        private CloudBlobContainer getSoundbiteContainer()
        {
            return _blobStorageService.getCloudBlobContainer();
        }


        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private CloudTable table;
        private CloudQueueService _queueStorageService = new CloudQueueService();

        private CloudQueue getSoundbiteMakerQueue()
        {
            return _queueStorageService.getCloudQueue();
        }

        public SampleController()
        {
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("Sample");
        }

        /// <summary>
        /// Get all products
        /// </summary>
        /// <returns></returns>
        // GET: api/Products
        public IEnumerable<Sample> Get()
        {
            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));
            List<SampleEntity> entityList = new List<SampleEntity>(table.ExecuteQuery(query));

            // Basically create a list of Product from the list of ProductEntity with a 1:1 object relationship, filtering data as needed
            IEnumerable<Sample> sampleList = from e in entityList
                                               select new Sample()
                                               {
                                                   SampleID = e.RowKey,
                                                   Title = e.Title,
                                                   Artist = e.Artist,                                                  
                                                   CreatedDate = e.CreatedDate,
                                                   Mp3Blob = e.Mp3Blob,
                                                   SampleMp3Blob = e.SampleMp3Blob,
                                                   SampleMp3URL = e.SampleMp3URL,
                                                   SampleDate =e.SampleDate
                                               
                                               
                                               };
            return sampleList;
        }

        // GET: api/Products/5
        /// <summary>
        /// Get a product
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult GetSample(string id)
        {
            // Create a retrieve operation that takes a product entity.
            TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the retrieve operation.
            TableResult getOperationResult = table.Execute(getOperation);

            // Construct response including a new DTO as apprporiatte
            if (getOperationResult.Result == null) return NotFound();
            else
            {
                SampleEntity e = (SampleEntity)getOperationResult.Result;
                Sample p = new Sample()
                {
                    SampleID = e.RowKey,
                    Title = e.Title,
                    Artist = e.Artist,                                                
                    CreatedDate = e.CreatedDate,
                    Mp3Blob = e.Mp3Blob,
                    SampleMp3Blob = e.SampleMp3Blob,
                    SampleMp3URL = e.SampleMp3URL,
                    SampleDate = e.SampleDate
                };
                return Ok(p);
            }
        }

        // POST: api/Products
        /// <summary>
        /// Create a new product
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        //[SwaggerResponse(HttpStatusCode.Created)]
        [ResponseType(typeof(Sample))]
        public IHttpActionResult PostSample(Sample sample)
        {
            SampleEntity sampleEntity = new SampleEntity()
            {
                RowKey = getNewMaxRowKeyValue(),
                PartitionKey = partitionName,
                Title = sample.Title,
                Artist = sample.Artist,
                CreatedDate = sample.CreatedDate,
                Mp3Blob = sample.Mp3Blob,
                SampleMp3Blob = sample.SampleMp3Blob,
                SampleMp3URL = sample.SampleMp3URL,
                SampleDate = sample.SampleDate
            };

            // Create the TableOperation that inserts the product entity.
            var insertOperation = TableOperation.Insert(sampleEntity);

            // Execute the insert operation.
            table.Execute(insertOperation);

            return CreatedAtRoute("DefaultApi", new { id = sampleEntity.RowKey }, sampleEntity);
        }

        // PUT: api/Products/5
        /// <summary>
        /// Update a product
        /// </summary>
        /// <param name="id"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        //[SwaggerResponse(HttpStatusCode.NoContent)]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutSample(string id, Sample sample)
        {
            if (id != sample.SampleID)
            {
                return BadRequest();
            }

            // Create a retrieve operation that takes a product entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Assign the result to a ProductEntity object.
            SampleEntity updateEntity = (SampleEntity)retrievedResult.Result;

            String mp3Sample = updateEntity.SampleMp3Blob;
            deleteOldSampleBlobs(mp3Sample);

            updateEntity.Title = sample.Title;
            updateEntity.Artist = sample.Artist;
            updateEntity.CreatedDate = sample.CreatedDate;
            updateEntity.Mp3Blob = sample.Mp3Blob;
            updateEntity.SampleMp3Blob = sample.SampleMp3Blob;
            updateEntity.SampleMp3URL = sample.SampleMp3URL;

            
            var JsonSam = JsonConvert.SerializeObject(updateEntity);

            getSoundbiteMakerQueue().AddMessage(new CloudQueueMessage(JsonSam));
            
            

            // Create the TableOperation that inserts the product entity.
            // Note semantics of InsertOrReplace() which are consistent with PUT
            // See: https://stackoverflow.com/questions/14685907/difference-between-insert-or-merge-entity-and-insert-or-replace-entity
            var updateOperation = TableOperation.InsertOrReplace(updateEntity);

            // Execute the insert operation.
            table.Execute(updateOperation);

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE: api/Products/5
        /// <summary>
        /// Delete a product
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult DeleteSample(string id)
        {
            // Create a retrieve operation that takes a product entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result == null) return NotFound();
            else
            {
                SampleEntity deleteEntity = (SampleEntity)retrievedResult.Result;
                String samplemp3blob = deleteEntity.SampleMp3Blob;
                String mp3blob = deleteEntity.Mp3Blob;
                deleteOldBlobs(mp3blob);
                deleteOldSampleBlobs(samplemp3blob);

                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                table.Execute(deleteOperation);

                return Ok(retrievedResult.Result);
            }
        }

        private String getNewMaxRowKeyValue()
        {
            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));

            int maxRowKeyValue = 0;
            foreach (SampleEntity entity in table.ExecuteQuery(query))
            {
                int entityRowKeyValue = Int32.Parse(entity.RowKey);
                if (entityRowKeyValue > maxRowKeyValue) maxRowKeyValue = entityRowKeyValue;
            }
            maxRowKeyValue++;
            return maxRowKeyValue.ToString();
        }

        private void deleteOldBlobs(String Mp3Blob)
        {

            String path = "mp3s/" + Mp3Blob;
            var blob = getSoundbiteContainer().GetBlockBlobReference(path);

            blob.DeleteIfExists();
        }

        private void deleteOldSampleBlobs(String Mp3Blob)
        {

            String path = "musicclip/" + Mp3Blob;
            var blob = getSoundbiteContainer().GetBlockBlobReference(path);

            blob.DeleteIfExists();
        }


    }
}
