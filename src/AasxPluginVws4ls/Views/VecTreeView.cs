using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Controls;
using AasCore.Aas3_0;
using Extensions;
using static AasxPluginVws4ls.VecSMUtils;
using System.Xml;

namespace AasxPluginVws4ls
{
    class VecTreeView
    {
        private AdminShellPackageEnv env;
        private Submodel submodel;
        private DockPanel panel;
        private AasCore.Aas3_0.File vecFileElement;
        private XmlDocument vecXml;

        public object FillWithWpfControls(
            AdminShellPackageEnv env, Submodel submodel, DockPanel panel)
        {
            if (env == null || submodel == null || panel == null)
            {
                return null;
            }

            this.env = env;
            this.submodel = submodel;
            this.panel = panel;

            this.vecFileElement = submodel.GetVecFileElement();

            if (this.vecFileElement == null)
            {
                return null;
            }

            LoadXmlFile();

            if (this.vecXml == null)
            {
                return null;
            }

            // create TOP controls
            var spTop = new StackPanel();
            spTop.Orientation = Orientation.Vertical;
            DockPanel.SetDock(spTop, Dock.Top);
            this.panel.Children.Add(spTop);

            var treeView = new TreeView() { Name = "vecTree" };
            treeView.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            
            foreach(var element in this.vecXml.ChildNodes.OfType<System.Xml.XmlElement>())
            {
                FillTreeviewRecursively(treeView, element);
            }

            spTop.Children.Add(treeView);

            return treeView;
        }

        private void FillTreeviewRecursively(object parent, XmlNode xmlParent)
        {
            if (xmlParent.NodeType == XmlNodeType.Text || xmlParent.NodeType == XmlNodeType.CDATA)
            {
                return;
            }
            
            var elementName = "<El> " + xmlParent.Name;
            
            var elementText = xmlParent.ChildNodes.OfType<XmlText>().DefaultIfEmpty().First()?.Value;
            if (elementText != null)
            {
                elementName = elementName + " = '" + elementText + "'";
            }

            var childItem = new TreeViewItem() { Header = elementName};
            (parent as ItemsControl)?.Items.Add(childItem);

            foreach (XmlAttribute xmlAttribute in xmlParent.Attributes)
            {
                var attributeItem = new TreeViewItem() { Header = "<Att> " + xmlAttribute.Name + " = '" + xmlAttribute.Value + "'"};
                childItem.Items.Add(attributeItem);
            }

            foreach (XmlNode xmlChild in xmlParent.ChildNodes)
            {
                FillTreeviewRecursively(childItem, xmlChild);
            }
        }

        private void LoadXmlFile()
        {
            var vecFile = env.GetListOfSupplementaryFiles().Find(f => f.Uri.ToString() == vecFileElement.Value);
            byte[] vecFileContents = env.GetByteArrayFromUriOrLocalPackage(vecFile.Uri.ToString());

            vecXml = new XmlDocument();
            string xml = Encoding.UTF8.GetString(vecFileContents);
            vecXml.LoadXml(xml);
        }
    }
}
