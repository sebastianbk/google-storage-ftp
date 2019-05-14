using System;
using System.Threading.Tasks;
using FubarDev.FtpServer;
using FubarDev.FtpServer.FileSystem;

namespace GoogleStorageFtp.FileSystem.Gcs
{
	public class GcsFileSystemProvider : IFileSystemClassFactory
	{
		public Task<IUnixFileSystem> Create(IAccountInformation accountInformation)
		{
            // TODO: Update the bucketName variable with the actual name of the bucket
			var bucketName = "[BUCKET-NAME]";

			return Task.FromResult<IUnixFileSystem>(new GcsFileSystem(bucketName));
		}
	}
}