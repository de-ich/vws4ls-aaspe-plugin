using AasCore.Aas3_0;
using Extensions;
using NPOI.Util.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AasxPluginVws4ls.BasicAasUtils;

namespace AasxPluginVws4ls
{
    public static class CapabilitySMUtils
    {
        public const string SEM_ID_CAP_SM = "https://wiki.eclipse.org/BaSyx_/_Documentation_/_Submodels_/_Capability";
        public const string ID_SHORT_REQUIRED_CAP_SM = "RequiredCapabilities";
        public const string ID_SHORT_OFFERED_CAP_SM = "OfferedCapabilities";
        public const string SEM_ID_CAP_SET = "https://wiki.eclipse.org/BaSyx_/_Documentation_/_Submodels_/_Capability#CapabilitySet";
        public const string ID_SHORT_CAP_SET = "CapabilitySet";
        public const string SEM_ID_CAP_CONTAINER = "https://wiki.eclipse.org/BaSyx_/_Documentation_/_Submodels_/_Capability#CapabilityContainer";
        public const string ID_SHORT_CAP_CONTAINER = "CapabilityContainer";
        public const string SEM_ID_COMMENT = "https://wiki.eclipse.org/BaSyx_/_Documentation_/_Submodels_/_Capability#Comment";
        public const string ID_SHORT_COMMENT = "Comment";
        public const string SEM_ID_CAP_RELATIONSHIPS = "https://wiki.eclipse.org/BaSyx_/_Documentation_/_Submodels_/_Capability#CapabilityRelationships";
        public const string ID_SHORT_CAP_RELATIONSHIPS = "CapabilityRelationships";
        public const string SEM_ID_PROP_SET = "https://wiki.eclipse.org/BaSyx_/_Documentation_/_Submodels_/_Capability#PropertySet";
        public const string ID_SHORT_PROP_SET = "PropertySet";
        public const string SEM_ID_PROP_CONTAINER = "https://wiki.eclipse.org/BaSyx_/_Documentation_/_Submodels_/_Capability#PropertyContainer";
        public const string ID_SHORT_PROP_CONTAINER = "PropertyContainer";
        public const string ID_SHORT_CONSTRAINT_CONTAINER = "ConstraintContainer";

        public static bool IsCapabilitySubmodel(this ISubmodel submodel)
        {
            return submodel?.HasSemanticId(KeyTypes.GlobalReference, SEM_ID_CAP_SM) ?? false;
        }

        public static bool IsRequiredCapabilitySubmodel(this ISubmodel submodel)
        {
            return submodel.IsCapabilitySubmodel() && submodel.IdShort == ID_SHORT_REQUIRED_CAP_SM;
        }

        public static bool IsOfferedCapabilitySubmodel(this ISubmodel submodel)
        {
            return submodel.IsCapabilitySubmodel() && submodel.IdShort == ID_SHORT_OFFERED_CAP_SM;
        }

        public static bool IsCapabilityContainer(this ISubmodelElement submodelElement)
        {
            var smc = submodelElement as ISubmodelElementCollection;

            return smc?.HasSemanticId(KeyTypes.GlobalReference, SEM_ID_CAP_CONTAINER) ?? false;
        }

        public static IEnumerable<ISubmodelElementCollection> FindCapabilityContainers(this ISubmodel capabilitySubmodel)
        {
            var capabilitySet = FindCapabilitySet(capabilitySubmodel);

            return capabilitySet?.OverValueOrEmpty().Where(sme => sme.IsCapabilityContainer()).Select(sme => sme as ISubmodelElementCollection) ?? new List<ISubmodelElementCollection>();
        }

        public static ISubmodelElementCollection FindCapabilitySet(ISubmodel capabilitySubmodel, bool createIfNonExistant = false)
        {
            var capabilitySet = capabilitySubmodel.OverSubmodelElementsOrEmpty().FirstOrDefault(sme => sme.HasSemanticId(KeyTypes.GlobalReference, SEM_ID_CAP_SET)) as ISubmodelElementCollection;

            if (capabilitySet == null && createIfNonExistant)
            {
                capabilitySet = new SubmodelElementCollection(idShort: ID_SHORT_CAP_SET, semanticId: CreateSemanticId(KeyTypes.GlobalReference, SEM_ID_CAP_SET));
                capabilitySubmodel.AddChild(capabilitySet);
            }

            return capabilitySet;
        }

        public static ISubmodelElementCollection CreateCapabilityContainer(ISubmodelElementCollection capabilitySet, string capabilityName, string capabilitySemanticId, bool createRelationshipContainer = false)
        {
            var numberOfExistingCapabilities = capabilitySet.OverValueOrEmpty().Count();
            var idShort = $"{ID_SHORT_CAP_CONTAINER}{(numberOfExistingCapabilities + 1):00}";

            var capabilityContainer = new SubmodelElementCollection(idShort: idShort, semanticId: CreateSemanticId(KeyTypes.GlobalReference, SEM_ID_CAP_CONTAINER));

            capabilityContainer.AddChild(new Capability(idShort: capabilityName, semanticId: CreateSemanticId(KeyTypes.GlobalReference, capabilitySemanticId)));
            capabilityContainer.AddChild(new MultiLanguageProperty(idShort: ID_SHORT_COMMENT, semanticId: CreateSemanticId(KeyTypes.GlobalReference, SEM_ID_COMMENT)));

            capabilityContainer.AddChild(new SubmodelElementCollection(idShort: ID_SHORT_PROP_SET, semanticId: CreateSemanticId(KeyTypes.GlobalReference, SEM_ID_PROP_SET)));

            if (createRelationshipContainer)
            {
                capabilityContainer.AddChild(new SubmodelElementCollection(idShort: ID_SHORT_CAP_RELATIONSHIPS, semanticId: CreateSemanticId(KeyTypes.GlobalReference, SEM_ID_CAP_RELATIONSHIPS)));
            }

            capabilitySet.AddChild(capabilityContainer);

            return capabilityContainer;
        }

