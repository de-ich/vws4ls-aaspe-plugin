using AasCore.Aas3_0;
using AasxIntegrationBase;
using AnyUi;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginVws4ls.AnyUi
{
    public static class DeriveAasDialog
    {
        public class DeriveAasDialogResult
        {
            public string NameOfDerivedAas { get; set; } = string.Empty;
            public string SubjectID { get; set; } = string.Empty;
            public string PartNumber { get; set; } = string.Empty;
        }

        public static async Task<DeriveAasDialogResult> DetermineDeriveAasConfiguration(
            AnyUiContextPlusDialogs displayContext,
            IAssetAdministrationShell aasToDeriveFrom)
        {
            var idShortOfAasToDeriveFrom = aasToDeriveFrom.IdShort;
            var specficAssetIdForOwnPartNumber = aasToDeriveFrom.GetPartNumberSpecificAssetId();
            var partNumberOfAasToDeriveFrom = specficAssetIdForOwnPartNumber?.Value ?? "";

            var dialogResult = new DeriveAasDialogResult();
            
            var uc = new AnyUiDialogueDataModalPanel("Configure Derive AAS");
            uc.ActivateRenderPanel(dialogResult,
                (uci) => RenderMainDialogPanel(idShortOfAasToDeriveFrom, partNumberOfAasToDeriveFrom, dialogResult)
            );

            if(!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return null;
            }

            return dialogResult;
;

        }

        private static AnyUiPanel RenderMainDialogPanel(string nameOfAasToDeriveFrom, string partNumberOfAasToDeriveFrom, DeriveAasDialogResult dialogResult)
        {
            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(3, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            // specify name of the derived AAS
            helper.AddSmallLabelTo(grid, 0, 0, content: "Name of the Derived AAS:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 0, 1, text: nameOfAasToDeriveFrom),
                (text) => { dialogResult.NameOfDerivedAas = text; }
            );

            // specify part number of the derived AAS
            helper.AddSmallLabelTo(grid, 1, 0, content: "Part Number for the Derived Asset:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 1, 1, text: partNumberOfAasToDeriveFrom),
                (text) => { dialogResult.PartNumber = text; }
            );

            // specify subject id of the derived AAS
            helper.AddSmallLabelTo(grid, 2, 0, content: "Subject ID for the Derived AAS:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 2, 1, text: "https://www.my-company.com"),
                (text) => { dialogResult.SubjectID = text; }
            );

            return panel;
        }
    }
}
