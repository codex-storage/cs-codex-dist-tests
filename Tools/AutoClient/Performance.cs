namespace AutoClient
{
    public class Performance
    {
        internal void DownloadFailed(Exception ex)
        {
            throw new NotImplementedException();
        }

        internal void DownloadSuccessful(long? size, TimeSpan time)
        {
            throw new NotImplementedException();
        }

        internal void StorageContractCancelled()
        {
            throw new NotImplementedException();
        }

        internal void StorageContractErrored(string error)
        {
            throw new NotImplementedException();
        }

        internal void StorageContractFinished()
        {
            throw new NotImplementedException();
        }

        internal void StorageContractStarted()
        {
            throw new NotImplementedException();
        }

        internal void UploadFailed(Exception exc)
        {
            throw new NotImplementedException();
        }

        internal void UploadSuccessful(long length, TimeSpan time)
        {
            throw new NotImplementedException();
        }
    }
}
