// Entity class for Azure table
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace JordanODonnellCourseworkB.Models
{

    public class SampleEntity : TableEntity
    {
        //All the fields for the sample entity
        public string Title { get; set; }
        public string Artist { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Mp3Blob { get; set; }
        public string SampleMp3Blob { get; set; }
        public string SampleMp3URL { get; set; }
        public DateTime? SampleDate { get; set; }
       

        public SampleEntity(string partitionKey, string sampleID)
        {
            PartitionKey = partitionKey;
            RowKey = sampleID;
        }

        public SampleEntity() { }

    }
}
