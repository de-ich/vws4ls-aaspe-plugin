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
    /// This class allows to associate a set of subassemblies to a configuration (that can e.g. be ordered by an OEM).
    /// </summary>
    public class SubassemblyToConfigurationAssociator
    {
        public SubassemblyToConfigurationAssociator(
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

        // things specified in 'AssociateSubassemblies(...)
        protected IEnumerable<Entity> subassembliesToAssociate;
        protected IEntity configuration;

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

            if (selectedEntities.Any(e => !e.RepresentsSubAssembly()))
            {
                throw new ArgumentException("Only entities from a manufacturing BOM may be selected!");
            }
        }

        public void AssociateSubassemblies(
            IEnumerable<Entity> subassembliesToAssociate,
            IEntity configuration)
        {
            this.subassembliesToAssociate = subassembliesToAssociate ?? throw new ArgumentNullException(nameof(subassembliesToAssociate));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            ValidateSelection(subassembliesToAssociate);

            DoAssociatedSubassemblies(subassembliesToAssociate, configuration);
        }

        private static void DoAssociatedSubassemblies(IEnumerable<Entity> subassembliesToAssociate, IEntity configuration)
        {
            foreach (var subassembly in subassembliesToAssociate)
            {
                // create the node for the subassembly in the configuration bom
                var subassemblyInConfigurationBom = CreateNode(subassembly, configuration);

                // linke the node for the subassembly in the configuration bom with the subassembly in the manufacturing bom
                CreateSameAsRelationship(subassemblyInConfigurationBom, subassembly);
            }
        }
    }
}
