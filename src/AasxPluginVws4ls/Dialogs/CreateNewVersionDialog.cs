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
    public static class CreateNewVersionDialog
    {
        public class CreatNewVersionDialogResult
        {
            public string Version { get; set; } = string.Empty;
            public bool DeleteOldVersion { get; set; } = true;
        }

        public static async Task<CreatNewVersionDialogResult> DetermineCreateNewVersionConfiguration(
            AnyUiContextPlusDialogs displayContext,
            string currentVersion,
            bool showDeleteOldVersionOption)
        {

            var dialogResult = new CreatNewVersionDialogResult()
            {
                Version = currentVersion
            };

            var uc = new AnyUiDialogueDataModalPanel("Configure New Version");
            uc.ActivateRenderPanel(dialogResult,
                (uci) => RenderMainDialogPanel(dialogResult, showDeleteOldVersionOption)
            );

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return null;
            }

            return dialogResult;
            ;

        }

        private static AnyUiPanel RenderMainDialogPanel(CreatNewVersionDialogResult dialogResult, bool showDeleteOldVersionOption)
        {
            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(2, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            // specify verison
            helper.AddSmallLabelTo(grid, 0, 0, content: "Version:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 0, 1, text: dialogResult.Version),
                (text) => { dialogResult.Version = text; }
            );

            // specify whether to delete old version of a submodel
            if (showDeleteOldVersionOption)
            {
                helper.AddSmallLabelTo(grid, 1, 0, content: "Delete (reference to) old version:");
                AnyUiUIElement.SetBoolFromControl(
                    helper.AddSmallCheckBoxTo(grid, 1, 1, isChecked: dialogResult.DeleteOldVersion),
                    (isChecked) => { dialogResult.DeleteOldVersion = isChecked;}
                );
            }

            return panel;
        }
    }
}
