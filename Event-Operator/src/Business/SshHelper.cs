using System.IO;
using System.Threading.Tasks;
using Renci.SshNet;

namespace FP.ContainerTraining.EventOperator.Business
{
    public class SshHelper
    {
        public static async Task<byte[]> GetRemoteFile(string host, string user, string password, string path)
        {
            using var sftp = new SftpClient(host, user, password);
            sftp.Connect();

            byte[] data;

            await using (var memoryStream = new MemoryStream())
            {
                sftp.DownloadFile(path, memoryStream);
                await memoryStream.FlushAsync();

                data = memoryStream.ToArray();
            }

            sftp.Disconnect();

            return data;
        }
    }
}
