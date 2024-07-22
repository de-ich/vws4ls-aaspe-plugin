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
using static AasxPluginVws4ls.CapabilitySMUtils;
using static AasxPluginVws4ls.BomSMUtils;
using Namotion.Reflection;
using AasxPackageLogic;

using static AasxPluginVws4ls.CapabilitySMUtils;
using static AasxPluginVws4ls.Vws4lsCapabilitySMUtils;

namespace AasxPluginVws4ls
{
    /// <summary>
    /// This class allows to create a required capability.
    /// </summary>
    public class RequiredCapabilityCreator
    {
        public RequiredCapabilityCreator(
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
                if (selectedElements.First() is not ISubmodel)
                {
                    throw new ArgumentException("Invalid selection: A RequiredCapability SM needs to be selected!");
                }

                if (!(selectedElements.First() as ISubmodel).IsRequiredCapabilitySubmodel())
                {
                    throw new ArgumentException("Invalid selection: A RequiredCapability SM needs to be selected!");
                }
            } else
            {
                throw new ArgumentException("Invalid selection: Only a single element may be selected!");
            }
        }

        public ISubmodelElementCollection CreateRequiredCapability(
            ISubmodel requiredCapabilitySM, string capabilitySemanticId, Dictionary<string, object> properties)
        {
            var capabilityName = capabilitySemanticId.Split("/").Last();
            var capabilityConstraints = ConstraintsByCapability[capabilitySemanticId];

            var capabilitySet = FindCapabilitySet(requiredCapabilitySM, true);

            var capabilityContainer = CreateCapabilityContainer(capabilitySet, capabilityName, capabilitySemanticId);

            var propertySet = capabilityContainer.FindPropertySet();

            foreach (var propertyName in properties.Keys)
            {
                var propertyDefinition = capabilityConstraints.FirstOrDefault(p => p.Name == propertyName);

                var property = propertySet.CreateProperty(propertyName, properties[propertyName], propertyDefinition.ValueType);
            }

            return capabilityContainer;
        }
    }
}
