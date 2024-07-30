using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using AasCore.Aas3_0;
using Extensions;
using System.Xml.Linq;

namespace AasxPluginVws4ls
{
    public static class BasicAasUtils
    {
        public const string SEM_ID_PART_NUMBER = "0173-1#02-AAO676#003";

        private static Random MyRnd = new Random();

        // The version of 'GenerateIdAccordingTemplate' from 'AdminShellUtil' does not ensure unique IDs when
        // being called multiple times in rapid succession (more than two times in one ten thousandths of a second).
        // Hence, we dupliate and adapt this method to use a random time insstead of 'UTCNow' as base for id generation.
        public static string GenerateIdAccordingTemplate(string tpl)
        {
             // generate a deterministic decimal digit string
             var decimals = String.Format("{0:fffffffyyMMddHHmmss}", new DateTime(MyRnd.Next(Int32.MaxValue)));
             decimals = new string(decimals.Reverse().ToArray());
             // convert this to an int
             if (!Int64.TryParse(decimals, out Int64 decii))
                decii = MyRnd.Next(Int32.MaxValue);
            
            // make an hex out of this
            string hexamals = decii.ToString("X");
            // make an alphanumeric string out of this
            string alphamals = "";
            var dii = decii;
            while (dii >= 1)
            {
                var m = dii % 26;
                alphamals += Convert.ToChar(65 + m);
                dii = dii / 26;
            }

            // now, "salt" the strings
            for (int i = 0; i < 32; i++)
            {
                var c = Convert.ToChar(48 + MyRnd.Next(10));
                decimals += c;
                hexamals += c;
                alphamals += c;
            }

            // now, can just use the template
            var id = "";
            foreach (var tpli in tpl)
            {
                if (tpli == 'D' && decimals.Length > 0)
                {
                    id += decimals[0];
                    decimals = decimals.Remove(0, 1);
                }
                else
                if (tpli == 'X' && hexamals.Length > 0)
                {
                    id += hexamals[0];
                    hexamals = hexamals.Remove(0, 1);
                }
                else
                if (tpli == 'A' && alphamals.Length > 0)
                {
                    id += alphamals[0];
                    alphamals = alphamals.Remove(0, 1);
                }
                else
                    id += tpli;
            }

            // ok
            return id;
        }

        public static AssetAdministrationShell CreateAAS(string aasIdShort, string aasIriTemplate, string assetIriTemplate, AasCore.Aas3_0.Environment env, AssetKind assetKind = AssetKind.Instance)
        {
            var assetInformation = new AssetInformation(assetKind, GenerateIdAccordingTemplate(assetIriTemplate));

            var aas = new AssetAdministrationShell(
                GenerateIdAccordingTemplate(aasIriTemplate),
                assetInformation,
                idShort: aasIdShort);
            
            env.AssetAdministrationShells.Add(aas);

            return aas;
        }

        public static T FindReferencedElementInSubmodel<T>(ISubmodel submodel, IReference elementReference) where T : ISubmodelElement
        {
            if (submodel == null || submodel.ToKey() == null || elementReference == null || elementReference.Keys == null || elementReference.Keys.IsEmpty())
            {
                return default(T);
            }

            if (!submodel.ToKey().Matches(elementReference.Keys.First())) {
                return default(T);
            }

            return submodel.SubmodelElements.FindDeep<T>(e => e.GetReference().Matches(elementReference)).FirstOrDefault();
        }

        public static Submodel CreateSubmodel(string idShort, string iriTemplate, string semanticId = null, IAssetAdministrationShell aas = null, AasCore.Aas3_0.Environment env = null, string supplementarySemanticId = null)
        {
            var iri = GenerateIdAccordingTemplate(iriTemplate);

            var submodel = new Submodel(iri, idShort: idShort);

            if (semanticId != null)
            {
                submodel.SemanticId = CreateSemanticId(KeyTypes.Submodel, semanticId);
            }

            if (supplementarySemanticId != null)
            {
                submodel.SupplementalSemanticIds = new List<IReference>() {
                    CreateSemanticId(KeyTypes.Submodel, supplementarySemanticId)
                };
            }

            if (env != null)
            {
                env.Submodels ??= new List<ISubmodel>();
                env.Submodels.Add(submodel);
            }

            if (aas != null)
            {
                aas.AddSubmodelReference(submodel.GetReference());
            }

            return submodel;
        }

        public static IReference CreateSemanticId(KeyTypes keyType, string value)
        {
            return new Reference(ReferenceTypes.ExternalReference, new List<IKey> { new Key(keyType, value) });
        }

