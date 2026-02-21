namespace ContosoDashboard.Models;

public enum DocumentScanStatus
{
    Pending,
    Clean,
    Rejected
}

public enum DocumentActivityType
{
    Upload,
    Download,
    Preview,
    MetadataEdit,
    Replace,
    Delete,
    Share
}
