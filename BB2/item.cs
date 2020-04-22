using System.Xml.Serialization;

namespace BB2
{
    public class item
    {
        [XmlAttribute]
        public string key;
        [XmlAttribute]
        public decimal value;
    }
}
