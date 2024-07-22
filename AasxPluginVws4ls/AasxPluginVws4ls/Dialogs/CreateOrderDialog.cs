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
    public static class CreateOrderDialog
    {
        public class CreateOrderDialogResult
        {
            public string OrderNumber { get; set; } = string.Empty;
        }

        public static async Task<CreateOrderDialogResult> DetermineCreateOrderConfiguration(
            AnyUiContextPlusDialogs displayContext)
        {
            
            var dialogResult = new CreateOrderDialogResult();
            
            var uc = new AnyUiDialogueDataModalPanel("Configure Order");
            uc.ActivateRenderPanel(dialogResult,
                (uci) => RenderMainDialogPanel(dialogResult)
            );

            if(!(await displayContext.StartFlyoverModalAsync(uc)))
            {
                return null;
            }

            return dialogResult;
;

        }

        private static AnyUiPanel RenderMainDialogPanel(CreateOrderDialogResult dialogResult)
        {
            var panel = new AnyUiStackPanel();
            var helper = new AnyUiSmallWidgetToolkit();

            var grid = helper.AddSmallGrid(1, 2, new[] { "200", "*" }, padding: new AnyUiThickness(0, 5, 0, 5));
            panel.Add(grid);

            // specify subassembly entity name
            helper.AddSmallLabelTo(grid, 0, 0, content: "Order Number:");
            AnyUiUIElement.SetStringFromControl(
                helper.AddSmallTextBoxTo(grid, 0, 1, text: dialogResult.OrderNumber),
                (text) => { dialogResult.OrderNumber = text; }
            );

            return panel;
        }
    }
}
