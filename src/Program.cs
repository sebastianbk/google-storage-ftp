using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using GoogleStorageFtp.FileSystem.Gcs;
using FubarDev.FtpServer;
using FubarDev.FtpServer.AccountManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace GoogleStorageFtp
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection().AddLogging(config => config.SetMinimumLevel(LogLevel.Trace));

#if !DEBUG
			var disableTls = Environment.GetEnvironmentVariable("DISABLE_TLS") ?? "False";
			if (bool.TryParse(disableTls, out var disable))
			{
				if (!disable)
				{
					var cert = new X509Certificate2("ftp.pfx", Environment.GetEnvironmentVariable("PFX_PASSWORD"));
					services.Configure<AuthTlsOptions>(cfg => cfg.ServerCertificate = cert);
				}
			}
#endif

			services.Configure<FtpConnectionOptions>(options => options.DefaultEncoding = System.Text.Encoding.UTF8);
			services.Configure<SimplePasvOptions>(options =>
			{
				options.PasvMinPort = 10000;
				options.PasvMaxPort = 10009;
				options.PublicAddress = IPAddress.Parse(Environment.GetEnvironmentVariable("PUBLIC_IP") ?? "127.0.0.1");
			});
			
            services.AddFtpServer(builder =>
			{
				builder.Services.AddSingleton<IMembershipProvider, CustomMembershipProvider>();
				builder.UseGcsFileSystem();
			});

            // Build the service provider
            using (var serviceProvider = services.BuildServiceProvider())
            {
				var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                loggerFactory.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
                NLog.LogManager.LoadConfiguration("NLog.config");

				try
				{
					// Initialize the FTP server
					var ftpServerHost = serviceProvider.GetRequiredService<IFtpServerHost>();

					// Start the FTP server
					ftpServerHost.StartAsync(CancellationToken.None).ConfigureAwait(false);

					Console.WriteLine("The FTP server is running. Press any key to kill the server...");
					Console.ReadLine();

					// Stop the FTP server
					ftpServerHost.StopAsync(CancellationToken.None).ConfigureAwait(false);
				}
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            }
        }
    }
}
