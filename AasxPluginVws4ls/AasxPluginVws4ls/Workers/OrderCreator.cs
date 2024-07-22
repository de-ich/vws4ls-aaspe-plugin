/*
Copyright (c) 2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Matthias Freund

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AasxIntegrationBase;
using AdminShellNS;
using AasCore.Aas3_0;
using Extensions;
using static AasxPluginVws4ls.BomSMUtils;
using static AasxPluginVws4ls.VecSMUtils;
using static AasxPluginVws4ls.BasicAasUtils;
using static AasxPluginVws4ls.SubassemblyUtils;

namespace AasxPluginVws4ls
{
    /// <summary>
    /// This class allows to create an order based on a set of selected orderable modules.
    /// </summary>
    public class OrderCreator
    {
        public OrderCreator(
            AasCore.Aas3_0.Environment env,
            IAssetAdministrationShell aas,
            Vws4lsOptions options)
        {
            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.aas = aas ?? throw new ArgumentNullException(nameof(aas));   
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        protected AasCore.Aas3_0.Environment env;
        protected IAssetAdministrationShell aas;
        protected Vws4lsOptions options;

        // things specified in 'CreateOrder(...)
        protected IEnumerable<Entity> selectedConfigurations;
        protected string orderNumber;

        // the bom models and elements in the existing AAS
        protected ISubmodel existingConfigurationBom;
        protected IEnumerable<IEntity> subassembliesAssociatedWithSelectedConfigurations;

        // the aas to be created (representing the specific order)
        protected AssetAdministrationShell orderAas;

        // te models in the new aas to be created (representing the order)
        protected Submodel orderConfigurationBom;
        protected Submodel orderManufacturingBom;

        public void ValidateSelection(IEnumerable<IEntity> selectedElements)
        {
            if (selectedElements.Any(e => e is not IEntity))
            {
                throw new ArgumentException("Only entities may be selected!");
            }

            var selectedEntities = selectedElements.Select(e => e as IEntity);

            var allBomSubmodels = FindBomSubmodels(env, aas);
            // make sure all parents are set for all potential submodels involved in this action
            allBomSubmodels.ToList().ForEach(sm => sm.SetAllParents());

            if (selectedEntities.Any(e => !e.RepresentsConfiguration()))
            {
                throw new ArgumentException("Only entities from a configuration BOM may be selected!");
            }

            var submodelsContainingSelectedEntities = FindCommonSubmodelParents(selectedEntities);

            if (submodelsContainingSelectedEntities.Count == 0)
            {
                throw new ArgumentException("Unable to determine configuration BOM(s) that contain(s) the selected entities!");
            }

            if (submodelsContainingSelectedEntities.Count > 1)
            {
                throw new ArgumentException("Entities from more than one configuration BOMs selected. This is not supported!");
            }
        }

        public IAssetAdministrationShell CreateOrder(
            IEnumerable<Entity> selectedConfigurations,
            string orderNumber)
        {
            this.selectedConfigurations = selectedConfigurations ?? throw new ArgumentNullException(nameof(selectedConfigurations));
            this.orderNumber = orderNumber ?? throw new ArgumentNullException(nameof(orderNumber));

            ValidateSelection(selectedConfigurations);

            DoCreateOrder(selectedConfigurations, orderNumber);

            return orderAas;
        }

        private void DoCreateOrder(IEnumerable<Entity> selectedConfigurations, string orderNumber)
        {
            DetermineExistingSubmodels();

            // create the new aas representing the order
            var orderAasIdShort = aas.IdShort + "_Order_" + orderNumber;
            orderAas = CreateAAS(orderAasIdShort, options.GetTemplateIdAas(aas.GetSubjectId()), options.GetTemplateIdAsset(aas.GetSubjectId()), env);
            orderAas.DerivedFrom = aas.GetReference();

            // create the configuration bom in the new aas
            orderConfigurationBom = CreateBomSubmodel(ID_SHORT_CONFIGURATION_BOM_SM, options.GetTemplateIdSubmodel(orderAas.GetSubjectId()), aas: orderAas, env: env);

            foreach (var configuration in selectedConfigurations)
            {
                // create the configuration in the order configuration bom
                var configurationInOrderConfigurationBom = CreateNode(configuration, orderConfigurationBom.FindEntryNode());

                // link the entity repesenting the configuration in the order configuration bom to the original configuration bom
                CreateSameAsRelationship(configurationInOrderConfigurationBom, configuration);
            }

            // create the manufacturing bom in the new aas
            orderManufacturingBom = CreateBomSubmodel(ID_SHORT_MANUFACTURING_BOM_SM, options.GetTemplateIdSubmodel(orderAas.GetSubjectId()), aas: orderAas, env: env);

            foreach (var associatedSubassembly in subassembliesAssociatedWithSelectedConfigurations)
            {
                // create the entity in the new manufacturing bom
                var subassemblyInOrderManufacturingBom = CreateNode(associatedSubassembly, orderManufacturingBom.FindEntryNode());
                subassemblyInOrderManufacturingBom.GlobalAssetId = null; // reset the global asset id because we do not yet now the specific subassembly instance used in product
                subassemblyInOrderManufacturingBom.EntityType = EntityType.SelfManagedEntity; // set to 'self-managed' although we do not yet now the specific instance that will be used in production

                // link the entity in the new manufacturing bom to the subassembly in the original manufacturing bom
                CreateSameAsRelationship(subassemblyInOrderManufacturingBom, associatedSubassembly);
            }
        }

        private void DetermineExistingSubmodels()
        {
            existingConfigurationBom = selectedConfigurations.FirstOrDefault(e => e.RepresentsConfiguration()).GetParentSubmodel();

            if (existingConfigurationBom == null)
            {
                throw new Exception("Internal Error: Unable to determine configuration BOM that contains the selected configurations!");
            }

            subassembliesAssociatedWithSelectedConfigurations = selectedConfigurations.SelectMany(m => FindAssociatedSubassemblies(m, env)).ToHashSet();

            if (subassembliesAssociatedWithSelectedConfigurations.Any(s => s == null))
            {
                throw new Exception("Internal Error: At least one subassembly associated with a selected module could not be determined!");
            }
        }
    }
}
