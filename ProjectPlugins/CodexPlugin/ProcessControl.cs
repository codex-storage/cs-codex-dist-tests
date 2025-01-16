using Logging;

namespace CodexPlugin
{
    public interface IProcessControl
    {
        void Stop(ICodexInstance instance, bool waitTillStopped);
        IDownloadedLog DownloadLog(ICodexInstance instance, LogFile file);
        void DeleteDataDirFolder(ICodexInstance instance);
    }


    //public void DeleteDataDirFolder()
    //{
    //    try
    //    {
    //        var dataDirVar = container.Recipe.EnvVars.Single(e => e.Name == "CODEX_DATA_DIR");
    //        var dataDir = dataDirVar.Value;
    //        var workflow = tools.CreateWorkflow();
    //        workflow.ExecuteCommand(container, "rm", "-Rfv", $"/codex/{dataDir}/repo");
    //        log.Log("Deleted repo folder.");
    //    }
    //    catch (Exception e)
    //    {
    //        log.Log("Unable to delete repo folder: " + e);
    //    }
    //}

}
