using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

public class Replacer
{
    private readonly string rootPath;

    public Replacer(string rootPath)
    {
        this.rootPath = rootPath;
        if (!Directory.Exists(this.rootPath))
        {
            throw new DirectoryNotFoundException("Root directory not found: " + this.rootPath);
        }
    }

    public void PerformBackup(List<string> replacements)
    {
        Console.WriteLine(" [ Backing up files ]");
        var backupDir = Path.Combine(this.rootPath, "_bak");
        if (Directory.Exists(backupDir))
        {
            Directory.Delete(backupDir, true);
        }

        Directory.CreateDirectory(backupDir);
        
        foreach (var path in replacements)
        {
            Console.WriteLine("Backing up " + Path.GetFileName(path) + "...");
            var fullPath = Path.Combine(this.rootPath, path);
            var backupPath = Path.Combine(backupDir, Path.GetRelativePath(this.rootPath, fullPath));
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
            File.Copy(fullPath, backupPath, true);
        }
    }

    public string GetBackupDir()
    {
        return Path.Combine(this.rootPath, "_bak");
    }

    public void RestoreBackup(string target = null)
    {
        Console.WriteLine(" [ Restoring backup ] ");
        var backupDir = GetBackupDir();
        target ??= this.rootPath;
        
        foreach (var file in Directory.EnumerateFiles(backupDir, "*.*", SearchOption.AllDirectories))
        {
            Console.WriteLine("Restoring " + Path.GetFileName(file) + "...");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(target, Path.GetRelativePath(backupDir, file)))!);
            File.Copy(file, Path.Combine(target, Path.GetRelativePath(backupDir, file)), true);
        }
    }

    public void ProcessReplacements(string outputDir, Dictionary<string, string> replacements)
    {
        Console.WriteLine(" [ Patching files ] ");
        foreach (var (path, value) in replacements)
        {
            Console.WriteLine("Patching " + Path.GetFileName(path) + "...");
            var outputPath = Path.Combine(outputDir, Path.GetRelativePath(this.rootPath, path));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, value);
        }
    }
    
    public Dictionary<string, string> GetAllReplacements()
    {
        Console.WriteLine(" [ Evaluating replacements ] ");
        
        var diffNodesDict = ResolveDiffNodesFromFiles();
        var outputFilesDict = EvaluateAllReplacements(diffNodesDict);

        return outputFilesDict;
    }

    private Dictionary<string, string> EvaluateAllReplacements(Dictionary<string, List<DiffNode>> diffNodesDict)
    {
        var outputFilesDict = new Dictionary<string, string>();
        foreach (var (filePath, diffNodes) in diffNodesDict)
        {
            var relativePath = Path.GetRelativePath(this.rootPath, filePath);
            var postfix = diffNodes.Count > 1 ? $" ({diffNodes.Count} patches)" : "";
            Console.WriteLine($"Patching {relativePath}{postfix}...");
            var fileText = File.ReadAllText(filePath);

            foreach (var (diffNode, _) in diffNodes.OrderBy(n => n.Order))
            {
                fileText = PerformXmlReplacements(fileText, diffNode);
                outputFilesDict[filePath] = fileText;
            }
        }

        return outputFilesDict;
    }

    private Dictionary<string, List<DiffNode>> ResolveDiffNodesFromFiles()
    {
        var diffNodesDict = new Dictionary<string, List<DiffNode>>();
        var templatePaths = Directory.GetFiles("replacements", "*.xml", SearchOption.AllDirectories);
        foreach (var templatePath in templatePaths)
        {
            TryResolveDiffNodes(templatePath, diffNodesDict);
        }

        return diffNodesDict;
    }

    private void TryResolveDiffNodes(string templatePath, Dictionary<string, List<DiffNode>> output)
    {
        try
        {
            Console.WriteLine("Evaluating updates from " + Path.GetFileName(templatePath) + "...");
            ResolveDiffNodes(templatePath, output);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error occurred while reading template {Path.GetFileName(templatePath)}");
            Console.WriteLine(e);
        }
    }

    private void ResolveDiffNodes(string templatePath, Dictionary<string, List<DiffNode>> output)
    {
        var doc = ReadXml(templatePath);
        var diffNodes = doc.SelectNodes("Diff|DiffCollection/Diff");
        if (diffNodes == null || diffNodes.Count == 0)
        {
            throw new Exception("Diff node not found");
        }

        var nodes = diffNodes.Cast<XmlElement>().ToList();
        foreach (var diffNode in nodes)
        {
            var fileAttr = diffNode.Attributes?["file"];
            if (fileAttr == null)
            {
                throw new Exception("\"file\" attribute not found");
            }

            var filePath = Path.Combine(this.rootPath, fileAttr.Value!);
            if (!File.Exists(filePath))
            {
                throw new Exception($"File {filePath} not found");
            }

            int.TryParse(diffNode.Attributes?["order"]?.Value, out var order);
            var item = new DiffNode(diffNode, order);
            
            // add node to output
            if (output.TryGetValue(filePath, out var diffCollection))
            {
                diffCollection.Add(item);
            }
            else
            {
                output[filePath] = new List<DiffNode> { item };
            }
        }
    }
    
    private string PerformXmlReplacements(string fileText, XmlNode diff)
    {
        XDocument? fileDoc = null;

        void EnsureFileDoc()
        {
            if (fileDoc == null)
            {
                fileDoc = XDocument.Parse(fileText);
            }
        }
        
        foreach (var node in diff.ChildNodes)
        {
            if (node is not XmlElement replaceNode)
            {
                continue;
            }
            
            switch (replaceNode.LocalName)
            {
                case "replace":
                {
                    EnsureFileDoc();
                    
                    var sel = replaceNode.GetAttribute("sel");
                    var asNode = replaceNode.GetAttribute("asNode") == "true";
                    var value = replaceNode.InnerXml;

                    if (string.IsNullOrEmpty(sel) || string.IsNullOrEmpty(value))
                    {
                        throw new Exception("Invalid replace node");
                    }

                    var nodes = fileDoc.EnumerateXPathObjects(sel).ToList();
                    if (nodes.Count == 0)
                    {
                        Console.WriteLine($"Cannot find node to replace: {sel}");
                    }
                    foreach (var selectNode in nodes)
                    {
                        if (selectNode is XAttribute attr)
                        {
                            attr.Value = value;
                        }
                        else if (selectNode is XElement el)
                        {
                            if (asNode)
                            {
                                el.ReplaceWith(XElement.Parse(value));
                            }
                            else
                            {
                                el.RemoveNodes();
                                foreach (var child in replaceNode.ChildNodes)
                                {
                                    if (child is XmlText text)
                                    {
                                        el.Add(text.Value);
                                    }
                                    else
                                    {
                                        el.Add(XElement.Parse(value));
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid node ({0}) selected {1}", selectNode.GetType(), sel);
                        }
                    }

                    fileText = fileDoc.ToString();
                    break;
                }
                case "add":
                {
                    EnsureFileDoc();
                    
                    var sel = replaceNode.GetAttribute("sel");
                    var value = replaceNode.InnerXml;
                    var afterSel = replaceNode.GetAttribute("after");

                    if (string.IsNullOrEmpty(sel) || string.IsNullOrEmpty(value))
                    {
                        throw new Exception("Invalid add node");
                    }

                    foreach (var selectNode in fileDoc.EnumerateXPathObjects(sel))
                    {
                        if (selectNode is XElement el)
                        {
                            var frag = XElement.Parse(value);
                            if (string.IsNullOrEmpty(afterSel))
                            {
                                el.Add(frag);
                            }
                            else
                            {
                                el.Document.XPathSelectElement(afterSel).AddAfterSelf(frag);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid node ({0}) selected {1}", selectNode.GetType(), sel);
                        }
                    }

                    fileText = fileDoc.ToString();
                    break;
                }
                case "text-replace":
                {
                    var sel = replaceNode.GetAttribute("sel");
                    var value = replaceNode.InnerText;
                    var ignoreCase = replaceNode.GetAttribute("ignore-case") == "true";

                    if (string.IsNullOrEmpty(sel) || string.IsNullOrEmpty(value))
                    {
                        throw new Exception("Invalid replace node");
                    }

                    fileText = fileText.Replace(sel, value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                    if (fileDoc != null)
                    {
                        fileDoc = XDocument.Parse(fileText);
                    }
                    break;
                }
                default:
                    Console.WriteLine("Invalid node {0}", replaceNode.LocalName);
                    break;
            }
        }
        
        return fileText;
    }

    
    private static XmlDocument ReadXml(string path)
    {
        using var stream = File.OpenRead(path);
        var doc = new XmlDocument();
        doc.Load(stream);
        return doc;
    }

    private record DiffNode(XmlElement XmlNode, int Order);
}