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
    public static class DeriveSubassemblyDialog
    {
        public class DeriveSubassemblyDialogResult
        {
            public string SubassemblyAASName { get; set; } = string.Empty;
            public string SubassemblyEntityName { get; set; } = string.Empty;
            public Dictionary<string, string> PartNames { get; } = new Dictionary<string, string>();
        }

        public static async Task<DeriveSubassemblyDialogResult> DetermineDeriveSubassemblyConfiguration(
            AnyUiContextPlusDialogs displayContext,
            IEnumerable<Entity> entitiesToBeMadeSubassembly)
        {
            DeriveSubassemblyDialogResult dialogResult = InitializeDialogResult(entitiesToBeMadeSubassembly);

            var uc = new AnyUiDialogueDataModalPanel("Configure Subassembly");
            uc.ActivateRenderPanel(
                dialogResult,
                (uci) => RenderMainDialogPanel(entitiesToBeMadeSubassembly, dialogResult)
            );

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return null;
            }

            return dialogResult;
        }

        private static DeriveSubassemblyDialogResult InitializeDialogResult(IEnumerable<Entity> entitiesToBeMadeSubassembly)
        {
            string joinedEntityIdShorts = string.Join("_", entitiesToBeMadeSubassembly.Select(e => e.IdShort));

            var dialogResult = new DeriveSubassemblyDialogResult
            {
                SubassemblyAASName = "AAS_" + joinedEntityIdShorts,
                SubassemblyEntityName = "Subassembly_" + joinedEntityIdShorts,
            };

            foreach (var entity in entitiesToBeMadeSubassembly)
            {
                dialogResult.PartNames[entity.IdShort] = entity.IdShort;
            }

            return dialogResult;
        }

        private static AnyUiPanel RenderMainDialogPanel(IEnumerable<Entity> entitiesToBeMadeSubassembly, DeriveSubassemblyDialogResult dialogResult)
        {
            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(3, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            // specify subassembly entity name
            helper.AddSmallLabelTo(grid, 0, 0, content: "Name of subassembly instance in the manufacturing BOM of existing AAS:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 0, 1, text: dialogResult.SubassemblyEntityName),
                (text) => { dialogResult.SubassemblyEntityName = text; }
            );

            // specify subassembly aas name
            helper.AddSmallLabelTo(grid, 1, 0, content: "Name of subassembly AAS to be created:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 1, 1, text: dialogResult.SubassemblyAASName),
                (text) => { dialogResult.SubassemblyAASName = text; }
            );

            helper.AddSmallLabelTo(grid, 2, 0, content: "How should the parts of the subassembly be named in the subassembly AAS?");

            // specify name of subassembly parts
            foreach (var entity in entitiesToBeMadeSubassembly)
            {
                grid.RowDefinitions.Add(new AnyUiRowDefinition());
                var currentRow = grid.RowDefinitions.Count() - 1;
                helper.AddSmallLabelTo(grid, currentRow, 0, content: entity.IdShort);
                AnyUiUIElement.SetStringFromControl(
                    helper.AddSmallTextBoxTo(grid, currentRow, 1, text: entity.IdShort),
                    (text) => { dialogResult.PartNames[entity.IdShort] = text; }
                );
            }

            return panel;
        }

    }
}
