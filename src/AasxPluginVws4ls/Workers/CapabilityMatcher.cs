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

namespace AasxPluginVws4ls
{
    /// <summary>
    /// This class allows to find one or more ressource able to fulfil a required capability.
    /// </summary>
    public class CapabilityMatcher
    {
        public CapabilityMatcher(
            AasCore.Aas3_0.Environment env,
            Vws4lsOptions options)
        {
            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private const string SEM_ID_REQUIREDSLOT = "http://arena2036.de/requiredSlot/1/0";
        private const string SEM_ID_OFFEREDSLOT = "http://arena2036.de/offeredSlot/1/0";

        protected AasCore.Aas3_0.Environment env;
        protected Vws4lsOptions options;

        public void ValidateSelection(IEnumerable<object> selectedElements)
        {
            if (selectedElements.Count() == 0)
            {
                throw new ArgumentException("Nothing selected!");
            } else if (selectedElements.Count() == 1)
            {
                if (selectedElements.First() is not ISubmodelElementCollection)
                {
                    throw new ArgumentException("Invalid selection: An SMC representing a CapabilityContainer needs to be selected!");
                }

                if (!(selectedElements.First() as ISubmodelElementCollection).IsCapabilityContainer())
                {
                    throw new ArgumentException("Invalid selection: An SMC representing a CapabilityContainer needs to be selected!");
                }
            } else
            {
                throw new ArgumentException("Invalid selection: Only a single element may be selected!");
            }
        }

        protected OfferedCapabilityResult? CheckRessourceFulfillsRequiredCapability(ISubmodelElementCollection requiredCapabilityContainer, IAssetAdministrationShell aas)
        {
            var requiredProperties = requiredCapabilityContainer.FindPropertySet()?.FindProperties();

            var requiredCapabilitySemId = requiredCapabilityContainer.GetCapabilitySemanticId();

            // Step 1: Check if the selected AAS provides an "Offered Capability Container" with the correct semantic ID for the required capability
            var offeredCapabilityContainers = FindOfferedCapabilitiesWithSemanticId(requiredCapabilitySemId, aas);

            if (!offeredCapabilityContainers.Any())
            {
                return null;
            }

            var offeredCapabilityContainer = offeredCapabilityContainers.First().Item2;

            var offeredCapabilityResult = new OfferedCapabilityResult()
            {
                RessourceAas = aas,
                OfferedCapabilityContainer = offeredCapabilityContainer
            };

            // Step 2: For the "Offered Capability Container", check if each property constraint fulfills the values from the required capability
            foreach (var requiredProperty in requiredProperties)
            {
                var propertyResult = CheckProperty(requiredProperty, offeredCapabilityContainer);
                offeredCapabilityResult.PropertyMatchResults[requiredProperty] = propertyResult;
            }

            return offeredCapabilityResult;
        }

        public CapabiltyCheckResult ExecuteCapabiltyCheck(ISubmodelElementCollection requiredCapabilityContainer, IAssetAdministrationShell machineAas)
        {
            var result = new CapabiltyCheckResult()
            {
                RequiredCapabilityContainer = requiredCapabilityContainer,
            };

            // Step 1: Check if the selected machine AAS can fulfill the required capability
            var machineResult = CheckRessourceFulfillsRequiredCapability(requiredCapabilityContainer, machineAas);
            result.OfferedCapabilityResult = machineResult;

            if (!machineResult?.Success ?? false)
            {
                return result;
            }

            // Step 2: Check if the machine requires a tool to execute the capability and which tool type is required
            var requiredToolType = ((machineResult.OfferedCapabilityContainer
                .OverValueOrEmpty().FirstOrDefault(sme => sme is ISubmodelElementCollection && sme.IdShort == "CapabilityRelationships") as ISubmodelElementCollection)?
                .OverValueOrEmpty().FirstOrDefault(sme => sme is ISubmodelElementCollection && sme.HasSemanticId(KeyTypes.GlobalReference, "ConditionContainer")) as ISubmodelElementCollection)?
                .OverValueOrEmpty().FirstOrDefault(sme => sme is IProperty && sme.IdShort == "RequiresToolCondition")?.ValueAsText();

            if (requiredToolType == null)
            {
                result.Success = true;
                return result;
            }
            
            // Step 3a: Find the tools that fulfill the required capability
            var isInstance = machineAas.AssetInformation.AssetKind == AssetKind.Instance;

            var toolAASes = env.AssetAdministrationShells
                .Where(aas => aas.AssetInformation.AssetKind == machineAas.AssetInformation.AssetKind)
                .Where(aas => aas.OverExtensionsOrEmpty().Any(e => e.HasSemanticId(KeyTypes.GlobalReference, "http://arena2036.de/toolType/1/0") && e.Value == requiredToolType));

            // Step 3b: Depending on if we look at a type or at an instance, select either all tools or only the currently mounted ones
            var toolAASesToBeConsidered = new List<IAssetAdministrationShell>();
            if (isInstance)
            {
                var bomSubmodel = FindFirstBomSubmodel(env, machineAas);
                var leafNodes = bomSubmodel?.GetLeafNodes();

                var mountedToolAASes = toolAASes.Where(aas => leafNodes?.Any(node => node.GlobalAssetId == aas.AssetInformation.GlobalAssetId) ?? false);
                toolAASesToBeConsidered.AddRange(mountedToolAASes);
            } else
            {
                toolAASesToBeConsidered.AddRange(toolAASes);
            }

            var aasesOfSuitableTools = toolAASesToBeConsidered.Where(toolAas => CheckRessourceFulfillsRequiredCapability(requiredCapabilityContainer, toolAas)?.Success ?? false);

            // Step 4: Find the tools that can be mounted in the machine
            foreach(var toolAas in aasesOfSuitableTools)
            {
                var mountingPaths = DetermineMountingPaths(toolAas);
                var mountingPathsLeadingToMachine = mountingPaths.Where(path => path.Last().Item2 == machineAas);

                foreach( var mountingPath in mountingPathsLeadingToMachine)
                {
                    mountingPath.Insert(0, new Tuple<string, IAssetAdministrationShell>("", toolAas));
                    result.ToolOptions.Add(mountingPath);   
                }
            }

            if (result.ToolOptions.Any() || (isInstance && aasesOfSuitableTools.Any()))
            {
                result.Success = true;
            }

            return result;
        }

        protected IEnumerable<Tuple<IAssetAdministrationShell, ISubmodelElementCollection>> FindOfferedCapabilitiesWithSemanticId(string capabilitySemId)
        {
            return env.AssetAdministrationShells.SelectMany(aas => FindOfferedCapabilitiesWithSemanticId(capabilitySemId, aas));
        }

        protected IEnumerable<Tuple<IAssetAdministrationShell, ISubmodelElementCollection>> FindOfferedCapabilitiesWithSemanticId(string capabilitySemId, IAssetAdministrationShell aas)
        {
            var offeredCapabilitySubmodels = FindAllSubmodels(env, aas).Where(sm => sm.IsOfferedCapabilitySubmodel());

            var suitableCapabilityContainers = offeredCapabilitySubmodels.SelectMany(sm => FindOfferedCapabilitiesWithSemanticId(capabilitySemId, sm));

            return suitableCapabilityContainers.Select(cap => new Tuple<IAssetAdministrationShell, ISubmodelElementCollection>(aas, cap));
        }

        protected static IEnumerable<ISubmodelElementCollection> FindOfferedCapabilitiesWithSemanticId(string capabilitySemId, ISubmodel capabilitySubmodel)
        {
            var capabilityContainers = capabilitySubmodel.FindCapabilityContainers();

            foreach (var capabilityContainer in capabilityContainers.Where(cap => cap.GetCapabilitySemanticId() == capabilitySemId))
            {
                yield return capabilityContainer;
            }
        }
    
        protected PropertyMatchResult CheckProperty(IProperty requiredProperty, ISubmodelElementCollection offeredCapabilityContainer)
        {
            var offeredPropertySet = offeredCapabilityContainer.FindPropertySet();

            if (offeredPropertySet == null)
            {
                return new PropertyMatchResult()
                {
                    DetailResult = PropertyMatchResultType.NotFound
                };
            }

            var offeredProperty = offeredPropertySet.FindProperty(requiredProperty.IdShort);

            if (offeredProperty == null)
            {
                return new PropertyMatchResult()
                {
                    DetailResult = PropertyMatchResultType.NotFound
                };
            }

            var capabilityRelationships = offeredCapabilityContainer.FindCapabilityRelationships();

            if (capabilityRelationships == null)
            {
                return new PropertyMatchResult()
                {
                    DetailResult = PropertyMatchResultType.FoundAndNotConstrained
                };
            }

            var constraint = FindConstraintForProperty(capabilityRelationships, offeredProperty, env);

            if (constraint == null)
            {
                return new PropertyMatchResult()
                {
                    DetailResult = PropertyMatchResultType.FoundAndNotConstrained
                };
            }

            var constraintResult = CheckConstraint(constraint, requiredProperty);

            if(constraintResult)
            {
                return new PropertyMatchResult()
                {
                    DetailResult = PropertyMatchResultType.FoundAndSatisfied
                };
            } else
            {
                return new PropertyMatchResult()
                {
                    DetailResult = PropertyMatchResultType.FoundButNotSatisfied
                };
            }
        }

        protected static bool CheckConstraint(ISubmodelElement constraint, IProperty property)
        {
            var propertyValue = property.Value;

            if (constraint is IRange rangeConstraint)
            {
                var propertyValueAsDouble = ParseDouble(propertyValue);
                return (rangeConstraint.Min == null || ParseDouble(rangeConstraint.Min) <= propertyValueAsDouble) && 
                    (rangeConstraint.Max == null || ParseDouble(rangeConstraint.Max) >= propertyValueAsDouble);

            } else if (constraint is ISubmodelElementList listConstraint)
            {
                var allowedPropertyValues = listConstraint.Value.Select(v => (v as IProperty)?.Value);
                return allowedPropertyValues.Contains(propertyValue);
            }

            throw new ApplicationException($"Unsupported type of submodel element encountered as constraint: {constraint.GetType().Name}");
        }

        protected static Double ParseDouble(string value)
        {
            // hacky solution as xs:doubles should (!) always be represented with a '.' separator
            return Double.Parse(value.Replace(".", ","));
        }

        protected List<List<Tuple<string, IAssetAdministrationShell>>> DetermineMountingPaths(IAssetAdministrationShell ressourceAas)
        {
            var mountingPaths = new List<List<Tuple<string, IAssetAdministrationShell>>>();

            var requiredSlotExtension = GetRequiredSlotExtension(ressourceAas);

            if (requiredSlotExtension == null)
            {
                mountingPaths.Add(new List<Tuple<string, IAssetAdministrationShell>>());
                return mountingPaths;
            }

            var slotName = requiredSlotExtension.Value;

            foreach (var aas in env.AssetAdministrationShells)
            {
                var offeredSlotExtension = GetOfferedSlotExtension(aas);

                if (offeredSlotExtension == null ||
                    offeredSlotExtension.Value != slotName)
                {
                    // aas/ressource does not provide a suitable slot
                    continue;
                }

                var subPaths = DetermineMountingPaths(aas);
                foreach( var subPath in subPaths)
                {
                    subPath.Insert(0, new Tuple<string, IAssetAdministrationShell>(slotName, aas));
                    mountingPaths.Add(subPath);
                }
                
            }

            return mountingPaths;
        }
        
        protected RessourceDependencyTree FindRequiredRessourcesRecursively(IAssetAdministrationShell ressourceAas)
        {
            var dependencyTree = new RessourceDependencyTree(ressourceAas);
        
            var requiredSlotExtension = GetRequiredSlotExtension(ressourceAas);

            if (requiredSlotExtension == null)
            {
                return dependencyTree;
            }

            var slotName = requiredSlotExtension.Value;

            dependencyTree.SlotDependencies[slotName] = new DependencyOptions();

            foreach (var aas in env.AssetAdministrationShells)
            {
                var offeredSlotExtension = GetOfferedSlotExtension(aas);

                if (offeredSlotExtension == null || 
                    offeredSlotExtension.Value != slotName)
                {
                    // aas/ressource does not provide a suitable slot
                    continue;
                }

                dependencyTree.SlotDependencies[slotName].Options.Add(FindRequiredRessourcesRecursively(aas));
            }

            return dependencyTree;

        }

        protected IExtension? GetRequiredSlotExtension(IAssetAdministrationShell aas)
        {
            return aas?.Extensions?.FirstOrDefault(e => e.HasSemanticId(KeyTypes.GlobalReference, SEM_ID_REQUIREDSLOT));
        }

        protected IExtension? GetOfferedSlotExtension(IAssetAdministrationShell aas)
        {
            return aas?.Extensions?.FirstOrDefault(e => e.HasSemanticId(KeyTypes.GlobalReference, SEM_ID_OFFEREDSLOT));
        }
    }