        public static bool HasSemanticId(this IHasSemantics element, KeyTypes keyType, string value)
        {
            var requestedSemanticId = CreateSemanticId(keyType, value);

            // check the main semantic id
            if (requestedSemanticId.Matches(element.SemanticId))
            {
                return true;
            }

            // check the supplementary semanticids
            return element.OverSupplementalSemanticIdsOrEmpty().Any(semId => requestedSemanticId.Matches(semId));
        }

        public static IEnumerable<ISubmodel> FindAllSubmodels(AasCore.Aas3_0.Environment env, IAssetAdministrationShell aas = null)
        {
            if (aas == null)
            {
                return env.Submodels;
            } else
            {
                var submodelRefs = aas?.Submodels ?? new List<IReference>();
                var submodels = submodelRefs.ToList().Select(smRef => GetSubmodel(smRef, env));
                return submodels;
            }
        }

        public static ISubmodel GetSubmodel(IReference submodelRef, AasCore.Aas3_0.Environment env)
        {
            return env?.Submodels?.Find(sm => sm.GetReference().Matches(submodelRef));
        }

        public static HashSet<Submodel> FindCommonSubmodelParents(IEnumerable<ISubmodelElement> elements)
        {
            return elements.Select(e => e.FindParentFirstIdentifiable() as Submodel).ToHashSet();
        }

        public static Submodel FindCommonSubmodelParent(IEnumerable<ISubmodelElement> elements)
        {
            var submodel = elements.First().FindParentFirstIdentifiable() as Submodel;
            submodel.SetAllParents();
            
            if (elements.Any(e => e.FindParentFirstIdentifiable() != submodel))
            {
                return null;
            }

            return submodel;
        }

        public static IAssetAdministrationShell GetAasContainingElements(IEnumerable<ISubmodelElement> elements, AasCore.Aas3_0.Environment env)
        {
            var submodelReferences = new HashSet<Reference>(elements.Select(e => e.GetParentSubmodel().GetReference() as Reference));
            var aas = env.AssetAdministrationShells.FirstOrDefault(aas =>
            {
                try
                {
                    // this try-catch block is necessary as the current implementation of 'aas.HasSubmodelReference' will throw
                    // an exception if the ass has no assigned submodels
                    return submodelReferences.All(r => aas.HasSubmodelReference(r));
                } catch (Exception ex)
                {
                    return false;
                }
            });
            return aas;
        }

        public static ISubmodel DeepCloneSubmodel(ISubmodel submodelToCopy, string iriTemplate)
        {
            submodelToCopy.SetAllParents();

            var copy = new Submodel(
                GenerateIdAccordingTemplate(iriTemplate),
                submodelToCopy.Extensions?.Copy(),
                submodelToCopy.Category,
                submodelToCopy.IdShort,
                submodelToCopy.DisplayName?.Copy(),
                submodelToCopy.Description?.Copy(),
                submodelToCopy.Administration?.Copy(),
                submodelToCopy.Kind?.Copy(),
                submodelToCopy.SemanticId?.Copy(),
                submodelToCopy.SupplementalSemanticIds?.Copy(),
                submodelToCopy.Qualifiers?.Copy(),
                submodelToCopy.EmbeddedDataSpecifications?.Copy(),
                submodelToCopy.SubmodelElements?.Copy());

            copy.SetAllParents();

            return copy;
        }

        /**
         * Create a clone of the existing AAS.
         * Note: This will not clone all submodels referenced by the original AAS but only clone references to the same submodels!
         */
        public static IAssetAdministrationShell CloneAas(IAssetAdministrationShell aasToClone, string iriTemplate)
        {
            var clone = new AssetAdministrationShell(
               GenerateIdAccordingTemplate(iriTemplate),
               aasToClone.AssetInformation.Copy(),
               aasToClone.Extensions.Copy(),
               aasToClone.Category,
               aasToClone.IdShort,
               aasToClone.DisplayName.Copy(),
               aasToClone.Description.Copy(),
               aasToClone.Administration.Copy(),
               aasToClone.EmbeddedDataSpecifications.Copy(),
               aasToClone.DerivedFrom.Copy(),
               aasToClone.Submodels.Copy());
            return clone;
        }

        public static string GetSubjectId(string iri)
        {
            if (iri == null)
            {
                return null;
            }

            // we assume that the subject ID is simply the 'host' part of the IRI
            return new UriBuilder(iri).Uri.Host;
        }

        public static string GetSubjectId(this IAssetAdministrationShell aas)
        {
            // we assume that the subject ID is simply the 'host' part of the IRI used for asset identification
            return GetSubjectId(aas.AssetInformation.GlobalAssetId);
        }

        public static string GetSubjectId(this ISubmodel sm)
        {
            // we assume that the subject ID is simply the 'host' part of the IRI used for submodel identification
            return GetSubjectId(sm.Id);
        }

