using FubarDev.FtpServer;
using FubarDev.FtpServer.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleStorageFtp.FileSystem.Gcs
{
    public static class GcsFtpServerBuilderExtensions
    {
        public static IFtpServerBuilder UseGcsFileSystem(this IFtpServerBuilder builder)
        {
            builder.Services.AddSingleton<IFileSystemClassFactory, GcsFileSystemProvider>();
            return builder;
        }
    }
}