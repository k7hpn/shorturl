namespace ShortURL.Model
{
    public class ApplicationConfiguration
    {
        public string DatabaseProvider { get; set; }
        public string DefaultLink { get; set; }
        public string DistributedCache { get; set; }
        public string DistributedCacheConfiguration { get; set; }
        public string DistributedCacheDiscriminator { get; set; }
        public string Instance { get; set; }
        public string RequestLogging { get; set; }
        public string ReverseProxy { get; set; }
        public string SeqApiKey { get; set; }
        public string SeqEndpoint { get; set; }
    }
}