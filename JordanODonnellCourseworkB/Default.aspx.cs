using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.IO;
using System.Linq;
using System.Configuration;
using JordanODonnellCourseworkB.Models;
using JordanODonnellCourseworkB.Migrations;
using Newtonsoft.Json;

// Remember: code behind is run at the server.

namespace JordanODonnellCourseworkB
{
    public partial class _Default : System.Web.UI.Page
    {
        // accessor variables and methods for blob containers and queues

        private BlobStorageService _blobStorageService = new BlobStorageService();
        private CloudQueueService _queueStorageService = new CloudQueueService();
        const String partitionName = "Samples_Partition_1";
      





        private CloudBlobContainer getSoundbiteContainer()
        {
            return _blobStorageService.getCloudBlobContainer();
        }

        private CloudQueue getSoundbiteMakerQueue()
        {
            return _queueStorageService.getCloudQueue();
        }


        private string GetMimeType(string Filename)
        {
            try
            {
                string ext = Path.GetExtension(Filename).ToLowerInvariant();
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
                if (key != null)
                {
                    string contentType = key.GetValue("Content Type") as String;
                    if (!String.IsNullOrEmpty(contentType))
                    {
                        return contentType;
                    }
                }
            }
            catch
            {
            }
            return "application/octet-stream";
        }

        // User clicked the "Submit" button
        protected void submitButton_Click(object sender, EventArgs e)
        {
            if (upload.HasFile)
            {

                // Get the file name specified by the user. 
                var ext = Path.GetExtension(upload.FileName);

                // Add more information to it so as to make it unique
                // within all the files in that blob container
                var name = string.Format("{0}{1}", Guid.NewGuid(), ext);

                var title = Path.GetFileNameWithoutExtension(upload.FileName);

               
                // Upload photo to the cloud. Store it in a new 
                // blob in the specified blob container. 

                // Go to the container, instantiate a new blob
                // with the descriptive name
                String path = "mp3s/" + name;

                var blob = getSoundbiteContainer().GetBlockBlobReference(path);

                //var sample = sample.jsonConverttoSerialisedoject();

                System.Diagnostics.Debug.WriteLine("The blob title was: " + title);

                //Checks the size of the table to make the new upload the next available row
                String y = getNewMaxRowKeyValue();
                // The blob properties object (the label on the bucket)
                // contains an entry for MIME type. Set that property.
                blob.Properties.ContentType = GetMimeType(upload.FileName);

                SampleEntity sampleEnt = new SampleEntity(partitionName, y);
                sampleEnt.Title = title;
                sampleEnt.Artist = (title +" National Anthem");
                sampleEnt.CreatedDate = DateTime.Now;
                sampleEnt.Mp3Blob = name;
                sampleEnt.SampleMp3Blob = null;
                sampleEnt.SampleMp3URL = null;
                sampleEnt.SampleDate = null;

                //Insets the new data into the table
                insertIntoTable(sampleEnt);

                //creates a searialized object for the entity in json
                var sample = JsonConvert.SerializeObject(sampleEnt);

                
                System.Diagnostics.Debug.WriteLine("Here is the json" + sample);

                // Actually upload the data to the
                // newly instantiated blob
                blob.UploadFromStream(upload.FileContent);

                blob.Metadata["Title"] = title;

                blob.SetMetadata();


                // Place a message in the queue to tell the worker
                // role that a new photo blob exists, which will 
                // cause it to create a thumbnail blob of that photo
                // for easier display. 
                
                //Generates the sample for the entity
                getSoundbiteMakerQueue().AddMessage(new CloudQueueMessage(sample));

                System.Diagnostics.Trace.WriteLine(String.Format("*** WebRole: Enqueued '{0}'", path));
            }
        }

        // rerun every timer click - set by timer control on aspx page to be every 1000ms
        protected void Page_PreRender(object sender, EventArgs e)
        {
            try
            {
                // Look at blob container that contains the thumbnails
                // generated by the worker role. Perform a query
                // of the its contents and return the list of all of the
                // blobs whose name begins with the string "thumbnails". 
                // It returns an enumerator of their URLs. 
                // Place that enumerator into list view as its data source. 
                SoundbiteDisplayControl.DataSource = from o in getSoundbiteContainer().GetDirectoryReference("musicclip").ListBlobs()
                                                     select new { Url = o.Uri, Title = getMetaData(o.Uri) };

                // Tell the list view to bind to its data source, thereby
                // showing 
                SoundbiteDisplayControl.DataBind();
            }
            catch (Exception)
            {
            }
        }

        protected string getMetaData(Uri uri)
        {
            try
            {
                CloudBlockBlob blob = new CloudBlockBlob(uri);
                blob.FetchAttributes();
                return blob.Metadata["Title"];
            }
            catch (Exception)
            {
                return "Title not found";
            }


        }

        //insets the entity into the table
        public static void insertIntoTable(SampleEntity sampleEnt)
        {

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference("Sample");

            if (table.Exists())
            {
                TableOperation tab = TableOperation.InsertOrReplace(sampleEnt);
                table.Execute(tab);
            }
        }

        //Gets the max row size from table
        private String getNewMaxRowKeyValue()
        {

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference("Sample");
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



    }


}
