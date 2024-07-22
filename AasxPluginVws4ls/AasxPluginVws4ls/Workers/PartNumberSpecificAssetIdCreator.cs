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


namespace AasxPluginVws4ls
{
    /// <summary>
    /// This class allows to create a specific asset ID representing a part number.
    /// </summary>
    public class PartNumberSpecificAssetIdCreator
    {
        public PartNumberSpecificAssetIdCreator(
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
                if (selectedElements.First() is not IAssetAdministrationShell)
                {
                    throw new ArgumentException("Invalid selection: An AAS must be selected!");
                }
            } else
            {
                throw new ArgumentException("Invalid selection: Only a single element may be selected!");
            }
        }

        public ISpecificAssetId CreateSpecificAssetIdPartNumber(
            IAssetAdministrationShell aas, string partNumber, string subjectId)
        {
            var specificAssetId = CreatePartNumberSpecificAssetId(partNumber, subjectId);

            aas.AssetInformation.SpecificAssetIds ??= new List<ISpecificAssetId>();

            aas.AssetInformation.SpecificAssetIds.Add(specificAssetId);

            return specificAssetId;
        }
    }
}