        public static string? GetCapabilitySemanticId(this ISubmodelElementCollection capabilityContainer)
        {
            if (!capabilityContainer.IsCapabilityContainer())
            {
                return null;
            }

            return capabilityContainer.Value?.FirstOrDefault(sme => sme is ICapability)?.SemanticId?.Keys?.LastOrDefault()?.Value;
        }

        public static bool IsPropertySet(this ISubmodelElement submodelElement)
        {
            return submodelElement is ISubmodelElementCollection && submodelElement.HasSemanticId(KeyTypes.GlobalReference, SEM_ID_PROP_SET);
        }

        public static ISubmodelElementCollection? FindPropertySet(this ISubmodelElementCollection capabilityContainer)
        {
            return capabilityContainer?.OverValueOrEmpty().FirstOrDefault(sme => sme.IsPropertySet()) as ISubmodelElementCollection;
        }

        public static bool IsPropertyContainer(this ISubmodelElement submodelElement)
        {
            var smc = submodelElement as ISubmodelElementCollection;

            return smc?.HasSemanticId(KeyTypes.GlobalReference, SEM_ID_PROP_CONTAINER) ?? false;
        }

        public static IEnumerable<IProperty> FindProperties(this ISubmodelElementCollection propertySet)
        {
            var propertyContainers = propertySet.OverValueOrEmpty().Where(sme => sme.IsPropertyContainer()).Select(sme => sme as ISubmodelElementCollection);

            return propertyContainers.Select(pc => pc.OverValueOrEmpty().FirstOrDefault(sme => sme is IProperty) as IProperty).Where(prop => prop != null);
        }

        public static IProperty FindProperty(this ISubmodelElementCollection propertySet, string propertyName)
        {
            return propertySet.FindProperties().FirstOrDefault(prop => prop.IdShort == propertyName);
        }

        public static ISubmodelElementCollection CreateProperty(this ISubmodelElementCollection propertySet, string propertyName, object propertyValue = null, DataTypeDefXsd valueType = DataTypeDefXsd.String)
        {
            var numberOfExistingProperties = propertySet.OverValueOrEmpty().Count();

            var propertyContainer = new SubmodelElementCollection(
                    idShort: $"{ID_SHORT_PROP_CONTAINER}{(numberOfExistingProperties + 1):00}",
                    semanticId: CreateSemanticId(KeyTypes.GlobalReference, SEM_ID_PROP_CONTAINER)
                );

            propertyContainer.AddChild(
                new Property(
                    valueType,
                    idShort: propertyName,
                    value: propertyValue?.ToString()
                )
            );
            propertySet.AddChild(propertyContainer);

            return propertyContainer;
        }

        public static bool IsCapabilityRelationships(this ISubmodelElement submodelElement)
        {
            var smc = submodelElement as ISubmodelElementCollection;

            return smc?.HasSemanticId(KeyTypes.GlobalReference, SEM_ID_CAP_RELATIONSHIPS) ?? false;
        }

        public static ISubmodelElementCollection? FindCapabilityRelationships(this ISubmodelElementCollection capabilityContainer)
        {
            return capabilityContainer?.OverValueOrEmpty().FirstOrDefault(sme => sme.IsCapabilityRelationships()) as ISubmodelElementCollection;
        }

        public static bool IsConstraintContainer(this ISubmodelElement submodelElement)
        {
            var smc = submodelElement as ISubmodelElementCollection;

            return smc?.HasSemanticId(KeyTypes.GlobalReference, ID_SHORT_CONSTRAINT_CONTAINER) ?? false;
        }

        public static ISubmodelElement? FindConstraintForProperty(ISubmodelElementCollection capabilityRelationships, IProperty property, AasCore.Aas3_0.Environment env)
        {
            var constraintContainers = capabilityRelationships.OverValueOrEmpty().Where(sme => sme.IsConstraintContainer()).Select(sme => sme as ISubmodelElementCollection);

            var constraintRelationships = constraintContainers.Select(smc => smc.FindConstraintRelationShip()).Where(r => r != null);

            var referenceToConstriantElement = constraintRelationships.FirstOrDefault(r => r.First?.Matches(property.GetReference()) ?? false)?.Second;

            return env.FindReferableByReference(referenceToConstriantElement) as ISubmodelElement;
        }

        public static IRelationshipElement? FindConstraintRelationShip(this ISubmodelElementCollection constraintContainer)
        {
            return constraintContainer.OverValueOrEmpty().FirstOrDefault(sme => sme is IRelationshipElement) as IRelationshipElement;
        }
    }
}
