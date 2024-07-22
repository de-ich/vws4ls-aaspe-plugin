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
using static AasxPluginVws4ls.VersionUtils;

namespace AasxPluginVws4ls
{
    /// <summary>
    /// This class allows to create a new version of a (set of) selected submodel(s) or an AAS.
    /// </summary>
    public class NewVersionCreator
    {
        public NewVersionCreator(
            AasCore.Aas3_0.Environment env,
            Vws4lsOptions options)
        {
            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        protected AasCore.Aas3_0.Environment env;
        protected Vws4lsOptions options;

        public void ValidateSelection(IEnumerable<object> selectedElements)
        {
            if (selectedElements.Count() == 0)
            {
                throw new ArgumentException("Nothing selected!");
            } else if (selectedElements.Count() == 1)
            {
                if (selectedElements.First() is not IIdentifiable)
                {
                    throw new ArgumentException("Invalid selection: Either an AAS or a submodel must be selected!");
                }
            } else
            {
                throw new ArgumentException("Invalid selection: Only a single element may be selected!");
            }
        }

        public IIdentifiable CreateNewVersion(
            object aasOrReferenceToSubmodel, string newVersion, bool deleteOldVersion = true)
        {

            if (aasOrReferenceToSubmodel is IReference referenceToSubmodel)
            {
                var submodel = env.FindReferableByReference(referenceToSubmodel) as ISubmodel;

                if (submodel == null)
                {
                    throw new ArgumentException("Unable to determine selected submodel!");
                }

                var containingAas = env.AssetAdministrationShells.FirstOrDefault(aas => aas.Submodels.Contains(referenceToSubmodel));

                if (containingAas == null)
                {
                    throw new ArgumentException("Unable to determine AAS containing the selected submodel!");
                }

                var newSubmodelVersion = CreateNewSubmodelVersion(submodel, newVersion);

                env.Submodels.Add(newSubmodelVersion);
                containingAas.Submodels.Add(newSubmodelVersion.GetReference());

                if (deleteOldVersion)
                {
                    containingAas.Submodels.Remove(referenceToSubmodel);
                }

                return newSubmodelVersion;

            } else if (aasOrReferenceToSubmodel is IAssetAdministrationShell aas)
            {
                var newAasVersion = CreateNewAasVersion(aas, newVersion);

                env.AssetAdministrationShells.Add(newAasVersion);

                return newAasVersion;
            } else
            {
                throw new ArgumentException("Unable to create new version. Encountered unknown type of Identifiable!");
            }
        }

        private ISubmodel CreateNewSubmodelVersion(ISubmodel submodel, string newVersion)
        {
            var hostName = GetSubjectId(submodel.Id);
            var clone = DeepCloneSubmodel(submodel, options.GetTemplateIdSubmodel(hostName));

            SetVersion(clone, newVersion);
            SetReferenceToPreviousVersion(submodel, clone);

            return clone;
        }

        private IAssetAdministrationShell CreateNewAasVersion(IAssetAdministrationShell aas, string newVersion)
        {
            var hostName = GetSubjectId(aas.Id);
            var clone = CloneAas(aas, options.GetTemplateIdAas(hostName));

            SetVersion(clone, newVersion);
            SetReferenceToPreviousVersion(aas, clone);

            return clone;
        }

        private static void SetVersion(IIdentifiable identifiable, string version)
        {
            var administration = identifiable.Administration ?? new AdministrativeInformation();
            administration.Version = version;
            administration.Revision = null;

            identifiable.Administration = administration;
        }

        private static void SetReferenceToPreviousVersion(IIdentifiable previousVersion, IIdentifiable currentVersion)
        {
            var existingPreviousVersionExtension = GetPreviousVersionExtension(currentVersion);
            if (existingPreviousVersionExtension != null)
            {
                currentVersion.Extensions.Remove(existingPreviousVersionExtension);
            }

            AddReferenceToPreviousVersion(currentVersion, previousVersion);
        }
    }
}
