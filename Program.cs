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

            ExecuteChoice(account); 
        }

        public static void ExecuteChoice(CloudStorageAccount account)
        {
            Console.WriteLine("\nWhat type of transfer would you like to execute?\n1. Local file --> Azure Blob\n2. Local directory --> Azure Blob directory\n3. URL (e.g. Amazon S3 file) --> Azure Blob\n4. Azure Blob --> Azure Blob");
            int choice = int.Parse(Console.ReadLine());

            SetNumberOfParallelOperations();

            if(choice == 1)
            {
                TransferLocalFileToAzureBlob(account).Wait();
            }
            else if(choice == 2)
            {
                TransferLocalDirectoryToAzureBlobDirectory(account).Wait();
            }
            else if(choice == 3)
            {
                TransferUrlToAzureBlob(account).Wait();
            }
            else if(choice == 4)
            {
                TransferAzureBlobToAzureBlob(account).Wait();
            }
        }

        public static SingleTransferContext GetSingleTransferContext(TransferCheckpoint checkpoint)
        {
            SingleTransferContext context = new SingleTransferContext(checkpoint);

            context.ProgressHandler = new Progress<TransferStatus>((progress) =>
            {
                Console.Write("\rBytes transferred: {0}", progress.BytesTransferred );
            });
            
            return context;
        }

        public static DirectoryTransferContext GetDirectoryTransferContext(TransferCheckpoint checkpoint)
        {
            DirectoryTransferContext context = new DirectoryTransferContext(checkpoint);

            context.ProgressHandler = new Progress<TransferStatus>((progress) =>
            {
                Console.Write("\rBytes transferred: {0}", progress.BytesTransferred );
            });
            
            return context;
        }

        public static void SetNumberOfParallelOperations()
        {
            Console.WriteLine("\nHow many parallel operations would you like to use?");
            string parallelOperations = Console.ReadLine();
            TransferManager.Configurations.ParallelOperations = int.Parse(parallelOperations);
        }

        public static string GetSourcePath()
        {
            Console.WriteLine("\nProvide path for source:");
            string sourcePath = Console.ReadLine();

            return sourcePath;
        }

        public static CloudBlockBlob GetBlob(CloudStorageAccount account)
        {
            CloudBlobClient blobClient = account.CreateCloudBlobClient();

            Console.WriteLine("\nProvide name of Blob container:");
            string containerName = Console.ReadLine();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExistsAsync().Wait();

            Console.WriteLine("\nProvide name of Blob:");
            string blobName = Console.ReadLine();
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            return blob;
        }

        public static CloudBlobDirectory GetBlobDirectory(CloudStorageAccount account)
        {
            CloudBlobClient blobClient = account.CreateCloudBlobClient();

            Console.WriteLine("\nProvide name of Blob container:");
            string containerName = Console.ReadLine();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExistsAsync().Wait();

            CloudBlobDirectory blobDirectory = container.GetDirectoryReference("");

            return blobDirectory;
        }

        public static async Task TransferLocalFileToAzureBlob(CloudStorageAccount account)
        { 
            string localFilePath = GetSourcePath();
            CloudBlockBlob blob = GetBlob(account); 
            TransferCheckpoint checkpoint = null;
            SingleTransferContext context = GetSingleTransferContext(checkpoint); 
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
                context = GetSingleTransferContext(checkpoint);
                Console.WriteLine("\nResuming transfer...\n");
                await TransferManager.UploadAsync(localFilePath, blob, null, context);
            }

            stopWatch.Stop();
            Console.WriteLine("\nTransfer operation completed in " + stopWatch.Elapsed.TotalSeconds + " seconds.");
            ExecuteChoice(account);
        }

        public static async Task TransferLocalDirectoryToAzureBlobDirectory(CloudStorageAccount account)
        { 
            string localDirectoryPath = GetSourcePath();
            CloudBlobDirectory blobDirectory = GetBlobDirectory(account); 
            TransferCheckpoint checkpoint = null;
            DirectoryTransferContext context = GetDirectoryTransferContext(checkpoint); 
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
                context = GetDirectoryTransferContext(checkpoint);
                Console.WriteLine("\nResuming transfer...\n");
                await TransferManager.UploadDirectoryAsync(localDirectoryPath, blobDirectory, options, context);
            }

            stopWatch.Stop();
            Console.WriteLine("\nTransfer operation completed in " + stopWatch.Elapsed.TotalSeconds + " seconds.");
            ExecuteChoice(account);
        }

        public static async Task TransferUrlToAzureBlob(CloudStorageAccount account)
        {
            Uri uri = new Uri(GetSourcePath());
            CloudBlockBlob blob = GetBlob(account); 
            TransferCheckpoint checkpoint = null;
            SingleTransferContext context = GetSingleTransferContext(checkpoint); 
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
                context = GetSingleTransferContext(checkpoint);
                Console.WriteLine("\nResuming transfer...\n");
                await TransferManager.CopyAsync(uri, blob, true, null, context, cancellationSource.Token);
            }

            stopWatch.Stop();
            Console.WriteLine("\nTransfer operation completed in " + stopWatch.Elapsed.TotalSeconds + " seconds.");
            ExecuteChoice(account);
        }

        public static async Task TransferAzureBlobToAzureBlob(CloudStorageAccount account)
        {
            CloudBlockBlob sourceBlob = GetBlob(account);
            CloudBlockBlob destinationBlob = GetBlob(account); 
            TransferCheckpoint checkpoint = null;
            SingleTransferContext context = GetSingleTransferContext(checkpoint); 
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            Console.WriteLine("\nTransfer started...\nPress 'c' to temporarily cancel your transfer...\n");

            Stopwatch stopWatch = Stopwatch.StartNew();
            Task task;
            ConsoleKeyInfo keyinfo;
            try
            {
                task = TransferManager.CopyAsync(sourceBlob, destinationBlob, true, null, context, cancellationSource.Token);
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
                context = GetSingleTransferContext(checkpoint);
                Console.WriteLine("\nResuming transfer...\n");
                await TransferManager.CopyAsync(sourceBlob, destinationBlob, false, null, context, cancellationSource.Token);
            }

            stopWatch.Stop();
            Console.WriteLine("\nTransfer operation completed in " + stopWatch.Elapsed.TotalSeconds + " seconds.");
            ExecuteChoice(account);
        }
    }
}

