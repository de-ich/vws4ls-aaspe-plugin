using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using static AasxPluginVws4ls.BomSMUtils;
using System.Xml.Linq;
using AasCore.Aas3_0;
using Extensions;
using static AasxPluginVws4ls.BasicAasUtils;
using File = AasCore.Aas3_0.File;

namespace AasxPluginVws4ls
{
    public static class VecSMUtils
    {
        public const string VEC_SUBMODEL_ID_SHORT = "ProductSpecification";
        public const string VEC_FILE_ID_SHORT = "VecFile";
        public const string VEC_REFERENCE_ID_SHORT = "SameAs";
        public const string SEM_ID_VEC_SUBMODEL = "https://arena2036.de/vws4ls/submodels/vec/1/0";
        public const string SEM_ID_VEC_FILE_REFERENCE = "http://arena2036.de/vws4ls/vec/VecFileReference/1/0";
        public const string SEM_ID_VEC_FRAGMENT_REFERENCE = "https://admin-shell.io/idta/HierarchicalStructures/SameAs/1/0";

        public static Submodel CreateVecSubmodel(string pathToVecFile, string iriTemplate, IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env, AdminShellPackageEnv packageEnv = null)
        {
            // add the file to the package
            var localFilePath = "/aasx/files/" + System.IO.Path.GetFileName(pathToVecFile);
            packageEnv?.AddSupplementaryFileToStore(pathToVecFile, localFilePath, false);

            // create the VEC file submodel element
            var file = new File("text/xml", idShort: VEC_FILE_ID_SHORT, value: localFilePath);

            // create the VEC submodel
            var vecSubmodel = CreateVecSubmodel(file, iriTemplate, aas, env);

            return vecSubmodel;
        }

        public static Submodel CreateVecSubmodel(File vecFile, string iriTemplate, IAssetAdministrationShell aas, AasCore.Aas3_0.Environment env)
        {
            // create the VEC submodel
            var vecSubmodel = CreateSubmodel(VEC_SUBMODEL_ID_SHORT, iriTemplate, SEM_ID_VEC_SUBMODEL, aas, env);

            // create the VEC file submodel element
            var file = new File(vecFile.ContentType, value: vecFile.Value, idShort: vecFile.IdShort)
            {
                SemanticId = CreateSemanticId(KeyTypes.Submodel, SEM_ID_VEC_FILE_REFERENCE)
            };
            vecSubmodel.Add(file);

            return vecSubmodel;
        }

        public static IEnumerable<ISubmodel> FindVecSubmodels(AasCore.Aas3_0.Environment env, IAssetAdministrationShell aas)
        {
            var submodels = FindAllSubmodels(env, aas);
            return submodels.Where(IsVecSubmodel);
        }

        public static bool IsVecSubmodel(this ISubmodel submodel)
        {
            return submodel.HasSemanticId(KeyTypes.Submodel, SEM_ID_VEC_SUBMODEL);
        }

        public static File GetVecFileElement(this ISubmodel submodel)
        {
            return submodel?.FindFirstIdShortAs<File>(VEC_FILE_ID_SHORT);
        }

        public static RelationshipElement CreateVecRelationship(IEntity target, string xpathToVecElement, File vecFileSubmodelElement, IReferable parent = null)
        {

            var idShort = VEC_REFERENCE_ID_SHORT + "_" + target.GetParentSubmodel().IdShort + "_" + target.IdShort;
            var semanticId = CreateSemanticId(KeyTypes.ConceptDescription, SEM_ID_VEC_FRAGMENT_REFERENCE);

            var first = vecFileSubmodelElement.GetReference();
            first.Keys.Add(new Key(KeyTypes.FragmentReference, xpathToVecElement));

            return CreateRelationship(first, target.GetReference(), parent ?? vecFileSubmodelElement.GetParentSubmodel(), idShort, semanticId);
        }

        public static RelationshipElement CreateVecRelationship(IEntity newTarget, IRelationshipElement existingRelationship, AasCore.Aas3_0.Environment env, IReferable parent = null)
        {
            var referencedVecFile = existingRelationship.FindReferencedVecFileSME(env);
            var xpathToVecElement = existingRelationship.First.Keys.Last().Value;

            return CreateVecRelationship(newTarget, xpathToVecElement, referencedVecFile, parent);
        }

        public static IEnumerable<RelationshipElement> GetVecRelationships(this ISubmodel vecSubmodel)
        {
            return vecSubmodel.OverSubmodelElementsOrEmpty().Select(e => e as RelationshipElement).Where(e => IsVecRelationship(e));
        }

        public static RelationshipElement GetVecRelationship(this IEntity entity, AasCore.Aas3_0.Environment env, IAssetAdministrationShell aas = null)
        {
            var vecRelationships = FindVecSubmodels(env, aas).SelectMany(sm => sm.GetVecRelationships());

            return vecRelationships.FirstOrDefault(r => r?.Second?.Matches(entity.GetReference()) ?? false);
        }

        public static bool IsVecRelationship(this RelationshipElement rel)
        {
            return rel?.IdShort?.StartsWith(VEC_REFERENCE_ID_SHORT) ?? false && rel?.SemanticId?.Last()?.Value == SEM_ID_VEC_FRAGMENT_REFERENCE;
        }

        public static File FindReferencedVecFileSME(this IEntity entity, AasCore.Aas3_0.Environment env, IAssetAdministrationShell aas = null)
        {
            var vecRelationship = entity.GetVecRelationship(env, aas);
            return vecRelationship?.FindReferencedVecFileSME(env);
        }

        public static File FindReferencedVecFileSME(this IRelationshipElement vecRelationship, AasCore.Aas3_0.Environment env)
        {
            var fragmentReferenceKeys = vecRelationship?.First?.Keys;
            var keysToVecFile = fragmentReferenceKeys?.Take(fragmentReferenceKeys.ToList().Count - 1);
            var referenceToVecFile = new Reference(ReferenceTypes.ModelReference, keysToVecFile?.ToList() ?? new List<IKey>());
            return env.FindReferableByReference(referenceToVecFile) as File;
        }
    }
}
