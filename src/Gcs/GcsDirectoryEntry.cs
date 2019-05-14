using System;
using System.Linq;
using FubarDev.FtpServer.FileSystem;
using FubarDev.FtpServer.FileSystem.Generic;

namespace GoogleStorageFtp.FileSystem.Gcs
{
    public class GcsDirectoryEntry : IUnixDirectoryEntry
    {
        public GcsDirectoryEntry(GcsFileSystem fileSystem)
        {
            FileSystem = fileSystem;

            var accessMode = new GenericAccessMode(true, true, true);
            Permissions = new GenericUnixPermissions(accessMode, accessMode, accessMode);
        }

        public bool IsRoot => string.IsNullOrEmpty(Name);

        public bool IsDeletable => !IsRoot;

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                FullName = value;
                _name = value.Trim('/').Split('/').LastOrDefault();
            }
        }

        public string FullName { get; private set; }

        public IUnixPermissions Permissions { get; set; }

        public DateTimeOffset? LastWriteTime { get; set; }

        public DateTimeOffset? CreatedTime { get; set; }

        public long NumberOfLinks => 1;

        public IUnixFileSystem FileSystem { get; set; }

        public string Owner => "owner";

        public string Group => "group";
    }
}