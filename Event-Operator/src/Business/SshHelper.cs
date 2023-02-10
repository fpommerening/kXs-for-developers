using Renci.SshNet;

namespace FP.ContainerTraining.EventOperator.Business;

public class SshHelper
{
    public static async Task<byte[]> GetRemoteFile(string host, string user, string password, string filePath)
    {
        using var sftp = new SftpClient(host, user, password);
        sftp.Connect();

        byte[] data;

        await using (var memoryStream = new MemoryStream())
        {
            sftp.DownloadFile(filePath, memoryStream);
            await memoryStream.FlushAsync();

            data = memoryStream.ToArray();
        }

        sftp.Disconnect();

        return data;
    }

    public static async Task<byte[]> GetRemoteFile(string host, string user, string sshKeyPath, string sshKeyPassword,
        string filePath)
    {
        using var sftp = new SftpClient(host, user, new PrivateKeyFile(sshKeyPath, sshKeyPassword));
        sftp.Connect();

        byte[] data;

        await using (var memoryStream = new MemoryStream())
        {
            sftp.DownloadFile(filePath, memoryStream);
            await memoryStream.FlushAsync();
            data = memoryStream.ToArray();
        }

        sftp.Disconnect();
        return data;
    }
}
