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
    /// This class allows to derive a subassembly based on a set of selected entities.
    /// The entities need to be part of either a product BOM or a manufacturing BOM submodel - a mix of both is allowed as well.
    /// 
    /// Naming conventions:
    /// - The AAS/submodels containing the selected entities are prefixed 'original' (e.g. 'originalManufacturingBom')
    /// - The AAS/submodels containing the new subassembly to be created are prefixed 'new' (e.g. 'newManufacturingBom')
    /// - If a selected entity already represents a subassembly (that is to be incorporated in the 'new' subassembly), this AAS
    ///   and the respective submodels/elements are called 'source' (e.g. 'sourceManufacturingBom').
    /// - The building blocks of a subassembly (either an existing one or the new one to be created) are called 'part'.
    /// </summary>
    public class SubassemblyDeriver
    {

        public SubassemblyDeriver(
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

        // things specified in 'DeriveSubassembly(...)
        protected IEnumerable<IEntity> entitiesToBeMadeSubassembly;
        protected string newSubassemblyAasName;
        protected string nameOfSubassemblyEntityInOriginalMbom;
        protected Dictionary<string, string> newPartNamesByOriginalPartNames;

        // the bom models and elements in the existing AAS
        protected ISubmodel originalProductBom;
        protected ISubmodel originalManufacturingBom;
        protected IEntity subassemblyInOriginalManufacturingBom;

        // the new AAS to be created (representing the subassembly)
        protected AssetAdministrationShell newSubassemblyAas;

        // the models in the new AAS to be created (representing the subassembly)
        protected Submodel newVecSubmodel;
        protected ISubmodel newProductBom;
        protected ISubmodel newManufacturingBom;

        // Ensure the selected 'entitiesToBeMadeSubassembly' represent suitable elements
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

            if (selectedEntities.Any(e => !e.RepresentsSubAssembly() && !e.RepresentsBasicComponent()))
            {
                throw new ArgumentException("Only entities from a product BOM and/or a manufacturing BOM may be selected!");
            }

            var submodelsContainingSelectedEntities = FindCommonSubmodelParents(selectedEntities);

            if (submodelsContainingSelectedEntities.Count == 0)
            {
                throw new ArgumentException("Unable to determine product BOM(s) that contain(s) the selected entities!");
            }

            if (submodelsContainingSelectedEntities.Count > 2)
            {
                throw new ArgumentException("Entities from more than 2 product BOMs selected. This is not supported!");
            }
        }

        public IEntity DeriveSubassembly(
            IEnumerable<Entity> entitiesToBeMadeSubassembly,
            string newSubassemblyAasName,
            string nameOfSubassemblyEntityInOriginalMbom,
            Dictionary<string, string> newPartNamesByOriginalPartNames)
        {
            this.entitiesToBeMadeSubassembly = entitiesToBeMadeSubassembly ?? throw new ArgumentNullException(nameof(entitiesToBeMadeSubassembly));
            this.newSubassemblyAasName = newSubassemblyAasName ?? throw new ArgumentNullException(nameof(newSubassemblyAasName));
            this.nameOfSubassemblyEntityInOriginalMbom = nameOfSubassemblyEntityInOriginalMbom ?? throw new ArgumentNullException(nameof(nameOfSubassemblyEntityInOriginalMbom));
            this.newPartNamesByOriginalPartNames = newPartNamesByOriginalPartNames ?? throw new ArgumentNullException(nameof(newPartNamesByOriginalPartNames));

            ValidateSelection(entitiesToBeMadeSubassembly);

            DoDeriveSubassembly();

            return subassemblyInOriginalManufacturingBom;
        }

        private void DoDeriveSubassembly()
        {
            InitializeSubmodelsInOriginalAas();

            CreateNewAasAndInitializeSubmodels();

            // create the entity representing the subassembly in the original mbom
            subassemblyInOriginalManufacturingBom = CreateNode(nameOfSubassemblyEntityInOriginalMbom, originalManufacturingBom.FindEntryNode(), newSubassemblyAas, true);

            // for each entity to be incorporated into the subassembly create the required elements and relationships
            foreach (var entity in entitiesToBeMadeSubassembly)
            {
                if (IsPartOfWireHarnessBom(entity))
                {
                    // a single part from the product bom
                    AddSimpleComponentToSubassembly(entity);

                }
                else if (IsPartOfWireHarnessMBom(entity))
                {
                    // a subassembly entity (from the manufacturing bom) that consists of one or multiple parts
                    AddSubassemblyComponentToSubassembly(entity);
                }
            }
        }

        private void InitializeSubmodelsInOriginalAas()
        {
            originalManufacturingBom = this.entitiesToBeMadeSubassembly.FirstOrDefault(e => e.RepresentsSubAssembly()).GetParentSubmodel();
            originalProductBom = this.entitiesToBeMadeSubassembly.FirstOrDefault(e => e.RepresentsBasicComponent()).GetParentSubmodel();

            if (originalProductBom == null && originalManufacturingBom == null)
            {
                throw new Exception("Internal Error: Unable to determine existing product and manufacturing BOM!");
            }

            // no bom submodel was selected so we look for an existing one in the aas that is associated with the selected mbom submodel
            originalProductBom ??= FindProductBom(originalManufacturingBom, aas, env);

            // if we still did not find the product bom in focus, there was some kind of error
            if (originalProductBom == null)
            {
                throw new Exception("Internal Error: Unable to determine existing product BOM!");
            }

            // no mbom submodel was selected so we look for an existing one in the aas that is associated with the selected bom submodel
            originalManufacturingBom ??= FindManufacturingBom(originalProductBom, aas, env);

            // no mbom submodel was found in the aas so we create a new one
            originalManufacturingBom ??= CreateManufacturingBom(options.GetTemplateIdSubmodel(aas.GetSubjectId()), originalProductBom, aas, env);
        }

        public void CreateNewAasAndInitializeSubmodels()
        {
            var referencedVecFileSMEs = entitiesToBeMadeSubassembly.Select(e => e.FindReferencedVecFileSME(env, aas)).Where(v => v != null);

            if (referencedVecFileSMEs.ToHashSet().Count > 1)
            {
                throw new Exception("Unable to determine VEC file referenced by the BOM submodel(s)!");
            }

            var existingVecFileSME = referencedVecFileSMEs.FirstOrDefault();

            // the AAS for the new sub-assembly
            this.newSubassemblyAas = CreateAAS(this.newSubassemblyAasName, options.GetTemplateIdAas(aas.GetSubjectId()), options.GetTemplateIdAsset(aas.GetSubjectId()), env, AssetKind.Type);

            if (existingVecFileSME != null)
            {
                // FIXME probably, we should not just copy the whole existing VEC file but extract the relevant parts only into a new file
                newVecSubmodel = InitializeVecSubmodel(newSubassemblyAas, env, existingVecFileSME);
            }

            newProductBom = CreateBomSubmodel(ID_SHORT_PRODUCT_BOM_SM, options.GetTemplateIdSubmodel(aas.GetSubjectId()), aas: newSubassemblyAas, env: env, supplementarySemanticId: SEM_ID_PRODUCT_BOM_SM);
            CopyVecRelationship(originalManufacturingBom.FindEntryNode(), newProductBom.FindEntryNode());

            newManufacturingBom = CreateManufacturingBom(options.GetTemplateIdSubmodel(aas.GetSubjectId()), newProductBom, aas: newSubassemblyAas, env: env);
        }

        private void AddSimpleComponentToSubassembly(IEntity simpleComponentToAdd)
        {
            var partIdShortInNewAas = this.newPartNamesByOriginalPartNames[simpleComponentToAdd.IdShort];

            // create the part of the subassembly in the original mbom
            var partInOriginalMBom = CreateNode(simpleComponentToAdd, subassemblyInOriginalManufacturingBom);

            // link the entity in the original mbom to the part in the original product bom
            CreateSameAsRelationship(partInOriginalMBom, simpleComponentToAdd);

            // create the entity in the new product bom
            var partInNewBom = CreateNode(simpleComponentToAdd, newProductBom.FindEntryNode(), partIdShortInNewAas);
            CopyVecRelationship(simpleComponentToAdd, partInNewBom);

            // create the entity in the new mbom
            var subassemblyInNewMBom = CreateNode(simpleComponentToAdd, newManufacturingBom.FindEntryNode(), partIdShortInNewAas);

            // link the entity in the new mbom to the part in the new product bom
            CreateSameAsRelationship(subassemblyInNewMBom, partInNewBom);

            // link the part in the original mbom to the part in the new mbom
            CreateSameAsRelationship(partInOriginalMBom, partInNewBom);
        }

        private void AddSubassemblyComponentToSubassembly(IEntity subassemblyComponentToAdd)
        {
            var subassemblyIdShortInNewAas = this.newPartNamesByOriginalPartNames[subassemblyComponentToAdd.IdShort];

            var existingSubassemblyAas = env.AssetAdministrationShells.FirstOrDefault(aas => aas.AssetInformation.GlobalAssetId == subassemblyComponentToAdd.GlobalAssetId);

            if (existingSubassemblyAas == null)
            {
                throw new Exception("Unable to determine referenced AAS for selected entity!");
            }

            var mbomSubmodelInExistingSubassemblyAas = FindManufacturingBom(existingSubassemblyAas, env);
            var bomSubmodelInExistingSubassemblyAas = FindProductBom(existingSubassemblyAas, env);

            // create the entity for the subassembly in the new mbom
            var subassemblyInNewMBom = CreateNode(subassemblyComponentToAdd, newManufacturingBom.FindEntryNode(), subassemblyIdShortInNewAas);

            var partsOfSelectedSubassembly = subassemblyComponentToAdd.GetChildEntities();
            foreach (var part in partsOfSelectedSubassembly)
            {
                var partInOriginalBom = part.GetSameAsEntity(env, originalProductBom);
                var partInBomOfExistingSubassembly = part.GetSameAsEntity(env, bomSubmodelInExistingSubassemblyAas);

                // create the entity in the original mbom
                var partInOriginalMBom = CreateNode(partInOriginalBom, subassemblyInOriginalManufacturingBom);

                // link the entity in the original mbom to the part in the original bom
                CreateSameAsRelationship(partInOriginalMBom, partInOriginalBom);

                // create the entity in the new bom
                var partInNewBom = CreateNode(partInBomOfExistingSubassembly, newProductBom.FindEntryNode());
                CopyVecRelationship(partInOriginalBom, partInNewBom);

                // create the entity in the new mbom
                var partInNewMBom = CreateNode(partInNewBom, subassemblyInNewMBom);

                // link the part in the original mbom to the part in the new bom
                CreateSameAsRelationship(partInOriginalMBom, partInNewBom);

                // link the part in the new mbom to the part in the new bom
                CreateSameAsRelationship(partInNewMBom, partInNewBom);

                // link the part in the new mbom to the part in the source bom
                CreateSameAsRelationship(partInNewMBom, partInBomOfExistingSubassembly);
            }

            // delete the old subassembly that is now incorporated in the new subassembly
            var hasPartRelationshipToSelectedEntity = subassemblyComponentToAdd.GetHasPartRelationshipFromParent();
            originalManufacturingBom.FindEntryNode().Remove(subassemblyComponentToAdd);
            originalManufacturingBom.FindEntryNode().Remove(hasPartRelationshipToSelectedEntity);
        }
        
        private void CopyVecRelationship(IEntity partEntityInOriginalAAS, IEntity partEntityInNewAAS)
        {
            var vecRelationship = partEntityInOriginalAAS.GetVecRelationship(env, aas);
            if (vecRelationship != null)
            {
                var xpathToVecElement = vecRelationship.First.Keys.Last().Value;
                var vecFileElement = newVecSubmodel.GetVecFileElement();
                CreateVecRelationship(partEntityInNewAAS, xpathToVecElement, vecFileElement);
            }
        }

        protected Submodel InitializeVecSubmodel(AssetAdministrationShell aas, AasCore.Aas3_0.Environment env, AasCore.Aas3_0.File existingVecFileSME)
        {
            // create the VEC submodel
            return CreateVecSubmodel(existingVecFileSME, options.GetTemplateIdSubmodel(aas.GetSubjectId()), aas, env);
        }

        protected bool IsPartOfWireHarnessBom(IEntity entity)
        {
            return entity.GetParentSubmodel() == this.originalProductBom;
        }

        protected bool IsPartOfWireHarnessMBom(IEntity entity)
        {
            return entity.GetParentSubmodel() == this.originalManufacturingBom;
        }
    }
}
