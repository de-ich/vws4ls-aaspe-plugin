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
    /// This class allows to reuse an existing subassembly for a set of selected entities
    /// in a product BOM submodel.
    /// 
    /// Naming conventions:
    /// - The AAS/submodels containing the selected entities are prefixed 'original' (e.g. 'originalManufacturingBom')
    /// - The AAS/submodels containing the subassembly to be reused are prefixed 'reused' (e.g. 'reusedManufacturingBom')
    /// - The building blocks of a subassembly (either an existing one or the new one to be created) are called 'part'.
    /// </summary>
    public class SubassemblyReuser
    {
        public SubassemblyReuser(
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

        // things specified in 'ReuseSubassembly(...)
        protected IEnumerable<Entity> entitiesToBeMadeSubassembly;
        protected IAssetAdministrationShell subassemblyAasToReuse;
        protected string nameOfSubassemblyEntityInOriginalMbom;
        protected Dictionary<string, string> reusedPartNamesByOriginalPartNames;

        // the bom models and elements in the existing AAS
        protected ISubmodel existingProductBom;
        protected ISubmodel existingManufacturingBom;
        protected IEntity subassemblyInOriginalManufacturingBom;

        // the models in the AAS to be reused (representing the subassembly)
        protected ISubmodel reusedProductBom;

        public void ValidateSelection(IEnumerable<object> selectedElements)
        {
            if (selectedElements.Any(e => e is not IEntity))
            {
                throw new ArgumentException("Only entities may be selected!");
            }

            var selectedEntities = selectedElements.Select(e => e as IEntity);

            var allBomSubmodels = FindBomSubmodels(env, aas);
            // make sure all parents are set for all potential submodels involved in this action
            allBomSubmodels.ToList().ForEach(sm => sm.SetAllParents());

            if (selectedEntities.Any(e => !e.RepresentsBasicComponent()))
            {
                throw new ArgumentException("Only entities from a product BOM may be selected!");
            }

            var submodelsContainingSelectedEntities = FindCommonSubmodelParents(selectedEntities);

            if (submodelsContainingSelectedEntities.Count == 0)
            {
                throw new ArgumentException("Unable to determine product BOM that contains the selected entities!");
            }

            if (submodelsContainingSelectedEntities.Count > 1)
            {
                throw new ArgumentException("Entities from more than one product BOM selected. This is not supported!");
            }
        }

        public IEntity ReuseSubassembly(
            IEnumerable<Entity> entitiesToBeMadeSubassembly,
            IAssetAdministrationShell subassemblyAasToReuse,
            string nameOfSubassemblyEntityInOriginalMbom,
            Dictionary<string, string> reusedPartNamesByOriginalPartNames)
        {
            this.entitiesToBeMadeSubassembly = entitiesToBeMadeSubassembly ?? throw new ArgumentNullException(nameof(entitiesToBeMadeSubassembly));
            this.subassemblyAasToReuse = subassemblyAasToReuse ?? throw new ArgumentNullException(nameof(subassemblyAasToReuse));
            this.nameOfSubassemblyEntityInOriginalMbom = nameOfSubassemblyEntityInOriginalMbom ?? throw new ArgumentNullException(nameof(nameOfSubassemblyEntityInOriginalMbom));
            this.reusedPartNamesByOriginalPartNames = reusedPartNamesByOriginalPartNames ?? throw new ArgumentNullException(nameof(reusedPartNamesByOriginalPartNames));

            ValidateSelection(entitiesToBeMadeSubassembly);

            DoReuseSubassembly();

            return subassemblyInOriginalManufacturingBom;
        }

        private void DoReuseSubassembly()
        {
            InitializeSubmodelsInOriginalAas();

            DetermineSubmodelsInAasToReuse();

            // create the entity representing the subassembly in the orginal mbom
            subassemblyInOriginalManufacturingBom = CreateNode(nameOfSubassemblyEntityInOriginalMbom, existingManufacturingBom.FindEntryNode(), subassemblyAasToReuse, true);

            var partsInReusedProductBom = reusedProductBom.FindEntryNode()?.GetChildEntities();
            foreach (var entity in entitiesToBeMadeSubassembly)
            {
                // create the part of the subassembly in the original mbom
                var partInOriginalMBom = CreateNode(entity, subassemblyInOriginalManufacturingBom);

                // link the entity in the original mbom to the part in the original product bom
                CreateSameAsRelationship(partInOriginalMBom, entity);

                // determine the entity in the reused product bom that represents the selected entity in the original product bom
                var idShort = this.reusedPartNamesByOriginalPartNames[entity.IdShort];
                var partInReusedProductBom = partsInReusedProductBom?.First(e => e.IdShort == idShort);

                // link the entity in the original mbom to eh part in the reused product bom
                CreateSameAsRelationship(partInOriginalMBom, partInReusedProductBom);
            }
        }

        private bool InitializeSubmodelsInOriginalAas()
        {
            existingProductBom = entitiesToBeMadeSubassembly.First().GetParentSubmodel();

            if (existingProductBom == null || !existingProductBom.IsProductBom())
            {
                throw new Exception("Internal Error: Unable to determine the product BOM that contains the selected entities!");
            }

            // look for an existing mbom submodel in the existing aas
            existingManufacturingBom = FindManufacturingBom(aas, env);

            // no mbom submodel was found in the aas so we create a new one
            existingManufacturingBom ??= CreateManufacturingBom(options.GetTemplateIdSubmodel(aas.GetSubjectId()), existingProductBom, aas, env);

            return true;
        }

        private void DetermineSubmodelsInAasToReuse()
        {
            reusedProductBom = FindFirstBomSubmodel(env, subassemblyAasToReuse);
            reusedProductBom.SetAllParents();
        }
    }
}
