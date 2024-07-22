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
using static AasxPluginVws4ls.BasicAasUtils;
using static AasxPluginVws4ls.SubassemblyUtils;
using static AasxPluginVws4ls.BomSMUtils;

namespace AasxPluginVws4ls
{
    /// <summary>
    /// This class allows to derive a new AAS from an existing on which is then linked via the specific asset ID.
    /// </summary>
    public class AasDeriver
    {
        public AasDeriver(
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

        // things specified in 'DeriveAas(...)
        protected string nameOfDerivedAas;
        protected string partNumber;
        protected string subjectId;

        // the aas to be created/derived
        protected AssetAdministrationShell derivedAas;

        public IAssetAdministrationShell DeriveAas(
            string nameOfDerivedAas,
            string partNumber = null,
            string subjectId = null)
        {
            this.nameOfDerivedAas = nameOfDerivedAas ?? throw new ArgumentNullException(nameof(nameOfDerivedAas));
            this.partNumber = partNumber;
            this.subjectId = subjectId ?? aas.GetSubjectId();

            DoDeriveAas();

            return derivedAas;
        }

        private void DoDeriveAas()
        {
            // create the new (derived) aas
            derivedAas = CreateAAS(nameOfDerivedAas, options.GetTemplateIdAas(subjectId), options.GetTemplateIdAsset(subjectId), env);
            derivedAas.AssetInformation.AssetKind = aas.AssetInformation.AssetKind;
            derivedAas.Submodels = new List<IReference>();

            var specificAssetIds = new List<ISpecificAssetId>();

            // add a specific asset id for the own part number
            if (partNumber != null && subjectId != null)
            {
                var partNumberSpecificAssetId = CreatePartNumberSpecificAssetId(partNumber, subjectId);
                specificAssetIds.Add(partNumberSpecificAssetId);
            }

            // copy the specific asset ID(s) of the orignal AAS to establish a link to this AAS
            if (aas.AssetInformation.SpecificAssetIds != null)
            {
                specificAssetIds.AddRange(aas.AssetInformation.SpecificAssetIds.Copy());
            }

            if (specificAssetIds.Any())
            {
                derivedAas.AssetInformation.SpecificAssetIds = specificAssetIds;
            }

            // clone all submodels existing in the original AAS
            var existingSubmodels = aas.Submodels?.Select(smRef => GetSubmodel(smRef, env)) ?? new List<ISubmodel>();
            var clonedSubmodelsByExisting = new Dictionary<ISubmodel, ISubmodel>();
            foreach (var submodel in existingSubmodels)
            {
                var clonedSubmodel = DeepCloneSubmodel(submodel, options.GetTemplateIdSubmodel(subjectId));

                // add the cloned submodel to the aas and environment
                env.Submodels.Add(clonedSubmodel);
                derivedAas.Submodels.Add(clonedSubmodel.GetReference());

                clonedSubmodelsByExisting[submodel] = clonedSubmodel;
            }

            // update all references to point to the 'new' subject
            // NOTE: we need to do this after cloning all submodels so that inter-submodel references can be updated
            foreach (var (originalSubmodel, clonedSubmodel) in clonedSubmodelsByExisting)
            {
                UpdateReferences(originalSubmodel, clonedSubmodel);
            }
        }

        private void UpdateReferences(ISubmodel originalSubmodel, ISubmodel copy)
        {
            // update all references to point to entities/submodels/... owned by the 'new' subject
            UpdateEntityAssetReferences(copy);
            UpdateRelationships(copy, originalSubmodel, env);
            LinkEntitiesWithOriginalSubmodel(copy, originalSubmodel);
        }

        

        private void UpdateEntityAssetReferences(ISubmodel newSubmodel)
        {
            var selfManagedEntities = newSubmodel.FindDeep<IEntity>().Where(e => e.EntityType == EntityType.SelfManagedEntity);

            var assetIdOfOriginalAas = aas.AssetInformation.GlobalAssetId;
            var assetIdOfDerivedAas = derivedAas.AssetInformation.GlobalAssetId;

            foreach (var entity in selfManagedEntities)
            {
                UpdateEntityAssetReference(entity, assetIdOfOriginalAas, assetIdOfDerivedAas, env);
            }
        }

        private void UpdateEntityAssetReference(IEntity entity, string assetIdOfOriginalAas, string assetIdOfDerivedAas, AasCore.Aas3_0.Environment env)
        {
            if(entity.EntityType != EntityType.SelfManagedEntity)
            {
                // nothing to do for co-managed entites
                return;
            }

            if (entity.GlobalAssetId == assetIdOfOriginalAas)
            {
                // simply update the asset id to point to the derived asset
                entity.GlobalAssetId = assetIdOfDerivedAas;
            }
            else
            {
                // try to determine an equivalent asset in the domain of the 'new' subject id/host
                var originalEntityAssetId = entity.GlobalAssetId;
                var entityReferencesAssetOfOriginalSubject = GetSubjectId(originalEntityAssetId) == GetSubjectId(assetIdOfOriginalAas);

                if (!entityReferencesAssetOfOriginalSubject)
                {
                    return;
                }
                // replace with reference to asset in the domain of the 'new' subject
                var originalReferencedAas = env.FindAasWithAssetInformation(originalEntityAssetId);
                var originalPartNumber = originalReferencedAas.GetPartNumberSpecificAssetId()?.Value;
                    
                if (originalPartNumber == null)
                {
                    return;
                }

                var originalSubjectId = originalReferencedAas.GetSubjectId();
                var newSubjectId = GetSubjectId(derivedAas.AssetInformation.GlobalAssetId);

                var aasWithNewPartNumber = env.FindAasForPartNumber(originalPartNumber, originalSubjectId, newSubjectId);

                if (aasWithNewPartNumber == null)
                {
                    return;
                }

                entity.GlobalAssetId = aasWithNewPartNumber.AssetInformation.GlobalAssetId;
            }
        }

        private void LinkEntitiesWithOriginalSubmodel(ISubmodel newSubmodel, ISubmodel originalSubmodel)
        {
            var entities = newSubmodel.FindDeep<IEntity>();

            foreach (var entity in entities)
            {
                var idShortPath = entity.CollectIdShortByParent();
                var originalEntity = originalSubmodel.FindDeep<IEntity>().FirstOrDefault(e => e.CollectIdShortByParent() == idShortPath);

                if (originalEntity != null)
                {
                    CreateSameAsRelationship(entity, originalEntity);
                }
            }
        }
    }
}
