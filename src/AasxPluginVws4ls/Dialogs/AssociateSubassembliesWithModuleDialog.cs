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
using static AasxPluginVws4ls.SubassemblyUtils;

namespace AasxPluginVws4ls.AnyUi
{
    public static class AssociateSubassembliesWithModuleDialog
    {
        public class AssociateSubassembliesWithConfigurationDialogResult
        {
            public IEntity SelectedConfiguration { get; set; }
        }

        public static async Task<AssociateSubassembliesWithConfigurationDialogResult> DetermineAssociateSubassembliesWithModuleConfiguration(
            AnyUiContextPlusDialogs displayContext,
            IEnumerable<Entity> entitiesToBeMadeSubassembly,
            IAssetAdministrationShell aas,
            AasCore.Aas3_0.Environment env)
        {
            var configurationBoms = FindBomSubmodels(env, aas).Where(sm => sm.IsConfigurationBom());
            var configurationsToSelect = configurationBoms.SelectMany(sm => sm.FindEntryNode().GetChildEntities());

            var dialogData = new AssociateSubassembliesWithConfigurationDialogResult();
            
            var uc = new AnyUiDialogueDataModalPanel("Select Configuration");
            uc.ActivateRenderPanel(
                dialogData,
                (uci) => RenderMainDialogPanel(configurationsToSelect, dialogData)
            );

            if(!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return null;
            }

            return dialogData;

        }

        private static AnyUiPanel RenderMainDialogPanel(IEnumerable<IEntity> configurationEntitiesToSelect, AssociateSubassembliesWithConfigurationDialogResult dialogData)
        {
            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(2, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            var configurationNames = configurationEntitiesToSelect.Select(e => e.IdShort).ToArray();

            // specify module
            helper.AddSmallLabelTo(grid, 0, 0, content: "Configuration to associate with selected subassemblies:");
            AnyUiUIElement.RegisterControl(
                helper.AddSmallComboBoxTo(grid, 0, 1, items: configurationNames),
                (text) =>
                {
                    dialogData.SelectedConfiguration = configurationEntitiesToSelect.First(s => s.IdShort == text);
                    return new AnyUiLambdaActionNone();
                }
            );

            return panel;
        }
    }
}