        public static ISpecificAssetId CreatePartNumberSpecificAssetId(string partNumber, string subjectId)
        {
            var externalSubjectId = new Reference(
                ReferenceTypes.ExternalReference, 
                new List<IKey>() { new Key(KeyTypes.GlobalReference, subjectId) }
            );

            return new SpecificAssetId(
                    "partNumber",
                    partNumber,
                    CreateSemanticId(KeyTypes.GlobalReference, SEM_ID_PART_NUMBER),
                    externalSubjectId: externalSubjectId
                );
        }

        public static bool HasPartNumberSpecificAssetId(this IAssetAdministrationShell aas, string partNumber, string subjectID = null)
        {
            var specificAssetId = aas.GetPartNumberSpecificAssetId(subjectID);
            return specificAssetId != null && specificAssetId.Value == partNumber;
        }

        public static ISpecificAssetId GetPartNumberSpecificAssetId(this IAssetAdministrationShell aas, string subjectID = null)
        {
            subjectID ??= aas.GetSubjectId();

            return aas.AssetInformation.OverSpecificAssetIdsOrEmpty().FirstOrDefault(id =>
            {
                var externalSubjectIdValue = id.ExternalSubjectId?.Keys.First()?.Value;
                var semanticIdValue = id.SemanticId?.Keys.First()?.Value;

                return externalSubjectIdValue != null &&
                    subjectID == externalSubjectIdValue &&
                    semanticIdValue == SEM_ID_PART_NUMBER;
            });
        }

        public static IAssetAdministrationShell FindAasForPartNumber(this AasCore.Aas3_0.Environment env, string partNumber, string subjectIdForPartNumber, string subjectIdOfAasToFind = null)
        {
            if (partNumber == null)
            {
                return null;
            }

            var adminShels = env.AssetAdministrationShells.Where(aas => subjectIdOfAasToFind == null || aas.GetSubjectId() == subjectIdOfAasToFind);

            return adminShels.FirstOrDefault(aas =>
            {
                var partNumberAssetId = aas.GetPartNumberSpecificAssetId(subjectIdForPartNumber);

                return partNumberAssetId?.Value == partNumber;
            });
        }

        /*
         * Find all 'RelationshipElement' instances within a given 'original' submodel. For each of these relationships, check if it
         * is a local reference to the 'original' submodel and update the reference to point to an equivalent 'new' submodel (a clone of the original one).
         */
        public static void UpdateRelationships(ISubmodel newSubmodel, ISubmodel originalSubmodel, AasCore.Aas3_0.Environment env)
        {
            var relationships = newSubmodel.FindDeep<IRelationshipElement>();

            var newSubjectId = GetSubjectId(newSubmodel.Id);
            var originalSubjectId = GetSubjectId(originalSubmodel.Id);

            foreach (var rel in relationships)
            {
                UpdateReferenceToNewSubject(rel.First, originalSubjectId, newSubjectId, env);
                UpdateReferenceToNewSubject(rel.Second, originalSubjectId, newSubjectId, env);
            }
        }

        /*
         * Update a reference that points to (an element within) a submodel to point to an equivalent submodel that belongs to a different subject (i.e. a clone of the original submodel with a different id).
         */
        public static void UpdateReferenceToNewSubject(IReference reference, string originalSubjectId, string newSubjectId, AasCore.Aas3_0.Environment env)
        {
            var referencedSubmodelId = reference?.Keys.FirstOrDefault().Value;
            var referencedSubject = GetSubjectId(referencedSubmodelId);

            if (referencedSubject != originalSubjectId)
            {
                // only update references that point to submodels 'owned' by the original subject
                return;
            }

            var referencedSubmodel = env.FindSubmodelById(referencedSubmodelId);

            if (referencedSubmodel == null)
            {
                // unable to find the referenced submodel so we are not able to update the reference
                return;
            }

            // try to find an equivalent submodel that 'belongs' to the new subject
            var equivalentSubmodelForNewSubject = env.Submodels.FirstOrDefault(sm => sm.IdShort == referencedSubmodel.IdShort && sm.GetSubjectId() == newSubjectId);

            if (equivalentSubmodelForNewSubject == null)
            {
                return;
            }

            var equivalentReference = new Reference(reference.Type, reference.Keys, reference.ReferredSemanticId);
            equivalentReference.Keys.First().Value = equivalentSubmodelForNewSubject.Id;

            // check if the reference can be resolved in the equivalent submodel;
            // we only check this if the original reference could also be resolved because resolving fails e.g. for fragment references
            //
            if (env.FindReferableByReference(reference) != null && env.FindReferableByReference(equivalentReference) == null)
            {
                return;
            }

            reference.Keys.First().Value = equivalentSubmodelForNewSubject.Id;

        }
    }
}
