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
    public static class ReuseSubassemblyDialog
    {
        public class ReuseSubassemblyDialogResult
        {
            public string SubassemblyEntityName { get; set; } = string.Empty;

            public IAssetAdministrationShell AasToReuse { get; set; }
            public Dictionary<string, string> PartNames { get; } = new Dictionary<string, string>();
        }

        public static async Task<ReuseSubassemblyDialogResult> DetermineReuseSubassemblyConfiguration(
            AnyUiContextPlusDialogs displayContext,
            IEnumerable<Entity> entitiesToBeMadeSubassembly,
            AasCore.Aas3_0.Environment environment)
        {
            var dialogResult = InitializeDialogResult(entitiesToBeMadeSubassembly);

            var potentialSubassemblyShellsToReuse = environment.AssetAdministrationShells;


            var uc = new AnyUiDialogueDataModalPanel("Configure Subassembly");
            uc.ActivateRenderPanel(
                dialogResult,
                (uci) => RenderMainDialogPanel(entitiesToBeMadeSubassembly, potentialSubassemblyShellsToReuse, environment, dialogResult, uc)
            );

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return null;
            }

            return dialogResult;

        }

        private static ReuseSubassemblyDialogResult InitializeDialogResult(IEnumerable<Entity> entitiesToBeMadeSubassembly)
        {
            return new ReuseSubassemblyDialogResult
            {
                SubassemblyEntityName = "Subassembly_" + string.Join("_", entitiesToBeMadeSubassembly.Select(e => e.IdShort))
            };
        }

        private static AnyUiPanel RenderMainDialogPanel(IEnumerable<Entity> entitiesToBeMadeSubassembly, IEnumerable<IAssetAdministrationShell> potentialSubassemblyShellsToReuse, AasCore.Aas3_0.Environment environment, ReuseSubassemblyDialogResult dialogResult, AnyUiDialogueDataModalPanel parentPanel)
        {
            var shellNames = potentialSubassemblyShellsToReuse.Select(s => s.IdShort).ToArray();
            var selectedEntityNames = entitiesToBeMadeSubassembly.Select(e => e.IdShort).ToArray();

            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(3, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            var mapPartsGrid = helper.AddSmallGrid(2, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));

            // specify subassembly entity name
            helper.AddSmallLabelTo(grid, 0, 0, content: "Name of subassembly instance in the manufacturing BOM of existing AAS:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 0, 1, text: dialogResult.SubassemblyEntityName),
                (text) => { dialogResult.SubassemblyEntityName = text; }
            );

            // specify subassembly to reuse
            helper.AddSmallLabelTo(grid, 1, 0, content: "Subassembly AAS to Reuse:");
            int? selectedIndex = dialogResult.AasToReuse == null ? null : shellNames.ToList().IndexOf(dialogResult.AasToReuse?.IdShort);
            AnyUiUIElement.RegisterControl(
                helper.AddSmallComboBoxTo(grid, 1, 1, items: shellNames, selectedIndex: selectedIndex),
                (text) =>
                {
                    dialogResult.AasToReuse = potentialSubassemblyShellsToReuse.First(s => s.IdShort == text);
                    return new AnyUiLambdaActionModalPanelReRender(parentPanel);
                }
            );

            if (dialogResult.AasToReuse == null)
            {
                return panel;
            }

            // specify name of subassembly parts
            var lab = new AnyUiSelectableTextBlock();
            lab.Text = "How should the parts of the subassembly be mapped to the selected components?";
            panel.Add(lab);
            var mapPartsPanel = new AnyUiStackPanel();
            panel.Add(mapPartsPanel);

            mapPartsPanel.Add(mapPartsGrid);

            var productBom = FindBomSubmodels(environment, dialogResult.AasToReuse).FirstOrDefault(sm => sm.IsProductBom());
            var atomicComponentEntities = productBom?.FindEntryNode()?.GetChildEntities() ?? new List<IEntity>();

            foreach (var entity in atomicComponentEntities)
            {
                mapPartsGrid.RowDefinitions.Add(new AnyUiRowDefinition());
                var currentRow = mapPartsGrid.RowDefinitions.Count() - 1;
                helper.AddSmallLabelTo(mapPartsGrid, currentRow, 0, content: entity.IdShort);
                AnyUiUIElement.RegisterControl(
                    helper.AddSmallComboBoxTo(mapPartsGrid, currentRow, 1, items: selectedEntityNames),
                    (text) =>
                    {
                        dialogResult.PartNames[text as string] = entity.IdShort;
                        return new AnyUiLambdaActionNone();
                    }
                );
            }


            return panel;
        }
    }
}
