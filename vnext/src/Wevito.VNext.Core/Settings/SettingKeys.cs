namespace Wevito.VNext.Core.Settings;

public static class SettingKeys
{
    public const string LocalDocumentRetrievalEnabled = "local_document_retrieval_enabled";
    public const string LocalDocumentRetrievalRoot = "local_document_retrieval_root";
    public const string LocalDocumentRetrievalMaxFileBytes = "local_document_retrieval_max_file_bytes";
    public const string LocalDocumentRetrievalDefaultMaxFileBytes = "4194304";

    public static string DefaultLocalDocumentRetrievalRoot()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, "Documents", "WevitoLocalNotes");
    }

    public static string DefaultLocalDocumentIndexPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WevitoVNext",
            "local-doc-index",
            "index.db");
    }
}
