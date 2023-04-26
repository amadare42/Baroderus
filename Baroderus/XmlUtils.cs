using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

public static class XmlUtils
{
    public static IEnumerable<XObject> EnumerateXPathObjects(this XDocument document, string xpath)
    {
        var result = document.XPathEvaluate(xpath);
        return ((IEnumerable<object>)result).Cast<XObject>();
    }

    public static string PrintXML(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        return PrintXml(doc);
    }
    
    public static string PrintXml(this XmlDocument document)
    {
        string result = "";
    
        MemoryStream mStream = new MemoryStream();
        XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode);
    
        try
        {
            writer.Formatting = Formatting.Indented;
    
            // Write the XML into a formatting XmlTextWriter
            document.WriteContentTo(writer);
            writer.Flush();
            mStream.Flush();
    
            // Have to rewind the MemoryStream in order to read
            // its contents.
            mStream.Position = 0;
    
            // Read MemoryStream contents into a StreamReader.
            StreamReader sReader = new StreamReader(mStream);
    
            // Extract the text from the StreamReader.
            string formattedXml = sReader.ReadToEnd();
    
            result = formattedXml;
        }
        catch (XmlException)
        {
            // Handle the exception
        }
    
        mStream.Close();
        writer.Close();
    
        return result;
    }
}