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
    public static class CreatePartNumberSpecificAssetIdDialog
    {
        public class CreatePartNumberSpecificAssetIdDialogResult
        {
            public string SubjectId { get; set; } = string.Empty;
            public string PartNumber { get; set; } = string.Empty;
        }

        public static async Task<CreatePartNumberSpecificAssetIdDialogResult> DetermineCreatePartNumberSpecificAssetIdConfiguration(
            AnyUiContextPlusDialogs displayContext,
            string subjectId)
        {

            var dialogResult = new CreatePartNumberSpecificAssetIdDialogResult()
            {
                SubjectId = subjectId ?? "https://www.example.com",
            };

            var uc = new AnyUiDialogueDataModalPanel("Configure Specific Asset ID");
            uc.ActivateRenderPanel(dialogResult,
                (uci) => RenderMainDialogPanel(dialogResult)
            );

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return null;
            }

            return dialogResult;
            ;

        }

        private static AnyUiPanel RenderMainDialogPanel(CreatePartNumberSpecificAssetIdDialogResult dialogResult)
        {
            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(2, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            // specify subject id
            helper.AddSmallLabelTo(grid, 0, 0, content: "Subject ID:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 0, 1, text: dialogResult.SubjectId),
                (text) => { dialogResult.SubjectId = text; }
            );

            // specify part number
            helper.AddSmallLabelTo(grid, 1, 0, content: "Part Number:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 1, 1, text: dialogResult.PartNumber),
                (text) => { dialogResult.PartNumber = text; }
            );

            return panel;
        }
    }
}