    public class CapabiltyCheckResult
    {
        public ISubmodelElementCollection RequiredCapabilityContainer { get; internal set; }
        public string RequiredCapabilitySemId => RequiredCapabilityContainer.GetCapabilitySemanticId();
        public bool Success = false;
        public List<IEnumerable<Tuple<string, IAssetAdministrationShell>>> ToolOptions = new List<IEnumerable<Tuple<string, IAssetAdministrationShell>>>();
        public OfferedCapabilityResult OfferedCapabilityResult { get; internal set; }
    }

    public class OfferedCapabilityResult
    {
        public bool Success => PropertyMatchResults.All(r => r.Value.Success);
        public IAssetAdministrationShell RessourceAas { get; internal set; }
        public string RessourceAssetId => RessourceAas.AssetInformation?.GlobalAssetId;
        public ISubmodelElementCollection OfferedCapabilityContainer { get; internal set; }
        public IDictionary<IProperty, PropertyMatchResult> PropertyMatchResults { get; } = new Dictionary<IProperty, PropertyMatchResult>();
    }

    public class PropertyMatchResult
    {
        public bool Success => DetailResult != PropertyMatchResultType.FoundButNotSatisfied;
        public PropertyMatchResultType DetailResult { get; internal set; }
    }

    public enum PropertyMatchResultType
    {
        FoundAndNotConstrained, FoundAndSatisfied, FoundButNotSatisfied, NotFound
    }

    public class RessourceDependencyTree
    {
        public IAssetAdministrationShell Ressource { get; set; }
        public Dictionary<string, DependencyOptions> SlotDependencies { get; } = new Dictionary<string, DependencyOptions>();
        public bool CanBeFulfilled => SlotDependencies.Values.All(o => o.CanBeFulfilled);

        public RessourceDependencyTree(IAssetAdministrationShell ressource)
        {
            Ressource = ressource;
        }
    }

    public class DependencyOptions
    {
        public List<RessourceDependencyTree> Options { get; } = new List<RessourceDependencyTree>();
        public bool CanBeFulfilled => Options.Any() && Options.Any(rdt => rdt.CanBeFulfilled);
    }
}
