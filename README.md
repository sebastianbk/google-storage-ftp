# FTP server for Google Cloud Storage

This application sets up an FTP server that communicates with Google Cloud Storage on the back end.
It provides an easy way for you to set up an FTP interface for your Google Cloud Storage bucket so that you can upload, download, and manage your GCS files using your favorite FTP client.

It supports FTP over TLS (explicit encryption) and, if you wish to make use of that functionality, you should bring your SSL/TLS certificate formatted as PKCS #12 (`.pfx`).

The server is written in .NET Core 2.2 and is most easily built and hosted using Docker. It makes use of Fubar Development's [fantastic FTP server](https://github.com/FubarDevelopment/FtpServer/) written for .NET Standard 2.0. If you do not know about that project, you should really go and check it out.

## Things to change before building

There are a few things you should change before building and running the server.

### 1. Installing the .NET Core 2.2 SDK

If you do not already have the .NET Core 2.2 SDK installed on your computer, you should download it from [Microsoft's website](https://dotnet.microsoft.com/download/dotnet-core/2.2). It's free and runs on Windows, macOS, and Linux.

### 2. Authentication

You should provide the environment variable  USERNAME and PWD when running the docker run command.

### 3. Bucket name

You should also provide the environment variable BUCKET_NAME  for your Google Cloud Storage bucket when running the docker run command.

### 4. Service account

In order to access your bucket, the server must have the private key of a service account with either `roles/storage.objectAdmin` or `roles/storage.admin` rights to the bucket.

You can create a service account from Google Cloud Console. Check out [this guide](https://cloud.google.com/iam/docs/creating-managing-service-account-keys) to learn how.

Once you created the service account, you should export its private key. You will get a `.json` file, which should be included in the Docker image. Update the following lines in the `Dockerfile` to match the name of your `.json` file.

```Docker
COPY my-service-account.json ./
ENV GOOGLE_APPLICATION_CREDENTIALS /app/my-service-account.json
```

Alternatively, if you do not wish to use Docker, you can simply set the environment variable `GOOGLE_APPLICATION_CREDENTIALS` to the full path of your `.json` file on the system where you want to run the server.

### 5. FTP over TLS

If you wish to support FTP over TLS, you must provide an SSL/TLS certificate, which should be protected with a password. By default, the certificate is expected to have the name `ftp.pfx` and be placed in the root of the project. The Docker build script will then include in the certificate in the Docker image.

You must provide the password of your `.pfx` file via the environment variable `PFX_PASSWORD`.

If you do not wish to make use of FTP over TLS, you should set environment variable `DISABLE_TLS` to `True`.

### 6. Logging

The server makes use of NLog. It can be combined with [Stackdriver Logging](https://cloud.google.com/logging/). In the `NLog.config` file replace `[YOUR PROJECT ID]` and `[YOUR_LOG_ID]` with your Google Cloud project ID and the desired ID of your logs, respectively.

## Building the server

Once you have completed all of the steps above, you can build the server really easily using Docker by running the following command:

```bash
docker build -t sebastianbk/google-storage-ftp .
```

If you do not wish to use Docker, you can use the `dotnet` command line tools. Try to run the following command:

```bash
dotnet build -c Release
```

## Running the server

Finally, run the server using Docker by executing this command:

```bash
docker run -d -it \
    -p 20:20 \
    -p 21:21 \
    -p 989:989 \
    -p 990:990 \
    -p 10000:10000 \
    -p 10001:10001 \
    -p 10002:10002 \
    -p 10003:10003 \
    -p 10004:10004 \
    -p 10005:10005 \
    -p 10006:10006 \
    -p 10007:10007 \
    -p 10008:10008 \
    -p 10009:10009 \
    -e DISABLE_TLS=True \
    -e PUBLIC_IP=192.168.117.11 \
    -e BUCKET_NAME=the-bucket-name \
    -e USERNAME=username \
    -e PWD=password \
    -e GOOGLE_APPLICATION_CREDENTIALS=/app/my-service-account.json \
    -v /root/serviceaccount-bucket.json:/app/my-service-account.json:ro \
    -v /root/NLog.config:/app/NLog.config:ro \
    sebastianbk/google-storage-ftp \
    ftp
```

If you decided to make use of FTP over TLS, you should include the following argument to the command above:

```bash
    -e PFX_PASSWORD=[YOUR_PFX_PASSWORD]
```

If you do not wish to use FTP over TLS, include this argument instead:

```bash
    -e DISABLE_TLS=True
```

If you are running the server on an IP different from `127.0.0.1`, you should also include this argument to the `docker run` command.

```bash
    -e PUBLIC_IP=[YOUR_IP]
```

After executing the command, your server should be running. You can test it out by using your favorite FTP client. If you don't already have one installed, a good choice is [FileZilla](https://filezilla-project.org).

## Support

If you run into any issues or if you have suggestions for how to improve the server, please either submit an issue or create a pull request with your suggested changes. I welcome any feedback to the project. :smiley: