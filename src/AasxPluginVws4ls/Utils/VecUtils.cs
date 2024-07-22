using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AasCore.Aas3_0;
using Extensions;
using static AasxPluginVws4ls.BomSMUtils;
using System.Xml.Linq;

namespace AasxPluginVws4ls
{
    public class VecProvider

    {

        public XDocument VecFile { get; }
        public List<XElement> HarnessDescriptions { get; }
        public Dictionary<string, XElement> PartVersionsById { get; }
        protected XElement VecContent;

        public VecProvider(string pathToVecFile)
        {
            this.VecFile = ParseVecFile(pathToVecFile);
            this.VecContent = GetVecContent(this.VecFile);
            this.HarnessDescriptions = FindHarnessDescriptions();
            this.PartVersionsById = new Dictionary<string, XElement>();
            foreach (var partVersion in FindPartVersions())
            {
                this.PartVersionsById.Add(GetXmlId(partVersion), partVersion);
            }
        }

        public string GetPartNumber(string partVersionId)
        {
            XElement partVersion = null;
            this.PartVersionsById.TryGetValue(partVersionId, out partVersion);

            return partVersion?.Element(XName.Get("PartNumber"))?.Value;
        }

        public static XDocument ParseVecFile(string pathToVecFile)
        {
            return XDocument.Load(pathToVecFile);
        }

        protected XElement GetVecContent(XDocument vecFile)
        {
            return vecFile.Element(XName.Get("VecContent", "http://www.prostep.org/ecad-if/2011/vec"));
        }

        protected List<XElement> FindHarnessDescriptions()
        {
            if (this.VecContent == null)
            {
                throw new Exception("Unable to find harness descriptions because VecContent is not set!");
            }

            return this.VecContent.Elements(XName.Get("DocumentVersion")).Where(doc => doc.Element(XName.Get("DocumentType"))?.Value == "HarnessDescription").ToList();
        }

        public static List<XElement> FindCompositionSpecifications(XElement harnessDescription)
        {
            return harnessDescription.Elements(XName.Get("Specification")).
                            Where(spec => spec.Attribute(XName.Get("type", "http://www.w3.org/2001/XMLSchema-instance"))?.Value == "vec:CompositionSpecification").ToList();
        }

        public static List<XElement> FindComponentsInComposition(XElement compositionSpecification)
        {
            return compositionSpecification.Elements(XName.Get("Component")).ToList();
        }

        public static bool IsPartWithSubComponents(XElement component)
        {
            var roles = component.Elements(XName.Get("Role")).ToList();

            return roles.FirstOrDefault(r => r.Attribute(XName.Get("type", "http://www.w3.org/2001/XMLSchema-instance"))?.Value == "vec:PartWithSubComponentsRole") != null;
        }

        public static List<XElement> FindPartStructureSpecifications(XElement harnessDescription)
        {
            return harnessDescription.Elements(XName.Get("Specification")).
                            Where(spec => spec.Attribute(XName.Get("type", "http://www.w3.org/2001/XMLSchema-instance"))?.Value == "vec:PartStructureSpecification").ToList();
        }

        public static List<string> FindIdsOfContainedParts(XElement partStructureSpecification)
        {
            var inBillOfMaterial = partStructureSpecification.Element(XName.Get("InBillOfMaterial"))?.Value;

            return inBillOfMaterial?.Split(' ').ToList() ?? new List<string>();
        }

        protected List<XElement> FindPartVersions()
        {
            if (this.VecContent == null)
            {
                throw new Exception("Unable to find part versions because VecContent is not set!");
            }

            return this.VecContent.Elements(XName.Get("PartVersion")).ToList();
        }

        public static Dictionary<string, string> GetDescription(XElement partVersion)
        {
            var descriptions = new Dictionary<string, string>();

            foreach (var description in partVersion?.Elements(XName.Get("Description")))
            {
                var language = description.Element(XName.Get("LanguageCode"))?.Value;
                var value = description.Element(XName.Get("Value"))?.Value;
                
                if (language != null && value != null)
                {
                    descriptions[language] = value;
                }

            }

            return descriptions;
        }

        public static string GetPartNumber(XElement partVersion)
        {
            return partVersion?.Element(XName.Get("PartNumber"))?.Value;
        }

        public static string GetIdentification(XElement element)
        {
            return element.Element(XName.Get("Identification"))?.Value ?? null;
        }

        public static string GetPartId(XElement component)
        {
            return component.Element(XName.Get("Part"))?.Value ?? null;
        }

        public static string GetCompanyName(XElement documentVersionElement)
        {
            string companyName = documentVersionElement.Element(XName.Get("CompanyName"))?.Value ?? null;

            if (companyName == null)
            {
                throw new Exception("Unable to determine CompanyName of harness description!");
            }

            return companyName;
        }

        public static string GetDocumentNumber(XElement documentVersionElement)
        {
            string documentNumber = documentVersionElement.Element(XName.Get("DocumentNumber"))?.Value ?? null;

            if (documentNumber == null)
            {
                throw new Exception("Unable to determine DocumentNumber of harness description!");
            }

            return documentNumber;
        }

        public static string GetDocumentVersion(XElement documentVersionElement)
        {
            string documentVersion = documentVersionElement.Element(XName.Get("DocumentVersion"))?.Value ?? null;

            if (documentVersion == null)
            {
                throw new Exception("Unable to determine DocumentVersion of harness description!");
            }

            return documentVersion;
        }

        public static string GetElementFragment(XElement element)
        {
            string name = element.Name.LocalName;

            if (name == "DocumentVersion")
            {
                string companyName = GetCompanyName(element);
                string documentNumber = GetDocumentNumber(element);
                string documentVersion = GetDocumentVersion(element);

                return $"//DocumentVersion[./CompanyName='{companyName}'][./DocumentNumber='{documentNumber}']​[./DocumentVersion='{documentVersion}']";
            }

            string identification = GetIdentification(element);

            if (identification != null)
            {
                return GetElementFragment(element.Parent) + $"/{name}[./Identification='{identification}']";
            }

            throw new Exception($"Unable to compile XPath fragment for element type {name}!");

        }

        public static string GetXmlId(XElement element)
        {
            return element.Attribute(XName.Get("id"))?.Value;
        }
    }
}
