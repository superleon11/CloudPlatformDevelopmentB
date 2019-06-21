﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Configuration;
using System.Diagnostics;

namespace JordanODonnellCourseworkB

{
    public class CloudQueueService
    {
        public CloudQueue getCloudQueue()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse
                 (ConfigurationManager.ConnectionStrings["AzureStorage"].ToString());

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue sampleQueue = queueClient.GetQueueReference("soundbitemaker");
            sampleQueue.CreateIfNotExists();

            Trace.TraceInformation("Queue initialized");

            return sampleQueue;
        }
    }
}

