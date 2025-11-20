namespace BigFourApp.Services
{
    public class BlobStorageOptions
    {
        public string? ConnectionString { get; set; }
        public string ContainerName { get; set; } = "uploads";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ConnectionString) &&
            !string.IsNullOrWhiteSpace(ContainerName);
    }
}
