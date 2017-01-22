using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.DataMovement;

namespace DMLibSample
{     
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Enter Storage account name:");           
            string accountName = Console.ReadLine();

            Console.WriteLine("\nEnter Storage account key:");           
            string accountKey = Console.ReadLine();

            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=" + accountName + ";AccountKey=" + accountKey;
            CloudStorageAccount account = CloudStorageAccount.Parse(storageConnectionString);

            executeChoice(account); 
        }

        public static void executeChoice(CloudStorageAccount account)
        {
            Console.WriteLine("\nWhat type of transfer would you like to execute?\n1. Local file --> Azure Blob\n2. Local directory --> Azure Blob directory\n3. URL (e.g. Amazon S3 file) --> Azure Blob");
            int choice = int.Parse(Console.ReadLine());

            setNumberOfParallelOperations();

            if(choice == 1)
            {
                transferLocalFileToAzureBlob(account);
            }
            else if(choice == 2)
            {
                transferLocalDirectoryToAzureBlobDirectory(account);
            }
            else if(choice == 3)
            {
                transferUrlToAzureBlob(account);
            }
        }

        public static SingleTransferContext getSingleTransferContext(TransferCheckpoint checkpoint)
        {
            SingleTransferContext context = new SingleTransferContext(checkpoint);

            context.ProgressHandler = new Progress<TransferStatus>((progress) =>
            {
                Console.Write("\rBytes transferred: {0}", progress.BytesTransferred );
            });
            
            return context;
        }

        public static DirectoryTransferContext getDirectoryTransferContext(TransferCheckpoint checkpoint)
        {
            DirectoryTransferContext context = new DirectoryTransferContext(checkpoint);

            context.ProgressHandler = new Progress<TransferStatus>((progress) =>
            {
                Console.Write("\rBytes transferred: {0}", progress.BytesTransferred );
            });
            
            return context;
        }

        public static void setNumberOfParallelOperations()
        {
            Console.WriteLine("\nHow many parallel operations would you like to use?");
            string parallelOperations = Console.ReadLine();
            TransferManager.Configurations.ParallelOperations = int.Parse(parallelOperations);
        }

        public static string getSourcePath()
        {
            Console.WriteLine("\nProvide path for source:");
            string sourcePath = Console.ReadLine();

            return sourcePath;
        }

        public static CloudBlockBlob getBlob(CloudStorageAccount account)
        {
            CloudBlobClient blobClient = account.CreateCloudBlobClient();

            Console.WriteLine("\nProvide name of Blob container. This can be a new or existing Blob container:");
            string containerName = Console.ReadLine();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExistsAsync().Wait();

            Console.WriteLine("\nProvide name of new Blob:");
            string blobName = Console.ReadLine();
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            return blob;
        }

        public static CloudBlobDirectory getBlobDirectory(CloudStorageAccount account)
        {
            CloudBlobClient blobClient = account.CreateCloudBlobClient();

            Console.WriteLine("\nProvide name of Blob container. This can be a new or existing Blob container:");
            string containerName = Console.ReadLine();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExistsAsync().Wait();

            CloudBlobDirectory blobDirectory = container.GetDirectoryReference("");

            return blobDirectory;
        }

        public static async void transferLocalFileToAzureBlob(CloudStorageAccount account)
        { 
            string localFilePath = getSourcePath();
            CloudBlockBlob blob = getBlob(account); 
            TransferCheckpoint checkpoint = null;
            SingleTransferContext context = getSingleTransferContext(checkpoint); 
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            Console.WriteLine("\nTransfer started...\nPress 'c' to temporarily cancel your transfer...\n");
            Stopwatch stopWatch = Stopwatch.StartNew();
            Task task;
            ConsoleKeyInfo keyinfo;
            try
            {
                task = TransferManager.UploadAsync(localFilePath, blob, null, context, cancellationSource.Token);
                while(!task.IsCompleted)
                {
                    if(Console.KeyAvailable)
                    {
                        keyinfo = Console.ReadKey(true);
                        if(keyinfo.Key == ConsoleKey.C)
                        {
                            cancellationSource.Cancel();
                        }
                    }
                }
                await task;
            }
            catch(Exception e)
            {
                Console.WriteLine("\nThe transfer is canceled: {0}", e.Message);  
            }
            if(cancellationSource.IsCancellationRequested)
            {
                Console.WriteLine("\nTransfer will resume in 3 seconds...");
                Thread.Sleep(3000);
                checkpoint = context.LastCheckpoint;
                context = getSingleTransferContext(checkpoint);
                Console.WriteLine("\nResuming transfer...\n");
                TransferManager.UploadAsync(localFilePath, blob, null, context).Wait();
            }
            stopWatch.Stop();
            Console.WriteLine("\nTransfer operation completed in " + stopWatch.Elapsed.TotalSeconds + " seconds.");
            executeChoice(account);
        }

        public static async void transferLocalDirectoryToAzureBlobDirectory(CloudStorageAccount account)
        { 
            string localDirectoryPath = getSourcePath();
            CloudBlobDirectory blobDirectory = getBlobDirectory(account); 
            TransferCheckpoint checkpoint = null;
            DirectoryTransferContext context = getDirectoryTransferContext(checkpoint); 
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            Console.WriteLine("\nTransfer started...\nPress 'c' to temporarily cancel your transfer...\n");
            Stopwatch stopWatch = Stopwatch.StartNew();
            Task task;
            ConsoleKeyInfo keyinfo;
            UploadDirectoryOptions options = new UploadDirectoryOptions()
            {
                Recursive = true
            };
            try
            {
                task = TransferManager.UploadDirectoryAsync(localDirectoryPath, blobDirectory, options, context, cancellationSource.Token);
                while(!task.IsCompleted)
                {
                    if(Console.KeyAvailable)
                    {
                        keyinfo = Console.ReadKey(true);
                        if(keyinfo.Key == ConsoleKey.C)
                        {
                            cancellationSource.Cancel();
                        }
                    }
                }
                await task;
            }
            catch(Exception e)
            {
                Console.WriteLine("\nThe transfer is canceled: {0}", e.Message);  
            }
            if(cancellationSource.IsCancellationRequested)
            {
                Console.WriteLine("\nTransfer will resume in 3 seconds...");
                Thread.Sleep(3000);
                checkpoint = context.LastCheckpoint;
                context = getDirectoryTransferContext(checkpoint);
                Console.WriteLine("\nResuming transfer...\n");
                TransferManager.UploadDirectoryAsync(localDirectoryPath, blobDirectory, options, context).Wait();
            }
            stopWatch.Stop();
            Console.WriteLine("\nTransfer operation completed in " + stopWatch.Elapsed.TotalSeconds + " seconds.");
            executeChoice(account);
        }

        public static async void transferUrlToAzureBlob(CloudStorageAccount account)
        {
            Uri uri = new Uri(getSourcePath());
            CloudBlockBlob blob = getBlob(account); 
            TransferCheckpoint checkpoint = null;
            SingleTransferContext context = getSingleTransferContext(checkpoint); 
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            Console.WriteLine("\nTransfer started...\nPress 'c' to temporarily cancel your transfer...\n");
            Stopwatch stopWatch = Stopwatch.StartNew();
            Task task;
            ConsoleKeyInfo keyinfo;
            try
            {
                task = TransferManager.CopyAsync(uri, blob, true, null, context, cancellationSource.Token);
                while(!task.IsCompleted)
                {
                    if(Console.KeyAvailable)
                    {
                        keyinfo = Console.ReadKey(true);
                        if(keyinfo.Key == ConsoleKey.C)
                        {
                            cancellationSource.Cancel();
                        }
                    }
                }
                await task;
            }
            catch(Exception e)
            {
                Console.WriteLine("\nThe transfer is canceled: {0}", e.Message);  
            }
            if(cancellationSource.IsCancellationRequested)
            {
                Console.WriteLine("\nTransfer will resume in 3 seconds...");
                Thread.Sleep(3000);
                checkpoint = context.LastCheckpoint;
                context = getSingleTransferContext(checkpoint);
                Console.WriteLine("\nResuming transfer...\n");
                task = TransferManager.CopyAsync(uri, blob, true, null, context, cancellationSource.Token);
            }
            stopWatch.Stop();
            Console.WriteLine("\nTransfer operation completed in " + stopWatch.Elapsed.TotalSeconds + " seconds.");
            executeChoice(account);
        }
    }
}

