using System;

namespace ShortURL.Model
{
    [Serializable]
    public class IdAndLink
    {
        public int? Id { get; set; }
        public string Link { get; set; }
    }
}