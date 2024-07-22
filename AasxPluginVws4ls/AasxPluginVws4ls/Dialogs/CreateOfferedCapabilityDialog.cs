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

using static AasxPluginVws4ls.Vws4lsCapabilitySMUtils;

namespace AasxPluginVws4ls.AnyUi
{
    public static class CreateOfferedCapabilityDialog
    {
        public class CreateOfferedCapabilityDialogResult
        {
            public string CapabilitySemanticId { get; set; } = string.Empty;
            public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        }

        public static async Task<CreateOfferedCapabilityDialogResult> DetermineCreateOfferedCapabilityConfiguration(
            AnyUiContextPlusDialogs displayContext)
        {

            var dialogResult = new CreateOfferedCapabilityDialogResult()
            {
                CapabilitySemanticId = SEM_ID_CAP_CUT,
                Properties = ConstraintsByCapability[SEM_ID_CAP_CUT].ToDictionary(p => p.Name, p => (object)null)
            };

            var uc = new AnyUiDialogueDataModalPanel("Configure Offer Capabillity");
            uc.ActivateRenderPanel(dialogResult,
                (uci) => RenderMainDialogPanel(dialogResult, uc)
            );

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return null;
            }

            return dialogResult;
            ;

        }

        private static AnyUiPanel RenderMainDialogPanel(CreateOfferedCapabilityDialogResult dialogResult, AnyUiDialogueDataModalPanel parentPanel)
        {
            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(2, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            var propertiesGrid = helper.AddSmallGrid(2, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));

            helper.AddSmallLabelTo(grid, 0, 0, content: "Offered Capability:");
            var capabilitySemanticIds = ConstraintsByCapability.Keys.ToList();
            int? selectedIndex = dialogResult.CapabilitySemanticId == null ? null : capabilitySemanticIds.IndexOf(dialogResult.CapabilitySemanticId);
            AnyUiUIElement.RegisterControl(
                helper.AddSmallComboBoxTo(grid, 0, 1, items: capabilitySemanticIds.ToArray(), selectedIndex: selectedIndex),
                (text) =>
                {
                    dialogResult.CapabilitySemanticId = text.ToString();
                    dialogResult.Properties = ConstraintsByCapability[dialogResult.CapabilitySemanticId].ToDictionary(p => p.Name, p => (object) null);
                    return new AnyUiLambdaActionModalPanelReRender(parentPanel);
                }
            );

            helper.AddSmallLabelTo(grid, 1, 0, content: "Property constraints:");

            var propertiesPanel = new AnyUiStackPanel();
            panel.Add(propertiesPanel);

            propertiesPanel.Add(propertiesGrid);

            foreach (var property in ConstraintsByCapability[dialogResult.CapabilitySemanticId])
            {
                propertiesGrid.RowDefinitions.Add(new AnyUiRowDefinition());
                var currentRow = propertiesGrid.RowDefinitions.Count() - 1;
                helper.AddSmallLabelTo(propertiesGrid, currentRow, 0, content: property.Name);
                AnyUiUIElement.RegisterControl(
                    helper.AddSmallTextBoxTo(propertiesGrid, currentRow, 1),
                    (text) =>
                    {
                        dialogResult.Properties[property.Name] = text as string;
                        return new AnyUiLambdaActionNone();
                    }
                );
            }

            return panel;
        }
    }
}
