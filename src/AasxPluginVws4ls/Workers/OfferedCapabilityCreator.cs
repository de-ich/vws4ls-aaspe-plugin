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
    public class OfferedCapabilityCreator
    {
        public OfferedCapabilityCreator(
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
                    throw new ArgumentException("Invalid selection: An OfferedCapability SM needs to be selected!");
                }

                if (!(selectedElements.First() as ISubmodel).IsOfferedCapabilitySubmodel())
                {
                    throw new ArgumentException("Invalid selection: An OfferedCapability SM needs to be selected!");
                }
            } else
            {
                throw new ArgumentException("Invalid selection: Only a single element may be selected!");
            }
        }

        public ISubmodelElementCollection CreateOfferedCapability(
            ISubmodel offeredCapabilitySM, string capabilitySemanticId, Dictionary<string, object> properties)
        {
            var capabilityName = capabilitySemanticId.Split("/").Last();
            var capabilityConstraints = ConstraintsByCapability[capabilitySemanticId];

            var capabilitySet = FindCapabilitySet(offeredCapabilitySM, true);

            var capabilityContainer = CreateCapabilityContainer(capabilitySet, capabilityName, capabilitySemanticId, true);

            var propertySet = capabilityContainer.FindPropertySet();
            var capabilityRelationships = capabilityContainer.FindCapabilityRelationships();

            foreach (var propertyName in properties.Keys)
            {
                var propertyDefinition = capabilityConstraints.FirstOrDefault(p => p.Name == propertyName);

                var propertyConstraintValue = properties[propertyName];
                var propertyValue = (propertyDefinition.ConstraintType == ConstraintType.FixedValue) ? propertyConstraintValue : null;
                var property = propertySet.CreateProperty(propertyName, propertyValue, propertyDefinition.ValueType);


                if (propertyConstraintValue != null)
                {

                    if (propertyDefinition.ConstraintType == ConstraintType.FixedValue)
                    {
                        // nothing to be done; already handled above
                        continue;
                    }

                    var valueParts = propertyConstraintValue.ToString().Split(';');

                    var numberOfExistingConstraints = capabilityRelationships.OverValueOrEmpty().Where(sme => sme.IdShort.StartsWith(ID_SHORT_CONSTRAINT_CONTAINER)).Count();
                    var idShort = $"{ID_SHORT_CONSTRAINT_CONTAINER}{(numberOfExistingConstraints + 1):00}";

                    var constraintContainer = new SubmodelElementCollection(idShort: idShort);
                    capabilityRelationships.AddChild(constraintContainer);

                    ISubmodelElement constraint = null;
                    if (propertyDefinition.ConstraintType == ConstraintType.Range)
                    {
                        var min = valueParts[0];
                        var max = valueParts.Count() > 1 ? valueParts[1] : valueParts[0];

                        constraint = new AasCore.Aas3_0.Range(propertyDefinition.ValueType, idShort: "Constraint", min: min, max: max);

                    } else if (propertyDefinition.ConstraintType == ConstraintType.List)
                    {
                        constraint = new SubmodelElementList(AasSubmodelElements.Property, idShort: "Constraint");

                        foreach(var value in valueParts)
                        {
                            constraint.AddChild(new AasCore.Aas3_0.Property(DataTypeDefXsd.String, value: value));
                        }
                    }

                    if (constraint == null)
                    {
                        continue;
                    }

                    constraintContainer.AddChild(constraint);
                    var constraintRelationship = new RelationshipElement(property.OverValueOrEmpty()?.First().GetReference(), constraint.GetReference(), idShort: "hasConstraint");

                    constraintContainer.AddChild(constraintRelationship);
                }
            }

            return capabilityContainer;
        }
    }
}
