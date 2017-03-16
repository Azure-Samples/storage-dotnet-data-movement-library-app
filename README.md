---
services: storage
platforms: dotnet
author: seguler
---

In this sample, we demonstrate how to use the Azure Storage Data Movement Library to interact with Blob Storage. To learn how this was created, check out the [Transfer Data with Azure Storage Data Movement Library](https://docs.microsoft.com/azure/storage/storage-use-data-movement-library) documentation.

If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212).

## Running this sample

To run this sample:

1. Create an [Azure Storage Account](https://docs.microsoft.com/en-us/azure/storage/storage-create-storage-account)
2. Install [Visual Studio Code](https://code.visualstudio.com/)
3. Install the Visual Studio Code [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp).
4. Install [.NET Core](https://www.microsoft.com/net/core). When selecting your environment, choose the command-line option. 
5. Clone this repo. Open it in Visual Studio Code. 
6. At this point, you should see two prompts. One is for adding "required assets to build and debug." Click "yes." Another prompt is for restoring unresolved dependencies. Click "restore."
7. Your application should now contain a `launch.json` file under the `.vscode` directory. In this file, change the `externalConsole` value to `true`.
8. Visual Studio Code allows you to debug .NET Core applications. Hit `F5` to run the application.

## More information
- [Data Movement Library Reference Documentation](https://msdn.microsoft.com/library/azure/microsoft.windowsazure.storage.datamovement.aspx)
- [Data Movement Library Source Code](https://github.com/Azure/azure-storage-net-data-movement)
- [Introduction to Microsoft Azure Storage](https://docs.microsoft.com/azure/storage/storage-introduction)
