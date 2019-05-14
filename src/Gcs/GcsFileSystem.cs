using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FubarDev.FtpServer.BackgroundTransfer;
using FubarDev.FtpServer.FileSystem;
using Google.Cloud.Storage.V1;

namespace GoogleStorageFtp.FileSystem.Gcs
{
	public class GcsFileSystem : IUnixFileSystem
	{
		private readonly string _bucketName;
		private readonly StorageClient _storageClient;

		public GcsFileSystem(string bucketName)
		{
			FileSystemEntryComparer = StringComparer.OrdinalIgnoreCase;
			Root = new GcsDirectoryEntry(this) { Name = "" };

			_bucketName = bucketName;
			_storageClient = StorageClient.Create();
		}

		public bool SupportsAppend { get; } = false;

		public bool SupportsNonEmptyDirectoryDelete { get; } = true;

		public StringComparer FileSystemEntryComparer { get; }

		public IUnixDirectoryEntry Root { get; }

		public Task<IBackgroundTransfer> AppendAsync(IUnixFileEntry fileEntry, long? startPosition, Stream data, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public async Task<IBackgroundTransfer> CreateAsync(IUnixDirectoryEntry targetDirectory, string fileName, Stream data, CancellationToken cancellationToken)
		{
			var targetDirEntry = (GcsDirectoryEntry)targetDirectory;
			var fullName = string.Join("/", new string[] { targetDirEntry.FullName.Trim('/'), fileName }).Trim('/');

			var obj = await _storageClient.UploadObjectAsync(_bucketName, fullName, null, data,
				null, cancellationToken);
			return null;
		}

		public async Task<IUnixDirectoryEntry> CreateDirectoryAsync(IUnixDirectoryEntry targetDirectory, string directoryName, CancellationToken cancellationToken)
		{
			var targetDirEntry = (GcsDirectoryEntry)targetDirectory;
			var fullName = $"{string.Join("/", new string[] { targetDirEntry.FullName.Trim('/'), directoryName }).Trim('/')}/";

			var dir = await _storageClient.UploadObjectAsync(_bucketName, fullName, null, new MemoryStream(),
				null, cancellationToken);
			return new GcsDirectoryEntry(null) { Name = fullName };
		}

		public async Task<IReadOnlyList<IUnixFileSystemEntry>> GetEntriesAsync(IUnixDirectoryEntry directoryEntry, CancellationToken cancellationToken)
		{
			List<IUnixFileSystemEntry> entries = null;
			var gcsDirEntry = directoryEntry as GcsDirectoryEntry;
			var dirName = string.Empty;
			if (gcsDirEntry != null)
			{
				dirName = gcsDirEntry.FullName;
			}

			// Get all objects and prefixes from Google Cloud Storage
			var objects = await _storageClient.ListObjectsAsync(_bucketName, dirName, new ListObjectsOptions
			{
				Delimiter = "/",
				PageSize = 1000
			}).AsRawResponses()?.ToList();

            // Beware of bug that can occur when number of objects is divisible by 1,000
            var items = objects?.SelectMany(obj => obj?.Items ?? new List<Google.Apis.Storage.v1.Data.Object>());
            var prefixes = objects?.SelectMany(obj => obj?.Prefixes ?? new List<string>());

			// Get files and folders
			var filesAndFolders = items.Where(obj => obj.Name != dirName && obj.ContentType != "application/x-directory")
				.Select(obj =>
				{
					DateTimeOffset? createdTime = null;
					if (obj.TimeCreated is DateTime created)
					{
						createdTime = new DateTimeOffset(created);
					}
					DateTimeOffset? updatedTime = null;
					if (obj.Updated is DateTime updated)
					{
						updatedTime = new DateTimeOffset(updated);
					}
					return new GcsFileEntry(this)
					{
						Name = obj.Name,
						Size = Convert.ToInt64(obj.Size),
						CreatedTime = createdTime,
						LastWriteTime = updatedTime
					} as IUnixFileSystemEntry;
				});

			// Get prefixes
			var folders = prefixes.Where(p => p != dirName).Select(p =>
			{
				return new GcsDirectoryEntry(this)
				{
					Name = p
				} as IUnixFileSystemEntry;
			});

			try
			{
				// Join the files, folders and prefixes
				entries = folders.Concat(filesAndFolders).ToList();
			}
			catch
			{
				try
				{
					// Return just the prefixes
					entries = folders.ToList();
				}
				catch
				{
					try
					{
						// Return just the files and folders
						entries = filesAndFolders.ToList();
					}
					catch { }
				}
			}

			return (entries ?? new List<IUnixFileSystemEntry>()).Where(e => !string.IsNullOrEmpty(e.Name)).ToList();
		}

		public async Task<IUnixFileSystemEntry> GetEntryByNameAsync(IUnixDirectoryEntry directoryEntry, string name, CancellationToken cancellationToken)
		{
			IUnixFileSystemEntry result = null;
			var parentEntry = (GcsDirectoryEntry)directoryEntry;
			var fullName = string.Join("/", new string[] { parentEntry.FullName.Trim('/'), name }).Trim('/');

			Google.Apis.Storage.v1.Data.Object obj = null;
			try
			{
				obj = await _storageClient.GetObjectAsync(_bucketName, fullName, null, cancellationToken);
			} catch { }
			if (obj == null)
			{
				var childEntry = new GcsDirectoryEntry(this) { Name = $"{fullName}/" };
				var directoryExists = await DirectoryExistsAsync(childEntry);
				if (directoryExists)
				{
					result = childEntry;
				}
			}
			else
			{
				if (obj.ContentType != "application/x-directory")
				{
					DateTimeOffset? createdTime = null;
					if (obj.TimeCreated is DateTime created)
					{
						createdTime = new DateTimeOffset(created);
					}
					DateTimeOffset? updatedTime = null;
					if (obj.Updated is DateTime updated)
					{
						updatedTime = new DateTimeOffset(updated);
					}
					return new GcsFileEntry(this)
					{
						Name = obj.Name,
						Size = Convert.ToInt64(obj.Size),
						CreatedTime = createdTime,
						LastWriteTime = updatedTime
					};
				}
			}

			return result;
		}

		private async Task<bool> DirectoryExistsAsync(IUnixDirectoryEntry directoryEntry)
		{
			var directoryExists = false;
			var gcsDirEntry = directoryEntry as GcsDirectoryEntry;
			var dirName = string.Empty;
			if (gcsDirEntry != null)
			{
				dirName = gcsDirEntry.FullName;
			}

			var objects = await _storageClient.ListObjectsAsync(_bucketName, dirName, new ListObjectsOptions
			{
				Delimiter = "/",
				PageSize = 1000
			}).AsRawResponses()?.ToList();

            // Beware of bug that can occur when number of objects is divisible by 1,000
            var items = objects?.SelectMany(obj => obj?.Items ?? new List<Google.Apis.Storage.v1.Data.Object>());
            var prefixes = objects?.SelectMany(obj => obj?.Prefixes ?? new List<string>());

			try
			{
				if (items?.Count() > 0)
				{
					directoryExists = true;
				}
                if (prefixes?.Count() > 0)
                {
                    directoryExists = true;
                }
			}
			catch (Exception ex)
			{
                Console.WriteLine($"DirectoryExistsAsync: {ex.Message}");
			}

			return directoryExists;
		}

		public async Task<IUnixFileSystemEntry> MoveAsync(IUnixDirectoryEntry parent, IUnixFileSystemEntry source, IUnixDirectoryEntry target, string fileName, CancellationToken cancellationToken)
		{
			var targetDirEntry = (GcsDirectoryEntry)target;
			var targetName = string.Join("/", new string[] { targetDirEntry.FullName.Trim('/'), fileName }).Trim('/');

			if (source is GcsFileEntry sourceFileEntry)
			{
				var sourceDirEntry = (GcsDirectoryEntry)parent;
				var sourceName = string.Join("/", new string[] { sourceDirEntry.FullName.Trim('/'), sourceFileEntry.Name }).Trim('/');
				try
				{
					var newObj = await _storageClient.CopyObjectAsync(_bucketName, sourceName,
																	  _bucketName, targetName,
																	  null, cancellationToken);
					if (newObj != null)
					{
						await _storageClient.DeleteObjectAsync(_bucketName, sourceName, null, cancellationToken);
					}
				}
				catch { }
			}
			else
			{
				throw new NotSupportedException();
			}

			return new GcsDirectoryEntry(this)
			{
				Name = targetName
			};
		}

		public async Task<Stream> OpenReadAsync(IUnixFileEntry fileEntry, long startPosition, CancellationToken cancellationToken)
		{
			var gcsFileEntry = (GcsFileEntry)fileEntry;

			try
			{
				var ms = new MemoryStream();
				await _storageClient.DownloadObjectAsync(_bucketName, gcsFileEntry.FullName, ms, null, cancellationToken);
				var data = ms.ToArray().Skip(Convert.ToInt32(startPosition)).ToArray();
				ms = new MemoryStream(data);
				return ms;
			}
			catch
			{
				return null;
			}
		}

		public async Task<IBackgroundTransfer> ReplaceAsync(IUnixFileEntry fileEntry, Stream data, CancellationToken cancellationToken)
		{
			var gcsFileEntry = (GcsFileEntry)fileEntry;

			try
			{
				var obj = await _storageClient.UploadObjectAsync(_bucketName, gcsFileEntry.FullName, null, data,
					null, cancellationToken);
			}
			catch { }
			
			return null;
		}

		public Task<IUnixFileSystemEntry> SetMacTimeAsync(IUnixFileSystemEntry entry, DateTimeOffset? modify, DateTimeOffset? access, DateTimeOffset? create, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public async Task UnlinkAsync(IUnixFileSystemEntry entry, CancellationToken cancellationToken)
		{
			if (entry is GcsFileEntry gcsFileEntry)
			{
				try
				{
					await _storageClient.DeleteObjectAsync(_bucketName, gcsFileEntry.FullName,
						null, cancellationToken);
				} catch { }
			}
			else if (entry is GcsDirectoryEntry gcsDirectoryEntry)
			{
				var foldersToDelete = new ConcurrentBag<GcsDirectoryEntry>();
				await UnlinkDirectoriesRecursivelyAsync(gcsDirectoryEntry, cancellationToken, foldersToDelete);
				var reversed = foldersToDelete.Reverse();
				foreach (var folderToDelete in reversed)
				{
					try
					{
						await _storageClient.DeleteObjectAsync(_bucketName, folderToDelete.FullName,
							null, cancellationToken);
					} catch { }
				}
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		private async Task UnlinkDirectoriesRecursivelyAsync(GcsDirectoryEntry gcsDirectoryEntry, CancellationToken cancellationToken,
			ConcurrentBag<GcsDirectoryEntry> foldersToDelete)
		{
			foldersToDelete.Add(gcsDirectoryEntry);

			try
			{
				var objects = await GetEntriesAsync(gcsDirectoryEntry, cancellationToken);
				foreach (var obj in objects)
				{
					if (obj is GcsFileEntry file)
					{
						try
						{
							await _storageClient.DeleteObjectAsync(_bucketName, file.FullName,
								null, cancellationToken);
						} catch { }
					}
					else if (obj is GcsDirectoryEntry dir)
					{
						await UnlinkDirectoriesRecursivelyAsync(dir, cancellationToken, foldersToDelete);
					}
				}
			}
			catch { }
		}

		#region IDisposable
		private bool disposedValue = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_storageClient.Dispose();
				}

				disposedValue = true;
			}
		}
		
		public void Dispose()
		{
			Dispose(true);
		}
		#endregion
	}
}