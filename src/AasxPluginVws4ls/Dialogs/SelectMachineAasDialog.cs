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
using static AasxPluginVws4ls.BomSMUtils;

namespace AasxPluginVws4ls.AnyUi
{
    public static class SelectMachineAasDialog
    {
        public class SelectMachineAasDialogResult
        {
            public IAssetAdministrationShell MachineAas { get; set; }
        }

        public static async Task<SelectMachineAasDialogResult> DetermineCapabilityMatcherConfiguration(
            AnyUiContextPlusDialogs displayContext,
            AasCore.Aas3_0.Environment environment)
        {
            var dialogResult = new SelectMachineAasDialogResult();

            var potentialShells = environment.AssetAdministrationShells;


            var uc = new AnyUiDialogueDataModalPanel("Select Machine AAS");
            uc.ActivateRenderPanel(
                dialogResult,
                (uci) => RenderMainDialogPanel(potentialShells, environment, dialogResult, uc)
            );

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return null;
            }

            return dialogResult;

        }

        private static AnyUiPanel RenderMainDialogPanel(IEnumerable<IAssetAdministrationShell> potentialShellsToSelect, AasCore.Aas3_0.Environment environment, SelectMachineAasDialogResult dialogResult, AnyUiDialogueDataModalPanel parentPanel)
        {
            var shellNames = potentialShellsToSelect.Select(s => s.IdShort).ToArray();

            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(2, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            // specify subassembly to reuse
            helper.AddSmallLabelTo(grid, 1, 0, content: "Machine AAS to Check:");
            int? selectedIndex = dialogResult.MachineAas == null ? null : shellNames.ToList().IndexOf(dialogResult.MachineAas?.IdShort);
            AnyUiUIElement.RegisterControl(
                helper.AddSmallComboBoxTo(grid, 1, 1, items: shellNames, selectedIndex: selectedIndex),
                (text) =>
                {
                    dialogResult.MachineAas = potentialShellsToSelect.First(s => s.IdShort == text);
                    return new AnyUiLambdaActionModalPanelReRender(parentPanel);
                }
            );

            return panel;
        }
    }
}
